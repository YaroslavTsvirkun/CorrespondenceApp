/* Inline-таблична функція: документи, які бачить користувач.
   Поєднує тенант-ізоляцію (@Bypass / @TenantOrganizationId) і зону видимості:
     @ScopeLevel 3 — уся кореспонденція, 2 — свого підрозділу, 1 — лише задіяні, 0 — нічого.
   Inline TVF розгортається оптимізатором у план запиту як підзапит, тому, на відміну від
   скалярної функції у WHERE, не виконується окремо для кожного рядка.
   Використання: FROM dbo.vw_Document d JOIN dbo.fn_VisibleDocuments(...) v ON v.Id = d.Id */
CREATE OR ALTER FUNCTION dbo.fn_VisibleDocuments
(
    @ScopeLevel           tinyint,
    @EmployeeId           int,
    @DepartmentId         int,
    @Bypass               bit,
    @TenantOrganizationId int
)
RETURNS TABLE
AS
RETURN
    SELECT d.Id
    FROM dbo.Document d
    LEFT JOIN dbo.Employee a ON a.Id = d.AuthorEmployeeId
    WHERE (@Bypass = 1 OR d.OrganizationId = @TenantOrganizationId)
      AND (
            @ScopeLevel >= 3
         OR (@ScopeLevel = 2 AND (d.SenderDepartmentId = @DepartmentId
                                  OR d.ReceiverDepartmentId = @DepartmentId
                                  OR a.DepartmentId = @DepartmentId))
         OR (@ScopeLevel = 1 AND (d.AuthorEmployeeId = @EmployeeId
                                  OR EXISTS (SELECT 1 FROM dbo.RegistrationRecord r
                                             WHERE r.DocumentId = d.Id AND r.RegistrarEmployeeId = @EmployeeId)
                                  OR EXISTS (SELECT 1 FROM dbo.Resolution x
                                             WHERE x.DocumentId = d.Id AND x.AuthorEmployeeId = @EmployeeId)
                                  OR EXISTS (SELECT 1 FROM dbo.ExecutionTask t
                                             WHERE t.DocumentId = d.Id AND t.ExecutorEmployeeId = @EmployeeId)))
          );
GO
