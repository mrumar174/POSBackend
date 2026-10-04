using POS.DTOs.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POS.Entities.IServices.Settings
{
    public interface ICompanySettingsService
    {
        Task<CompanySettingsDto> GetAsync();
        Task<CompanySettingsDto> UpdateAsync(UpdateCompanySettingsDto dto);
    }
}
