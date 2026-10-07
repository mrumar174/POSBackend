using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Dashboard;
using POS.Entities.IServices.Dashboard;

namespace POS.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<DashboardDto>> Get()
        {
            return Ok(await _dashboardService.GetDashboardDataAsync());
        }
    }
}