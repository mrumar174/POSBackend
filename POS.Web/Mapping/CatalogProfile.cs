using AutoMapper;
using POS.DTOs.Catalog;
using POS.Entities.Catalog;

namespace POS.Web.Mapping
{
    public class CatalogProfile : Profile
    {
        public CatalogProfile()
        {
            CreateMap<Category, CategoryDto>();
            CreateMap<CreateCategoryDto, Category>();
            CreateMap<UpdateCategoryDto, Category>();

            CreateMap<Brand, BrandDto>();
            CreateMap<CreateBrandDto, Brand>();
            CreateMap<UpdateBrandDto, Brand>();

            CreateMap<Unit, UnitDto>();
            CreateMap<CreateUnitDto, Unit>();
            CreateMap<UpdateUnitDto, Unit>();

            // CategoryName/BrandName/UnitName flatten automatically by AutoMapper's
            // naming convention (CategoryName <- Category.Name) as long as the
            // query includes .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Unit).
            CreateMap<Product, ProductDto>()
                .ForMember(d => d.Barcodes, o => o.MapFrom(s => s.Barcodes.Select(b => b.Barcode)));

            CreateMap<CreateProductDto, Product>()
                .ForMember(d => d.Barcodes, o => o.Ignore()); // build ProductBarcode rows from the string list in the service

            CreateMap<UpdateProductDto, Product>()
                .ForMember(d => d.Barcodes, o => o.Ignore());

            CreateMap<ProductBarcode, ProductBarcodeDto>();
            CreateMap<CreateProductBarcodeDto, ProductBarcode>();
        }
    }
}
