using POS.DTOs.Identity;

namespace POS.Entities.IServices
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserDto dto);
        Task<UserDto> UpdateAsync(UpdateUserDto dto);
        Task DeleteAsync(int id);
        Task ChangePasswordAsync(ChangePasswordDto dto);
    }
}