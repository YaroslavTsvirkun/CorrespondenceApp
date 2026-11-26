using System.Security.Cryptography;
using CorrespondenceApi.Infrastructure.Storage;
using CorrespondenceSystem.Contracts.Attachments;
using FluentValidation;
using MediatR;

namespace CorrespondenceApi.Features.Attachments;

/// <summary>
/// Слайс: завантаження вкладення. Сервіс даних перевіряє доступ до документа, файл кладеться у сховище
/// під згенерованим ключем, потім реєструється в БД (dbo.usp_Attachment_Create);
/// якщо реєстрація не вдалася — об'єкт видаляється.
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

    public class Handler(ISender sender, IFileStorage storage, ILogger<Handler> logger)
        : IRequestHandler<Command, int>
    {
        public async Task<int> Handle(Command c, CancellationToken ct)
        {
            // Документ має існувати й бути доступним — перевіряємо до завантаження байтів у сховище
            var organizationId = await sender.Send(new PrepareAttachmentUpload.Query(c.DocumentId), ct);

            var fileName = AttachmentFiles.SafeFileName(c.File.FileName);
            var contentType = AttachmentFiles.ContentTypeFor(fileName);
            var key = AttachmentFiles.NewStorageKey(organizationId);

            await using var stream = c.File.OpenReadStream();
            var sha256 = await SHA256.HashDataAsync(stream, ct);
            stream.Position = 0;
            await storage.PutAsync(key, stream, c.File.Length, contentType, ct);

            try
            {
                return await sender.Send(new RegisterAttachment.Command(
                    c.DocumentId, fileName, contentType, c.File.Length, sha256, key, c.Description), ct);
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
