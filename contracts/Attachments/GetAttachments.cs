using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Attachments.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Attachments;

/// <summary>Слайс: вкладення документа (з урахуванням тенанта й зони видимості документа).</summary>
public static class GetAttachments
{
    [RequirePermission(Permissions.DocumentsReadAny)]
    public record Query(int DocumentId) : IRequest<IReadOnlyList<AttachmentDto>>;
}
