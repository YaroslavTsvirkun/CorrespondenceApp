namespace CorrespondenceSystem.Contracts.Common;

/// <summary>Каталог пермішенів (опис і категорії зберігаються в БД: Database/Seed/001_ReferenceData.sql).</summary>
public static class Permissions
{
    public const string DocumentsReadAll = "documents.read.all";
    public const string DocumentsReadDepartment = "documents.read.department";
    public const string DocumentsReadInvolved = "documents.read.involved";
    public const string DocumentsCreate = "documents.create";
    public const string DocumentsUpdate = "documents.update";
    public const string DocumentsDelete = "documents.delete";
    public const string ResolutionsCreate = "resolutions.create";
    public const string TasksAssign = "tasks.assign";
    public const string TasksExecute = "tasks.execute";
    public const string PartiesManage = "parties.manage";
    public const string PartiesBankManage = "parties.bank.manage";
    public const string DictionariesManage = "dictionaries.manage";
    public const string EmployeesManage = "employees.manage";
    public const string DepartmentsManage = "departments.manage";
    public const string ReportsView = "reports.view";
    public const string DashboardOverview = "dashboard.overview";
    public const string DashboardControl = "dashboard.control";
    public const string DashboardMyTasks = "dashboard.my-tasks";
    public const string UsersManage = "users.manage";
    public const string OrganizationsManage = "organizations.manage";
    public const string SettingsManage = "settings.manage";

    // Композитні політики (будь-який зі складових пермішенів задовольняє)
    public const string DocumentsReadAny = "documents.read";
    public const string TasksWrite = "tasks.write";
    public const string DictionariesWrite = "dictionaries.write";

    public static readonly string[] ReadScopes =
        [DocumentsReadAll, DocumentsReadDepartment, DocumentsReadInvolved];

    public static readonly Dictionary<string, string[]> Composite = new()
    {
        [DocumentsReadAny] = ReadScopes,
        [TasksWrite] = [TasksAssign, TasksExecute],
        // Довідники кореспонденції (dictionaries.manage) і контрагентів (parties.manage) — див. DictionaryTables
        [DictionariesWrite] = [DictionariesManage, PartiesManage],
    };
}
