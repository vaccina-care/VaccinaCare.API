using System.Data;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccineService : IVaccineService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _logger;


    public VaccineService(IUnitOfWork unitOfWork, ILoggerService logger, IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimsService = claimsService;
    }

    /// <summary>
    /// Kiểm tra xem hai vaccine có thể tiêm cùng nhau không.
    /// </summary>
    /// <param name="vaccine1Id"></param>
    /// <param name="vaccine2Id"></param>
    /// <returns></returns>
    public async Task<bool> CanBeAdministeredTogether(Guid vaccine1Id, Guid vaccine2Id)
    {
        try
        {
            var rule = await _unitOfWork.VaccineIntervalRulesRepository
                .FirstOrDefaultAsync(r => (r.VaccineId == vaccine1Id && r.RelatedVaccineId == vaccine2Id) ||
                                          (r.VaccineId == vaccine2Id && r.RelatedVaccineId == vaccine1Id));

            return rule?.CanBeGivenTogether ?? false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Error in CanBeAdministeredTogether: {ex.Message}");
            throw;
        }

    }

    /// <summary>
    /// Lấy khoảng cách tối thiểu (tính theo ngày) giữa hai vaccine.
    /// </summary>
    /// <param name="vaccine1Id"></param>
    /// <param name="vaccine2Id"></param>
    /// <returns></returns>
    public async Task<int> GetMinIntervalDays(Guid vaccine1Id, Guid vaccine2Id)
    {
        try
        {
            var rule = await _unitOfWork.VaccineIntervalRulesRepository
           .FirstOrDefaultAsync(r => (r.VaccineId == vaccine1Id && r.RelatedVaccineId == vaccine2Id) ||
                                (r.VaccineId == vaccine2Id && r.RelatedVaccineId == vaccine1Id));

            return rule?.MinIntervalDays ?? 0;
        }
        catch (Exception ex)
        {

            _logger.Error($"Error in CanBeAdministeredTogether: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Lấy giá của Vaccine dựa trên ID
    /// </summary>
    /// <param name="vaccineId"></param>
    /// <returns></returns>
    public async Task<decimal> GetVaccinePrice(Guid vaccineId)
    {
        try
        {
            var vaccine = await _unitOfWork.VaccineRepository
                .FirstOrDefaultAsync(v => v.Id == vaccineId);

            if (vaccine == null)
            {
                _logger.Warn($"Vaccine with ID {vaccineId} not found.");
                return 0; // Nếu không tìm thấy vaccine, trả về 0 để tránh lỗi.
            }

            return vaccine.Price ?? 0; // Nếu giá trị `Price` là null, trả về 0.
        }
        catch (Exception ex)
        {
            _logger.Error($"Error retrieving price for vaccine {vaccineId}: {ex.Message}");
            throw;
        }
    }


    public async Task<VaccineDTO> UpdateVaccine(Guid id, VaccineDTO vaccineDTO)
    {
        _logger.Info($"Starting the update process for vaccine with ID: {id}");

        if (vaccineDTO == null)
        {
            _logger.Warn("Update failed: VaccineDTO is null.");
            throw new ArgumentNullException(nameof(vaccineDTO));
        }

        try
        {
            _logger.Info($"Fetching vaccine details for ID: {id}");
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(id);

            if (vaccine == null)
            {
                _logger.Warn($"Vaccine with ID: {id} not found in the database.");
                throw new KeyNotFoundException($"Vaccine with ID {id} not found.");
            }

            _logger.Info(
                $"Vaccine found. Current details: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}, BloodType = {vaccine.ForBloodType}, AvoidChronic = {vaccine.AvoidChronic}, AvoidAllergy = {vaccine.AvoidAllergy}, HasDrugInteraction = {vaccine.HasDrugInteraction}, HasSpecialWarning = {vaccine.HasSpecialWarning}"
            );

            // Updating only non-null fields
            vaccine.VaccineName = !string.IsNullOrWhiteSpace(vaccineDTO.VaccineName) ? vaccineDTO.VaccineName : vaccine.VaccineName;
            vaccine.Description = !string.IsNullOrWhiteSpace(vaccineDTO.Description) ? vaccineDTO.Description : vaccine.Description;
            vaccine.PicUrl = !string.IsNullOrWhiteSpace(vaccineDTO.PicUrl) ? vaccineDTO.PicUrl : vaccine.PicUrl;
            vaccine.Type = !string.IsNullOrWhiteSpace(vaccineDTO.Type) ? vaccineDTO.Type : vaccine.Type;
            vaccine.Price = vaccineDTO.Price >= 0 ? vaccineDTO.Price : vaccine.Price;
            vaccine.ForBloodType = vaccineDTO.ForBloodType ?? vaccine.ForBloodType;
            vaccine.AvoidChronic = vaccineDTO.AvoidChronic ?? vaccine.AvoidChronic;
            vaccine.AvoidAllergy = vaccineDTO.AvoidAllergy ?? vaccine.AvoidAllergy;
            vaccine.HasDrugInteraction = vaccineDTO.HasDrugInteraction ?? vaccine.HasDrugInteraction;
            vaccine.HasSpecialWarning = vaccineDTO.HasSpecialWarning ?? vaccine.HasSpecialWarning;

            _logger.Info(
                $"Updating vaccine to: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}, BloodType = {vaccine.ForBloodType}, AvoidChronic = {vaccine.AvoidChronic}, AvoidAllergy = {vaccine.AvoidAllergy}, HasDrugInteraction = {vaccine.HasDrugInteraction}, HasSpecialWarning = {vaccine.HasSpecialWarning}"
            );

            await _unitOfWork.VaccineRepository.Update(vaccine);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"Vaccine with ID {id} updated successfully.");

            var updatedVaccineDTO = new VaccineDTO
            {
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price,
                ForBloodType = vaccine.ForBloodType,
                AvoidChronic = vaccine.AvoidChronic,
                AvoidAllergy = vaccine.AvoidAllergy,
                HasDrugInteraction = vaccine.HasDrugInteraction,
                HasSpecialWarning = vaccine.HasSpecialWarning
            };

            return updatedVaccineDTO;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn($"Update failed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"500 - Error during vaccine update for ID {id}: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccineDTO> CreateVaccine(VaccineDTO vaccineDTO)
    {
        _logger.Info("Starting to create a new vaccine.");

        try
        {
            _logger.Info(
                $"Received VaccineDTO with VaccineName: {vaccineDTO.VaccineName}, Type: {vaccineDTO.Type}, Price: {vaccineDTO.Price}, BloodType: {vaccineDTO.ForBloodType}, AvoidChronic: {vaccineDTO.AvoidChronic}, AvoidAllergy: {vaccineDTO.AvoidAllergy}, HasDrugInteraction: {vaccineDTO.HasDrugInteraction}, HasSpecialWarning: {vaccineDTO.HasSpecialWarning}"
            );

            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(vaccineDTO.VaccineName))
                validationErrors.Add("Vaccine name is required.");

            if (vaccineDTO.Price <= 0)
                validationErrors.Add("Price must be greater than zero.");

            if (string.IsNullOrWhiteSpace(vaccineDTO.Description))
                validationErrors.Add("Description is required.");

            if (string.IsNullOrWhiteSpace(vaccineDTO.Type))
                validationErrors.Add("Type is required.");

            if (string.IsNullOrWhiteSpace(vaccineDTO.PicUrl))
                validationErrors.Add("PicUrl is required.");

            if (validationErrors.Any())
            {
                _logger.Warn($"Validation failed for VaccineDTO: {string.Join("; ", validationErrors)}");
                return null;
            }

            var vaccine = new Vaccine
            {
                VaccineName = vaccineDTO.VaccineName,
                Description = vaccineDTO.Description,
                PicUrl = vaccineDTO.PicUrl,
                Type = vaccineDTO.Type,
                Price = vaccineDTO.Price,
                ForBloodType = vaccineDTO.ForBloodType,
                AvoidChronic = vaccineDTO.AvoidChronic,
                AvoidAllergy = vaccineDTO.AvoidAllergy,
                HasDrugInteraction = vaccineDTO.HasDrugInteraction,
                HasSpecialWarning = vaccineDTO.HasSpecialWarning
            };

            _logger.Info($"Vaccine object created. Ready to save: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}");

            await _unitOfWork.VaccineRepository.AddAsync(vaccine);
            await _unitOfWork.SaveChangesAsync();

            var createdVaccineDTO = new VaccineDTO
            {
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price,
                ForBloodType = vaccine.ForBloodType,
                AvoidChronic = vaccine.AvoidChronic,
                AvoidAllergy = vaccine.AvoidAllergy,
                HasDrugInteraction = vaccine.HasDrugInteraction,
                HasSpecialWarning = vaccine.HasSpecialWarning
            };

            _logger.Success($"Vaccine '{createdVaccineDTO.VaccineName}' created successfully with ID {vaccine.Id}.");
            return createdVaccineDTO;
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while creating the vaccine. Error: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccineDTO> DeleteVaccine(Guid id)
    {
        _logger.Info($"Initiating vaccine deleted process for ID: {id}");

        try
        {
            _logger.Info($"Fetching vaccine details for ID: {id}");
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(id, v => v.VaccinePackageDetails);

            if (vaccine == null || vaccine.IsDeleted)
            {
                _logger.Warn($"Vaccine with ID: {id} not found or already deleted.");
                throw new KeyNotFoundException($"Vaccine with ID {id} not found or already deleted.");
            }

            _logger.Info(
                $"Vaccine found. Preparing to delete: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}");

            _logger.Info($"Fetching VaccinePackageDetails that contain Vaccine ID: {id}");
            var vaccinePackageDetails = await _unitOfWork.VaccinePackageDetailRepository
                .GetAllAsync(vpd => vpd.VaccineId == id);

            if (vaccinePackageDetails.Any())
            {
                _logger.Info($"Found {vaccinePackageDetails.Count} VaccinePackageDetails associated with Vaccine ID: {id}. Soft deleting...");

                bool packageDetailDeleteResult = await _unitOfWork.VaccinePackageDetailRepository.SoftRemoveRange(vaccinePackageDetails);
                if (!packageDetailDeleteResult)
                {
                    _logger.Warn($"Failed to soft delete VaccinePackageDetails for Vaccine ID: {id}");
                    return null;
                }
            }

            bool deleteResult = await _unitOfWork.VaccineRepository.SoftRemove(vaccine);
            if (!deleteResult)
            {
                _logger.Warn($"Vaccine with ID {id} could not be deleted.");
                return null;
            }
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"Vaccine with ID {id} ('{vaccine.VaccineName}') deleted successfully.");


            var deletedVaccineDTO = new VaccineDTO
            {
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price
            };

            return deletedVaccineDTO;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete vaccine with ID {id}. Error: {ex.Message}");
            throw;
        }
    }

    public async Task<PagedResult<VaccineDTO>> GetVaccines(string? search, string? type, string? sortBy, bool isDescending, int page, int pageSize)
    {
        var query = await _unitOfWork.VaccineRepository.GetAllAsync();
        var queryList = query.ToList();

        // Filtering
        if (!string.IsNullOrWhiteSpace(search))
        {
            string searchLower = search.Trim().ToLower();
            queryList = queryList.Where(v => v.VaccineName.ToLower().Contains(searchLower)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            queryList = queryList.Where(v => v.Type.ToLower().Contains(type.Trim().ToLower())).ToList();
        }
        //Sorting

        if (!string.IsNullOrWhiteSpace(sortBy))
        {
            switch (sortBy.ToLower())
            {
                case "vaccinename":
                    queryList = isDescending ? queryList.OrderByDescending(v => v.VaccineName).ToList() : queryList.OrderBy(v => v.VaccineName).ToList();
                    break;
                case "price":
                    queryList = isDescending ? queryList.OrderByDescending(v => v.Price).ToList() : queryList.OrderBy(v => v.Price).ToList();
                    break;
                case "type":
                    queryList = isDescending ? queryList.OrderByDescending(v => v.Type).ToList() : queryList.OrderBy(v => v.Type).ToList();
                    break;
                default:
                    _logger.Warn($"Unknown sort parameter: {sortBy}. Sorting by default (VaccineName).");
                    queryList = queryList.OrderBy(v => v.VaccineName).ToList();
                    break;
            }
        }

        // Pagination
        var totalItems = queryList.Count();
        var vaccines = queryList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Mapping to DTOs
        var vaccineDTOs = vaccines.Select(v => new VaccineDTO
        {
            VaccineName = v.VaccineName,
            Description = v.Description,
            PicUrl = v.PicUrl,
            Type = v.Type,
            Price = v.Price
        }).ToList();

        var result = new PagedResult<VaccineDTO>(vaccineDTOs, totalItems, page, pageSize);

        return result;
    }

    public async Task<VaccineDTO> GetVaccineById(Guid id)
    {
        _logger.Info($"Fetching vaccine with ID: {id}");
        try
        {
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(id);
            if (vaccine == null)
            {
                _logger.Warn($"Vaccine with ID {id} not found.");
                return null;
            }

            _logger.Info($"Vaccine with ID {id} found: {vaccine.VaccineName}");

            return new VaccineDTO
            {
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to fetch vaccine with ID {id}. Error: {ex.Message}");
            throw;
        }
    }
}