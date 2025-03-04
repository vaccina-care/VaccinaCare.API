using Microsoft.AspNetCore.Http;

namespace VaccinaCare.Domain.DTOs.VaccineDTOs.Request;

public class CreateVaccineRequest
{
    public CreateVaccineDto VaccineData { get; set; }
    public IFormFile? VaccinePictureFile { get; set; }
}