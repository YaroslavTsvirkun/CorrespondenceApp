using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class CorrespondentConfiguration : IEntityTypeConfiguration<Correspondent>
{
    public void Configure(EntityTypeBuilder<Correspondent> e)
    {
        e.ToTable("Correspondent");
        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.HasOne(x => x.CorrespondentType).WithMany(t => t.Correspondents)
            .HasForeignKey(x => x.CorrespondentTypeId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Organization).WithMany()
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}
