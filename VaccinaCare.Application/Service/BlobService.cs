using Minio;
using Minio.DataModel.Args;
using VaccinaCare.Application.Interface;

namespace VaccinaCare.Application.Service;

public class BlobService : IBlobService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName = "vaccinacare-bucket";

    public BlobService()
    {
        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        try
        {
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(beArgs);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
            }

            string contentType = GetContentType(fileName);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(putObjectArgs);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
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

    public string GetPublicFileUrl(string fileName)
    {
        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT");
        return $"http://{endpoint}/{_bucketName}/{fileName}";
    }


    public async Task<string> GetFileUrlAsync(string fileName)
    {
        try
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(7 * 24 * 60 * 60); // URL expires in 7 days

            return GetPublicFileUrl(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating file URL: {ex.Message}");
            return null;
        }
    }
}