using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Identity;
using POS.Entities.IServices;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<List<PermissionDto>>> GetAll([FromQuery] string? module)
        {
            var result = string.IsNullOrWhiteSpace(module)
                ? await _permissionService.GetAllAsync()
                : await _permissionService.GetByModuleAsync(module);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PermissionDto>> GetById(int id)
        {
            var permission = await _permissionService.GetByIdAsync(id);
            return permission is null ? NotFound() : Ok(permission);
        }

        // Anonymous ONLY so the very first deployment can seed permissions
        // before any user/token exists. Duplicate names are rejected, so
        // re-running this after go-live is safe but pointless. Once you have
        // a SuperAdmin role, switch this to [Authorize(Roles = "SuperAdmin")]
        // and seed permissions through Step 1's login flow instead.
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<PermissionDto>> Create(CreatePermissionDto dto)
        {
            try
            {
                var created = await _permissionService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("bulk")]
        public async Task<ActionResult<List<PermissionDto>>> CreateMany(List<CreatePermissionDto> dtos)
            => Ok(await _permissionService.CreateManyAsync(dtos));
    }
}