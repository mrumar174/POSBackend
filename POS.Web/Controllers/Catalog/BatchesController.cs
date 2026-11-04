using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Catalog;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BatchesController : ControllerBase
{
    private readonly IBatchService _batches;
    public BatchesController(IBatchService batches) => _batches = batches;

    // Batches with stock for one product (FEFO order). Used by the sale batch picker.
    [HttpGet("product/{productId:int}")]
    [ProducesResponseType(typeof(List<ProductBatchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductBatchDto>>> GetAvailableBatches(int productId)
        => Ok(await _batches.GetAvailableAsync(productId));

    [HttpGet("expiry-report")]
    [ProducesResponseType(typeof(List<ProductBatchDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductBatchDto>>> GetExpiryReport(
        [FromQuery] int? withinDays, [FromQuery] bool includeExpired = true)
        => Ok(await _batches.GetExpiryReportAsync(withinDays, includeExpired));
}