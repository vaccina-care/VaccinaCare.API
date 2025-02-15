namespace VaccinaCare.Application.Interface;

public interface IBlobService
{
    Task UploadFileAsync(string fileName, Stream fileStream);
    Task<string> GetFileUrlAsync(string fileName);
}