using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Settings;
using POS.Entities.IServices;
using POS.Entities.IServices.Settings;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentMethodsController : ControllerBase
    {
        private readonly IPaymentMethodService _service;

        public PaymentMethodsController(IPaymentMethodService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<PaymentMethodDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PaymentMethodDto>>> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaymentMethodDto>> GetById(int id)
        {
            var m = await _service.GetByIdAsync(id);
            return m is null ? NotFound() : Ok(m);
        }

        [RequirePermission("PaymentMethods.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PaymentMethodDto>> Create(CreatePaymentMethodDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("PaymentMethods.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(PaymentMethodDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PaymentMethodDto>> Update(int id, UpdatePaymentMethodDto dto)
        {
            if (id != dto.Id) return BadRequest(new { message = "Route id and body id must match." });
            try
            {
                return Ok(await _service.UpdateAsync(dto));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("PaymentMethods.Delete")]
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