using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceSystem.Contracts.Tasks;

/// <summary>Слайс: видалення доручення (dbo.usp_Task_Delete).</summary>
public static class DeleteTask
{
    public record Command(int Id) : IRequest;
}
