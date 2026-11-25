using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Correspondents.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Correspondents;

/// <summary>Слайс: кореспонденти організації користувача.</summary>
public static class GetCorrespondents
{
    [RequirePermission]
    public record Query : IRequest<IReadOnlyList<CorrespondentDto>>;
}
