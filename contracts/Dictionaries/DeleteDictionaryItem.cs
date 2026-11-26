using CorrespondenceSystem.Contracts.Common;
using MediatR;

namespace CorrespondenceSystem.Contracts.Dictionaries;

/// <summary>Слайс: видалення запису довідника (заборонено, якщо запис використовується).</summary>
public static class DeleteDictionaryItem
{
    [RequirePermission(Permissions.DictionariesManage)]
    public record Command(string Kind, int Id) : IRequest;
}
