using CorrespondenceApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CorrespondenceApi.Infrastructure.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> e)
    {
        e.ToTable("Document");
        e.Property(x => x.Title).HasMaxLength(300).IsRequired();
        e.Property(x => x.Summary).HasMaxLength(1000);
        e.Property(x => x.IsControlled).HasDefaultValue(false);

        e.HasOne(x => x.Direction).WithMany()
            .HasForeignKey(x => x.DirectionId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.DocumentType).WithMany()
            .HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.CurrentStatus).WithMany()
            .HasForeignKey(x => x.CurrentStatusId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.DeliveryChannel).WithMany()
            .HasForeignKey(x => x.DeliveryChannelId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(x => x.Correspondent).WithMany()
            .HasForeignKey(x => x.CorrespondentId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(x => x.SenderDepartment).WithMany()
            .HasForeignKey(x => x.SenderDepartmentId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.ReceiverDepartment).WithMany()
            .HasForeignKey(x => x.ReceiverDepartmentId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.AuthorEmployee).WithMany()
            .HasForeignKey(x => x.AuthorEmployeeId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Organization).WithMany()
            .HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);

        e.HasIndex(x => x.OrganizationId);
        e.HasIndex(x => x.DocumentDate);
        e.HasIndex(x => x.CurrentStatusId);
    }
}
