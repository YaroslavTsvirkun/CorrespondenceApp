using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Parties.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Parties;

/// <summary>Слайс: реєстр контрагентів організації (пошук за назвою або кодом).</summary>
public static class GetParties
{
    [RequirePermission]
    public record Query(string? Search = null, bool IncludeInactive = true) : IRequest<IReadOnlyList<PartyListItem>>;
}

/// <summary>Слайс: картка контрагента.</summary>
public static class GetParty
{
    [RequirePermission]
    public record Query(int Id) : IRequest<PartyDetail>;
}

/// <summary>Слайс: видалення контрагента (заборонено, якщо він є в документах або має філії).</summary>
public static class DeleteParty
{
    [RequirePermission(Permissions.PartiesManage)]
    public record Command(int Id) : IRequest;
}
