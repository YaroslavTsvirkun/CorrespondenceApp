using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.DataService.Common;
using Dapper;
namespace CorrespondenceSystem.DataService.Infrastructure.Database;

/// <summary>
/// Параметри тенант-ізоляції та зони видимості документів для SQL-запитів.
/// Імена властивостей збігаються з параметрами процедур (@Bypass, @TenantOrganizationId, …).
/// </summary>
public static class SqlScope
{
    /// <summary>Тенант: супер-адмін (organizations.manage) бачить усе, решта — лише свою організацію.</summary>
    public static DynamicParameters Tenant(ICurrentUser u)
    {
        var p = new DynamicParameters();
        p.Add("Bypass", u.Has(Permissions.OrganizationsManage));
        p.Add("TenantOrganizationId", u.OrganizationId);
        return p;
    }

    /// <summary>Тенант + зона видимості документів (для dbo.fn_CanReadDocument).</summary>
    public static DynamicParameters Documents(ICurrentUser u)
    {
        var p = Tenant(u);
        p.Add("ScopeLevel", DocumentScope.Level(u));
        p.Add("EmployeeId", u.EmployeeId);
        p.Add("DepartmentId", u.DepartmentId);
        return p;
    }

    /// <summary>Умова тенант-фільтра для таблиці з колонкою OrganizationId.</summary>
    public static string TenantFilter(string alias) =>
        $"(@Bypass = 1 OR {alias}.OrganizationId = @TenantOrganizationId)";

    /// <summary>
    /// Таблична функція «документи, видимі користувачу» (тенант + зона видимості) для JOIN у списках:
    /// <c>FROM dbo.vw_Document d JOIN {VisibleDocuments} v ON v.Id = d.Id</c>.
    /// Параметри — з <see cref="Documents"/>.
    /// </summary>
    public const string VisibleDocuments =
        "dbo.fn_VisibleDocuments(@ScopeLevel, @EmployeeId, @DepartmentId, @Bypass, @TenantOrganizationId)";

    /// <summary>
    /// Умова «конкретний документ видимий користувачу» (тенант + скалярна функція
    /// dbo.fn_CanReadDocument) — для вибірки одного документа за Id.
    /// </summary>
    public static string DocumentFilter(string alias) =>
        $"{TenantFilter(alias)} AND dbo.fn_CanReadDocument({alias}.Id, @ScopeLevel, @EmployeeId, @DepartmentId) = 1";
}
