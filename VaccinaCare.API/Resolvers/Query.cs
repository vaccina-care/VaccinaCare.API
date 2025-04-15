using VaccinaCare.Application.Interface;
using VaccinaCare.Domain.DTOs.VaccineDTOs;

namespace VaccinaCare.API.Resolvers;

public class Query
{
    private readonly IVaccineService _vaccineService;

    public Query(IVaccineService vaccineService)
    {
        _vaccineService = vaccineService;
    }

    [GraphQLDescription("Get a paginated list of vaccines with optional filtering and sorting")]
    public async Task<PagedResult<VaccineDto>> GetVaccines(
        [GraphQLDescription("Search term for vaccine name or description")]
        string? search = null,
        [GraphQLDescription("Filter vaccines by type")]
        string? type = null,
        [GraphQLDescription("Property to sort by")]
        string? sortBy = null,
        [GraphQLDescription("Sort in descending order if true")]
        bool isDescending = false,
        [GraphQLDescription("Page number (1-based)")]
        int page = 1,
        [GraphQLDescription("Number of items per page")]
        int pageSize = 10)
    {
        return await _vaccineService.GetVaccines(search, type, sortBy, isDescending, page, pageSize);
    }

    [GraphQLDescription("Get a specific vaccine by its ID")]
    public async Task<VaccineDto> GetVaccineById(
        [GraphQLDescription("The unique identifier of the vaccine")]
        Guid id)
    {
        return await _vaccineService.GetVaccineById(id);
    }

    [GraphQLDescription("Get the count of available vaccines")]
    public async Task<int> GetVaccineAvailable()
    {
        return await _vaccineService.GetVaccineAvailable();
    }

    [GraphQLDescription("Get the top 5 most booked vaccines")]
    public async Task<List<VaccineBookingDto>> GetTop5MostBookedVaccines()
    {
        return await _vaccineService.GetTop5MostBookedVaccinesAsync();
    }

    [GraphQLDescription("Check if a child can receive a specific vaccine")]
    public async Task<ChildVaccineEligibilityDto> CanChildReceiveVaccine(
        [GraphQLDescription("The unique identifier of the child")]
        Guid childId,
        [GraphQLDescription("The unique identifier of the vaccine")]
        Guid vaccineId)
    {
        var (isEligible, message) = await _vaccineService.CanChildReceiveVaccine(childId, vaccineId);
        return new ChildVaccineEligibilityDto
        {
            IsEligible = isEligible,
            Message = message
        };
    }
}

// Helper class to return eligibility check results in a structured format
public class ChildVaccineEligibilityDto
{
    public bool IsEligible { get; set; }
    public string Message { get; set; } = string.Empty;
}

