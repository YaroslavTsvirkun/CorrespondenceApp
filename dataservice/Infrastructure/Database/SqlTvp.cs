using System.Data;
using Dapper;
namespace CorrespondenceSystem.DataService.Infrastructure.Database;

/// <summary>
/// Табличні параметри (User-Defined Table Types з Database/Types) для передачі наборів у процедури.
/// </summary>
public static class SqlTvp
{
    /// <summary>dbo.IdList — унікальні Id.</summary>
    public static SqlMapper.ICustomQueryParameter IdList(IEnumerable<int> ids)
    {
        var t = new DataTable();
        t.Columns.Add("Id", typeof(int));
        foreach (var id in ids.Distinct()) t.Rows.Add(id);
        return t.AsTableValuedParameter("dbo.IdList");
    }

    /// <summary>dbo.CodeList — унікальні непорожні коди.</summary>
    public static SqlMapper.ICustomQueryParameter CodeList(IEnumerable<string> codes)
    {
        var t = new DataTable();
        t.Columns.Add("Code", typeof(string));
        foreach (var code in codes.Where(c => !string.IsNullOrWhiteSpace(c))
                     .Select(c => c.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            t.Rows.Add(code);
        return t.AsTableValuedParameter("dbo.CodeList");
    }

    /// <summary>dbo.TaskAssignmentList — набір доручень (рядки нумеруються за порядком).</summary>
    public static SqlMapper.ICustomQueryParameter TaskAssignments(
        IEnumerable<(int ExecutorEmployeeId, int TaskStatusId, string TaskText, DateOnly DueDate)> items)
    {
        var t = new DataTable();
        t.Columns.Add("RowNo", typeof(int));
        t.Columns.Add("ExecutorEmployeeId", typeof(int));
        t.Columns.Add("TaskStatusId", typeof(int));
        t.Columns.Add("TaskText", typeof(string));
        t.Columns.Add("DueDate", typeof(DateTime));
        var row = 0;
        foreach (var i in items)
            t.Rows.Add(++row, i.ExecutorEmployeeId, i.TaskStatusId, i.TaskText, i.DueDate.ToDateTime(TimeOnly.MinValue));
        return t.AsTableValuedParameter("dbo.TaskAssignmentList");
    }
}
