/* Тригер: журнал змін банківських рахунків (dbo.PartyBankAccountHistory).
   Записує створення, видалення й зміну реквізитів рахунку разом зі значеннями до й після (JSON,
   з назвою банку на той момент). Зміна лише BankId (банк додали до довідника пізніше) не записується.
   «Хто змінив» — користувач, якого застосунок передає через SESSION_CONTEXT(N'UserId'). */
CREATE OR ALTER TRIGGER dbo.trg_PartyBankAccount_History
ON dbo.PartyBankAccount
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM inserted) AND NOT EXISTS (SELECT 1 FROM deleted) RETURN;

    DECLARE @UserId int = TRY_CAST(SESSION_CONTEXT(N'UserId') AS int);

    INSERT dbo.PartyBankAccountHistory (AccountId, PartyId, Action, ChangedByUserId, OldValues, NewValues)
    SELECT COALESCE(i.Id, d.Id),
           COALESCE(i.PartyId, d.PartyId),
           CASE WHEN d.Id IS NULL THEN 1 WHEN i.Id IS NULL THEN 3 ELSE 2 END,
           @UserId,
           CASE WHEN d.Id IS NOT NULL THEN (
               SELECT d.Iban, COALESCE(bd.ShortName, bd.Name, d.BankName) AS BankName, d.SwiftBic, d.Currency,
                      d.Purpose, d.AccountHolderName, d.IsPrimary, d.ClosedAt, d.Note
               FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END,
           CASE WHEN i.Id IS NOT NULL THEN (
               SELECT i.Iban, COALESCE(bi.ShortName, bi.Name, i.BankName) AS BankName, i.SwiftBic, i.Currency,
                      i.Purpose, i.AccountHolderName, i.IsPrimary, i.ClosedAt, i.Note
               FOR JSON PATH, WITHOUT_ARRAY_WRAPPER, INCLUDE_NULL_VALUES) END
    FROM inserted i
    FULL JOIN deleted d ON d.Id = i.Id
    LEFT JOIN dbo.Bank bi ON bi.Id = i.BankId
    LEFT JOIN dbo.Bank bd ON bd.Id = d.BankId
    -- Зміна: лише якщо змінилися реквізити (не лише прив'язка до довідника банків)
    WHERE d.Id IS NULL OR i.Id IS NULL
       OR EXISTS (SELECT i.Iban, i.BankName, i.SwiftBic, i.Currency, i.Purpose, i.AccountHolderName,
                         i.IsPrimary, i.ClosedAt, i.Note
                  EXCEPT
                  SELECT d.Iban, d.BankName, d.SwiftBic, d.Currency, d.Purpose, d.AccountHolderName,
                         d.IsPrimary, d.ClosedAt, d.Note);
END;
GO
