using Microsoft.EntityFrameworkCore;
using POS.DTOs.Common;
using POS.DTOs.Inventory;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.Inventory;
using POS.Entities.IServices;
using POS.Entities.IServices.Inventory;

namespace POS.Entities.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public StockService(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        // ==========================================
        // 1. Stock Summary / Read-Only
        // ==========================================

        public async Task<List<StockSummaryDto>> GetSummaryAsync(int? categoryId, int? brandId, string? search, bool lowStockOnly)
        {
            var shopId = _currentUser.ShopId ?? 0;
            var shopName = shopId > 0
                ? await _db.Shops.Where(s => s.Id == shopId).Select(s => s.Name).FirstOrDefaultAsync()
                : "All Branches";

            var query = _db.Products.AsNoTracking().AsQueryable();

            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Name.Contains(search) || p.ProductCode.Contains(search));
            if (lowStockOnly) query = query.Where(p => p.StockTransactions.Sum(t => t.QuantityIn - t.QuantityOut) <= p.MinimumStock);

            return await query
                .Select(p => new StockSummaryDto
                {
                    ProductId = p.Id,
                    ProductCode = p.ProductCode,
                    ProductName = p.Name,
                    CategoryName = p.Category.Name,
                    UnitShortName = p.Unit.ShortName,
                    ShopId = shopId,
                    ShopName = shopName,
                    QuantityOnHand = p.StockTransactions.Sum(t => t.QuantityIn - t.QuantityOut),
                    MinimumStock = p.MinimumStock,
                    IsLowStock = p.StockTransactions.Sum(t => t.QuantityIn - t.QuantityOut) <= p.MinimumStock
                })
                .OrderBy(s => s.ProductName)
                .ToListAsync();
        }

        public async Task<decimal> GetProductStockAsync(int productId)
        {
            return await _db.StockTransactions
                .Where(t => t.ProductId == productId)
                .SumAsync(t => t.QuantityIn - t.QuantityOut);
        }

        // ==========================================
        // 2. Stock Adjustments
        // ==========================================

        private IQueryable<StockAdjustmentDto> ProjectAdjustmentToDto() =>
            _db.StockAdjustments
                .Include(a => a.StockAdjustmentDetails).ThenInclude(d => d.Product)
                .Select(a => new StockAdjustmentDto
                {
                    Id = a.Id,
                    AdjustmentNo = a.AdjustmentNo,
                    AdjustmentDate = a.AdjustmentDate,
                    Reason = a.Reason,
                    Remarks = a.Remarks,
                    Items = a.StockAdjustmentDetails.Select(d => new StockAdjustmentDetailDto
                    {
                        Id = d.Id,
                        ProductId = d.ProductId,
                        ProductName = d.Product.Name,
                        SystemQuantity = d.SystemQuantity,
                        PhysicalQuantity = d.PhysicalQuantity,
                        DifferenceQuantity = d.DifferenceQuantity
                    }).ToList()
                });

        public async Task<List<StockAdjustmentDto>> GetAllAdjustmentsAsync()
            => await ProjectAdjustmentToDto().OrderByDescending(a => a.AdjustmentDate).ThenByDescending(a => a.Id).ToListAsync();

        public async Task<StockAdjustmentDto?> GetAdjustmentByIdAsync(int id)
            => await ProjectAdjustmentToDto().FirstOrDefaultAsync(a => a.Id == id);

        public async Task<StockAdjustmentDto> CreateAdjustmentAsync(CreateStockAdjustmentDto dto)
        {
            ValidateItems(dto.Items);
            var shopId = _currentUser.ShopId ?? throw new InvalidOperationException("No shop selected.");
            var adjustmentNo = await GenerateNextAdjustmentNoAsync(shopId);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var adjustment = new StockAdjustment
                {
                    AdjustmentNo = adjustmentNo,
                    AdjustmentDate = dto.AdjustmentDate,
                    Reason = dto.Reason,
                    Remarks = dto.Remarks
                };

                await PopulateDetailsAndStockAsync(adjustment, dto.Items);

                _db.StockAdjustments.Add(adjustment);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetAdjustmentByIdAsync(adjustment.Id) ?? throw new Exception("Failed to reload adjustment.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<StockAdjustmentDto> UpdateAdjustmentAsync(int id, CreateStockAdjustmentDto dto)
        {
            var adjustment = await _db.StockAdjustments
                .Include(a => a.StockAdjustmentDetails)
                .FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new KeyNotFoundException($"Adjustment {id} not found.");

            ValidateItems(dto.Items);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // Reverse old stock impact
                var oldStockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "StockAdjustment" && s.ReferenceId == adjustment.Id)
                    .ToListAsync();
                _db.StockTransactions.RemoveRange(oldStockRows);

                // Clear old items
                _db.StockAdjustmentDetails.RemoveRange(adjustment.StockAdjustmentDetails);
                adjustment.StockAdjustmentDetails.Clear();

                // Update Header
                adjustment.AdjustmentDate = dto.AdjustmentDate;
                adjustment.Reason = dto.Reason;
                adjustment.Remarks = dto.Remarks;

                // Re-apply with current stock values
                await PopulateDetailsAndStockAsync(adjustment, dto.Items);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetAdjustmentByIdAsync(adjustment.Id) ?? throw new Exception("Failed to reload adjustment.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteAdjustmentAsync(int id)
        {
            var adjustment = await _db.StockAdjustments
                .Include(a => a.StockAdjustmentDetails)
                .FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new KeyNotFoundException($"Adjustment {id} not found.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var stockRows = await _db.StockTransactions
                    .Where(s => s.ReferenceType == "StockAdjustment" && s.ReferenceId == id)
                    .ToListAsync();

                _db.StockTransactions.RemoveRange(stockRows);
                _db.StockAdjustmentDetails.RemoveRange(adjustment.StockAdjustmentDetails);
                _db.StockAdjustments.Remove(adjustment);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task PopulateDetailsAndStockAsync(StockAdjustment adjustment, List<CreateStockAdjustmentDetailDto> items)
        {
            foreach (var item in items)
            {
                var systemQty = await GetProductStockAsync(item.ProductId);
                var diffQty = item.PhysicalQuantity - systemQty;

                adjustment.StockAdjustmentDetails.Add(new StockAdjustmentDetail
                {
                    ProductId = item.ProductId,
                    SystemQuantity = systemQty,
                    PhysicalQuantity = item.PhysicalQuantity,
                    DifferenceQuantity = diffQty
                });

                if (diffQty != 0)
                {
                    _db.StockTransactions.Add(new StockTransaction
                    {
                        ProductId = item.ProductId,
                        TransactionType = StockTransactionType.ADJUSTMENT,
                        QuantityIn = diffQty > 0 ? diffQty : 0,
                        QuantityOut = diffQty < 0 ? Math.Abs(diffQty) : 0,
                        ReferenceType = "StockAdjustment",
                        ReferenceId = adjustment.Id,
                        TransactionDate = adjustment.AdjustmentDate,
                        Remarks = $"Adjustment {adjustment.AdjustmentNo}: {adjustment.Reason}"
                    });
                }
            }
        }

        private static void ValidateItems(List<CreateStockAdjustmentDetailDto> items)
        {
            if (items is null || items.Count == 0)
                throw new InvalidOperationException("An adjustment must have at least one item.");

            if (items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Duplicate products are not allowed in the same adjustment.");

            foreach (var item in items)
            {
                if (item.PhysicalQuantity < 0)
                    throw new InvalidOperationException("Physical quantity cannot be negative.");
            }
        }

        private async Task<string> GenerateNextAdjustmentNoAsync(int shopId)
        {
            var shop = await _db.Shops.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == shopId)
                ?? throw new InvalidOperationException("Shop not found.");

            var prefix = "ADJ";
            var adjNos = await _db.StockAdjustments
                .IgnoreQueryFilters()
                .Where(a => a.ShopId == shopId)
                .Select(a => a.AdjustmentNo)
                .ToListAsync();

            var maxNumber = 0;
            var searchPrefix = prefix + "-";
            foreach (var no in adjNos)
            {
                if (no.StartsWith(searchPrefix) && int.TryParse(no.AsSpan(searchPrefix.Length), out var num) && num > maxNumber)
                    maxNumber = num;
            }

            return $"{prefix}-{(maxNumber + 1):D5}";
        }
        public async Task<Dictionary<int, decimal>> GetStockDictionaryAsync()
        {
            var shopId = _currentUser.ShopId ?? 0;

            // Group stock transactions by ProductId and calculate quantity on hand
            return await _db.StockTransactions
                .AsNoTracking()
                .GroupBy(t => t.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Stock = g.Sum(t => t.QuantityIn - t.QuantityOut)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.Stock);
        }
    }
}