using POS.DTOs.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices
{
    public interface IPermissionService
    {
        Task<List<PermissionDto>> GetAllAsync();
        Task<List<PermissionDto>> GetByModuleAsync(string module);
        Task<PermissionDto?> GetByIdAsync(int id);
        Task<PermissionDto> CreateAsync(CreatePermissionDto dto);
        Task<List<PermissionDto>> CreateManyAsync(List<CreatePermissionDto> dtos);
    }
}
