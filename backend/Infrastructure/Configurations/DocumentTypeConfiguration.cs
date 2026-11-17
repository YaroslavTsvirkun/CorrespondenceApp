using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> e)
    {
        e.ToTable("DocumentType");
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.Property(x => x.Code).HasMaxLength(20);
        e.HasIndex(x => x.Name).IsUnique();
    }
}
