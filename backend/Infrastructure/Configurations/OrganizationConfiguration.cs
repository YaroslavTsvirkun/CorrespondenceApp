using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> e)
    {
        e.ToTable("Organization");
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.Property(x => x.Code).HasMaxLength(30);
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.Property(x => x.RegistrationNumberPattern).HasMaxLength(100)
            .HasDefaultValue("{type}-{seq:000}/{year}");
        e.HasIndex(x => x.Name).IsUnique();
        e.HasOne(x => x.Parent).WithMany(p => p.Children)
            .HasForeignKey(x => x.ParentOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
