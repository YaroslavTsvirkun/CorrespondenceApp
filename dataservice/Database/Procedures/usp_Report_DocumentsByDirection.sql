/* Звіт: розподіл документів за напрямом кореспонденції (з часткою у відсотках). */
CREATE OR ALTER PROCEDURE dbo.usp_Report_DocumentsByDirection
    @ScopeLevel           tinyint,
    @EmployeeId           int,
    @DepartmentId         int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;

    WITH Grouped AS
    (
        SELECT d.Direction, COUNT(*) AS [Count]
        FROM dbo.vw_Document d
        JOIN dbo.fn_VisibleDocuments(@ScopeLevel, @EmployeeId, @DepartmentId, @Bypass, @TenantOrganizationId) v
            ON v.Id = d.Id
        GROUP BY d.Direction
    )
    SELECT g.Direction,
           g.[Count],
           dbo.fn_Percent(g.[Count], SUM(g.[Count]) OVER ()) AS [Percent]
    FROM Grouped g
    ORDER BY g.[Count] DESC, g.Direction;
END;
GO
