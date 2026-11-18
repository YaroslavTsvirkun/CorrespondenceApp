/* Скалярна функція: наступне значення лічильника реєстраційного номера
   для (організація, тип документа, рік). */
CREATE OR ALTER FUNCTION dbo.fn_NextRegistrationSequence
(
    @OrganizationId int,
    @DocumentTypeId int,
    @Year           int
)
RETURNS int
AS
BEGIN
    RETURN ISNULL((
        SELECT MAX(r.SequenceNumber)
        FROM dbo.RegistrationRecord r
        JOIN dbo.Document d ON d.Id = r.DocumentId
        WHERE d.OrganizationId = @OrganizationId
          AND d.DocumentTypeId = @DocumentTypeId
          AND r.RegistrationYear = @Year
    ), 0) + 1;
END;
GO
