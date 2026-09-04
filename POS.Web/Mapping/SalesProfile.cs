using AutoMapper;
using POS.DTOs.Sales;
using POS.Entities.Sales;

namespace POS.Web.Mapping
{
    public class SalesProfile : Profile
    {
        public SalesProfile()
        {
            CreateMap<SaleDetail, SaleDetailDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
            CreateMap<CreateSaleDetailDto, SaleDetail>();

            CreateMap<Sale, SaleDto>()
                .ForMember(d => d.Items, o => o.MapFrom(s => s.SaleDetails));

            // InvoiceNo/SubTotal/GrandTotal/DueAmount computed server-side —
            // never trust these from the client on a POS invoice.
            CreateMap<CreateSaleDto, Sale>()
                .ForMember(d => d.SaleDetails, o => o.MapFrom(s => s.Items));

            CreateMap<SaleReturnDetail, SaleReturnDetailDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
            CreateMap<CreateSaleReturnDetailDto, SaleReturnDetail>();

            CreateMap<SaleReturn, SaleReturnDto>()
                .ForMember(d => d.Items, o => o.MapFrom(s => s.SaleReturnDetails));

            CreateMap<CreateSaleReturnDto, SaleReturn>()
                .ForMember(d => d.SaleReturnDetails, o => o.MapFrom(s => s.Items));
        }
    }
}
