/* Тригер: підтримує денормалізоване поле RegistrationRecord.OrganizationId
   рівним організації документа (на ньому тримається унікальність номера
   в межах організації та року). */
CREATE OR ALTER TRIGGER dbo.trg_RegistrationRecord_SyncOrganization
ON dbo.RegistrationRecord
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) RETURN;

    UPDATE r
    SET OrganizationId = d.OrganizationId
    FROM dbo.RegistrationRecord r
    JOIN inserted i     ON i.Id = r.Id
    JOIN dbo.Document d ON d.Id = r.DocumentId
    WHERE r.OrganizationId <> d.OrganizationId;
END;
GO
