using Microsoft.AspNetCore.Http;

namespace VaccinaCare.Domain.DTOs.VaccineDTOs.Request;

public class UpdateVaccineRequest
{
    public UpdateVaccineDto VaccineData { get; set; }
    public IFormFile? VaccinePictureFile { get; set; }
}
