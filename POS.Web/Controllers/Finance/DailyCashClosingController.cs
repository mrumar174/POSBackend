using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Finance;
using POS.Entities.IServices.Finance;
using POS.Web.Authorization;

namespace POS.Web.Controllers.Finance
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DailyCashClosingController : ControllerBase
    {
        private readonly IDailyCashClosingService _service;
        public DailyCashClosingController(IDailyCashClosingService service) => _service = service;

        [HttpGet]
        [ProducesResponseType(typeof(List<DailyCashClosingDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<DailyCashClosingDto>>> GetAll([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => Ok(await _service.GetAllAsync(fromDate, toDate));

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(DailyCashClosingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<DailyCashClosingDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        // Preview only: computes, never saves.
        [RequirePermission("DailyCashClosing.Create")]
        [HttpPost("calculate")]
        [ProducesResponseType(typeof(DailyCashClosingPreviewDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DailyCashClosingPreviewDto>> Calculate([FromQuery] DateTime date)
        {
            try { return Ok(await _service.CalculateAsync(date)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("DailyCashClosing.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(DailyCashClosingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<DailyCashClosingDto>> Create(CreateDailyCashClosingDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        // "Reopen": deliberately no PUT exists. Latest closing only.
        [RequirePermission("DailyCashClosing.Reopen")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try { await _service.DeleteAsync(id); return NoContent(); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
