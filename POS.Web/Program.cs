using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.Services;
using POS.Web.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// Controllers + AutoMapper
// ---------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddAutoMapper(cfg => { }, typeof(Program).Assembly);

// ---------------------------------------------------------------
// EF Core + multi-tenant current-user resolver
// ---------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------
// Application services (add one line per service as you build it —
// see the Tenancy & Identity roadmap for the full list/order)
// ---------------------------------------------------------------
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITenantService, POS.Entities.IServices.TenantService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserService, UserService>();
// builder.Services.AddScoped<IAuthService, AuthService>();

// ---------------------------------------------------------------
// JWT Authentication — required because controllers use [Authorize].
// ICurrentUserService reads tenant_id/shop_id claims from this token.
// ---------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing from appsettings.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------
// Swagger — with a Bearer-token box so [Authorize] endpoints are
// testable directly from the UI (Authorize button -> paste "<token>").
// ---------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "POS API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste just the token — Swagger adds the 'Bearer ' prefix for you."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
        policy.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddSingleton<IAuthorizationPolicyProvider, POS.Web.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, POS.Web.Authorization.PermissionAuthorizationHandler>();
var app = builder.Build();

// ---------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "POS API v1");
    options.RoutePrefix = string.Empty; // Swagger UI is served at "/" so it's the first thing you see on run
});

app.UseHttpsRedirection();
app.UseCors("AllowAngularDev");
app.UseAuthentication(); // must run before UseAuthorization — ICurrentUserService depends on this populating HttpContext.User
app.UseAuthorization();

app.MapControllers();

app.Run();