using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Common;
using POS.DTOs.Purchasing;
using POS.Entities.IServices;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PurchaseReturnsController : ControllerBase
    {
        private readonly IPurchaseReturnService _returnService;
        private readonly IPurchaseService _purchaseService; // reused directly for the two lookup endpoints below

        public PurchaseReturnsController(IPurchaseReturnService returnService, IPurchaseService purchaseService)
        {
            _returnService = returnService;
            _purchaseService = purchaseService;
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResultDto<PurchaseReturnDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<PurchaseReturnDto>>> Search(
            [FromQuery] string? returnNo,
            [FromQuery] int? supplierId,
            [FromQuery] int? purchaseId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _returnService.SearchAsync(new PurchaseReturnQueryDto
            {
                ReturnNo = returnNo,
                SupplierId = supplierId,
                PurchaseId = purchaseId,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize
            });
            return Ok(result);
        }

        // Reuses IPurchaseService directly — the dropdown of purchases to
        // return against, scoped to the chosen supplier.
        [HttpGet("purchases-by-supplier")]
        [ProducesResponseType(typeof(List<PurchaseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PurchaseDto>>> GetPurchasesBySupplier([FromQuery] int supplierId)
            => Ok(await _purchaseService.GetBySupplierAsync(supplierId));

        // Reuses IPurchaseService directly — once a purchase is picked, this
        // returns its full item list so the return form can prefill rows.
        [HttpGet("purchase-items/{purchaseId:int}")]
        [ProducesResponseType(typeof(PurchaseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PurchaseDto>> GetPurchaseItems(int purchaseId)
        {
            var purchase = await _purchaseService.GetByIdAsync(purchaseId);
            return purchase is null ? NotFound() : Ok(purchase);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PurchaseReturnDto>> GetById(int id)
        {
            var purchaseReturn = await _returnService.GetByIdAsync(id);
            return purchaseReturn is null ? NotFound() : Ok(purchaseReturn);
        }

        [RequirePermission("PurchaseReturns.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PurchaseReturnDto>> Create(CreatePurchaseReturnDto dto)
        {
            try
            {
                var created = await _returnService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("PurchaseReturns.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(PurchaseReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PurchaseReturnDto>> Update(int id, CreatePurchaseReturnDto dto)
        {
            try
            {
                return Ok(await _returnService.UpdateAsync(id, dto));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("PurchaseReturns.Delete")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _returnService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}