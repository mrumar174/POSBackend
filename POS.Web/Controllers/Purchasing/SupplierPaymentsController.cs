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
    public class SupplierPaymentsController : ControllerBase
    {
        private readonly ISupplierPaymentService _service;

        public SupplierPaymentsController(ISupplierPaymentService service)
        {
            _service = service;
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResultDto<SupplierPaymentDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<SupplierPaymentDto>>> Search(
            [FromQuery] int? supplierId,
            [FromQuery] int? purchaseId,
            [FromQuery] string? paymentNo,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _service.SearchAsync(new SupplierPaymentQueryDto
            {
                SupplierId = supplierId,
                PurchaseId = purchaseId,
                PaymentNo = paymentNo,
                FromDate = fromDate,
                ToDate = toDate,
                Page = page,
                PageSize = pageSize
            });
            return Ok(result);
        }

        [HttpGet("unpaid-purchases")]
        [ProducesResponseType(typeof(List<UnpaidPurchaseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UnpaidPurchaseDto>>> GetUnpaidPurchases(
            [FromQuery] int? supplierId,
            [FromQuery] int? excludePaymentId)
            => Ok(await _service.GetUnpaidPurchasesAsync(supplierId, excludePaymentId));

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SupplierPaymentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupplierPaymentDto>> GetById(int id)
        {
            var payment = await _service.GetByIdAsync(id);
            return payment is null ? NotFound() : Ok(payment);
        }

        [RequirePermission("SupplierPayments.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(SupplierPaymentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SupplierPaymentDto>> Create(CreateSupplierPaymentDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("SupplierPayments.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(SupplierPaymentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SupplierPaymentDto>> Update(int id, CreateSupplierPaymentDto dto)
        {
            try
            {
                return Ok(await _service.UpdateAsync(id, dto));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("SupplierPayments.Delete")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}