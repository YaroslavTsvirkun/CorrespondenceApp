/* Версії вкладень: редагування документа (ONLYOFFICE) замінює вміст вкладення,
   попередній вміст зберігається тут — його можна переглянути, завантажити чи відновити.
   Кожен рядок — знімок вмісту, яким вкладення було до заміни (файл лишається у сховищі під StorageKey). */

-- Хто і коли востаннє змінював вміст вкладення
IF COL_LENGTH(N'dbo.Attachment', N'ModifiedAt') IS NULL
    ALTER TABLE dbo.Attachment ADD ModifiedAt datetime2 NULL;
GO

IF COL_LENGTH(N'dbo.Attachment', N'ModifiedByEmployeeId') IS NULL
    ALTER TABLE dbo.Attachment ADD ModifiedByEmployeeId int NULL
        CONSTRAINT FK_Attachment_Employee_ModifiedByEmployeeId REFERENCES dbo.Employee (Id);
GO

IF OBJECT_ID(N'dbo.AttachmentVersion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AttachmentVersion
    (
        Id                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AttachmentVersion PRIMARY KEY,
        AttachmentId        int           NOT NULL
            CONSTRAINT FK_AttachmentVersion_Attachment_AttachmentId
                REFERENCES dbo.Attachment (Id) ON DELETE CASCADE,
        VersionNo           int           NOT NULL,
        FileName            nvarchar(255) NOT NULL,
        ContentType         nvarchar(127) NOT NULL,
        SizeBytes           bigint        NOT NULL,
        Sha256              binary(32)    NULL,
        StorageKey          nvarchar(500) NOT NULL,
        -- Коли й ким створено цей вміст (завантаження або попереднє редагування)
        CreatedAt           datetime2     NOT NULL,
        CreatedByEmployeeId int           NULL
            CONSTRAINT FK_AttachmentVersion_Employee_CreatedByEmployeeId REFERENCES dbo.Employee (Id),
        CONSTRAINT UX_AttachmentVersion_AttachmentId_VersionNo UNIQUE (AttachmentId, VersionNo)
    );
    CREATE UNIQUE INDEX UX_AttachmentVersion_StorageKey ON dbo.AttachmentVersion (StorageKey);
END;
GO
