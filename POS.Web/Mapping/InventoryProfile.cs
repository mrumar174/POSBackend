using AutoMapper;
using POS.DTOs.Inventory;
using POS.Entities.Inventory;

namespace POS.Web.Mapping
{
    public class InventoryProfile : Profile
    {
        public InventoryProfile()
        {
            CreateMap<POS.Entities.Common.StockTransactionType, POS.DTOs.Common.StockTransactionType>();

            CreateMap<StockTransaction, StockTransactionDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));
            // No Create map — StockTransaction rows are only ever produced
            // internally by Sale/Purchase/Adjustment services, never posted
            // directly by a client.

            CreateMap<StockAdjustmentDetail, StockAdjustmentDetailDto>()
                .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name));

            // SystemQuantity/DifferenceQuantity are computed by the service at
            // posting time from the live stock balance, not mapped from the client.
            CreateMap<CreateStockAdjustmentDetailDto, StockAdjustmentDetail>()
                .ForMember(d => d.SystemQuantity, o => o.Ignore())
                .ForMember(d => d.DifferenceQuantity, o => o.Ignore());

            CreateMap<StockAdjustment, StockAdjustmentDto>()
                .ForMember(d => d.Items, o => o.MapFrom(s => s.StockAdjustmentDetails));

            CreateMap<CreateStockAdjustmentDto, StockAdjustment>()
                .ForMember(d => d.StockAdjustmentDetails, o => o.MapFrom(s => s.Items));
        }
    }
}
