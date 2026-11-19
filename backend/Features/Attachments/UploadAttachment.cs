using System.Security.Cryptography;
using CorrespondenceApi.Common;
using CorrespondenceApi.Infrastructure.Database;
using CorrespondenceApi.Infrastructure.Storage;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>
/// Слайс: завантаження вкладення. Спочатку файл кладеться у сховище під згенерованим ключем,
/// потім реєструється в БД (dbo.usp_Attachment_Create); якщо запис у БД не вдався — об'єкт видаляється.
/// </summary>
public static class UploadAttachment
{
    public record Command(int DocumentId, IFormFile File, string? Description) : IRequest<int>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(FileStorageOptions options)
        {
            RuleFor(x => x.File).NotNull().WithMessage("Оберіть файл");
            When(x => x.File is not null, () =>
            {
                RuleFor(x => x.File.Length).GreaterThan(0).WithMessage("Файл порожній")
                    .LessThanOrEqualTo(options.MaxFileSizeBytes)
                    .WithMessage($"Розмір файлу не може перевищувати {options.MaxFileSizeMb} МБ");
                RuleFor(x => AttachmentFiles.SafeFileName(x.File.FileName))
                    .NotEmpty().WithMessage("Некоректне ім'я файлу")
                    .MaximumLength(255).WithMessage("Ім'я файлу задовге (максимум 255 символів)")
                    .Must(name => options.AllowedExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
                    .WithMessage(x => $"Тип файлу «{Path.GetExtension(x.File.FileName)}» не дозволено")
                    .OverridePropertyName("File");
            });
            RuleFor(x => x.Description).MaximumLength(1000);
        }
    }

    public class Handler(Db db, ICurrentUser user, IFileStorage storage, ILogger<Handler> logger)
        : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            // Документ має існувати й бути доступним — перевіряємо до завантаження байтів у сховище
            var p = SqlScope.Documents(user);
            p.Add("DocumentId", c.DocumentId);
            var organizationId = await db.QuerySingleOrDefaultAsync<int?>(
                $"SELECT d.OrganizationId FROM dbo.Document d WHERE d.Id = @DocumentId AND {SqlScope.DocumentFilter("d")}",
                p, ct) ?? throw new NotFoundException($"Документ #{c.DocumentId} не знайдено");

            var fileName = AttachmentFiles.SafeFileName(c.File.FileName);
            var contentType = AttachmentFiles.ContentTypeFor(fileName);
            var now = DateTime.UtcNow;
            var key = $"{organizationId}/{now:yyyy}/{now:MM}/{Guid.NewGuid():N}";

            await using var stream = c.File.OpenReadStream();
            var sha256 = await SHA256.HashDataAsync(stream, ct);
            stream.Position = 0;
            await storage.PutAsync(key, stream, c.File.Length, contentType, ct);

            try
            {
                var tp = SqlScope.Tenant(user);
                tp.Add("DocumentId", c.DocumentId);
                tp.Add("FileName", fileName);
                tp.Add("ContentType", contentType);
                tp.Add("SizeBytes", c.File.Length);
                tp.Add("Sha256", sha256);
                tp.Add("StorageKey", key);
                tp.Add("Description", c.Description);
                tp.Add("AddedByEmployeeId", user.EmployeeId);
                return await db.ScalarProcAsync<int>("dbo.usp_Attachment_Create", tp, ct);
            }
            catch
            {
                // Компенсація: не залишаємо у сховищі файл без запису в БД
                try { await storage.DeleteAsync(key, CancellationToken.None); }
                catch (Exception ex) { logger.LogWarning(ex, "Не вдалося видалити осиротілий об'єкт {Key}", key); }
                throw;
            }
        }
    }
}
