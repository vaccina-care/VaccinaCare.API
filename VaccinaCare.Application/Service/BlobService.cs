﻿using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;

namespace VaccinaCare.Application.Service;

public class BlobService : IBlobService
{
    private readonly string _bucketName = "vaccinacare-bucket";
    private readonly ILoggerService _logger;
    private readonly long _maxFileSize; // Kích thước tối đa cho phép (bytes)
    private readonly IMinioClient _minioClient;

    public BlobService(ILoggerService logger)
    {
        _logger = logger;

        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ??
                       "minio.ae-tao-fullstack-api.site:9000";
        var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

        // Lấy giới hạn kích thước từ config hoặc mặc định là 10MB
        var maxFileSizeStr = Environment.GetEnvironmentVariable("MAX_FILE_SIZE_MB");
        if (long.TryParse(maxFileSizeStr, out var configSize))
            _maxFileSize = configSize * 1024 * 1024; // Chuyển đổi MB sang bytes
        else
            _maxFileSize = 10 * 1024 * 1024; // Mặc định 10MB

        try
        {
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(false)
                .Build();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to initialize MinIO client: {ex.Message}");
            throw;
        }
    }

    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        _logger.Info($"Starting file upload: {fileName}");

        try
        {
            // Kiểm tra kích thước file
            if (fileStream.Length > _maxFileSize)
            {
                var maxFileSizeMB = _maxFileSize / (1024 * 1024);
                _logger.Error(
                    $"File '{fileName}' size ({fileStream.Length / (1024 * 1024)} MB) exceeds the maximum allowed size ({maxFileSizeMB} MB)");
                throw new FileTooLargeException(fileName, fileStream.Length, _maxFileSize);
            }

            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            var found = await _minioClient.BucketExistsAsync(beArgs);
            _logger.Info($"Checking if bucket '{_bucketName}' exists: {found}");

            if (!found)
            {
                _logger.Warn($"Bucket '{_bucketName}' not found. Creating a new one...");
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
                _logger.Success($"Bucket '{_bucketName}' created successfully.");
            }

            var contentType = GetContentType(fileName);
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            _logger.Info($"Uploading file: {fileName} with content type {contentType}");
            await _minioClient.PutObjectAsync(putObjectArgs);
            _logger.Success($"File '{fileName}' uploaded successfully.");
        }
        catch (MinioException minioEx)
        {
            _logger.Error($"MinIO Error during upload: {minioEx.Message}");
            throw;
        }
        catch (FileTooLargeException)
        {
            // Chỉ ghi log lỗi, ném ngoại lệ để xử lý ở tầng cao hơn
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during file upload: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetPreviewUrlAsync(string fileName)
    {
        var minioHost = Environment.GetEnvironmentVariable("MINIO_HOST") ?? "https://minio.ae-tao-fullstack-api.site";
        _logger.Info($"Generating preview URL for file: {fileName}");

        var previewUrl =
            $"{minioHost}/api/v1/buckets/{_bucketName}/objects/download?preview=true&prefix={fileName}&version_id=null";
        _logger.Info($"Preview URL generated: {previewUrl}");

        return previewUrl;
    }

    public async Task<string> GetFileUrlAsync(string fileName)
    {
        try
        {
            _logger.Info($"Generating presigned URL for file: {fileName}");
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(7 * 24 * 60 * 60); // URL expires in 7 days

            var fileUrl = await GetPreviewUrlAsync(fileName);
            _logger.Success($"Presigned file URL generated: {fileUrl}");
            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error generating file URL: {ex.Message}");
            return null;
        }
    }

    private string GetContentType(string fileName)
    {
        _logger.Info($"Determining content type for file: {fileName}");
        var extension = Path.GetExtension(fileName)?.ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"
        };
    }
}

// Tạo exception riêng để xử lý lỗi file quá lớn
public class FileTooLargeException : Exception
{
    public FileTooLargeException(string fileName, long fileSize, long maxAllowedSize)
        : base(
            $"File '{fileName}' size ({fileSize / (1024 * 1024)} MB) exceeds the maximum allowed size ({maxAllowedSize / (1024 * 1024)} MB)")
    {
        FileName = fileName;
        FileSize = fileSize;
        MaxAllowedSize = maxAllowedSize;
    }

    public string FileName { get; }
    public long FileSize { get; }
    public long MaxAllowedSize { get; }
}