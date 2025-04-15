using VaccinaCare.Application.Interface;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.GraphQL.Queries;

public class VaccineQueries
{
    private readonly IVaccineService _vaccineService;
    private readonly IUnitOfWork _unitOfWork;

    public VaccineQueries(IVaccineService vaccineService, IUnitOfWork unitOfWork)
    {
        _vaccineService = vaccineService;
        _unitOfWork = unitOfWork;
    }

    [GraphQLDescription("Lấy danh sách tất cả các vaccine với phân trang và lọc")]
    public async Task<PagedResult<VaccineDto>> GetVaccines(
        [GraphQLDescription("Từ khóa tìm kiếm")]
        string? search = null,
        [GraphQLDescription("Loại vaccine")] string? type = null,
        [GraphQLDescription("Sắp xếp theo trường (vaccineName, price, type)")]
        string? sortBy = null,
        [GraphQLDescription("Sắp xếp giảm dần")]
        bool isDescending = false,
        [GraphQLDescription("Trang hiện tại")] int page = 1,
        [GraphQLDescription("Số lượng mỗi trang")]
        int pageSize = 10)
    {
        // Sử dụng service đã có để lấy dữ liệu
        return await _vaccineService.GetVaccines(search, type, sortBy, isDescending, page, pageSize);
    }

    [GraphQLDescription("Lấy thông tin chi tiết của một vaccine theo ID")]
    public async Task<VaccineDto> GetVaccineById(
        [GraphQLDescription("ID của vaccine cần lấy thông tin")]
        Guid id)
    {
        // Sử dụng service đã có để lấy dữ liệu
        return await _vaccineService.GetVaccineById(id);
    }

    [GraphQLDescription("Lấy tổng số vaccine khả dụng trong hệ thống")]
    public async Task<int> GetVaccineCount()
    {
        return await _vaccineService.GetVaccineAvailable();
    }

    [GraphQLDescription("Lấy danh sách vaccine theo nhóm máu")]
    public async Task<IEnumerable<Vaccine>> GetVaccinesByBloodType(
        [GraphQLDescription("Nhóm máu cần lọc")]
        string? bloodType = null)
    {
        var vaccines = await _unitOfWork.VaccineRepository.GetAllAsync();

        if (!string.IsNullOrEmpty(bloodType))
        {
            // Kiểm tra xem chuỗi bloodType có phải là enum BloodType hợp lệ không
            if (Enum.TryParse(bloodType, true, out Domain.Enums.BloodType bloodTypeEnum))
            {
                return vaccines.Where(v => v.ForBloodType == bloodTypeEnum).ToList();
            }
        }

        return vaccines;
    }

    [GraphQLDescription("Lấy danh sách vaccine có cảnh báo đặc biệt")]
    public async Task<IEnumerable<Vaccine>> GetVaccinesWithWarnings()
    {
        var vaccines = await _unitOfWork.VaccineRepository.GetAllAsync();

        return vaccines.Where(v =>
            v.HasSpecialWarning == true ||
            v.AvoidChronic == true ||
            v.AvoidAllergy == true ||
            v.HasDrugInteraction == true
        ).ToList();
    }

    [GraphQLDescription("Tìm kiếm vaccine nâng cao với nhiều tiêu chí")]
    public async Task<IEnumerable<Vaccine>> SearchVaccines(
        [GraphQLDescription("Từ khóa tìm kiếm trong tên và mô tả")]
        string? keyword = null,
        [GraphQLDescription("Giá tối thiểu")] decimal? minPrice = null,
        [GraphQLDescription("Giá tối đa")] decimal? maxPrice = null,
        [GraphQLDescription("Nhóm máu")] string? bloodType = null,
        [GraphQLDescription("Loại vaccine")] string? vaccineType = null,
        [GraphQLDescription("Số liều tối thiểu")]
        int? minDoses = null)
    {
        var vaccines = await _unitOfWork.VaccineRepository.GetAllAsync();
        var filteredVaccines = vaccines.AsEnumerable();

        // Lọc theo từ khóa
        if (!string.IsNullOrEmpty(keyword))
        {
            var lowerKeyword = keyword.ToLower();
            filteredVaccines = filteredVaccines.Where(v =>
                (v.VaccineName?.ToLower().Contains(lowerKeyword) ?? false) ||
                (v.Description?.ToLower().Contains(lowerKeyword) ?? false)
            );
        }

        // Lọc theo khoảng giá
        if (minPrice.HasValue)
        {
            filteredVaccines = filteredVaccines.Where(v => v.Price >= minPrice);
        }

        if (maxPrice.HasValue)
        {
            filteredVaccines = filteredVaccines.Where(v => v.Price <= maxPrice);
        }

        // Lọc theo nhóm máu
        if (!string.IsNullOrEmpty(bloodType))
        {
            if (Enum.TryParse(bloodType, true, out Domain.Enums.BloodType bloodTypeEnum))
            {
                filteredVaccines = filteredVaccines.Where(v => v.ForBloodType == bloodTypeEnum);
            }
        }

        // Lọc theo loại vaccine
        if (!string.IsNullOrEmpty(vaccineType))
        {
            filteredVaccines = filteredVaccines.Where(v =>
                v.Type?.ToLower() == vaccineType.ToLower()
            );
        }

        // Lọc theo số liều tối thiểu
        if (minDoses.HasValue)
        {
            filteredVaccines = filteredVaccines.Where(v => v.RequiredDoses >= minDoses);
        }

        return filteredVaccines.ToList();
    }
}