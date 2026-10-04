using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Settings;
using POS.Entities.IServices.Settings;
using POS.Web.Authorization;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompanySettingsController : ControllerBase
    {
        private readonly ICompanySettingsService _companySettingsService;

        public CompanySettingsController(ICompanySettingsService companySettingsService)
        {
            _companySettingsService = companySettingsService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CompanySettingsDto>> Get()
        {
            return Ok(await _companySettingsService.GetAsync());
        }

        [RequirePermission("CompanySettings.Edit")]
        [HttpPut]
        [ProducesResponseType(typeof(CompanySettingsDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<CompanySettingsDto>> Update(UpdateCompanySettingsDto dto)
        {
            return Ok(await _companySettingsService.UpdateAsync(dto));
        }
    }
}