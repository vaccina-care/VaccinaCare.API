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
        _minioClient = new MinioClient()
            .WithEndpoint("103.211.201.162:9000")
            .WithCredentials("103.211.201.162", "Ccubin2003@")
            .Build();
    }

    public async Task UploadFileAsync(string fileName, Stream fileStream)
    {
        try
        {
            // Kiểm tra bucket có tồn tại không, nếu không thì tạo mới
            var beArgs = new BucketExistsArgs().WithBucket(_bucketName);
            bool found = await _minioClient.BucketExistsAsync(beArgs);
            if (!found)
            {
                var mbArgs = new MakeBucketArgs().WithBucket(_bucketName);
                await _minioClient.MakeBucketAsync(mbArgs);
            }

            // Upload file lên MinIO
            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithStreamData(fileStream)
                .WithObjectSize(fileStream.Length)
                .WithContentType("application/octet-stream");

            await _minioClient.PutObjectAsync(putObjectArgs);
            Console.WriteLine($"File {fileName} uploaded successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
        }
    }

    public async Task<string> GetFileUrlAsync(string fileName)
    {
        try
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(7 * 24 * 60 * 60); // URL có hạn sử dụng 7 ngày

            string url = await _minioClient.PresignedGetObjectAsync(args);
            return url;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating file URL: {ex.Message}");
            return null;
        }
    }
}
