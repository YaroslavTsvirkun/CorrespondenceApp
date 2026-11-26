using CorrespondenceSystem.DataService.Auth;
namespace CorrespondenceSystem.DataService.Common;

/// <summary>Користувач, від імені якого backend викликає операцію Data API.</summary>
public interface ICurrentUser
{
    int? UserId { get; }
    int? EmployeeId { get; }
    int? DepartmentId { get; }
    int? OrganizationId { get; }
    bool IsAuthenticated { get; }
    bool Has(string permission);
}

/// <summary>
/// Поточний користувач запиту. Заповнюється фільтром Data API із заголовка <c>X-User-Id</c>:
/// актуальні дані доступу (працівник, підрозділ, організація, пермішени) щоразу читаються з БД,
/// тож зміни прав діють одразу. Неактивний або невідомий користувач — анонімний.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private HashSet<string> _permissions = new(StringComparer.Ordinal);

    internal UserAccess? Access { get; private set; }

    public int? UserId => Access?.User.Id;
    public int? EmployeeId => Access?.User.EmployeeId;
    public int? DepartmentId => Access?.User.DepartmentId;
    public int? OrganizationId => Access?.User.OrganizationId;
    public bool IsAuthenticated => Access is not null;
    public bool Has(string permission) => _permissions.Contains(permission);

    internal void Set(UserAccess access)
    {
        Access = access;
        _permissions = new HashSet<string>(access.Permissions, StringComparer.Ordinal);
    }
}
