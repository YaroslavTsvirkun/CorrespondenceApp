namespace CorrespondenceApi.Domain;

public class AppUser
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FullName { get; set; } = null!;
    // null = супер-адмін (не прив'язаний до жодної організації)
    public int? OrganizationId { get; set; }
    public Organization? Organization { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public bool IsActive { get; set; } = true;
    public List<UserRole> UserRoles { get; set; } = [];
}
