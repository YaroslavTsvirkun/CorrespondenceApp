using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class StatusHistoryConfiguration : IEntityTypeConfiguration<StatusHistory>
{
    public void Configure(EntityTypeBuilder<StatusHistory> e)
    {
        e.ToTable("StatusHistory");
        e.Property(x => x.ChangedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.Document).WithMany(d => d.StatusHistory)
            .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.PreviousStatus).WithMany()
            .HasForeignKey(x => x.PreviousStatusId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.NewStatus).WithMany()
            .HasForeignKey(x => x.NewStatusId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ChangedByEmployee).WithMany()
            .HasForeignKey(x => x.ChangedByEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
