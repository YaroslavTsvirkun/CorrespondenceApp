using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class RegistrationRecordConfiguration : IEntityTypeConfiguration<RegistrationRecord>
{
    public void Configure(EntityTypeBuilder<RegistrationRecord> e)
    {
        e.ToTable("RegistrationRecord");
        e.Property(x => x.RegistrationNumber).HasMaxLength(30).IsRequired();
        e.Property(x => x.RegisteredAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.Document).WithOne(d => d.Registration)
            .HasForeignKey<RegistrationRecord>(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.RegistrarEmployee).WithMany()
            .HasForeignKey(x => x.RegistrarEmployeeId).OnDelete(DeleteBehavior.Restrict);
        // Унікальність реєстраційного номера в межах організації та року
        e.HasIndex(x => new { x.OrganizationId, x.RegistrationYear, x.RegistrationNumber }).IsUnique();
    }
}
