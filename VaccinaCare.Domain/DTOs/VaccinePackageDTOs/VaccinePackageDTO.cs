using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.DTOs.VaccineDTOs;

namespace VaccinaCare.Domain.DTOs.VaccinePackageDTOs;

public class VaccinePackageDTO
{
    public Guid Id { get; set; }
    public string PackageName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public List<VaccinePackageDetailDTO> VaccineDetails { get; set; } = new();
}

public class VaccinePackageDetailDTO
{
    public Guid VaccineId { get; set; }
    public int DoseOrder { get; set; }
}

public class CreateVaccinePackageDTO
{
    public string PackageName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public List<VaccinePackageDetailDTO> VaccineDetails { get; set; }
}

public class UpdateVaccinePackageDTO
{
    public string? PackageName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public List<VaccinePackageDetailDTO>? VaccineDetails { get; set; }
}