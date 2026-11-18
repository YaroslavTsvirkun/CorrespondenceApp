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
        SELECT t.ExecutorName AS Executor,
               COUNT(*) AS Total,
               SUM(CASE WHEN t.StatusName = N'Виконано' THEN 1 ELSE 0 END) AS Done
        FROM dbo.vw_Task t
        JOIN dbo.fn_VisibleDocuments(@ScopeLevel, @EmployeeId, @DepartmentId, @Bypass, @TenantOrganizationId) v
            ON v.Id = t.DocumentId
        GROUP BY t.ExecutorName
    )
    SELECT Executor, Total, Done, dbo.fn_Percent(Done, Total) AS [Percent]
    FROM Stats
    ORDER BY Total DESC, Executor;
END;
GO
