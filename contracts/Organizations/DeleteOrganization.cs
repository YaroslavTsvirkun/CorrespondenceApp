using MediatR;

namespace CorrespondenceSystem.Contracts.Organizations;

/// <summary>Слайс: видалення організації (dbo.usp_Organization_Delete перевіряє залежності).</summary>
public static class DeleteOrganization
{
    public record Command(int Id) : IRequest;
}
