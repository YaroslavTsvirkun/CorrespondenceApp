using CorrespondenceSystem.Contracts.Organizations.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Organizations;

/// <summary>Слайс: усі організації з лічильниками підрозділів і користувачів.</summary>
public static class GetOrganizations
{
    public record Query : IRequest<IReadOnlyList<OrganizationDto>>;
}
