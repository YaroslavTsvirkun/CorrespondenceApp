using System.Data;
using CorrespondenceSystem.Contracts.Parties.Dtos;
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

    /// <summary>dbo.PartyAddressList — адреси контрагента (порожні рядки → NULL).</summary>
    public static SqlMapper.ICustomQueryParameter PartyAddresses(IEnumerable<PartyAddressItem> items)
    {
        var t = Table("RowNo", "Kind", "Country", "PostalCode", "Region", "City", "Line");
        t.Columns["Kind"]!.DataType = typeof(byte);
        var row = 0;
        foreach (var a in items)
            t.Rows.Add(++row, a.Kind, Db(a.Country), Db(a.PostalCode), Db(a.Region), Db(a.City), Db(a.Line));
        return t.AsTableValuedParameter("dbo.PartyAddressList");
    }

    /// <summary>dbo.PartyContactList — контакти контрагента.</summary>
    public static SqlMapper.ICustomQueryParameter PartyContacts(IEnumerable<PartyContactItem> items)
    {
        var t = Table("RowNo", "Kind", "Value", "IsPrimary", "Note");
        t.Columns["Kind"]!.DataType = typeof(byte);
        t.Columns["IsPrimary"]!.DataType = typeof(bool);
        var row = 0;
        foreach (var c in items)
            t.Rows.Add(++row, c.Kind, c.Value.Trim(), c.IsPrimary, Db(c.Note));
        return t.AsTableValuedParameter("dbo.PartyContactList");
    }

    /// <summary>dbo.PartyPersonList — контактні особи контрагента.</summary>
    public static SqlMapper.ICustomQueryParameter PartyPersons(IEnumerable<PartyPersonItem> items)
    {
        var t = Table("RowNo", "FullName", "PositionName", "Phone", "Email", "IsSignatory", "Note");
        t.Columns["IsSignatory"]!.DataType = typeof(bool);
        var row = 0;
        foreach (var p in items)
            t.Rows.Add(++row, p.FullName.Trim(), Db(p.PositionName), Db(p.Phone), Db(p.Email), p.IsSignatory, Db(p.Note));
        return t.AsTableValuedParameter("dbo.PartyPersonList");
    }

    /// <summary>Таблиця з колонкою RowNo (int) і текстовими колонками; типи інших колонок задає виклик.</summary>
    private static DataTable Table(string rowNo, params string[] columns)
    {
        var t = new DataTable();
        t.Columns.Add(rowNo, typeof(int));
        foreach (var c in columns) t.Columns.Add(c, typeof(string));
        return t;
    }

    private static object Db(string? s) => string.IsNullOrWhiteSpace(s) ? DBNull.Value : s.Trim();

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
