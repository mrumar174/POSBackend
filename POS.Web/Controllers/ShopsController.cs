using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Tenancy;
using POS.Entities.IServices;

namespace POS.Web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ShopsController : ControllerBase
    {
        private readonly IShopService _shopService;

        public ShopsController(IShopService shopService)
        {
            _shopService = shopService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ShopDto>>> GetAll()
            => Ok(await _shopService.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ShopDto>> GetById(int id)
        {
            var shop = await _shopService.GetByIdAsync(id);
            return shop is null ? NotFound() : Ok(shop);
        }

        [HttpPost]
        public async Task<ActionResult<ShopDto>> Create(CreateShopDto dto)
        {
            try
            {
                var created = await _shopService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ShopDto>> Update(int id, UpdateShopDto dto)
        {
            if (id != dto.Id) return BadRequest("Route id does not match body id.");

            try
            {
                return Ok(await _shopService.UpdateAsync(dto));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _shopService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        // ---------- User <-> Shop assignment ----------

        [HttpPost("assign-user")]
        public async Task<IActionResult> AssignUser(AssignUserToShopDto dto)
        {
            try
            {
                await _shopService.AssignUserAsync(dto);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{shopId:int}/users/{userId:int}")]
        public async Task<IActionResult> UnassignUser(int shopId, int userId)
        {
            try
            {
                await _shopService.UnassignUserAsync(userId, shopId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPatch("{shopId:int}/users/{userId:int}/set-default")]
        public async Task<IActionResult> SetDefault(int shopId, int userId)
        {
            try
            {
                await _shopService.SetDefaultShopAsync(userId, shopId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}