/* Скалярна функція: номер документа для відображення — «номер/рік» або «—»,
   якщо документ ще не зареєстровано. */
CREATE OR ALTER FUNCTION dbo.fn_DocumentDisplayNumber
(
    @DocumentId int
)
RETURNS nvarchar(50)
AS
BEGIN
    RETURN ISNULL((
        SELECT TOP (1) r.RegistrationNumber + N'/' + CAST(r.RegistrationYear AS nvarchar(10))
        FROM dbo.RegistrationRecord r
        WHERE r.DocumentId = @DocumentId
    ), N'—');
END;
GO
