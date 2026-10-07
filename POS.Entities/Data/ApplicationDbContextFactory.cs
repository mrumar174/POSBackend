using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using POS.Entities.Common;

namespace POS.Entities.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "POS.Web");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "DefaultConnection was not found in POS.Web/appsettings.json.");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(
                optionsBuilder.Options,
                new DesignTimeCurrentUserService());
        }
    }

    internal class DesignTimeCurrentUserService : ICurrentUserService
    {
        public int TenantId => 1;

        public int? ShopId => 1;

        public int? UserId => 1;

        public bool IsAuthenticated => false;

        public bool IsPlatformAdmin => false;
    }
}