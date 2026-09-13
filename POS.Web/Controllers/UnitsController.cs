using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Catalog;
using POS.Entities.IServices;
using POS.Entities.IServices.Catalog;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UnitsController : ControllerBase
    {
        private readonly IUnitService _unitService;

        public UnitsController(IUnitService unitService)
        {
            _unitService = unitService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<UnitDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UnitDto>>> GetAll()
            => Ok(await _unitService.GetAllAsync());

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UnitDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UnitDto>> GetById(int id)
        {
            var unit = await _unitService.GetByIdAsync(id);
            return unit is null ? NotFound() : Ok(unit);
        }

        [RequirePermission("Units.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(UnitDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UnitDto>> Create(CreateUnitDto dto)
        {
            try
            {
                var created = await _unitService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Units.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(UnitDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UnitDto>> Update(int id, UpdateUnitDto dto)
        {
            if (id != dto.Id) return BadRequest(new { message = "Route id and body id must match." });

            try
            {
                return Ok(await _unitService.UpdateAsync(dto));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Units.Delete")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _unitService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}