using AutoMapper;
using POS.DTOs.Identity;
using POS.Entities.Identity;

namespace POS.Web.Mapping
{
    public class IdentityProfile : Profile
    {
        public IdentityProfile()
        {
            // Requires User queried with .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            // and .Include(u => u.UserShops) for Roles/ShopIds to populate.
            CreateMap<User, UserDto>()
                .ForMember(d => d.Roles, o => o.MapFrom(s => s.UserRoles.Select(ur => ur.Role.Name)))
                .ForMember(d => d.ShopIds, o => o.MapFrom(s => s.UserShops.Select(us => us.ShopId)));

            // PasswordHash is deliberately NOT mapped here. In the service:
            //   var user = _mapper.Map<User>(dto);
            //   user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            // RoleIds/ShopIds also aren't simple properties on User — build the
            // UserRole/UserShop join rows explicitly in the service after mapping.
            CreateMap<CreateUserDto, User>()
                .ForMember(d => d.UserRoles, o => o.Ignore())
                .ForMember(d => d.UserShops, o => o.Ignore());

            CreateMap<UpdateUserDto, User>()
                .ForMember(d => d.UserRoles, o => o.Ignore())
                .ForMember(d => d.UserShops, o => o.Ignore());

            // Requires Role queried with .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            CreateMap<Role, RoleDto>()
                .ForMember(d => d.Permissions, o => o.MapFrom(s => s.RolePermissions.Select(rp => rp.Permission.Name)));

            CreateMap<CreateRoleDto, Role>()
                .ForMember(d => d.RolePermissions, o => o.Ignore()); // build from PermissionIds in the service

            CreateMap<UpdateRoleDto, Role>()
                .ForMember(d => d.RolePermissions, o => o.Ignore());

            CreateMap<Permission, PermissionDto>();
        }
    }
}
