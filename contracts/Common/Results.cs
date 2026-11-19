namespace CorrespondenceSystem.Contracts.Common;

/// <summary>Сторінковий результат вибірки.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

/// <summary>Кидається слайсом, коли сутність не знайдено -> 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Невдала автентифікація -> 401.</summary>
public class UnauthorizedException(string message) : Exception(message);
