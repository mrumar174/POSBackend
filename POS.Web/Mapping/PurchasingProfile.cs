using AutoMapper;
using POS.DTOs.Purchasing;
using POS.Entities.Purchasing;

namespace POS.Web.Mapping
{
    public class PurchasingProfile : Profile
    {
        public PurchasingProfile()
        {
            CreateMap<Supplier, SupplierDto>();
            CreateMap<CreateSupplierDto, Supplier>();
            CreateMap<UpdateSupplierDto, Supplier>();

            // SupplierName/PaymentMethodName flatten automatically — requires
            // .Include(p => p.Supplier).Include(p => p.PaymentMethod) on the query.
            CreateMap<PurchaseDetail, PurchaseDetailDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
            CreateMap<CreatePurchaseDetailDto, PurchaseDetail>();

            CreateMap<Purchase, PurchaseDto>()
                .ForMember(d => d.Items, o => o.MapFrom(s => s.PurchaseDetails));

            // InvoiceNo/SubTotal/GrandTotal/DueAmount are NOT mapped from the
            // client — the service computes SubTotal/GrandTotal from Items and
            // generates InvoiceNo server-side (see CreatePurchaseDto comment).
            CreateMap<CreatePurchaseDto, Purchase>()
                .ForMember(d => d.PurchaseDetails, o => o.MapFrom(s => s.Items));

            CreateMap<PurchaseReturnDetail, PurchaseReturnDetailDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
            CreateMap<CreatePurchaseReturnDetailDto, PurchaseReturnDetail>();

            CreateMap<PurchaseReturn, PurchaseReturnDto>()
                .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier.Name))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.PurchaseReturnDetails));

            CreateMap<CreatePurchaseReturnDto, PurchaseReturn>()
                .ForMember(d => d.PurchaseReturnDetails, o => o.MapFrom(s => s.Items));

            CreateMap<SupplierPayment, SupplierPaymentDto>()
                .ForMember(d => d.SupplierName, o => o.MapFrom(s => s.Supplier.Name))
                .ForMember(d => d.PaymentMethodName, o => o.MapFrom(s => s.PaymentMethod.Name));

            CreateMap<CreateSupplierPaymentDto, SupplierPayment>();
        }
    }
}
