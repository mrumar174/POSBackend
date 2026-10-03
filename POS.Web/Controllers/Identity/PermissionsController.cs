using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Identity;
using POS.Entities.IServices.Identity;
using POS.Web.Authorization; // Added to enable RequirePermission

namespace POS.Web.Controllers.Identity
{
    [ApiController]
    [Authorize] // Applied globally to the controller
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet]
        public async Task<ActionResult<List<PermissionDto>>> GetAll([FromQuery] string? module)
        {
            var result = string.IsNullOrWhiteSpace(module)
                ? await _permissionService.GetAllAsync()
                : await _permissionService.GetByModuleAsync(module);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PermissionDto>> GetById(int id)
        {
            var permission = await _permissionService.GetByIdAsync(id);
            return permission is null ? NotFound() : Ok(permission);
        }

        [RequirePermission("Permissions.Create")] // Replaced [AllowAnonymous] with Permission check
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

        [RequirePermission("Permissions.Create")] // Replaced [AllowAnonymous] with Permission check
        [HttpPost("bulk")]
        public async Task<ActionResult<List<PermissionDto>>> CreateMany(List<CreatePermissionDto> dtos)
            => Ok(await _permissionService.CreateManyAsync(dtos));
    }
}