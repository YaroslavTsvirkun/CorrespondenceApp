using CorrespondenceSystem.Contracts.Common;
using CorrespondenceSystem.Contracts.Tasks.Dtos;
using MediatR;

namespace CorrespondenceSystem.Contracts.Tasks;

/// <summary>Слайс: доручення за документами, видимими користувачу (vw_Task + fn_VisibleDocuments).</summary>
public static class GetTasks
{
    public record Query : IRequest<IReadOnlyList<TaskDto>>;
}
