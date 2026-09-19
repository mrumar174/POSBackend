using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Purchasing;
using POS.Entities.IServices;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PurchasesController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;

        public PurchasesController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<PurchaseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PurchaseDto>>> GetAll() => Ok(await _purchaseService.GetAllAsync());

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PurchaseDto>> GetById(int id)
        {
            var purchase = await _purchaseService.GetByIdAsync(id);
            return purchase is null ? NotFound() : Ok(purchase);
        }

        [RequirePermission("Purchases.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PurchaseDto>> Create(CreatePurchaseDto dto)
        {
            try
            {
                var created = await _purchaseService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Purchases.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PurchaseDto>> Update(int id, CreatePurchaseDto dto)
        {
            try
            {
                return Ok(await _purchaseService.UpdateAsync(id, dto));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Purchases.Delete")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _purchaseService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}