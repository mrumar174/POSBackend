using POS.DTOs.Catalog;
using POS.Entities.Catalog;
using POS.Entities.Entities.Catalog;

public record BatchAllocation(int? BatchId, decimal Quantity);

public interface IBatchService
{
    Task<ProductBatch?> GetOrCreateAsync(Product product, BatchInputDto? input);
    Task<List<ProductBatchDto>> GetAvailableAsync(int? productId = null);
    Task<List<BatchAllocation>> AllocateFefoAsync(Product product, decimal quantity, bool allowExpired = false);
    Task<List<ProductBatchDto>> GetExpiryReportAsync(int? withinDays, bool includeExpired);
    Task EnsureNoNegativeBatchStockAsync(IEnumerable<int> batchIds);
}