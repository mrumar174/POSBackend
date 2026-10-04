using Microsoft.EntityFrameworkCore;
using POS.DTOs.Settings;
using POS.Entities.Common;
using POS.Entities.Data;
using POS.Entities.IServices;
using POS.Entities.IServices.Settings;
using POS.Entities.Settings;

namespace POS.Entities.Services.Settings
{
    public class CompanySettingsService : ICompanySettingsService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public CompanySettingsService(ApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<CompanySettingsDto> GetAsync()
        {
            // Global query filters automatically scope this to the current Tenant
            var settings = await _db.CompanySettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new CompanySettings
                {
                    CompanyName = "My Company",
                    Currency = "PKR"
                };

                _db.CompanySettings.Add(settings);
                await _db.SaveChangesAsync();
            }

            return new CompanySettingsDto
            {
                Id = settings.Id,
                CompanyName = settings.CompanyName,
                Address = settings.Address,
                ContactNo = settings.ContactNo,
                Email = settings.Email,
                TaxNumber = settings.TaxNumber,
                Currency = settings.Currency,
                ReceiptHeader = settings.ReceiptHeader,
                ReceiptFooter = settings.ReceiptFooter,
                LogoPath = settings.LogoPath
            };
        }

        public async Task<CompanySettingsDto> UpdateAsync(UpdateCompanySettingsDto dto)
        {
            var settings = await _db.CompanySettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new CompanySettings();
                _db.CompanySettings.Add(settings);
            }

            settings.CompanyName = dto.CompanyName;
            settings.Address = dto.Address;
            settings.ContactNo = dto.ContactNo;
            settings.Email = dto.Email;
            settings.TaxNumber = dto.TaxNumber;
            settings.Currency = dto.Currency;
            settings.ReceiptHeader = dto.ReceiptHeader;
            settings.ReceiptFooter = dto.ReceiptFooter;
            settings.LogoPath = dto.LogoPath;

            await _db.SaveChangesAsync();

            return new CompanySettingsDto
            {
                Id = settings.Id,
                CompanyName = settings.CompanyName,
                Address = settings.Address,
                ContactNo = settings.ContactNo,
                Email = settings.Email,
                TaxNumber = settings.TaxNumber,
                Currency = settings.Currency,
                ReceiptHeader = settings.ReceiptHeader,
                ReceiptFooter = settings.ReceiptFooter,
                LogoPath = settings.LogoPath
            };
        }
    }
}