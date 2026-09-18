using Microsoft.EntityFrameworkCore;
using POS.DTOs.Catalog;
using POS.DTOs.Common;
using POS.Entities.Catalog;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.IServices.Catalog;

namespace POS.Entities.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public ProductService(
            ApplicationDbContext db,
            ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // Manual projection: ProductDto flattens Category/Brand/Unit names,
        // so this stays explicit rather than relying on AutoMapper naming.
        private IQueryable<ProductDto> ProjectToDto() =>
            _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Unit)
                .Include(p => p.Barcodes)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    Name = p.Name,

                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.Name,

                    BrandId = p.BrandId,
                    BrandName = p.Brand != null
                        ? p.Brand.Name
                        : null,

                    UnitId = p.UnitId,
                    UnitName = p.Unit.Name,

                    PurchasePrice = p.PurchasePrice,
                    SalePrice = p.SalePrice,
                    MinimumStock = p.MinimumStock,
                    Description = p.Description,

                    Barcodes = p.Barcodes
                        .Select(b => b.Barcode)
                        .ToList()
                });

        public async Task<List<ProductDto>> GetAllAsync()
            => await ProjectToDto()
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<ProductDto?> GetByIdAsync(int id)
            => await ProjectToDto()
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            await ValidateReferencesAsync(
                dto.CategoryId,
                dto.BrandId,
                dto.UnitId);

            await ValidateBarcodesUniqueAsync(
                dto.Barcodes,
                excludingProductId: null);

            // Generate product code automatically.
            var productCode = await GenerateNextProductCodeAsync();

            var product = new Product
            {
                ProductCode = productCode,
                Name = dto.Name,
                CategoryId = dto.CategoryId,
                BrandId = dto.BrandId,
                UnitId = dto.UnitId,
                PurchasePrice = dto.PurchasePrice,
                SalePrice = dto.SalePrice,
                MinimumStock = dto.MinimumStock,
                Description = dto.Description
            };

            product.Barcodes = BuildBarcodeEntities(dto.Barcodes);

            _db.Products.Add(product);

            await _db.SaveChangesAsync();

            return await GetByIdAsync(product.Id)
                ?? throw new InvalidOperationException(
                    "Product was created but could not be reloaded.");
        }

        public async Task<ProductDto> UpdateAsync(UpdateProductDto dto)
        {
            var product = await _db.Products
                .Include(p => p.Barcodes)
                .FirstOrDefaultAsync(p => p.Id == dto.Id)
                ?? throw new KeyNotFoundException(
                    $"Product {dto.Id} not found.");

            await ValidateReferencesAsync(
                dto.CategoryId,
                dto.BrandId,
                dto.UnitId);

            await ValidateBarcodesUniqueAsync(
                dto.Barcodes,
                excludingProductId: dto.Id);

            // ProductCode is immutable after creation.
            // dto.ProductCode is intentionally ignored.
            product.Name = dto.Name;
            product.CategoryId = dto.CategoryId;
            product.BrandId = dto.BrandId;
            product.UnitId = dto.UnitId;
            product.PurchasePrice = dto.PurchasePrice;
            product.SalePrice = dto.SalePrice;
            product.MinimumStock = dto.MinimumStock;
            product.Description = dto.Description;

            // Remove old barcodes.
            _db.ProductBarcodes.RemoveRange(product.Barcodes);

            // Add new barcodes.
            product.Barcodes = BuildBarcodeEntities(dto.Barcodes);

            await _db.SaveChangesAsync();

            return await GetByIdAsync(product.Id)
                ?? throw new InvalidOperationException(
                    "Product was updated but could not be reloaded.");
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new KeyNotFoundException(
                    $"Product {id} not found.");

            var hasPurchases = await _db.PurchaseDetails
                .AnyAsync(pd => pd.ProductId == id);

            var hasSales = await _db.SaleDetails
                .AnyAsync(sd => sd.ProductId == id);

            if (hasPurchases || hasSales)
            {
                throw new InvalidOperationException(
                    "Cannot delete a product that has purchase or sale history.");
            }

            _db.Products.Remove(product);

            // Soft delete via SaveChanges override.
            await _db.SaveChangesAsync();
        }

        private List<ProductBarcode> BuildBarcodeEntities(
            List<string> barcodes)
        {
            return barcodes
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Select((b, index) => new ProductBarcode
                {
                    Barcode = b.Trim(),
                    IsPrimary = index == 0
                })
                .ToList();
        }

        private async Task ValidateBarcodesUniqueAsync(
            List<string> barcodes,
            int? excludingProductId)
        {
            var cleaned = barcodes
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .Select(b => b.Trim().ToLower())
                .ToList();

            if (cleaned.Count == 0)
                return;

            var conflicting = await _db.ProductBarcodes
                .Where(pb =>
                    cleaned.Contains(pb.Barcode.ToLower()) &&
                    (
                        excludingProductId == null ||
                        pb.ProductId != excludingProductId
                    ))
                .Select(pb => pb.Barcode)
                .ToListAsync();

            if (conflicting.Any())
            {
                throw new InvalidOperationException(
                    $"Barcode(s) already in use: {string.Join(", ", conflicting)}");
            }
        }

        private async Task ValidateReferencesAsync(
            int categoryId,
            int? brandId,
            int unitId)
        {
            if (!await _db.Categories.AnyAsync(c => c.Id == categoryId))
            {
                throw new InvalidOperationException(
                    $"Category {categoryId} not found.");
            }

            if (brandId.HasValue &&
                !await _db.Brands.AnyAsync(b => b.Id == brandId.Value))
            {
                throw new InvalidOperationException(
                    $"Brand {brandId} not found.");
            }

            if (!await _db.Units.AnyAsync(u => u.Id == unitId))
            {
                throw new InvalidOperationException(
                    $"Unit {unitId} not found.");
            }
        }

        public async Task<PagedResultDto<ProductDto>> SearchAsync(
            ProductQueryDto query)
        {
            var q = ProjectToDto();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.ToLower();

                q = q.Where(p =>
                    p.Name.ToLower().Contains(s) ||
                    p.ProductCode.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                q = q.Where(p =>
                    p.Name.ToLower()
                        .Contains(query.Name.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(query.ProductCode))
            {
                q = q.Where(p =>
                    p.ProductCode.ToLower()
                        .Contains(query.ProductCode.ToLower()));
            }

            if (query.CategoryId.HasValue)
            {
                q = q.Where(p =>
                    p.CategoryId == query.CategoryId.Value);
            }

            if (query.BrandId.HasValue)
            {
                q = q.Where(p =>
                    p.BrandId == query.BrandId.Value);
            }

            if (query.UnitId.HasValue)
            {
                q = q.Where(p =>
                    p.UnitId == query.UnitId.Value);
            }

            q = q.OrderBy(p => p.Name);

            var totalCount = await q.CountAsync();

            var page = Math.Max(query.Page, 1);

            var pageSize = Math.Clamp(
                query.PageSize,
                1,
                100);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResultDto<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private async Task<string> GenerateNextProductCodeAsync()
        {
            var codes = await _db.Products
                .IgnoreQueryFilters()
                .Where(p => p.TenantId == _currentUser.TenantId)
                .Select(p => p.ProductCode)
                .ToListAsync();

            var maxNumber = 0;

            foreach (var code in codes)
            {
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                if (code.StartsWith("PRO-") &&
                    int.TryParse(
                        code.Substring(4),
                        out var number))
                {
                    if (number > maxNumber)
                        maxNumber = number;
                }
            }

            return $"PRO-{(maxNumber + 1):D5}";
        }
    }
}