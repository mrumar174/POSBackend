using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Auth;
using POS.Entities.Common;
using POS.Entities.IServices;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ICurrentUserService _currentUser;

        public AuthController(IAuthService authService, IUserService userService, ICurrentUserService currentUser)
        {
            _authService = authService;
            _userService = userService;
            _currentUser = currentUser;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        // Handy for confirming a pasted token actually works in Swagger:
        // Authorize with the token, then hit GET /api/Auth/me.
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (_currentUser.UserId is not int userId)
                return Unauthorized();

            var user = await _userService.GetByIdAsync(userId);
            return user is null ? NotFound() : Ok(user);
        }
        [AllowAnonymous]
        [HttpPost("signup")]
        public async Task<ActionResult<AuthResponseDto>> Signup(TenantSignupDto dto)
        {
            try
            {
                var result = await _authService.SignupAsync(dto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }
    }
}