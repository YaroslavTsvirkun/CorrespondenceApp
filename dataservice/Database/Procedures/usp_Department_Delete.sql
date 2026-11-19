/* Процедура: видалення підрозділу (заборонено, якщо в ньому є працівники
   або підпорядковані підрозділи). */
CREATE OR ALTER PROCEDURE dbo.usp_Department_Delete
    @Id                   int,
    @Bypass               bit,
    @TenantOrganizationId int
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Department
                   WHERE Id = @Id AND (@Bypass = 1 OR OrganizationId = @TenantOrganizationId))
    BEGIN
        DECLARE @Message nvarchar(200) = CONCAT(N'Підрозділ #', @Id, N' не знайдено');
        THROW 50404, @Message, 1;
    END;

    IF EXISTS (SELECT 1 FROM dbo.Employee WHERE DepartmentId = @Id)
       OR EXISTS (SELECT 1 FROM dbo.Department WHERE ParentDepartmentId = @Id)
        THROW 50400, N'У підрозділі є працівники або підпорядковані підрозділи — видалення заборонено', 1;

    DELETE dbo.Department WHERE Id = @Id;
END;
GO
