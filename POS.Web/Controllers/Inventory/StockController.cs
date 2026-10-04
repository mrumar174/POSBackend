using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Inventory;
using POS.Entities.IServices;
using POS.Entities.IServices.Inventory;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        // ==========================================
        // 1. Stock Summary / Read-Only
        // ==========================================

        [HttpGet("summary")]
        [ProducesResponseType(typeof(List<StockSummaryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<StockSummaryDto>>> GetSummary(
            [FromQuery] int? categoryId,
            [FromQuery] int? brandId,
            [FromQuery] string? search,
            [FromQuery] bool lowStockOnly = false)
        {
            return Ok(await _stockService.GetSummaryAsync(categoryId, brandId, search, lowStockOnly));
        }

        [HttpGet("{productId:int}/balance")]
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        public async Task<ActionResult<decimal>> GetBalance(int productId)
        {
            return Ok(await _stockService.GetProductStockAsync(productId));
        }

        // ==========================================
        // 2. Stock Adjustments
        // ==========================================

        [HttpGet("adjustments")]
        [ProducesResponseType(typeof(List<StockAdjustmentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<StockAdjustmentDto>>> GetAllAdjustments()
            => Ok(await _stockService.GetAllAdjustmentsAsync());

        [HttpGet("adjustments/{id:int}")]
        [ProducesResponseType(typeof(StockAdjustmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StockAdjustmentDto>> GetAdjustmentById(int id)
        {
            var adj = await _stockService.GetAdjustmentByIdAsync(id);
            return adj is null ? NotFound() : Ok(adj);
        }

        [RequirePermission("StockAdjustments.Create")]
        [HttpPost("adjustments")]
        [ProducesResponseType(typeof(StockAdjustmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<StockAdjustmentDto>> CreateAdjustment(CreateStockAdjustmentDto dto)
        {
            try
            {
                var created = await _stockService.CreateAdjustmentAsync(dto);
                return CreatedAtAction(nameof(GetAdjustmentById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("StockAdjustments.Edit")]
        [HttpPut("adjustments/{id:int}")]
        [ProducesResponseType(typeof(StockAdjustmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StockAdjustmentDto>> UpdateAdjustment(int id, CreateStockAdjustmentDto dto)
        {
            try { return Ok(await _stockService.UpdateAdjustmentAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("StockAdjustments.Delete")]
        [HttpDelete("adjustments/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAdjustment(int id)
        {
            try
            {
                await _stockService.DeleteAdjustmentAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}