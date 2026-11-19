/* Скалярна функція: чи має користувач доступ до ОДНОГО документа згідно із зоною видимості.
   @ScopeLevel: 3 = уся кореспонденція, 2 = свого підрозділу, 1 = лише задіяні, 0 = немає доступу.
   Логіка зони видимості одна — делегується табличній функції dbo.fn_VisibleDocuments;
   тенант-ізоляція (організація) перевіряється окремо у запиті.
   Для списків використовуйте саму dbo.fn_VisibleDocuments (JOIN), а не цю функцію в WHERE. */
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
    RETURN CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.fn_VisibleDocuments(@ScopeLevel, @EmployeeId, @DepartmentId, 1, NULL) v
        WHERE v.Id = @DocumentId
    ) THEN 1 ELSE 0 END;
END;
GO
