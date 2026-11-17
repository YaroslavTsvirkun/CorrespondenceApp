namespace CorrespondenceApi.Domain;

public class Permission
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;   // напр. "documents.read.all"
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<RolePermission> RolePermissions { get; set; } = [];
}
