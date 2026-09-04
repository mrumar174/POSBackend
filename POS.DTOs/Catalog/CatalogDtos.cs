namespace POS.DTOs.Catalog
{
    // ---------- Category ----------
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class CreateCategoryDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class UpdateCategoryDto : CreateCategoryDto
    {
        public int Id { get; set; }
    }

    // ---------- Brand ----------
    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class CreateBrandDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class UpdateBrandDto : CreateBrandDto
    {
        public int Id { get; set; }
    }

    // ---------- Unit ----------
    public class UnitDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string ShortName { get; set; } = default!;
    }

    public class CreateUnitDto
    {
        public string Name { get; set; } = default!;
        public string ShortName { get; set; } = default!;
    }

    public class UpdateUnitDto : CreateUnitDto
    {
        public int Id { get; set; }
    }

    // ---------- Product ----------
    // Read DTO flattens Category/Brand/Unit names — AutoMapper maps these
    // automatically by convention (CategoryName <- Category.Name) as long as
    // the entity is queried with .Include(x => x.Category)/.Include(x => x.Brand)/.Include(x => x.Unit).
    public class ProductDto
    {
        public int Id { get; set; }
        public string ProductCode { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? BrandId { get; set; }
        public string? BrandName { get; set; }
        public int UnitId { get; set; }
        public string? UnitName { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinimumStock { get; set; }
        public string? Description { get; set; }
        public List<string> Barcodes { get; set; } = new();
    }

    public class CreateProductDto
    {
        public string ProductCode { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int UnitId { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinimumStock { get; set; }
        public string? Description { get; set; }
        public List<string> Barcodes { get; set; } = new();
    }

    public class UpdateProductDto : CreateProductDto
    {
        public int Id { get; set; }
    }

    // ---------- ProductBarcode ----------
    public class ProductBarcodeDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Barcode { get; set; } = default!;
        public bool IsPrimary { get; set; }
    }

    public class CreateProductBarcodeDto
    {
        public int ProductId { get; set; }
        public string Barcode { get; set; } = default!;
        public bool IsPrimary { get; set; }
    }
}
