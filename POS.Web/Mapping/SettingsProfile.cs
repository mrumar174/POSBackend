using AutoMapper;
using POS.DTOs.Settings;
using POS.Entities.Settings;

namespace POS.Web.Mapping
{
    public class SettingsProfile : Profile
    {
        public SettingsProfile()
        {
            CreateMap<PaymentMethod, PaymentMethodDto>();
            CreateMap<CreatePaymentMethodDto, PaymentMethod>();
            CreateMap<UpdatePaymentMethodDto, PaymentMethod>();

            CreateMap<CompanySettings, CompanySettingsDto>();
            CreateMap<UpdateCompanySettingsDto, CompanySettings>();
        }
    }
}
