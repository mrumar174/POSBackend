using POS.DTOs.Auth;

namespace POS.Entities.IServices
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> SignupAsync(TenantSignupDto dto);
    }
}