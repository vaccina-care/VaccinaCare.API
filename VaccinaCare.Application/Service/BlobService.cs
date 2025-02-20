using Minio;
using Minio.DataModel.Args;
using System;
using System.IO;
using System.Threading.Tasks;
using Minio.Exceptions;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;

public class BlobService : IBlobService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName = "vaccinacare-bucket";
    private readonly ILoggerService _logger;

    public BlobService(ILoggerService logger)
    {
        _logger = logger;

        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ??
                       "minio.ae-tao-fullstack-api.site:9000";
        var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

        _logger.Info($"Connecting to MinIO at: {endpoint}");

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(false)
            .Build();
    }


    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        try
        {
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(beArgs);
            _logger.Info($"Bucket exists: {found}");

            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
                _logger.Info($"Bucket {_bucketName} created.");
            }

            string contentType = GetContentType(fileName);
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);
            _logger.Info($"File {fileName} uploaded successfully.");
        }
        catch (MinioException minioEx)
        {
            _logger.Error($"MinIO Error: {minioEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected Error: {ex.Message}");
            throw;
        }
    }


    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }

    public async Task<string> GetPreviewUrlAsync(string fileName)
    {
        var minioHost = Environment.GetEnvironmentVariable("MINIO_HOST") ?? "https://minio.ae-tao-fullstack-api.site";
        var bucketName = _bucketName; // Sử dụng bucket đã khai báo sẵn

        // Format link MinIO UI
        return $"{minioHost}/api/v1/buckets/{bucketName}/objects/download?preview=true&prefix={fileName}&version_id=null";
    }

    public async Task<string> GetFileUrlAsync(string fileName)
    {
        try
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(7 * 24 * 60 * 60); // URL expires in 7 days

            return await GetPreviewUrlAsync(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating file URL: {ex.Message}");
            return null;
        }
    }
}