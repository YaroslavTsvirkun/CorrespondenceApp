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

    SELECT t.Id AS TaskId, t.DocumentNumber AS Document, t.ExecutorName AS Executor,
           t.DueDate, t.DaysOverdue
    FROM dbo.vw_Task t
    JOIN dbo.fn_VisibleDocuments(@ScopeLevel, @EmployeeId, @DepartmentId, @Bypass, @TenantOrganizationId) v
        ON v.Id = t.DocumentId
    WHERE t.DaysOverdue > 0
    ORDER BY t.DaysOverdue DESC;
END;
GO
