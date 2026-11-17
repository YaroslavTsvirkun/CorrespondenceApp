using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> e)
    {
        e.ToTable("Department");
        e.Property(x => x.Name).HasMaxLength(150).IsRequired();
        e.Property(x => x.ShortName).HasMaxLength(50);
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
        e.HasOne(x => x.Organization).WithMany(o => o.Departments)
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Parent).WithMany(p => p.Children)
            .HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
