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

    SELECT t.Name AS [Type], COUNT(*) AS [Count]
    FROM dbo.Document d
    JOIN dbo.DocumentType t ON t.Id = d.DocumentTypeId
    WHERE (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId)
      AND dbo.fn_CanReadDocument(d.Id, @ScopeLevel, @EmployeeId, @DepartmentId) = 1
    GROUP BY t.Name
    ORDER BY [Count] DESC, [Type];
END;
GO
