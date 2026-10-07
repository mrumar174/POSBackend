using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Finance;
using POS.Entities.IServices.Finance;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ExpenseCategoriesController : ControllerBase
    {
        private readonly IExpenseCategoryService _service;
        public ExpenseCategoriesController(IExpenseCategoryService service) => _service = service;

        [HttpGet]
        [ProducesResponseType(typeof(List<ExpenseCategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ExpenseCategoryDto>>> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ExpenseCategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpenseCategoryDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [RequirePermission("ExpenseCategories.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(ExpenseCategoryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ExpenseCategoryDto>> Create(CreateExpenseCategoryDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("ExpenseCategories.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ExpenseCategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpenseCategoryDto>> Update(int id, UpdateExpenseCategoryDto dto)
        {
            if (id != dto.Id) return BadRequest(new { message = "Id mismatch." });
            try { return Ok(await _service.UpdateAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("ExpenseCategories.Delete")]
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