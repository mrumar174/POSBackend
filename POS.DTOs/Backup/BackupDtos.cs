// Put in: POS.DTOs/Backup/BackupDtos.cs
using System.ComponentModel.DataAnnotations;

namespace POS.DTOs.Backup
{
    /// <summary>Job states are plain strings so the generated TypeScript client stays readable.</summary>
    public static class BackupJobStatus
    {
        public const string Idle = "Idle";
        public const string Running = "Running";
        public const string Completed = "Completed";
        public const string Failed = "Failed";
    }

    public class BackupFileDto
    {
        public string FileName { get; set; } = default!;
        public long SizeBytes { get; set; }
        public DateTime CreatedOn { get; set; }
        /// <summary>"Manual", "Auto" or "Other"</summary>
        public string Type { get; set; } = "Other";
    }

    public class BackupJobDto
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = BackupJobStatus.Idle;
        public int PercentComplete { get; set; }
        public string? Message { get; set; }
        public string? FileName { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime? CompletedOn { get; set; }
        public string? Error { get; set; }
    }

    public class BackupSettingsDto
    {
        public bool AutoBackupEnabled { get; set; } = true;

        /// <summary>24-hour "HH:mm", e.g. "23:00".</summary>
        [Required]
        public string BackupTime { get; set; } = "23:00";

        /// <summary>Automatic backups older than this many days are deleted. Manual backups are never deleted automatically.</summary>
        [Range(1, 3650)]
        public int RetentionDays { get; set; } = 14;

        // Read-only information (ignored when saving)
        public string? BackupFolder { get; set; }
        public DateTime? LastAutoBackupOn { get; set; }
    }
}