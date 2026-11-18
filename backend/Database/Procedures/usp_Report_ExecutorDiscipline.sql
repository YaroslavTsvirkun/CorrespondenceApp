/* Звіт: виконавська дисципліна — скільки доручень призначено й виконано кожним працівником. */
CREATE OR ALTER PROCEDURE dbo.usp_Report_ExecutorDiscipline
    @ScopeLevel           tinyint,
    @EmployeeId           int,
    @DepartmentId         int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;

    WITH Stats AS
    (
        SELECT e.FullName AS Executor,
               COUNT(*) AS Total,
               SUM(CASE WHEN s.Name = N'Виконано' THEN 1 ELSE 0 END) AS Done
        FROM dbo.ExecutionTask t
        JOIN dbo.Document d   ON d.Id = t.DocumentId
        JOIN dbo.Employee e   ON e.Id = t.ExecutorEmployeeId
        JOIN dbo.TaskStatus s ON s.Id = t.TaskStatusId
        WHERE (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId)
          AND dbo.fn_CanReadDocument(d.Id, @ScopeLevel, @EmployeeId, @DepartmentId) = 1
        GROUP BY e.FullName
    )
    SELECT Executor, Total, Done, dbo.fn_Percent(Done, Total) AS [Percent]
    FROM Stats
    ORDER BY Total DESC, Executor;
END;
GO
