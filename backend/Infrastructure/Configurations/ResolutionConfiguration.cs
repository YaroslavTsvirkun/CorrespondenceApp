using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class ResolutionConfiguration : IEntityTypeConfiguration<Resolution>
{
    public void Configure(EntityTypeBuilder<Resolution> e)
    {
        e.ToTable("Resolution");
        e.Property(x => x.ResolutionText).HasMaxLength(1000).IsRequired();
        e.HasOne(x => x.Document).WithMany(d => d.Resolutions)
            .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.AuthorEmployee).WithMany()
            .HasForeignKey(x => x.AuthorEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
