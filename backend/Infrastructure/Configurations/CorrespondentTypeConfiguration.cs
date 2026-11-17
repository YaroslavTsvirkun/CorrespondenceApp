using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class CorrespondentTypeConfiguration : IEntityTypeConfiguration<CorrespondentType>
{
    public void Configure(EntityTypeBuilder<CorrespondentType> e)
    {
        e.ToTable("CorrespondentType");
        e.Property(x => x.Name).HasMaxLength(100).IsRequired();
        e.HasIndex(x => x.Name).IsUnique();
    }
}
