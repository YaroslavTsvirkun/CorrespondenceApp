/* Скалярна функція: чи зайнятий реєстраційний номер в організації за рік.
   @ExcludeDocumentId — документ, який не враховується (під час редагування). */
CREATE OR ALTER FUNCTION dbo.fn_IsRegistrationNumberTaken
(
    @OrganizationId    int,
    @Year              int,
    @Number            nvarchar(30),
    @ExcludeDocumentId int
)
RETURNS bit
AS
BEGIN
    RETURN CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.RegistrationRecord r
        JOIN dbo.Document d ON d.Id = r.DocumentId
        WHERE d.OrganizationId = @OrganizationId
          AND r.RegistrationYear = @Year
          AND r.RegistrationNumber = @Number
          AND (@ExcludeDocumentId IS NULL OR r.DocumentId <> @ExcludeDocumentId)
    ) THEN 1 ELSE 0 END;
END;
GO
