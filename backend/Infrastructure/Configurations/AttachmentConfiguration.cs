using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> e)
    {
        e.ToTable("Attachment");
        e.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        e.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        e.Property(x => x.AddedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.Document).WithMany(d => d.Attachments)
            .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.AddedByEmployee).WithMany()
            .HasForeignKey(x => x.AddedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
