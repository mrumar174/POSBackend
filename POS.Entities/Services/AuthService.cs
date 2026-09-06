using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using POS.DTOs.Auth;
using POS.DTOs.Identity;
using POS.Entities.Data;
using POS.Entities.Identity;
using POS.Entities.IServices;
using POS.Entities.Tenancy;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace POS.Entities.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext db, IMapper mapper, IConfiguration config)
        {
            _db = db;
            _mapper = mapper;
            _config = config;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _db.Users
                .IgnoreQueryFilters()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
                .Include(u => u.UserShops)
                .FirstOrDefaultAsync(u =>
                    u.TenantId == dto.TenantId &&
                    u.UserName.ToLower() == dto.UserName.ToLower());

            if (user is null)
                throw new UnauthorizedAccessException("Invalid username.");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid password.");

            if (!user.UserShops.Any())
                throw new UnauthorizedAccessException("This user is not assigned to any shop.");

            int shopId;
            if (dto.ShopId.HasValue)
            {
                var hasAccess = user.UserShops.Any(us => us.ShopId == dto.ShopId.Value);
                if (!hasAccess)
                    throw new UnauthorizedAccessException("You do not have access to the requested shop.");
                shopId = dto.ShopId.Value;
            }
            else
            {
                shopId = user.UserShops.First().ShopId;
            }

            var roles = user.UserRoles.Select(ur => ur.Role).ToList();
            var permissions = roles
                .SelectMany(r => r.RolePermissions.Select(rp => rp.Permission.Name))
                .Distinct()
                .ToList();

            var (token, expiresAtUtc) = GenerateToken(user.Id, dto.TenantId, shopId, user.UserName, roles.Select(r => r.Name), permissions);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                TenantId = dto.TenantId,
                ShopId = shopId,
                User = _mapper.Map<UserDto>(user)
            };
        }
        private (string token, DateTime expiresAtUtc) GenerateToken(
            int userId, int tenantId, int shopId, string userName,
            IEnumerable<string> roles, IEnumerable<string> permissions)
        {
            var jwtKey = _config["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is missing from appsettings.json.");
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            var expiryMinutes = _config.GetValue<int?>("Jwt:ExpiryMinutes") ?? 480;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.Name, userName),
                new("tenant_id", tenantId.ToString()),
                new("shop_id", shopId.ToString()),
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            claims.AddRange(permissions.Select(p => new Claim("permission", p)));

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
        }
        public async Task<AuthResponseDto> SignupAsync(TenantSignupDto dto)
        {
            var slug = await GenerateUniqueSlugAsync(dto.BusinessName);
            var tenantCode = await GenerateNextTenantCodeAsync();

            var tenant = new Tenant
            {
                Code = tenantCode,
                BusinessName = dto.BusinessName,
                OwnerName = dto.OwnerName,
                ContactNo = dto.ContactNo,
                Email = dto.Email,
                Address = dto.Address,
                City = dto.City,
                Slug = slug,
                SubscriptionStartDate = DateTime.Now
            };

            var shop = new Shop
            {
                Code = "SHOP-00001",
                Name = string.IsNullOrWhiteSpace(dto.MainShopName) ? "Main Branch" : dto.MainShopName,
                Address = dto.Address,
                City = dto.City,
                ContactNo = dto.ContactNo,
                IsMainBranch = true,
                InvoicePrefix = tenantCode
            };
            tenant.Shops.Add(shop);

            var adminUser = new User
            {
                UserName = dto.AdminUserName,
                FullName = dto.AdminFullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminPassword)
            };
            adminUser.UserShops.Add(new UserShop { Shop = shop, IsDefault = true });
            tenant.Users.Add(adminUser);

            // Tenant, Shop, User, and UserShop all save together in one transaction —
            // if anything fails, nothing is half-created.
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            // Log the new admin straight in — no separate login call needed after signup.
            var (token, expiresAtUtc) = GenerateToken(
                adminUser.Id, tenant.Id, shop.Id, adminUser.UserName,
                roles: Enumerable.Empty<string>(),        // no role assigned yet — attach one via /api/Roles + PUT /api/Users/{id}
                permissions: Enumerable.Empty<string>());

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                TenantId = tenant.Id,
                ShopId = shop.Id,
                User = new POS.DTOs.Identity.UserDto
                {
                    Id = adminUser.Id,
                    UserName = adminUser.UserName,
                    FullName = adminUser.FullName,
                    Roles = new List<string>(),
                    ShopIds = new List<int> { shop.Id }
                }
            };
        }

        private async Task<string> GenerateUniqueSlugAsync(string businessName)
        {
            var baseSlug = new string(businessName.ToLower()
                    .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                    .ToArray())
                .Trim('-');
            while (baseSlug.Contains("--")) baseSlug = baseSlug.Replace("--", "-");

            var slug = baseSlug;
            var suffix = 1;
            while (await _db.Tenants.AnyAsync(t => t.Slug == slug))
                slug = $"{baseSlug}-{suffix++}";

            return slug;
        }

        private async Task<string> GenerateNextTenantCodeAsync()
        {
            var count = await _db.Tenants.CountAsync();
            return $"TEN-{(count + 1):D5}";
        }
    }
}