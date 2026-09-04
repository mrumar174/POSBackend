using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Tenancy;
using POS.Entities.IServices;

namespace POS.Web.Controllers
{
    // NOTE: Tenant management is platform-admin territory. Once you have a
    // "SuperAdmin" role/policy, lock GetAll/Update/Delete down with it —
    // right now any authenticated user from any tenant can call them.
    [ApiController]
    //[Authorize]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [AllowAnonymous] // signup flow: a brand-new tenant has no token yet
        [HttpPost]
        public async Task<ActionResult<TenantDto>> Create(CreateTenantDto dto)
        {
            try
            {
                var created = await _tenantService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<TenantDto>>> GetAll()
            => Ok(await _tenantService.GetAllAsync());

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TenantDto>> GetById(int id)
        {
            var password = BCrypt.Net.BCrypt.HashPassword("123456");
            var tenant = await _tenantService.GetByIdAsync(id);
            return tenant is null ? NotFound() : Ok(tenant);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<TenantDto>> Update(int id, UpdateTenantDto dto)
        {
            if (id != dto.Id) return BadRequest("Route id does not match body id.");

            try
            {
                return Ok(await _tenantService.UpdateAsync(dto));
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

        [HttpPatch("subscription")]
        public async Task<ActionResult<TenantDto>> UpdateSubscription(UpdateSubscriptionDto dto)
        {
            try
            {
                return Ok(await _tenantService.UpdateSubscriptionAsync(dto));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _tenantService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}