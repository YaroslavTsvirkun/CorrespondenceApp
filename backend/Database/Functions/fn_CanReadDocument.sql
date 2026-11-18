/* Скалярна функція: чи має користувач доступ до документа згідно із зоною видимості.
   @ScopeLevel: 3 = уся кореспонденція (documents.read.all)
                2 = свого підрозділу (documents.read.department)
                1 = лише задіяні документи (documents.read.involved)
                0 = немає доступу
   Тенант-ізоляція (організація) перевіряється окремо у запиті. */
CREATE OR ALTER FUNCTION dbo.fn_CanReadDocument
(
    @DocumentId   int,
    @ScopeLevel   tinyint,
    @EmployeeId   int,
    @DepartmentId int
)
RETURNS bit
AS
BEGIN
    DECLARE @result bit = 0;

    IF @ScopeLevel >= 3
        SET @result = 1;
    ELSE IF @ScopeLevel = 2
        SET @result = CASE WHEN EXISTS (
            SELECT 1
            FROM dbo.Document d
            LEFT JOIN dbo.Employee a ON a.Id = d.AuthorEmployeeId
            WHERE d.Id = @DocumentId
              AND (d.SenderDepartmentId = @DepartmentId
                   OR d.ReceiverDepartmentId = @DepartmentId
                   OR a.DepartmentId = @DepartmentId)
        ) THEN 1 ELSE 0 END;
    ELSE IF @ScopeLevel = 1
        SET @result = CASE
            WHEN EXISTS (SELECT 1 FROM dbo.Document d WHERE d.Id = @DocumentId AND d.AuthorEmployeeId = @EmployeeId) THEN 1
            WHEN EXISTS (SELECT 1 FROM dbo.RegistrationRecord r WHERE r.DocumentId = @DocumentId AND r.RegistrarEmployeeId = @EmployeeId) THEN 1
            WHEN EXISTS (SELECT 1 FROM dbo.Resolution x WHERE x.DocumentId = @DocumentId AND x.AuthorEmployeeId = @EmployeeId) THEN 1
            WHEN EXISTS (SELECT 1 FROM dbo.ExecutionTask t WHERE t.DocumentId = @DocumentId AND t.ExecutorEmployeeId = @EmployeeId) THEN 1
            ELSE 0 END;

    RETURN @result;
END;
GO
