using CorrespondenceApi.Common;
using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CorrespondenceApi.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser)
    : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<CorrespondentType> CorrespondentTypes => Set<CorrespondentType>();
    public DbSet<Correspondent> Correspondents => Set<Correspondent>();
    public DbSet<DocumentDirection> Directions => Set<DocumentDirection>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<DocumentStatus> DocumentStatuses => Set<DocumentStatus>();
    public DbSet<DeliveryChannel> Channels => Set<DeliveryChannel>();
    public DbSet<TaskState> TaskStatuses => Set<TaskState>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<RegistrationRecord> Registrations => Set<RegistrationRecord>();
    public DbSet<Resolution> Resolutions => Set<Resolution>();
    public DbSet<ExecutionTask> Tasks => Set<ExecutionTask>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<StatusHistory> StatusHistory => Set<StatusHistory>();

    // Авторизація
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Тенант-ізоляція: супер-адмін (право organizations.manage) бачить усе;
    // решта обмежені своєю організацією. Властивості читаються EF пер-запит.
    public bool TenantBypass => currentUser.Has(CorrespondenceApi.Common.Permissions.OrganizationsManage);
    public int? TenantOrganizationId => currentUser.OrganizationId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Глобальні фільтри тенант-ізоляції за організацією поточного користувача
        modelBuilder.Entity<Department>().HasQueryFilter(x => TenantBypass || x.OrganizationId == TenantOrganizationId);
        modelBuilder.Entity<Employee>().HasQueryFilter(x => TenantBypass || x.OrganizationId == TenantOrganizationId);
        modelBuilder.Entity<Correspondent>().HasQueryFilter(x => TenantBypass || x.OrganizationId == TenantOrganizationId);
        modelBuilder.Entity<Document>().HasQueryFilter(x => TenantBypass || x.OrganizationId == TenantOrganizationId);
    }
}
