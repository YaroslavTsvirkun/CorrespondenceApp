using System.Net;
using System.Net.Http.Json;
using CorrespondenceSystem.Contracts.Access;
using CorrespondenceSystem.Contracts.Access.Dtos;
using CorrespondenceSystem.Contracts.Attachments;
using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Documents;
using CorrespondenceSystem.Contracts.Documents.Dtos;
using CorrespondenceSystem.Contracts.Employees;
using CorrespondenceSystem.Contracts.Employees.Dtos;
using CorrespondenceSystem.Contracts.Tasks;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using CorrespondenceSystem.Contracts.Users;
using CorrespondenceSystem.DataService.Common;
using static CorrespondenceSystem.DataService.Tests.DataServiceFactory;

namespace CorrespondenceSystem.DataService.Tests;

/// <summary>
/// Авторизація в сервісі даних на демо-даних: дві організації («Промінь» — org1, «Вектор» — org2),
/// користувачі admin (супер-адмін), orgadmin1/orgadmin2, registrar (уся кореспонденція org1),
/// manager (свій підрозділ + задіяні), executor (лише задіяні, tasks.execute).
/// </summary>
[Collection(DemoDatabaseCollection.Name)]
public sealed class AuthorizationTests(DataServiceFactory app)
{
    private Task<int> UserId(string userName) =>
        app.LoginAsync(userName, userName.StartsWith("orgadmin") ? "orgadmin123" : userName + "123")
            .ContinueWith(t => t.Result.UserId);

    private async Task<IReadOnlyList<int>> VisibleDocumentIds(int userId)
    {
        var page = await app.RpcOkAsync<PagedResult<DocumentListItem>>(new GetDocuments.Query { PageSize = 200 }, userId);
        return page.Items.Select(d => d.Id).ToList();
    }

    // ---- Розмітка операцій і базовий доступ ----

    [Fact]
    public void Every_operation_declares_its_permissions() =>
        OperationAuthorization.EnsureAllOperationsAnnotated();

    [Fact]
    public async Task Request_without_api_key_is_rejected()
    {
        var response = await app.RpcAsync(new GetDocuments.Query(), await UserId("admin"), apiKey: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_user_cannot_read_documents()
    {
        var response = await app.RpcAsync(new GetDocuments.Query(), userId: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_without_permission_gets_403()
    {
        var response = await app.RpcAsync(new GetUsers.Query(), await UserId("executor"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- Ролі: лише супер-адмін ----

    [Fact]
    public async Task Organization_admin_cannot_escalate_through_roles()
    {
        var orgAdmin = await UserId("orgadmin1");
        var roles = await app.RpcOkAsync<IReadOnlyList<RoleDto>>(new GetRoles.Query(), orgAdmin);
        var ownRole = roles.Single(r => r.Name == "Адміністратор організації");

        var escalate = await app.RpcAsync(new SaveRole.Command(ownRole.Id,
            new RoleInput(ownRole.Name, null, [.. ownRole.Permissions, Permissions.OrganizationsManage])), orgAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, escalate.StatusCode);

        var delete = await app.RpcAsync(new DeleteRole.Command(ownRole.Id), orgAdmin);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);

        var after = await app.RpcOkAsync<IReadOnlyList<RoleDto>>(new GetRoles.Query(), orgAdmin);
        Assert.DoesNotContain(Permissions.OrganizationsManage, after.Single(r => r.Id == ownRole.Id).Permissions);
    }

    [Fact]
    public async Task Super_admin_can_manage_roles()
    {
        var admin = await UserId("admin");
        var id = await app.RpcOkAsync<int>(
            new SaveRole.Command(null, new RoleInput("Тестова роль", null, [Permissions.ReportsView])), admin);
        await EnsureStatusAsync(await app.RpcAsync(new DeleteRole.Command(id), admin), HttpStatusCode.NoContent);
    }

    // ---- Ізоляція організацій і зона видимості документів ----

    [Fact]
    public async Task Organizations_do_not_see_each_others_documents()
    {
        var org1Docs = await VisibleDocumentIds(await UserId("registrar"));
        var orgAdmin2 = await UserId("orgadmin2");
        var org2Docs = await VisibleDocumentIds(orgAdmin2);

        Assert.NotEmpty(org1Docs);
        Assert.Empty(org1Docs.Intersect(org2Docs));

        var foreign = await app.RpcAsync(new GetDocumentById.Query(org1Docs[0]), orgAdmin2);
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
    }

    [Fact]
    public async Task Limited_scope_sees_only_part_of_organization_documents()
    {
        var allOrg1 = await VisibleDocumentIds(await UserId("registrar"));
        var manager = await UserId("manager");
        var managerDocs = await VisibleDocumentIds(manager);

        Assert.NotEmpty(managerDocs);
        Assert.Subset(allOrg1.ToHashSet(), managerDocs.ToHashSet());
        var hidden = allOrg1.Except(managerDocs).ToList();
        Assert.NotEmpty(hidden);

        var response = await app.RpcAsync(new GetDocumentById.Query(hidden[0]), manager);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- Доручення ----

    private static TaskInput InputFrom(TaskDto t) => new(t.DocumentId, t.ResolutionText, t.ExecutorEmployeeId,
        t.StatusId, t.TaskText, t.DueDate, t.CompletedAt is { } c ? DateOnly.FromDateTime(c) : null, t.ResultText);

    [Fact]
    public async Task Executor_changes_only_progress_of_own_task()
    {
        var executor = await app.LoginAsync("executor", "executor123");
        var admin = await UserId("admin");
        var tasks = await app.RpcOkAsync<IReadOnlyList<TaskDto>>(new GetTasks.Query(), executor.UserId);
        var own = tasks.First(t => t.ExecutorEmployeeId == executor.EmployeeId);
        var otherStatus = own.StatusId == 3 ? 2 : 3;

        var attempt = InputFrom(own) with
        {
            StatusId = otherStatus,
            ResultText = "Виконано",
            TaskText = "Підмінене завдання",
            ResolutionText = "Підмінена резолюція",
            DueDate = own.DueDate.AddYears(5),
            ExecutorEmployeeId = own.ExecutorEmployeeId + 1,
        };
        await EnsureStatusAsync(await app.RpcAsync(new UpdateTask.Command(own.Id, attempt), executor.UserId),
            HttpStatusCode.NoContent);

        var saved = (await app.RpcOkAsync<IReadOnlyList<TaskDto>>(new GetTasks.Query(), admin)).Single(t => t.Id == own.Id);
        Assert.Equal(otherStatus, saved.StatusId);
        Assert.Equal("Виконано", saved.ResultText);
        Assert.Equal(own.TaskText, saved.TaskText);
        Assert.Equal(own.ResolutionText, saved.ResolutionText);
        Assert.Equal(own.DueDate, saved.DueDate);
        Assert.Equal(own.ExecutorEmployeeId, saved.ExecutorEmployeeId);

        await EnsureStatusAsync(await app.RpcAsync(new UpdateTask.Command(own.Id, InputFrom(own)), admin),
            HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Executor_cannot_update_someone_elses_task()
    {
        var executor = await app.LoginAsync("executor", "executor123");
        var tasks = await app.RpcOkAsync<IReadOnlyList<TaskDto>>(new GetTasks.Query(), executor.UserId);
        var foreign = tasks.FirstOrDefault(t => t.ExecutorEmployeeId != executor.EmployeeId);
        Assert.NotNull(foreign); // демо-дані: виконавець бачить доручення колег у задіяних документах

        var response = await app.RpcAsync(new UpdateTask.Command(foreign.Id, InputFrom(foreign)), executor.UserId);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Executor_from_another_organization_is_rejected()
    {
        var admin = await UserId("admin");
        var org2Employee = (await app.RpcOkAsync<IReadOnlyList<EmployeeDto>>(new GetEmployees.Query(),
            await UserId("orgadmin2")))[0];
        var task = (await app.RpcOkAsync<IReadOnlyList<TaskDto>>(new GetTasks.Query(), await UserId("registrar")))[0];

        var update = await app.RpcAsync(new UpdateTask.Command(task.Id,
            InputFrom(task) with { ExecutorEmployeeId = org2Employee.Id }), admin);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);

        var create = await app.RpcAsync(new CreateTask.Command(
            InputFrom(task) with { ExecutorEmployeeId = org2Employee.Id }), await UserId("manager"));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
    }

    // ---- Вкладення: зміни лише в зоні видимості документа ----

    [Fact]
    public async Task Attachment_changes_require_document_visibility()
    {
        var registrar = await UserId("registrar");
        var manager = await UserId("manager");
        var hiddenDoc = (await VisibleDocumentIds(registrar)).Except(await VisibleDocumentIds(manager)).First();

        var register = await app.RpcAsync(new RegisterAttachment.Command(hiddenDoc, "test.pdf", "application/pdf",
            1, new byte[32], $"tests/{Guid.NewGuid():N}", null), manager);
        Assert.Equal(HttpStatusCode.NotFound, register.StatusCode);

        var attachmentId = await app.RpcOkAsync<int>(new RegisterAttachment.Command(hiddenDoc, "test.pdf",
            "application/pdf", 1, new byte[32], $"tests/{Guid.NewGuid():N}", null), registrar);

        var replace = new ReplaceAttachmentContent.Command(attachmentId, "x.pdf", "application/pdf", 1, new byte[32], "tests/x");
        Assert.Equal(HttpStatusCode.NotFound, (await app.RpcAsync(replace, manager)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.RpcAsync(replace, await UserId("executor"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await app.RpcAsync(new DeleteAttachmentRecord.Command(attachmentId), manager)).StatusCode);

        await EnsureStatusAsync(await app.RpcAsync(new DeleteAttachmentRecord.Command(attachmentId), registrar),
            HttpStatusCode.OK);
    }
}
