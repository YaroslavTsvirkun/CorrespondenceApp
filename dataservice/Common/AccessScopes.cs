using CorrespondenceSystem.Contracts.Common;
namespace CorrespondenceSystem.DataService.Common;

/// <summary>
/// Зона видимості документів за пермішенами поточного користувача. Рівень передається у функцію БД
/// dbo.fn_CanReadDocument: 3 — уся кореспонденція, 2 — свого підрозділу, 1 — лише задіяні документи, 0 — немає доступу.
/// </summary>
public static class DocumentScope
{
    public static byte Level(ICurrentUser u)
    {
        if (u.Has(Permissions.DocumentsReadAll)) return 3;
        if (u.Has(Permissions.DocumentsReadDepartment) && u.DepartmentId is not null) return 2;
        if (u.Has(Permissions.DocumentsReadInvolved) && u.EmployeeId is not null) return 1;
        return 0;
    }
}

/// <summary>Організація поточного користувача для налаштувань рівня організації.</summary>
public static class OrganizationScope
{
    public static int RequireOrg(ICurrentUser user) =>
        user.OrganizationId ?? throw Errors.Validation("organization",
            "Супер-адміністратор не прив'язаний до організації — задайте патерн у картці організації");
}
