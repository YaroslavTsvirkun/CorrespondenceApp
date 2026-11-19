/* =====================================================================
   001_Tables.sql — структура БД (таблиці, ключі, індекси).
   Скрипт ОДНОРАЗОВИЙ: виконується деплоєром один раз і фіксується в
   журналі dbo.__SchemaVersions. Кожна таблиця створюється лише якщо її
   ще немає — тож скрипт безпечний і для БД, створеної раніше EF Core.
   ===================================================================== */

-- ---------- Організації та оргструктура ----------
IF OBJECT_ID(N'dbo.Organization', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Organization
    (
        Id                        int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Organization PRIMARY KEY,
        Name                      nvarchar(200) NOT NULL,
        Code                      nvarchar(30)  NULL,
        ParentOrganizationId      int           NULL
            CONSTRAINT FK_Organization_Organization_ParentOrganizationId REFERENCES dbo.Organization (Id),
        IsActive                  bit           NOT NULL CONSTRAINT DF_Organization_IsActive DEFAULT (1),
        -- Токени: {seq}/{seq:000}, {year}, {yy}, {type}, {dir}
        RegistrationNumberPattern nvarchar(100) NOT NULL
            CONSTRAINT DF_Organization_RegistrationNumberPattern DEFAULT (N'{type}-{seq:000}/{year}')
    );
    CREATE UNIQUE INDEX IX_Organization_Name ON dbo.Organization (Name);
    CREATE INDEX IX_Organization_ParentOrganizationId ON dbo.Organization (ParentOrganizationId);
END;
GO

IF OBJECT_ID(N'dbo.Department', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Department
    (
        Id                 int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Department PRIMARY KEY,
        OrganizationId     int           NOT NULL
            CONSTRAINT FK_Department_Organization_OrganizationId REFERENCES dbo.Organization (Id),
        ParentDepartmentId int           NULL
            CONSTRAINT FK_Department_Department_ParentDepartmentId REFERENCES dbo.Department (Id),
        Name               nvarchar(150) NOT NULL,
        ShortName          nvarchar(50)  NULL,
        Description        nvarchar(max) NULL,
        IsActive           bit           NOT NULL CONSTRAINT DF_Department_IsActive DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_Department_OrganizationId_Name ON dbo.Department (OrganizationId, Name);
    CREATE INDEX IX_Department_ParentDepartmentId ON dbo.Department (ParentDepartmentId);
END;
GO

IF OBJECT_ID(N'dbo.Employee', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Employee
    (
        Id             int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Employee PRIMARY KEY,
        OrganizationId int           NOT NULL
            CONSTRAINT FK_Employee_Organization_OrganizationId REFERENCES dbo.Organization (Id),
        DepartmentId   int           NOT NULL
            CONSTRAINT FK_Employee_Department_DepartmentId REFERENCES dbo.Department (Id),
        FullName       nvarchar(150) NOT NULL,
        PositionName   nvarchar(100) NOT NULL,
        Phone          nvarchar(max) NULL,
        Email          nvarchar(100) NULL,
        IsActive       bit           NOT NULL CONSTRAINT DF_Employee_IsActive DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_Employee_Email ON dbo.Employee (Email) WHERE Email IS NOT NULL;
    CREATE INDEX IX_Employee_DepartmentId ON dbo.Employee (DepartmentId);
    CREATE INDEX IX_Employee_OrganizationId ON dbo.Employee (OrganizationId);
END;
GO

-- ---------- Довідники ----------
IF OBJECT_ID(N'dbo.CorrespondentType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CorrespondentType
    (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CorrespondentType PRIMARY KEY,
        Name        nvarchar(100) NOT NULL,
        Description nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX IX_CorrespondentType_Name ON dbo.CorrespondentType (Name);
END;
GO

IF OBJECT_ID(N'dbo.Correspondent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Correspondent
    (
        Id                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Correspondent PRIMARY KEY,
        OrganizationId      int           NOT NULL
            CONSTRAINT FK_Correspondent_Organization_OrganizationId REFERENCES dbo.Organization (Id),
        CorrespondentTypeId int           NOT NULL
            CONSTRAINT FK_Correspondent_CorrespondentType_CorrespondentTypeId REFERENCES dbo.CorrespondentType (Id),
        Name                nvarchar(200) NOT NULL,
        Address             nvarchar(max) NULL,
        Phone               nvarchar(max) NULL,
        Email               nvarchar(max) NULL,
        ContactPerson       nvarchar(max) NULL,
        Note                nvarchar(max) NULL
    );
    CREATE INDEX IX_Correspondent_CorrespondentTypeId ON dbo.Correspondent (CorrespondentTypeId);
    CREATE INDEX IX_Correspondent_OrganizationId ON dbo.Correspondent (OrganizationId);
END;
GO

IF OBJECT_ID(N'dbo.DocumentDirection', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentDirection
    (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentDirection PRIMARY KEY,
        Code        nvarchar(10)  NOT NULL,
        Name        nvarchar(50)  NOT NULL,
        Description nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX IX_DocumentDirection_Code ON dbo.DocumentDirection (Code);
    CREATE UNIQUE INDEX IX_DocumentDirection_Name ON dbo.DocumentDirection (Name);
END;
GO

IF OBJECT_ID(N'dbo.DocumentType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentType
    (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentType PRIMARY KEY,
        Name        nvarchar(100) NOT NULL,
        Code        nvarchar(20)  NULL,   -- токен {type} у патерні реєстраційного номера
        Description nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX IX_DocumentType_Name ON dbo.DocumentType (Name);
END;
GO

IF OBJECT_ID(N'dbo.DocumentStatus', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentStatus
    (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DocumentStatus PRIMARY KEY,
        Name        nvarchar(50)  NOT NULL,
        IsFinal     bit           NOT NULL,
        Description nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX IX_DocumentStatus_Name ON dbo.DocumentStatus (Name);
END;
GO

IF OBJECT_ID(N'dbo.DeliveryChannel', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeliveryChannel
    (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_DeliveryChannel PRIMARY KEY,
        Name        nvarchar(80)  NOT NULL,
        Description nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX IX_DeliveryChannel_Name ON dbo.DeliveryChannel (Name);
END;
GO

IF OBJECT_ID(N'dbo.TaskStatus', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TaskStatus
    (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskStatus PRIMARY KEY,
        Name        nvarchar(50)  NOT NULL,
        IsFinal     bit           NOT NULL,
        Description nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX IX_TaskStatus_Name ON dbo.TaskStatus (Name);
END;
GO

-- ---------- Документи ----------
IF OBJECT_ID(N'dbo.Document', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Document
    (
        Id                   int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Document PRIMARY KEY,
        OrganizationId       int            NOT NULL
            CONSTRAINT FK_Document_Organization_OrganizationId REFERENCES dbo.Organization (Id),
        DirectionId          int            NOT NULL
            CONSTRAINT FK_Document_DocumentDirection_DirectionId REFERENCES dbo.DocumentDirection (Id),
        DocumentTypeId       int            NOT NULL
            CONSTRAINT FK_Document_DocumentType_DocumentTypeId REFERENCES dbo.DocumentType (Id),
        CurrentStatusId      int            NOT NULL
            CONSTRAINT FK_Document_DocumentStatus_CurrentStatusId REFERENCES dbo.DocumentStatus (Id),
        DeliveryChannelId    int            NULL
            CONSTRAINT FK_Document_DeliveryChannel_DeliveryChannelId REFERENCES dbo.DeliveryChannel (Id) ON DELETE SET NULL,
        CorrespondentId      int            NULL
            CONSTRAINT FK_Document_Correspondent_CorrespondentId REFERENCES dbo.Correspondent (Id) ON DELETE SET NULL,
        SenderDepartmentId   int            NULL
            CONSTRAINT FK_Document_Department_SenderDepartmentId REFERENCES dbo.Department (Id),
        ReceiverDepartmentId int            NULL
            CONSTRAINT FK_Document_Department_ReceiverDepartmentId REFERENCES dbo.Department (Id),
        AuthorEmployeeId     int            NULL
            CONSTRAINT FK_Document_Employee_AuthorEmployeeId REFERENCES dbo.Employee (Id),
        Title                nvarchar(300)  NOT NULL,
        Summary              nvarchar(1000) NULL,
        DocumentDate         date           NOT NULL,
        IncomingNumber       nvarchar(max)  NULL,
        IsControlled         bit            NOT NULL CONSTRAINT DF_Document_IsControlled DEFAULT (0),
        Note                 nvarchar(max)  NULL
    );
    CREATE INDEX IX_Document_OrganizationId ON dbo.Document (OrganizationId);
    CREATE INDEX IX_Document_DocumentDate ON dbo.Document (DocumentDate);
    CREATE INDEX IX_Document_CurrentStatusId ON dbo.Document (CurrentStatusId);
    CREATE INDEX IX_Document_AuthorEmployeeId ON dbo.Document (AuthorEmployeeId);
END;
GO

IF OBJECT_ID(N'dbo.RegistrationRecord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RegistrationRecord
    (
        Id                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_RegistrationRecord PRIMARY KEY,
        DocumentId          int           NOT NULL
            CONSTRAINT FK_RegistrationRecord_Document_DocumentId REFERENCES dbo.Document (Id) ON DELETE CASCADE,
        RegistrarEmployeeId int           NOT NULL
            CONSTRAINT FK_RegistrationRecord_Employee_RegistrarEmployeeId REFERENCES dbo.Employee (Id),
        RegistrationNumber  nvarchar(30)  NOT NULL,
        RegistrationYear    int           NOT NULL,
        -- Денормалізовано з документа (підтримується тригером) — для унікальності номера
        OrganizationId      int           NOT NULL,
        -- Значення лічильника за (організація, тип, рік); NULL = номер уведено вручну
        SequenceNumber      int           NULL,
        RegisteredAt        datetime2     NOT NULL CONSTRAINT DF_RegistrationRecord_RegisteredAt DEFAULT (SYSUTCDATETIME()),
        Comment             nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX IX_RegistrationRecord_DocumentId ON dbo.RegistrationRecord (DocumentId);
    CREATE UNIQUE INDEX IX_RegistrationRecord_OrganizationId_RegistrationYear_RegistrationNumber
        ON dbo.RegistrationRecord (OrganizationId, RegistrationYear, RegistrationNumber);
    CREATE INDEX IX_RegistrationRecord_RegistrarEmployeeId ON dbo.RegistrationRecord (RegistrarEmployeeId);
END;
GO

IF OBJECT_ID(N'dbo.Resolution', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Resolution
    (
        Id               int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Resolution PRIMARY KEY,
        DocumentId       int            NOT NULL
            CONSTRAINT FK_Resolution_Document_DocumentId REFERENCES dbo.Document (Id) ON DELETE CASCADE,
        AuthorEmployeeId int            NOT NULL
            CONSTRAINT FK_Resolution_Employee_AuthorEmployeeId REFERENCES dbo.Employee (Id),
        ResolutionText   nvarchar(1000) NOT NULL,
        ResolutionDate   date           NOT NULL,
        DueDate          date           NULL,
        IsControlled     bit            NOT NULL
    );
    CREATE INDEX IX_Resolution_DocumentId ON dbo.Resolution (DocumentId);
    CREATE INDEX IX_Resolution_AuthorEmployeeId ON dbo.Resolution (AuthorEmployeeId);
END;
GO

IF OBJECT_ID(N'dbo.ExecutionTask', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExecutionTask
    (
        Id                 int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExecutionTask PRIMARY KEY,
        ResolutionId       int            NOT NULL
            CONSTRAINT FK_ExecutionTask_Resolution_ResolutionId REFERENCES dbo.Resolution (Id),
        DocumentId         int            NOT NULL
            CONSTRAINT FK_ExecutionTask_Document_DocumentId REFERENCES dbo.Document (Id) ON DELETE CASCADE,
        ExecutorEmployeeId int            NOT NULL
            CONSTRAINT FK_ExecutionTask_Employee_ExecutorEmployeeId REFERENCES dbo.Employee (Id),
        TaskStatusId       int            NOT NULL
            CONSTRAINT FK_ExecutionTask_TaskStatus_TaskStatusId REFERENCES dbo.TaskStatus (Id),
        TaskText           nvarchar(1000) NOT NULL,
        AssignedAt         datetime2      NOT NULL CONSTRAINT DF_ExecutionTask_AssignedAt DEFAULT (SYSUTCDATETIME()),
        DueDate            date           NOT NULL,
        CompletedAt        datetime2      NULL,
        ResultText         nvarchar(max)  NULL
    );
    CREATE INDEX IX_ExecutionTask_DocumentId ON dbo.ExecutionTask (DocumentId);
    CREATE INDEX IX_ExecutionTask_ResolutionId ON dbo.ExecutionTask (ResolutionId);
    CREATE INDEX IX_ExecutionTask_ExecutorEmployeeId ON dbo.ExecutionTask (ExecutorEmployeeId);
    CREATE INDEX IX_ExecutionTask_TaskStatusId ON dbo.ExecutionTask (TaskStatusId);
    CREATE INDEX IX_ExecutionTask_DueDate ON dbo.ExecutionTask (DueDate);
END;
GO

IF OBJECT_ID(N'dbo.Attachment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Attachment
    (
        Id                int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Attachment PRIMARY KEY,
        DocumentId        int           NOT NULL
            CONSTRAINT FK_Attachment_Document_DocumentId REFERENCES dbo.Document (Id) ON DELETE CASCADE,
        AddedByEmployeeId int           NULL
            CONSTRAINT FK_Attachment_Employee_AddedByEmployeeId REFERENCES dbo.Employee (Id),
        FileName          nvarchar(255) NOT NULL,
        FileType          nvarchar(max) NULL,
        FilePath          nvarchar(500) NOT NULL,
        AddedAt           datetime2     NOT NULL CONSTRAINT DF_Attachment_AddedAt DEFAULT (SYSUTCDATETIME()),
        Description       nvarchar(max) NULL
    );
    CREATE INDEX IX_Attachment_DocumentId ON dbo.Attachment (DocumentId);
END;
GO

IF OBJECT_ID(N'dbo.StatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.StatusHistory
    (
        Id                  int IDENTITY(1,1) NOT NULL CONSTRAINT PK_StatusHistory PRIMARY KEY,
        DocumentId          int           NOT NULL
            CONSTRAINT FK_StatusHistory_Document_DocumentId REFERENCES dbo.Document (Id) ON DELETE CASCADE,
        PreviousStatusId    int           NULL
            CONSTRAINT FK_StatusHistory_DocumentStatus_PreviousStatusId REFERENCES dbo.DocumentStatus (Id),
        NewStatusId         int           NOT NULL
            CONSTRAINT FK_StatusHistory_DocumentStatus_NewStatusId REFERENCES dbo.DocumentStatus (Id),
        ChangedByEmployeeId int           NOT NULL
            CONSTRAINT FK_StatusHistory_Employee_ChangedByEmployeeId REFERENCES dbo.Employee (Id),
        ChangedAt           datetime2     NOT NULL CONSTRAINT DF_StatusHistory_ChangedAt DEFAULT (SYSUTCDATETIME()),
        Comment             nvarchar(max) NULL
    );
    CREATE INDEX IX_StatusHistory_DocumentId ON dbo.StatusHistory (DocumentId);
END;
GO

-- ---------- Авторизація ----------
IF OBJECT_ID(N'dbo.AppUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUser
    (
        Id             int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUser PRIMARY KEY,
        UserName       nvarchar(50)  NOT NULL,
        PasswordHash   nvarchar(256) NOT NULL,   -- PBKDF2-SHA256: base64(salt).base64(hash)
        FullName       nvarchar(150) NOT NULL,
        -- NULL = супер-адмін (не прив'язаний до жодної організації)
        OrganizationId int           NULL
            CONSTRAINT FK_AppUser_Organization_OrganizationId REFERENCES dbo.Organization (Id) ON DELETE SET NULL,
        EmployeeId     int           NULL
            CONSTRAINT FK_AppUser_Employee_EmployeeId REFERENCES dbo.Employee (Id) ON DELETE SET NULL,
        IsActive       bit           NOT NULL CONSTRAINT DF_AppUser_IsActive DEFAULT (1)
    );
    CREATE UNIQUE INDEX IX_AppUser_UserName ON dbo.AppUser (UserName);
END;
GO

IF OBJECT_ID(N'dbo.Role', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Role
    (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Role PRIMARY KEY,
        Name        nvarchar(50)  NOT NULL,
        Description nvarchar(max) NULL
    );
    CREATE UNIQUE INDEX IX_Role_Name ON dbo.Role (Name);
END;
GO

IF OBJECT_ID(N'dbo.Permission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permission
    (
        Id          int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permission PRIMARY KEY,
        Code        nvarchar(80)  NOT NULL,   -- напр. documents.read.all
        Description nvarchar(max) NULL,
        Category    nvarchar(50)  NULL
    );
    CREATE UNIQUE INDEX IX_Permission_Code ON dbo.Permission (Code);
END;
GO

IF OBJECT_ID(N'dbo.UserRole', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRole
    (
        UserId int NOT NULL CONSTRAINT FK_UserRole_AppUser_UserId REFERENCES dbo.AppUser (Id) ON DELETE CASCADE,
        RoleId int NOT NULL CONSTRAINT FK_UserRole_Role_RoleId REFERENCES dbo.Role (Id) ON DELETE CASCADE,
        CONSTRAINT PK_UserRole PRIMARY KEY (UserId, RoleId)
    );
    CREATE INDEX IX_UserRole_RoleId ON dbo.UserRole (RoleId);
END;
GO

IF OBJECT_ID(N'dbo.RolePermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermission
    (
        RoleId       int NOT NULL CONSTRAINT FK_RolePermission_Role_RoleId REFERENCES dbo.Role (Id) ON DELETE CASCADE,
        PermissionId int NOT NULL CONSTRAINT FK_RolePermission_Permission_PermissionId REFERENCES dbo.Permission (Id) ON DELETE CASCADE,
        CONSTRAINT PK_RolePermission PRIMARY KEY (RoleId, PermissionId)
    );
    CREATE INDEX IX_RolePermission_PermissionId ON dbo.RolePermission (PermissionId);
END;
GO
