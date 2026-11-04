// Put in: POS.Entities/IServices/Backup/IBackupService.cs
using POS.DTOs.Backup;

namespace POS.Entities.IServices.Backup
{
    public interface IBackupService
    {
        BackupSettingsDto GetSettings();
        BackupSettingsDto SaveSettings(BackupSettingsDto dto);

        List<BackupFileDto> GetBackups();

        /// <summary>Starts a background backup and returns immediately. Throws if one is already running.</summary>
        BackupJobDto StartManualBackup();
        BackupJobDto GetJob(string jobId);
        /// <summary>The running job, or a job with Status = "Idle" when nothing is running.</summary>
        BackupJobDto GetCurrentJob();

        /// <summary>Validates the name and returns the full path. Throws if invalid or missing.</summary>
        string GetBackupFilePath(string fileName);
        void DeleteBackup(string fileName);

        /// <summary>Called every minute by the hosted service.</summary>
        void TryRunAutoBackup();
    }
}