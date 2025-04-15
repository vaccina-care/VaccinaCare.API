using HotChocolate.Types;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.API.GraphQL.Types
{
    public class VaccineType : ObjectType<Vaccine>
    {
        protected override void Configure(IObjectTypeDescriptor<Vaccine> descriptor)
        {
            // Định nghĩa các trường cơ bản
            descriptor.Field(v => v.Id).Type<NonNullType<IdType>>()
                .Description("ID duy nhất của vaccine");
                
            descriptor.Field(v => v.VaccineName).Type<StringType>()
                .Description("Tên của vaccine");
                
            descriptor.Field(v => v.Description).Type<StringType>()
                .Description("Mô tả chi tiết về vaccine");
                
            descriptor.Field(v => v.PicUrl).Type<StringType>()
                .Description("Đường dẫn đến hình ảnh của vaccine");
                
            descriptor.Field(v => v.Type).Type<StringType>()
                .Description("Loại vaccine");
                
            descriptor.Field(v => v.Price).Type<DecimalType>()
                .Description("Giá của vaccine");
                
            descriptor.Field(v => v.RequiredDoses).Type<IntType>()
                .Description("Số liều vaccine cần thiết");
                
            descriptor.Field(v => v.DoseIntervalDays).Type<IntType>()
                .Description("Số ngày giữa các liều vaccine");
                
            descriptor.Field(v => v.ForBloodType).Type<EnumType<BloodType>>()
                .Description("Nhóm máu phù hợp để tiêm vaccine này");
                
            descriptor.Field(v => v.AvoidChronic).Type<BooleanType>()
                .Description("Cờ cho biết có nên tránh cho người mắc bệnh mãn tính không");
                
            descriptor.Field(v => v.AvoidAllergy).Type<BooleanType>()
                .Description("Cờ cho biết có nên tránh cho người bị dị ứng không");
                
            descriptor.Field(v => v.HasDrugInteraction).Type<BooleanType>()
                .Description("Cờ cho biết vaccine có tương tác thuốc không");
                
            descriptor.Field(v => v.HasSpecialWarning).Type<BooleanType>()
                .Description("Cờ cho biết vaccine có cảnh báo đặc biệt không");
                
            descriptor.Field(v => v.CreatedAt).Type<DateTimeType>()
                .Description("Thời điểm vaccine được tạo");
                
            descriptor.Field(v => v.UpdatedAt).Type<DateTimeType>()
                .Description("Thời điểm vaccine được cập nhật lần cuối");
                
            // Các trường tính toán (computed fields)
            descriptor.Field("bloodTypeDisplay")
                .Type<StringType>()
                .Description("Hiển thị nhóm máu dưới dạng chuỗi thân thiện với người dùng")
                .Resolve(context => {
                    var vaccine = context.Parent<Vaccine>();
                    return vaccine.ForBloodType.HasValue 
                        ? vaccine.ForBloodType.ToString() 
                        : "Tất cả các nhóm máu";
                });
                
            descriptor.Field("safetyInfo")
                .Type<StringType>()
                .Description("Thông tin an toàn tổng hợp về vaccine")
                .Resolve(context => {
                    var vaccine = context.Parent<Vaccine>();
                    var warnings = new List<string>();
                    
                    if (vaccine.AvoidChronic == true)
                        warnings.Add("Không khuyến nghị cho người có bệnh mãn tính");
                        
                    if (vaccine.AvoidAllergy == true)
                        warnings.Add("Không khuyến nghị cho người bị dị ứng");
                        
                    if (vaccine.HasDrugInteraction == true)
                        warnings.Add("Có thể có tương tác với một số loại thuốc");
                        
                    if (vaccine.HasSpecialWarning == true)
                        warnings.Add("Có cảnh báo đặc biệt");
                        
                    return warnings.Count > 0 
                        ? string.Join(". ", warnings) 
                        : "Không có cảnh báo đặc biệt";
                });
                
            // Các mối quan hệ 
            // Lưu ý: Trong GraphQL, việc load các mối quan hệ có thể dẫn đến N+1 queries, 
            // nên chúng ta sẽ cần xử lý chúng một cách thông minh.
            
            // Định nghĩa mối quan hệ với VaccinationRecords
            descriptor.Field(v => v.VaccinationRecords)
                .Description("Danh sách các bản ghi tiêm chủng của vaccine này")
                .UseFiltering()
                .UseSorting();
                
            // Định nghĩa mối quan hệ với VaccinePackageDetails
            descriptor.Field(v => v.VaccinePackageDetails)
                .Description("Danh sách các gói vaccine có chứa vaccine này")
                .UseFiltering()
                .UseSorting();
        }
    }
}