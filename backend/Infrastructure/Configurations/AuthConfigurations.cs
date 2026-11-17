using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> e)
    {
        e.ToTable("AppUser");
        e.Property(x => x.UserName).HasMaxLength(50).IsRequired();
        e.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
        e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.HasIndex(x => x.UserName).IsUnique();
        e.HasOne(x => x.Employee).WithMany()
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(x => x.Organization).WithMany(o => o.Users)
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> e)
    {
        e.ToTable("Role");
        e.Property(x => x.Name).HasMaxLength(50).IsRequired();
        e.HasIndex(x => x.Name).IsUnique();
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> e)
    {
        e.ToTable("Permission");
        e.Property(x => x.Code).HasMaxLength(80).IsRequired();
        e.Property(x => x.Category).HasMaxLength(50);
        e.HasIndex(x => x.Code).IsUnique();
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> e)
    {
        e.ToTable("UserRole");
        e.HasKey(x => new { x.UserId, x.RoleId });
        e.HasOne(x => x.User).WithMany(u => u.UserRoles)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Role).WithMany(r => r.UserRoles)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> e)
    {
        e.ToTable("RolePermission");
        e.HasKey(x => new { x.RoleId, x.PermissionId });
        e.HasOne(x => x.Role).WithMany(r => r.RolePermissions)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Permission).WithMany(p => p.RolePermissions)
            .HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
