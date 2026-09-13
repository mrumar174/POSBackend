using POS.DTOs.Auth;

namespace POS.Entities.IServices.Identity
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> SignupAsync(TenantSignupDto dto);
    }
}