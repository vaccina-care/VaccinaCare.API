using System.Data;
using Microsoft.AspNetCore.Http;
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
    private readonly IBlobService _blobService;

    public VaccineService(IUnitOfWork unitOfWork, ILoggerService logger, IClaimsService claimsService,
        IBlobService blobService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimsService = claimsService;
        _blobService = blobService;
    }

    //CRUD Vaccines
    public async Task<PagedResult<VaccineDto>> GetVaccines(string? search, string? type, string? sortBy,
        bool isDescending, int page, int pageSize)
    {
        try
        {
            var query = await _unitOfWork.VaccineRepository.GetAllAsync();
            var queryList = query.ToList();

            // Filtering
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                queryList = queryList.Where(v => v.VaccineName.ToLower().Contains(searchLower)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(type))
                queryList = queryList.Where(v => v.Type.ToLower().Contains(type.Trim().ToLower())).ToList();
            //Sorting

            if (!string.IsNullOrWhiteSpace(sortBy))
                switch (sortBy.ToLower())
                {
                    case "vaccinename":
                        queryList = isDescending
                            ? queryList.OrderByDescending(v => v.VaccineName).ToList()
                            : queryList.OrderBy(v => v.VaccineName).ToList();
                        break;

                    case "price":
                        queryList = isDescending
                            ? queryList.OrderByDescending(v => v.Price).ToList()
                            : queryList.OrderBy(v => v.Price).ToList();
                        break;

                    case "type":
                        queryList = isDescending
                            ? queryList.OrderByDescending(v => v.Type).ToList()
                            : queryList.OrderBy(v => v.Type).ToList();
                        break;

                    default:
                        _logger.Warn($"Unknown sort parameter: {sortBy}. Sorting by default (VaccineName).");
                        queryList = queryList.OrderBy(v => v.VaccineName).ToList();
                        break;
                }

            // Pagination
            var totalItems = queryList.Count();
            var vaccines = queryList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Mapping to DTOs
            var vaccineDTOs = vaccines.Select(v => new VaccineDto
            {
                Id = v.Id,
                VaccineName = v.VaccineName,
                Description = v.Description,
                PicUrl = v.PicUrl,
                Type = v.Type,
                Price = v.Price,
                RequiredDoses = v.RequiredDoses,
                DoseIntervalDays = v.DoseIntervalDays,
                ForBloodType = v.ForBloodType,
                AvoidChronic = v.AvoidChronic,
                AvoidAllergy = v.AvoidAllergy,
                HasDrugInteraction = v.HasDrugInteraction,
                HasSpecialWarning = v.HasSpecialWarning
            }).ToList();

            var result = new PagedResult<VaccineDto>(vaccineDTOs, totalItems, page, pageSize);

            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<VaccineDto> GetVaccineById(Guid id)
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

            return new VaccineDto
            {
                Id = vaccine.Id,
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price,
                RequiredDoses = vaccine.RequiredDoses,
                DoseIntervalDays = vaccine.DoseIntervalDays,
                ForBloodType = vaccine.ForBloodType,
                AvoidChronic = vaccine.AvoidChronic,
                AvoidAllergy = vaccine.AvoidAllergy,
                HasDrugInteraction = vaccine.HasDrugInteraction,
                HasSpecialWarning = vaccine.HasSpecialWarning
            };
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to fetch vaccine with ID {id}. Error: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccineDto> CreateVaccine(CreateVaccineDto createVaccineDto, IFormFile vaccinePictureFile)
    {
        _logger.Info("Starting to create a new vaccine.");

        try
        {
            _logger.Info(
                $"Received VaccineDTO with VaccineName: {createVaccineDto.VaccineName}, Type: {createVaccineDto.Type}, Price: {createVaccineDto.Price}, BloodType: {createVaccineDto.ForBloodType}, AvoidChronic: {createVaccineDto.AvoidChronic}, AvoidAllergy: {createVaccineDto.AvoidAllergy}, HasDrugInteraction: {createVaccineDto.HasDrugInteraction}, HasSpecialWarning: {createVaccineDto.HasSpecialWarning}"
            );

            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(createVaccineDto.VaccineName))
                validationErrors.Add("Vaccine name is required.");

            if (createVaccineDto.Price <= 0)
                validationErrors.Add("Price must be greater than zero.");

            if (string.IsNullOrWhiteSpace(createVaccineDto.Description))
                validationErrors.Add("Description is required.");

            if (string.IsNullOrWhiteSpace(createVaccineDto.Type))
                validationErrors.Add("Type is required.");

            if (createVaccineDto.RequiredDoses <= 0)
                validationErrors.Add("RequiredDoses must be greater than zero.");

            if (vaccinePictureFile == null || vaccinePictureFile.Length == 0)
                validationErrors.Add("Vaccine picture is required.");

            if (validationErrors.Any())
            {
                _logger.Warn($"Validation failed for VaccineDTO: {string.Join("; ", validationErrors)}");
                return null;
            }

            var fileName = $"vaccines/{Guid.NewGuid()}{Path.GetExtension(vaccinePictureFile.FileName)}";
            using (var stream = vaccinePictureFile.OpenReadStream())
            {
                await _blobService.UploadFileAsync(fileName, stream);
            }

            // Lấy URL của ảnh đã upload
            var picUrl = await _blobService.GetFileUrlAsync(fileName);

            var vaccine = new Vaccine
            {
                VaccineName = createVaccineDto.VaccineName,
                Description = createVaccineDto.Description,
                PicUrl = picUrl, // Lưu URL của ảnh vào database
                Type = createVaccineDto.Type,
                Price = createVaccineDto.Price,
                RequiredDoses = createVaccineDto.RequiredDoses,
                DoseIntervalDays = createVaccineDto.DoseIntervalDays,
                ForBloodType = createVaccineDto.ForBloodType,
                AvoidChronic = createVaccineDto.AvoidChronic,
                AvoidAllergy = createVaccineDto.AvoidAllergy,
                HasDrugInteraction = createVaccineDto.HasDrugInteraction,
                HasSpecialWarning = createVaccineDto.HasSpecialWarning
            };

            _logger.Info(
                $"Vaccine object created. Ready to save: VaccineName = {vaccine.VaccineName}, Type = {vaccine.Type}, Price = {vaccine.Price}");

            await _unitOfWork.VaccineRepository.AddAsync(vaccine);
            await _unitOfWork.SaveChangesAsync();

            var createdVaccineDTO = new VaccineDto
            {
                Id = vaccine.Id,
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price,
                RequiredDoses = vaccine.RequiredDoses,
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

    public async Task<VaccineDto> UpdateVaccine(Guid id, UpdateVaccineDto updateDto, IFormFile? vaccinePictureFile)
    {
        if (updateDto == null)
        {
            _logger.Warn("Update failed: UpdateVaccineDto is null.");
            throw new ArgumentNullException(nameof(updateDto));
        }

        try
        {
            _logger.Info($"Fetching vaccine details for ID: {id}");
            var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(id);

            if (vaccine == null)
            {
                _logger.Warn($"Vaccine with ID: {id} not found.");
                throw new KeyNotFoundException($"Vaccine with ID {id} not found.");
            }

            _logger.Info("Vaccine found. Preparing to update...");

            // Nếu có ảnh mới, upload lên MinIO và cập nhật PicUrl
            var newPicUrl = vaccine.PicUrl;
            if (vaccinePictureFile != null && vaccinePictureFile.Length > 0)
            {
                _logger.Info("Uploading new vaccine image...");

                var fileName = $"vaccines/{Guid.NewGuid()}{Path.GetExtension(vaccinePictureFile.FileName)}";
                using (var stream = vaccinePictureFile.OpenReadStream())
                {
                    await _blobService.UploadFileAsync(fileName, stream);
                }

                newPicUrl = await _blobService.GetFileUrlAsync(fileName);
                _logger.Success($"New image uploaded. Updated PicUrl: {newPicUrl}");
            }

            // Cập nhật chỉ các trường có giá trị hợp lệ
            vaccine.VaccineName = !string.IsNullOrWhiteSpace(updateDto.VaccineName)
                ? updateDto.VaccineName
                : vaccine.VaccineName;
            vaccine.Description = !string.IsNullOrWhiteSpace(updateDto.Description)
                ? updateDto.Description
                : vaccine.Description;
            vaccine.PicUrl = newPicUrl; // Cập nhật PicUrl nếu có hình mới
            vaccine.Type = !string.IsNullOrWhiteSpace(updateDto.Type) ? updateDto.Type : vaccine.Type;
            vaccine.Price = updateDto.Price ?? vaccine.Price;
            vaccine.RequiredDoses = updateDto.RequiredDoses ?? vaccine.RequiredDoses;
            vaccine.DoseIntervalDays = updateDto.DoseIntervalDays ?? vaccine.DoseIntervalDays;
            vaccine.ForBloodType = updateDto.ForBloodType ?? vaccine.ForBloodType;
            vaccine.AvoidChronic = updateDto.AvoidChronic ?? vaccine.AvoidChronic;
            vaccine.AvoidAllergy = updateDto.AvoidAllergy ?? vaccine.AvoidAllergy;
            vaccine.HasDrugInteraction = updateDto.HasDrugInteraction ?? vaccine.HasDrugInteraction;
            vaccine.HasSpecialWarning = updateDto.HasSpecialWarning ?? vaccine.HasSpecialWarning;

            _logger.Info("Saving updated vaccine details...");

            await _unitOfWork.VaccineRepository.Update(vaccine);
            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"Vaccine with ID {id} updated successfully.");

            return new VaccineDto
            {
                Id = vaccine.Id,
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price,
                RequiredDoses = vaccine.RequiredDoses,
                ForBloodType = vaccine.ForBloodType,
                AvoidChronic = vaccine.AvoidChronic,
                AvoidAllergy = vaccine.AvoidAllergy,
                HasDrugInteraction = vaccine.HasDrugInteraction,
                HasSpecialWarning = vaccine.HasSpecialWarning
            };
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

    public async Task<VaccineDto> DeleteVaccine(Guid id)
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
                _logger.Info(
                    $"Found {vaccinePackageDetails.Count} VaccinePackageDetails associated with Vaccine ID: {id}. Soft deleting...");

                var packageDetailDeleteResult =
                    await _unitOfWork.VaccinePackageDetailRepository.SoftRemoveRange(vaccinePackageDetails);
                if (!packageDetailDeleteResult)
                {
                    _logger.Warn($"Failed to soft delete VaccinePackageDetails for Vaccine ID: {id}");
                    return null;
                }
            }

            var deleteResult = await _unitOfWork.VaccineRepository.SoftRemove(vaccine);
            if (!deleteResult)
            {
                _logger.Warn($"Vaccine with ID {id} could not be deleted.");
                return null;
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.Success($"Vaccine with ID {id} ('{vaccine.VaccineName}') deleted successfully.");

            var deletedVaccineDTO = new VaccineDto
            {
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price,
                RequiredDoses = vaccine.RequiredDoses,
                ForBloodType = vaccine.ForBloodType,
                AvoidChronic = vaccine.AvoidChronic,
                AvoidAllergy = vaccine.AvoidAllergy,
                HasDrugInteraction = vaccine.HasDrugInteraction,
                HasSpecialWarning = vaccine.HasSpecialWarning
            };

            return deletedVaccineDTO;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete vaccine with ID {id}. Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Kiểm tra trẻ có đủ điều kiện để tiêm vaccine không.
    /// </summary>
    public async Task<(bool isEligible, string message)> CanChildReceiveVaccine(Guid childId, Guid vaccineId)
    {
        var child = await _unitOfWork.ChildRepository.GetByIdAsync(childId);
        var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(vaccineId);

        if (child == null)
        {
            var message = $"Child ID {childId} not found.";
            _logger.Error(message);
            return (false, message);
        }

        if (vaccine == null)
        {
            var message = $"Vaccine ID {vaccineId} not found.";
            _logger.Error(message);
            return (false, message);
        }

        if (vaccine.AvoidChronic == true && child.HasChronicIllnesses)
        {
            var message =
                $"Child {child.FullName} has chronic illnesses, and vaccine ID {vaccineId} should not be given to such cases.";
            _logger.Warn(message);
            return (false, message);
        }

        if (vaccine.AvoidAllergy == true && child.HasAllergies)
        {
            var message = $"Child {child.FullName} has allergies, which makes vaccine ID {vaccineId} unsafe.";
            _logger.Warn(message);
            return (false, message);
        }

        var successMessage = $"Child {child.FullName} is eligible for vaccine ID {vaccineId}.";
        _logger.Success(successMessage);
        return (true, successMessage);
    }

    public async Task<int> GetNextDoseNumber(Guid childId, Guid vaccineId)
    {
        // Fetch vaccine details to check required doses
        var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(vaccineId);
        if (vaccine == null)
        {
            _logger.Error($"[GetNextDoseNumber] Vaccine ID {vaccineId} not found.");
            throw new ArgumentException($"Vaccine ID {vaccineId} không tồn tại.");
        }

        // Get all vaccination records for the specific vaccine
        var records = await _unitOfWork.VaccinationRecordRepository
            .GetAllAsync(vr => vr.ChildId == childId && vr.VaccineId == vaccineId);

        // If there are no records, start from Dose 1
        if (!records.Any()) return 1;

        // Find the highest dose number given for this vaccine
        var lastDoseNumber = records.Max(r => r.DoseNumber);

        // Next dose is simply the last administered dose +1
        var nextDose = lastDoseNumber + 1;

        // Ensure we do not exceed the required doses
        if (nextDose > vaccine.RequiredDoses)
        {
            _logger.Warn($"[GetNextDoseNumber] Child {childId} has already received all {vaccine.RequiredDoses} doses.");
            return vaccine.RequiredDoses; // Cap at max required dose
        }

        _logger.Info($"[GetNextDoseNumber] Last dose: {lastDoseNumber}, Next dose: {nextDose}");
        return nextDose;
    }


    /// <summary>
    /// Kiểm tra xem vaccine này có thể tiêm chung với các vaccine khác hay không.
    /// - Nếu vaccine có quy tắc "không thể tiêm chung" → trả về false.
    /// - Nếu vaccine có yêu cầu khoảng cách tối thiểu giữa các lần tiêm,
    ///   kiểm tra lịch hẹn gần nhất của vaccine đã đặt trước đó.
    /// - Nếu khoảng cách không đủ → trả về false.
    /// - Nếu tất cả kiểm tra hợp lệ → trả về true.
    /// </summary>
    public async Task<bool> CheckVaccineCompatibility(Guid vaccineId, List<Guid> bookedVaccineIds,
        DateTime appointmentDate)
    {
        _logger.Info(
            $"[CheckVaccineCompatibility] Start checking for vaccine {vaccineId} with booked vaccines: {string.Join(", ", bookedVaccineIds)} on {appointmentDate}");

        foreach (var bookedVaccineId in bookedVaccineIds)
        {
            _logger.Info(
                $"Checking compatibility between VaccineId: {vaccineId} and BookedVaccineId: {bookedVaccineId}");

            // Lấy quy tắc tiêm giữa vaccine được chọn và các vaccine đã đặt lịch trước đó
            var rule = await _unitOfWork.VaccineIntervalRulesRepository
                .FirstOrDefaultAsync(r =>
                    (r.VaccineId == vaccineId && r.RelatedVaccineId == bookedVaccineId) ||
                    (r.VaccineId == bookedVaccineId && r.RelatedVaccineId == vaccineId));

            // Nếu có quy tắc xác định
            if (rule != null)
            {
                _logger.Info(
                    $"Found VaccineIntervalRule for {vaccineId} and {bookedVaccineId}: CanBeGivenTogether = {rule.CanBeGivenTogether}, MinIntervalDays = {rule.MinIntervalDays}");

                // Nếu hai loại vaccine không thể tiêm chung, trả về false
                if (!rule.CanBeGivenTogether)
                {
                    _logger.Info($"Vaccine {vaccineId} and {bookedVaccineId} cannot be given together.");
                    return false;
                }

                // Nếu có yêu cầu về khoảng cách tối thiểu giữa các mũi tiêm
                if (rule.MinIntervalDays > 0)
                {
                    // Kiểm tra lịch hẹn gần nhất của vaccine đã đặt trước đó
                    var lastAppointment = await _unitOfWork.AppointmentsVaccineRepository
                        .FirstOrDefaultAsync(a =>
                            a.VaccineId == bookedVaccineId &&
                            a.Appointment.AppointmentDate.HasValue &&
                            a.Appointment.AppointmentDate.Value.AddDays(rule.MinIntervalDays) > appointmentDate);

                    // Nếu có lịch hẹn vi phạm khoảng cách tối thiểu, từ chối lịch tiêm
                    if (lastAppointment != null)
                    {
                        _logger.Info(
                            $"Vaccine {vaccineId} must be scheduled at least {rule.MinIntervalDays} days after vaccine {bookedVaccineId}. Appointment denied.");
                        return false;
                    }
                }
            }
            else
            {
                _logger.Info(
                    $"No interval rule found between VaccineId: {vaccineId} and BookedVaccineId: {bookedVaccineId}. Assuming compatible.");
            }
        }

        _logger.Info($"[CheckVaccineCompatibility] Vaccine {vaccineId} is compatible with all booked vaccines.");
        return true;
    }
}