using CorrespondenceSystem.Contracts.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace CorrespondenceSystem.Contracts.Dictionaries;

/// <summary>Слайс: видалення запису довідника (заборонено, якщо запис використовується).</summary>
public static class DeleteDictionaryItem
{
    public record Command(string Kind, int Id) : IRequest;
}
