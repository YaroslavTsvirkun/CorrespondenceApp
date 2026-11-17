using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> e)
    {
        e.ToTable("Employee");
        e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        e.Property(x => x.PositionName).HasMaxLength(100).IsRequired();
        e.Property(x => x.Email).HasMaxLength(100);
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.HasIndex(x => x.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        e.HasOne(x => x.Department).WithMany(d => d.Employees)
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Organization).WithMany()
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}
