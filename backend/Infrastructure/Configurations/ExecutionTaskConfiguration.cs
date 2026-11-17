using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class ExecutionTaskConfiguration : IEntityTypeConfiguration<ExecutionTask>
{
    public void Configure(EntityTypeBuilder<ExecutionTask> e)
    {
        e.ToTable("ExecutionTask");
        e.Property(x => x.TaskText).HasMaxLength(1000).IsRequired();
        e.Property(x => x.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        e.HasOne(x => x.Document).WithMany(d => d.Tasks)
            .HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.Resolution).WithMany(r => r.Tasks)
            .HasForeignKey(x => x.ResolutionId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ExecutorEmployee).WithMany()
            .HasForeignKey(x => x.ExecutorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.TaskStatus).WithMany()
            .HasForeignKey(x => x.TaskStatusId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => x.ExecutorEmployeeId);
        e.HasIndex(x => x.DueDate);
    }
}
