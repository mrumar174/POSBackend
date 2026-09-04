using AutoMapper;
using POS.DTOs.Tenancy;
using POS.Entities.Tenancy;

namespace POS.Web.Mapping
{
    public class TenancyProfile : Profile
    {
        public TenancyProfile()
        {
            // Entity enum <-> DTO enum: two separate types with matching member
            // names (see POS.DTOs.Common.Enums for why they're duplicated).
            // AutoMapper maps enums by name, but needs the pair registered.
            CreateMap<POS.Entities.Common.SubscriptionStatus, POS.DTOs.Common.SubscriptionStatus>();
            CreateMap<POS.DTOs.Common.SubscriptionStatus, POS.Entities.Common.SubscriptionStatus>();
            CreateMap<POS.Entities.Common.DataIsolationMode, POS.DTOs.Common.DataIsolationMode>();

            CreateMap<Tenant, TenantDto>();
            CreateMap<CreateTenantDto, Tenant>();
            CreateMap<UpdateTenantDto, Tenant>();

            CreateMap<Shop, ShopDto>();
            CreateMap<CreateShopDto, Shop>();
            CreateMap<UpdateShopDto, Shop>();
        }
    }
}
