using Microsoft.EntityFrameworkCore;
using POS.DTOs.Catalog;
using POS.Entities.Catalog;
using POS.Entities.Data;
using POS.Entities.Entities.Catalog;

public class BatchService : IBatchService
{
    private const int DefaultAlertDays = 30;
    private readonly ApplicationDbContext _db;

    public BatchService(ApplicationDbContext db) => _db = db;

    // Does NOT call SaveChanges: the caller saves in the same unit of work as the
    // purchase. Link via the navigation (detail.Batch = batch) so no Id is needed yet.
    public async Task<ProductBatch?> GetOrCreateAsync(Product product, BatchInputDto? input)
    {
        if (!product.TrackBatch && !product.TrackExpiry) return null;

        var batchNo = string.IsNullOrWhiteSpace(input?.BatchNo) ? null : input!.BatchNo!.Trim();
        var expiry = input?.ExpiryDate?.Date;
        var mfg = input?.ManufactureDate?.Date;

        if (product.TrackBatch && batchNo is null)
            throw new InvalidOperationException($"Batch number is required for '{product.Name}'.");
        if (product.TrackExpiry && expiry is null)
            throw new InvalidOperationException($"Expiry date is required for '{product.Name}'.");
        if (mfg.HasValue && expiry.HasValue && mfg > expiry)
            throw new InvalidOperationException(
                $"Manufacture date is after expiry date for '{product.Name}'.");

        if (!product.TrackBatch) batchNo = null;
        if (!product.TrackExpiry) { expiry = null; mfg = null; }

        var existing = await _db.ProductBatches.FirstOrDefaultAsync(b =>
            b.ProductId == product.Id && b.BatchNo == batchNo && b.ExpiryDate == expiry);
        if (existing != null) return existing;

        // Also check batches added earlier in this unit of work but not yet saved.
        var pending = _db.ChangeTracker.Entries<ProductBatch>()
            .Select(e => e.Entity)
            .FirstOrDefault(b => b.ProductId == product.Id && b.BatchNo == batchNo && b.ExpiryDate == expiry);
        if (pending != null) return pending;

        var batch = new ProductBatch
        {
            ProductId = product.Id,
            BatchNo = batchNo,
            ManufactureDate = mfg,
            ExpiryDate = expiry
        };
        _db.ProductBatches.Add(batch);
        return batch;
    }

    public async Task<List<ProductBatchDto>> GetAvailableAsync(int? productId = null)
    {
        var today = DateTime.Today;

        var balances = await _db.StockTransactions
            .Where(t => t.BatchId != null && (productId == null || t.ProductId == productId))
            .GroupBy(t => t.BatchId!.Value)
            .Select(g => new { BatchId = g.Key, Qty = g.Sum(t => t.QuantityIn - t.QuantityOut) })
            .Where(x => x.Qty > 0)
            .ToListAsync();

        var ids = balances.Select(x => x.BatchId).ToList();
        var qtyById = balances.ToDictionary(x => x.BatchId, x => x.Qty);

        var batches = await _db.ProductBatches
            .Include(b => b.Product).ThenInclude(p => p.Unit)
            .Where(b => ids.Contains(b.Id))
            .ToListAsync();

        // FEFO order: earliest expiry first, no-expiry last, then oldest batch.
        return batches
            .OrderBy(b => b.ExpiryDate == null)
            .ThenBy(b => b.ExpiryDate)
            .ThenBy(b => b.Id)
            .Select(b => Map(b, qtyById[b.Id], today))
            .ToList();
    }

    public async Task<List<BatchAllocation>> AllocateFefoAsync(
        Product product, decimal quantity, bool allowExpired = false)
    {
        if (!product.TrackBatch && !product.TrackExpiry)
            return new() { new BatchAllocation(null, quantity) };

        var result = new List<BatchAllocation>();
        var remaining = quantity;

        foreach (var b in await GetAvailableAsync(product.Id))
        {
            if (remaining <= 0) break;
            if (b.Status == BatchExpiryStatus.Expired && !allowExpired) continue;

            var take = Math.Min(b.QuantityOnHand, remaining);
            result.Add(new BatchAllocation(b.Id, take));
            remaining -= take;
        }

        // Stock received before tracking was switched on has no batch (BatchId == null).
        if (remaining > 0)
        {
            var legacy = await _db.StockTransactions
                .Where(t => t.ProductId == product.Id && t.BatchId == null)
                .SumAsync(t => t.QuantityIn - t.QuantityOut);

            var fromLegacy = Math.Min(Math.Max(legacy, 0), remaining);
            if (fromLegacy > 0)
            {
                result.Add(new BatchAllocation(null, fromLegacy));
                remaining -= fromLegacy;
            }
        }

        if (remaining > 0)
            throw new InvalidOperationException(
                $"Not enough sellable stock for '{product.Name}'. Expired batches cannot be sold.");

        return result;
    }

    public async Task<List<ProductBatchDto>> GetExpiryReportAsync(int? withinDays, bool includeExpired)
    {
        var all = await GetAvailableAsync();
        return all.Where(b => b.ExpiryDate != null && (
                b.Status == BatchExpiryStatus.Expired
                    ? includeExpired
                    : withinDays.HasValue
                        ? b.DaysToExpiry <= withinDays
                        : b.Status == BatchExpiryStatus.NearExpiry))
            .ToList();
    }

    private static ProductBatchDto Map(ProductBatch b, decimal qty, DateTime today) => new()
    {
        Id = b.Id,
        ProductId = b.ProductId,
        ProductCode = b.Product.ProductCode,
        ProductName = b.Product.Name,
        UnitName = b.Product.Unit.Name,
        BatchNo = b.BatchNo,
        ManufactureDate = b.ManufactureDate,
        ExpiryDate = b.ExpiryDate,
        QuantityOnHand = qty,
        DaysToExpiry = b.ExpiryDate.HasValue ? (int?)(b.ExpiryDate.Value.Date - today).TotalDays : null,
        Status = StatusOf(b, today)
    };

    private static BatchExpiryStatus StatusOf(ProductBatch b, DateTime today)
    {
        if (b.ExpiryDate is null) return BatchExpiryStatus.NoExpiry;
        var expiry = b.ExpiryDate.Value.Date;
        if (expiry < today) return BatchExpiryStatus.Expired;
        var alert = b.Product.ExpiryAlertDays ?? DefaultAlertDays;
        return (expiry - today).TotalDays <= alert ? BatchExpiryStatus.NearExpiry : BatchExpiryStatus.Ok;
    }
    // Call after SaveChanges, inside the transaction, when stock rows were removed or edited.
    // Editing/deleting a purchase whose batch was already partly sold would push that batch below zero.
    public async Task EnsureNoNegativeBatchStockAsync(IEnumerable<int> batchIds)
    {
        var ids = batchIds.Distinct().ToList();
        if (ids.Count == 0) return;

        var negativeIds = await _db.StockTransactions
            .Where(t => t.BatchId != null && ids.Contains(t.BatchId.Value))
            .GroupBy(t => t.BatchId!.Value)
            .Select(g => new { BatchId = g.Key, Qty = g.Sum(t => t.QuantityIn - t.QuantityOut) })
            .Where(x => x.Qty < 0)
            .Select(x => x.BatchId)
            .ToListAsync();

        if (negativeIds.Count == 0) return;

        var names = await _db.ProductBatches
            .Where(b => negativeIds.Contains(b.Id))
            .Select(b => new { b.Product.Name, b.BatchNo })
            .ToListAsync();

        throw new InvalidOperationException(
            "This change would make batch stock negative (stock from it was already sold or returned): " +
            string.Join(", ", names.Select(n => $"{n.Name} [{n.BatchNo ?? "no batch no."}]")));
    }
}