// Put in: POS.Web/Controllers/Backup/BackupsController.cs
//
// A backup contains EVERY tenant's data (and password hashes), so keep this restricted.
// Create a permission named "Backups.Manage" (Permissions screen) and give it only to SuperAdmin.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.DTOs.Backup;
using POS.Entities.IServices.Backup;
using POS.Web.Authorization;

namespace POS.Web.Controllers.Backup
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BackupsController : ControllerBase
    {
        private readonly IBackupService _backups;

        public BackupsController(IBackupService backups) => _backups = backups;

        // Action names are unique on purpose so the NSwag client gets clean method names:
        // getBackups, startManualBackup, getBackupJob, getCurrentBackup, getBackupSettings,
        // saveBackupSettings, downloadBackup, deleteBackup

        [RequirePermission("Backups.Manage")]
        [HttpGet]
        [ProducesResponseType(typeof(List<BackupFileDto>), StatusCodes.Status200OK)]
        public ActionResult<List<BackupFileDto>> GetBackups()
            => Ok(_backups.GetBackups());

        [RequirePermission("Backups.Manage")]
        [HttpPost]
        [ProducesResponseType(typeof(BackupJobDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<BackupJobDto> StartManualBackup()
        {
            try { return Ok(_backups.StartManualBackup()); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Backups.Manage")]
        [HttpGet("jobs/{jobId}")]
        [ProducesResponseType(typeof(BackupJobDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<BackupJobDto> GetBackupJob(string jobId)
        {
            try { return Ok(_backups.GetJob(jobId)); }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        [RequirePermission("Backups.Manage")]
        [HttpGet("current")]
        [ProducesResponseType(typeof(BackupJobDto), StatusCodes.Status200OK)]
        public ActionResult<BackupJobDto> GetCurrentBackup()
            => Ok(_backups.GetCurrentJob());

        [RequirePermission("Backups.Manage")]
        [HttpGet("settings")]
        [ProducesResponseType(typeof(BackupSettingsDto), StatusCodes.Status200OK)]
        public ActionResult<BackupSettingsDto> GetBackupSettings()
            => Ok(_backups.GetSettings());

        [RequirePermission("Backups.Manage")]
        [HttpPut("settings")]
        [ProducesResponseType(typeof(BackupSettingsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<BackupSettingsDto> SaveBackupSettings(BackupSettingsDto dto)
        {
            try { return Ok(_backups.SaveSettings(dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        // File names are passed in the query string (not the path) because some servers (IIS)
        // block request paths that end in ".bak".
        [RequirePermission("Backups.Manage")]
        [HttpGet("download")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DownloadBackup([FromQuery] string fileName)
        {
            try
            {
                var path = _backups.GetBackupFilePath(fileName);
                var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                return File(stream, "application/octet-stream", fileName);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [RequirePermission("Backups.Manage")]
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult DeleteBackup([FromQuery] string fileName)
        {
            try
            {
                _backups.DeleteBackup(fileName);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}