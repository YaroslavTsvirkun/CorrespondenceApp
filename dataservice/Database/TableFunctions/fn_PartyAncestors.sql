/* Inline-таблична функція: контрагент і всі його головні контрагенти вгору по ієрархії
   (філія -> головна юрособа -> …). Для перевірки циклів у dbo.usp_Party_Save. */
CREATE OR ALTER FUNCTION dbo.fn_PartyAncestors
(
    @PartyId int
)
RETURNS TABLE
AS
RETURN
    WITH chain AS (
        SELECT Id, ParentPartyId FROM dbo.Party WHERE Id = @PartyId
        UNION ALL
        SELECT p.Id, p.ParentPartyId FROM dbo.Party p JOIN chain c ON p.Id = c.ParentPartyId
    )
    SELECT Id FROM chain;
GO
