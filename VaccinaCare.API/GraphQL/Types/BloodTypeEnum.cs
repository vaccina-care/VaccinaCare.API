using HotChocolate.Types;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.API.GraphQL.Types
{
    public class BloodTypeEnum : EnumType<BloodType>
    {
        protected override void Configure(IEnumTypeDescriptor<BloodType> descriptor)
        {
            descriptor.Name("BloodType");
            descriptor.Description("Các nhóm máu");
            
            descriptor.Value(BloodType.A)
                .Description("Nhóm máu A");
                
            descriptor.Value(BloodType.B)
                .Description("Nhóm máu B");
                
            descriptor.Value(BloodType.AB)
                .Description("Nhóm máu AB");
                
            descriptor.Value(BloodType.O)
                .Description("Nhóm máu O");
                
            descriptor.Value(BloodType.Unknown)
                .Description("Nhóm máu không xác định");
        }
    }
}