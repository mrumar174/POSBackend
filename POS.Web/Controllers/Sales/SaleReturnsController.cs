using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Common;
using POS.DTOs.Sales;
using POS.Entities.IServices.Sales;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SaleReturnsController : ControllerBase
    {
        private readonly ISaleReturnService _saleReturnService;

        public SaleReturnsController(ISaleReturnService saleReturnService)
        {
            _saleReturnService = saleReturnService;
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResultDto<SaleReturnDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResultDto<SaleReturnDto>>> Search([FromQuery] SaleReturnQueryDto query)
            => Ok(await _saleReturnService.SearchAsync(query));

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SaleReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleReturnDto>> GetById(int id)
        {
            var ret = await _saleReturnService.GetByIdAsync(id);
            return ret is null ? NotFound() : Ok(ret);
        }

        [RequirePermission("SaleReturns.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(SaleReturnDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SaleReturnDto>> Create(CreateSaleReturnDto dto)
        {
            try
            {
                var created = await _saleReturnService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("SaleReturns.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(SaleReturnDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<SaleReturnDto>> Update(int id, CreateSaleReturnDto dto)
        {
            try { return Ok(await _saleReturnService.UpdateAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("SaleReturns.Delete")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _saleReturnService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}