/* Представлення: документ з реєстраційним записом і назвами з довідників.
   Одне джерело для списку, картки та форми редагування документа.
   Не фільтрує доступ — поєднуйте з dbo.fn_VisibleDocuments.
   LEFT JOIN-и за первинними ключами, колонки яких не використано в запиті,
   оптимізатор відкидає (join elimination), тож для списку зайвих з'єднань немає. */
CREATE OR ALTER VIEW dbo.vw_Document
AS
SELECT d.Id,
       d.OrganizationId,
       d.DirectionId,          dir.Code AS DirectionCode, dir.Name AS Direction,
       d.DocumentTypeId,       t.Code   AS DocumentTypeCode, t.Name AS DocumentType,
       d.CurrentStatusId,      s.Name   AS Status, s.IsFinal AS StatusIsFinal,
       d.DeliveryChannelId,    ch.Name  AS Channel,
       -- Назва кореспондента на момент реєстрації (знімок); без знімка — поточна назва контрагента
       d.CorrespondentId,      COALESCE(d.CorrespondentName, c.ShortName, c.FullName) AS Correspondent,
       COALESCE(c.ShortName, c.FullName) AS CorrespondentCurrent,
       d.SenderDepartmentId,   sd.Name  AS SenderDepartment,
       d.ReceiverDepartmentId, rd.Name  AS ReceiverDepartment,
       d.AuthorEmployeeId,     a.FullName AS Author,
       d.Title, d.Summary, d.DocumentDate, d.IncomingNumber, d.IsControlled, d.Note,
       -- Реєстрація (NULL, якщо документ ще не зареєстровано)
       r.RegistrationNumber, r.RegistrationYear, r.SequenceNumber, r.RegisteredAt,
       r.RegistrarEmployeeId,  reg.FullName AS Registrar
FROM dbo.Document d
JOIN dbo.DocumentDirection dir     ON dir.Id = d.DirectionId
JOIN dbo.DocumentType t            ON t.Id = d.DocumentTypeId
JOIN dbo.DocumentStatus s          ON s.Id = d.CurrentStatusId
LEFT JOIN dbo.DeliveryChannel ch   ON ch.Id = d.DeliveryChannelId
LEFT JOIN dbo.Party c              ON c.Id = d.CorrespondentId
LEFT JOIN dbo.Department sd        ON sd.Id = d.SenderDepartmentId
LEFT JOIN dbo.Department rd        ON rd.Id = d.ReceiverDepartmentId
LEFT JOIN dbo.Employee a           ON a.Id = d.AuthorEmployeeId
LEFT JOIN dbo.RegistrationRecord r ON r.DocumentId = d.Id
LEFT JOIN dbo.Employee reg         ON reg.Id = r.RegistrarEmployeeId;
GO
