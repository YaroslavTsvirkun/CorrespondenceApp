using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceSystem.Contracts.Correspondents;

/// <summary>Слайс: видалення кореспондента (у документах посилання обнуляться — FK ON DELETE SET NULL).</summary>
public static class DeleteCorrespondent
{
    [RequirePermission(Permissions.CorrespondentsManage)]
    public record Command(int Id) : IRequest;
}
