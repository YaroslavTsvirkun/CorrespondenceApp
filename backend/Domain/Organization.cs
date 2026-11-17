namespace CorrespondenceApi.Domain;

public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public int? ParentOrganizationId { get; set; }
    public Organization? Parent { get; set; }
    public List<Organization> Children { get; set; } = [];
    public bool IsActive { get; set; } = true;

    // Патерн автогенерації реєстраційного номера документів організації.
    // Токени: {seq}/{seq:000} (лічильник за типом+роком), {year}, {yy}, {type}, {dir}.
    public string RegistrationNumberPattern { get; set; } = "{type}-{seq:000}/{year}";

    public List<Department> Departments { get; set; } = [];
    public List<AppUser> Users { get; set; } = [];
}
