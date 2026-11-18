/* Звіт: прострочені доручення (нефінальний статус і строк минув). */
CREATE OR ALTER PROCEDURE dbo.usp_Report_OverdueTasks
    @ScopeLevel           tinyint,
    @EmployeeId           int,
    @DepartmentId         int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT x.TaskId, x.Document, x.Executor, x.DueDate, x.DaysOverdue
    FROM
    (
        SELECT t.Id AS TaskId,
               dbo.fn_DocumentDisplayNumber(t.DocumentId) AS Document,
               e.FullName AS Executor,
               t.DueDate,
               dbo.fn_TaskDaysOverdue(t.DueDate, t.TaskStatusId) AS DaysOverdue
        FROM dbo.ExecutionTask t
        JOIN dbo.Document d ON d.Id = t.DocumentId
        JOIN dbo.Employee e ON e.Id = t.ExecutorEmployeeId
        WHERE (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId)
          AND dbo.fn_CanReadDocument(d.Id, @ScopeLevel, @EmployeeId, @DepartmentId) = 1
    ) x
    WHERE x.DaysOverdue > 0
    ORDER BY x.DaysOverdue DESC;
END;
GO
