/* Представлення: обліковий запис користувача з організацією, працівником і підрозділом.
   Хеш пароля навмисно НЕ включено — його читає лише процедура автентифікації. */
CREATE OR ALTER VIEW dbo.vw_UserAccount
AS
SELECT u.Id,
       u.UserName,
       u.FullName,
       u.OrganizationId,
       o.Name       AS OrganizationName,
       u.EmployeeId,
       e.FullName   AS EmployeeName,
       e.DepartmentId,
       u.IsActive,
       CAST(CASE WHEN u.OrganizationId IS NULL THEN 1 ELSE 0 END AS bit) AS IsSuperAdmin
FROM dbo.AppUser u
LEFT JOIN dbo.Organization o ON o.Id = u.OrganizationId
LEFT JOIN dbo.Employee e     ON e.Id = u.EmployeeId;
GO
