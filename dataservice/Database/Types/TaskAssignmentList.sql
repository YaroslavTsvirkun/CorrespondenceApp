/* Табличний тип: набір доручень однієї резолюції (кілька виконавців за раз).
   Використовується процедурою dbo.usp_Task_CreateBatch. */
IF TYPE_ID(N'dbo.TaskAssignmentList') IS NULL
    CREATE TYPE dbo.TaskAssignmentList AS TABLE
    (
        RowNo              int            NOT NULL PRIMARY KEY,   -- порядок рядків у запиті
        ExecutorEmployeeId int            NOT NULL,
        TaskStatusId       int            NOT NULL,
        TaskText           nvarchar(1000) NOT NULL,
        DueDate            date           NOT NULL
    );
GO
