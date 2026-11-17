using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class DeliveryChannelConfiguration : IEntityTypeConfiguration<DeliveryChannel>
{
    public void Configure(EntityTypeBuilder<DeliveryChannel> e)
    {
        e.ToTable("DeliveryChannel");
        e.Property(x => x.Name).HasMaxLength(80).IsRequired();
        e.HasIndex(x => x.Name).IsUnique();
    }
}
