/* Представлення: доручення з документом, резолюцією, виконавцем і статусом.
   Використовується списком доручень, карткою документа та звітами.
   DaysOverdue обчислює скалярна функція dbo.fn_TaskDaysOverdue. */
CREATE OR ALTER VIEW dbo.vw_Task
AS
SELECT t.Id,
       t.DocumentId,
       d.OrganizationId,
       dbo.fn_DocumentDisplayNumber(t.DocumentId) AS DocumentNumber,
       d.Title             AS DocumentTitle,
       t.ResolutionId,
       res.ResolutionText,
       t.ExecutorEmployeeId,
       e.FullName          AS ExecutorName,
       t.TaskStatusId,
       s.Name              AS StatusName,
       s.IsFinal           AS StatusIsFinal,
       t.TaskText,
       t.AssignedAt,
       t.DueDate,
       t.CompletedAt,
       t.ResultText,
       dbo.fn_TaskDaysOverdue(t.DueDate, t.TaskStatusId) AS DaysOverdue
FROM dbo.ExecutionTask t
JOIN dbo.Document d     ON d.Id = t.DocumentId
JOIN dbo.Resolution res ON res.Id = t.ResolutionId
JOIN dbo.Employee e     ON e.Id = t.ExecutorEmployeeId
JOIN dbo.TaskStatus s   ON s.Id = t.TaskStatusId;
GO
