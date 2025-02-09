using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.Domain.DTOs.VaccineDTOs
{
    public class VaccineDTO
    {
        public string? VaccineName { get; set; }
        public string? Description { get; set; }
        public string? PicUrl { get; set; }
        public string? Type { get; set; }
        public decimal? Price { get; set; }
        public BloodType? ForBloodType { get; set; }
        public bool? AvoidChronic { get; set; }
        public bool? AvoidAllergy { get; set; }
        public bool? HasDrugInteraction { get; set; }
        public bool? HasSpecialWarning { get; set; }
    }
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);

        public PagedResult(List<T> items, int totalCount, int page, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
        }
    }
}
