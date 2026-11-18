/* Звіт: кількість документів за типами (у зоні видимості користувача). */
CREATE OR ALTER PROCEDURE dbo.usp_Report_DocumentsByType
    @ScopeLevel           tinyint,
    @EmployeeId           int,
    @DepartmentId         int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT d.DocumentType AS [Type], COUNT(*) AS [Count]
    FROM dbo.vw_Document d
    JOIN dbo.fn_VisibleDocuments(@ScopeLevel, @EmployeeId, @DepartmentId, @Bypass, @TenantOrganizationId) v
        ON v.Id = d.Id
    GROUP BY d.DocumentType
    ORDER BY [Count] DESC, [Type];
END;
GO
