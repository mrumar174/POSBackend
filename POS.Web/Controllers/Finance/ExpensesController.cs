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
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _service;
        public ExpensesController(IExpenseService service) => _service = service;

        // Named GetAll so NSwag generates `expensesAll(...)`, matching your other clients.
        [HttpGet]
        [ProducesResponseType(typeof(ExpensePageDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<ExpensePageDto>> GetAll([FromQuery] ExpenseQueryDto query)
            => Ok(await _service.GetPagedAsync(query));

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpenseDto>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }

        [RequirePermission("Expenses.Create")]
        [HttpPost]
        [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ExpenseDto>> Create(CreateExpenseDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Expenses.Edit")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ExpenseDto>> Update(int id, CreateExpenseDto dto)
        {
            try { return Ok(await _service.UpdateAsync(id, dto)); }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Expenses.Delete")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try { await _service.DeleteAsync(id); return NoContent(); }
            catch (KeyNotFoundException) { return NotFound(); }
        }
    }
}