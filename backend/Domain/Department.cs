namespace CorrespondenceApi.Domain;

public class Department
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public int? ParentDepartmentId { get; set; }
    public Department? Parent { get; set; }
    public List<Department> Children { get; set; } = [];
    public string Name { get; set; } = null!;
    public string? ShortName { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public List<Employee> Employees { get; set; } = [];
}
