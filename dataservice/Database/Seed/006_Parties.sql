/* =====================================================================
   006_Parties.sql — довідники й права для реєстру контрагентів
   (таблиці створює Schema/006_Parties.sql). Ідемпотентний.
     - correspondents.manage перейменовується на parties.manage (призначення
       ролям зберігаються — посилання за Id);
     - новий пермішен parties.bank.manage (банківські реквізити) — ролям,
       що керують користувачами (адміністратори);
     - правові форми та основні банки (довідник банків доповнюйте з
       довідника НБУ);
     - видаляється застарілий довідник dbo.CorrespondentType (його типи
       перенесено у види й категорії контрагентів).
   ===================================================================== */
SET NOCOUNT ON;

UPDATE dbo.Permission
SET Code = N'parties.manage', Description = N'Ведення реєстру контрагентів', Category = N'Контрагенти'
WHERE Code = N'correspondents.manage';

IF NOT EXISTS (SELECT 1 FROM dbo.Permission WHERE Code = N'parties.bank.manage')
    INSERT dbo.Permission (Code, Category, Description)
    VALUES (N'parties.bank.manage', N'Контрагенти', N'Банківські рахунки контрагентів і довідник банків');

INSERT dbo.RolePermission (RoleId, PermissionId)
SELECT DISTINCT rp.RoleId, bank.Id
FROM dbo.RolePermission rp
JOIN dbo.Permission users ON users.Id = rp.PermissionId AND users.Code = N'users.manage'
CROSS JOIN dbo.Permission bank
WHERE bank.Code = N'parties.bank.manage'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermission x WHERE x.RoleId = rp.RoleId AND x.PermissionId = bank.Id);

INSERT dbo.LegalForm (Code, Name)
SELECT v.Code, v.Name
FROM (VALUES
    (N'ТОВ',  N'Товариство з обмеженою відповідальністю'),
    (N'ТДВ',  N'Товариство з додатковою відповідальністю'),
    (N'АТ',   N'Акціонерне товариство'),
    (N'ПрАТ', N'Приватне акціонерне товариство'),
    (N'ПАТ',  N'Публічне акціонерне товариство'),
    (N'ПП',   N'Приватне підприємство'),
    (N'ДП',   N'Державне підприємство'),
    (N'КП',   N'Комунальне підприємство'),
    (N'КНП',  N'Комунальне некомерційне підприємство'),
    (N'ДУ',   N'Державна установа'),
    (N'КУ',   N'Комунальна установа'),
    (N'ГО',   N'Громадська організація'),
    (N'БО',   N'Благодійна організація'),
    (N'ОСББ', N'Об''єднання співвласників багатоквартирного будинку'),
    (N'ФГ',   N'Фермерське господарство')
) AS v (Code, Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.LegalForm f WHERE f.Name = v.Name);

INSERT dbo.Bank (Mfo, Name, ShortName)
SELECT v.Mfo, v.Name, v.ShortName
FROM (VALUES
    ('305299', N'АТ КБ «ПриватБанк»',                     N'ПриватБанк'),
    ('300465', N'АТ «Ощадбанк»',                          N'Ощадбанк'),
    ('322313', N'АТ «Укрексімбанк»',                      N'Укрексімбанк'),
    ('380805', N'АТ «Райффайзен Банк»',                   N'Райффайзен Банк'),
    ('334851', N'АТ «Перший Український Міжнародний Банк»', N'ПУМБ'),
    ('322001', N'АТ «Універсал Банк»',                    N'Універсал Банк'),
    ('320478', N'АБ «Укргазбанк»',                        N'Укргазбанк'),
    ('300346', N'АТ «Сенс Банк»',                         N'Сенс Банк'),
    ('300528', N'АТ «ОТП Банк»',                          N'ОТП Банк'),
    ('325365', N'АТ «Кредобанк»',                         N'Кредобанк'),
    ('820172', N'Державна казначейська служба України',   N'ДКСУ')
) AS v (Mfo, Name, ShortName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Bank b WHERE b.Mfo = v.Mfo);

IF OBJECT_ID(N'dbo.CorrespondentType', N'U') IS NOT NULL
    DROP TABLE dbo.CorrespondentType;
GO
