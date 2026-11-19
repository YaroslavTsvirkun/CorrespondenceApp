using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CorrespondenceApi.Infrastructure.Storage;

/// <summary>
/// Сховище на S3-сумісному сервері (MinIO локально, будь-який S3 у продакшені).
/// Бакет створюється автоматично під час першого звернення.
/// </summary>
public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private volatile bool _bucketReady;

    public S3FileStorage(FileStorageOptions options)
    {
        _bucket = options.Bucket;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = options.Endpoint,
                ForcePathStyle = true, // MinIO: http://host:port/bucket/key
                AuthenticationRegion = "us-east-1",
                // MinIO не потребує додаткових контрольних сум, які SDK v4 вмикає за замовчуванням
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });
    }

    public async Task PutAsync(string key, Stream content, long length, string contentType, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);
        var request = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };
        request.Headers.ContentLength = length;
        await _client.PutObjectAsync(request, ct);
    }

    public async Task<StoredObject> OpenReadAsync(string key, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);
        try
        {
            var response = await _client.GetObjectAsync(_bucket, key, ct);
            return new StoredObject(response.ResponseStream, response.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException($"Об'єкт «{key}» відсутній у сховищі", key, ex);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        await EnsureBucketAsync(ct);
        await _client.DeleteObjectAsync(_bucket, key, ct);
    }

    public async Task PingAsync(CancellationToken ct)
    {
        _bucketReady = false; // перевіряємо реальний стан, а не кеш
        await EnsureBucketAsync(ct);
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        if (_bucketReady) return;
        await _bucketLock.WaitAsync(ct);
        try
        {
            if (_bucketReady) return;
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(_client, _bucket))
                await _client.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, ct);
            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _bucketLock.Dispose();
    }
}

/// <summary>Health check сховища: бакет доступний (або успішно створений).</summary>
public sealed class FileStorageHealthCheck(IFileStorage storage) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await storage.PingAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Файлове сховище недоступне", ex);
        }
    }
}
