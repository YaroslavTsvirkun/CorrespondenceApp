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

    WITH Scoped AS
    (
        SELECT d.DirectionId
        FROM dbo.Document d
        WHERE (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId)
          AND dbo.fn_CanReadDocument(d.Id, @ScopeLevel, @EmployeeId, @DepartmentId) = 1
    ),
    Grouped AS
    (
        SELECT dir.Name AS Direction, COUNT(*) AS [Count]
        FROM Scoped s
        JOIN dbo.DocumentDirection dir ON dir.Id = s.DirectionId
        GROUP BY dir.Name
    )
    SELECT g.Direction,
           g.[Count],
           dbo.fn_Percent(g.[Count], (SELECT COUNT(*) FROM Scoped)) AS [Percent]
    FROM Grouped g
    ORDER BY g.[Count] DESC, g.Direction;
END;
GO
