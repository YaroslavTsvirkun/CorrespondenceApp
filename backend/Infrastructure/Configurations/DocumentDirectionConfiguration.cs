using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class DocumentDirectionConfiguration : IEntityTypeConfiguration<DocumentDirection>
{
    public void Configure(EntityTypeBuilder<DocumentDirection> e)
    {
        e.ToTable("DocumentDirection");
        e.Property(x => x.Code).HasMaxLength(10).IsRequired();
        e.Property(x => x.Name).HasMaxLength(50).IsRequired();
        e.HasIndex(x => x.Code).IsUnique();
        e.HasIndex(x => x.Name).IsUnique();
    }
}
