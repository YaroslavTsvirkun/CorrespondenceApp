using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class TaskStateConfiguration : IEntityTypeConfiguration<TaskState>
{
    public void Configure(EntityTypeBuilder<TaskState> e)
    {
        e.ToTable("TaskStatus");
        e.Property(x => x.Name).HasMaxLength(50).IsRequired();
        e.HasIndex(x => x.Name).IsUnique();
    }
}
