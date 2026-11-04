// Put in: POS.Entities/Services/Backup/BackupService.cs
//
// Registered as a SINGLETON (job progress lives in memory), so it must not depend on
// scoped services such as ApplicationDbContext. It talks to SQL Server directly.
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using POS.DTOs.Backup;
using POS.Entities.IServices.Backup;

namespace POS.Entities.Services.Backup
{
    public class BackupService : IBackupService
    {
        private const string SettingsFileName = "backup-settings.json";
        private const int PercentProcessedMessageNumber = 3211; // SQL Server "%d percent processed." (language independent)

        private static readonly Regex FileNameRegex = new(@"^[A-Za-z0-9_\-\.]+\.bak$", RegexOptions.Compiled);

        private readonly IConfiguration _config;
        private readonly ILogger<BackupService> _logger;

        private readonly object _jobLock = new();
        private readonly object _settingsLock = new();
        private readonly ConcurrentDictionary<string, BackupJobDto> _jobs = new();
        private string? _currentJobId;
        private string? _folder;
        private DateTime _lastAutoAttempt = DateTime.MinValue;

        public BackupService(IConfiguration config, ILogger<BackupService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ============================================================
        // Connection + folder
        // ============================================================

        // If your connection string has a different name, change it here.
        private string ConnectionString =>
            _config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        private string DatabaseName => new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;

        /// <summary>
        /// Backup folder: "Backup:Folder" from appsettings, otherwise SQL Server's own default backup
        /// folder (which the SQL Server service account is always allowed to write to).
        /// NOTE: the folder is written by SQL Server and read/deleted by this API, so both need access.
        /// </summary>
        private string GetFolder()
        {
            if (_folder != null) return _folder;

            var configured = _config["Backup:Folder"];
            string folder;

            if (!string.IsNullOrWhiteSpace(configured))
            {
                folder = configured;
            }
            else
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();
                using var cmd = new SqlCommand(
                    "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(512))", conn);
                folder = cmd.ExecuteScalar() as string
                         ?? Path.Combine(AppContext.BaseDirectory, "Backups");
            }

            Directory.CreateDirectory(folder);
            return _folder = folder;
        }

        // ============================================================
        // Settings (stored as a JSON file inside the backup folder)
        // ============================================================

        private string SettingsPath => Path.Combine(GetFolder(), SettingsFileName);

        private BackupSettingsDto ReadSettingsFile()
        {
            lock (_settingsLock)
            {
                try
                {
                    if (File.Exists(SettingsPath))
                    {
                        var loaded = JsonSerializer.Deserialize<BackupSettingsDto>(File.ReadAllText(SettingsPath));
                        if (loaded != null) return loaded;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read backup settings, using defaults.");
                }
                return new BackupSettingsDto();
            }
        }

        public BackupSettingsDto GetSettings()
        {
            var s = ReadSettingsFile();
            s.BackupFolder = GetFolder();
            s.LastAutoBackupOn = GetBackups()
                .Where(b => b.Type == "Auto")
                .Select(b => (DateTime?)b.CreatedOn)
                .OrderByDescending(d => d)
                .FirstOrDefault();
            return s;
        }

        public BackupSettingsDto SaveSettings(BackupSettingsDto dto)
        {
            if (!TimeSpan.TryParseExact(dto.BackupTime, @"hh\:mm", CultureInfo.InvariantCulture, out _))
                throw new InvalidOperationException("Backup time must be in HH:mm format (for example 23:00).");
            if (dto.RetentionDays < 1 || dto.RetentionDays > 3650)
                throw new InvalidOperationException("Retention must be between 1 and 3650 days.");

            lock (_settingsLock)
            {
                var json = JsonSerializer.Serialize(
                    new { dto.AutoBackupEnabled, dto.BackupTime, dto.RetentionDays },
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }

            return GetSettings();
        }

        // ============================================================
        // Files
        // ============================================================

        public List<BackupFileDto> GetBackups()
        {
            return new DirectoryInfo(GetFolder())
                .GetFiles("*.bak")
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => new BackupFileDto
                {
                    FileName = f.Name,
                    SizeBytes = f.Length,
                    CreatedOn = f.LastWriteTime,
                    Type = f.Name.Contains("_auto_") ? "Auto"
                         : f.Name.Contains("_manual_") ? "Manual"
                         : "Other"
                })
                .ToList();
        }

        public string GetBackupFilePath(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || !FileNameRegex.IsMatch(fileName))
                throw new InvalidOperationException("Invalid backup file name.");

            var path = Path.Combine(GetFolder(), fileName);
            if (!File.Exists(path))
                throw new KeyNotFoundException("Backup file not found.");

            return path;
        }

        public void DeleteBackup(string fileName)
        {
            var path = GetBackupFilePath(fileName);

            lock (_jobLock)
            {
                if (_currentJobId != null
                    && _jobs.TryGetValue(_currentJobId, out var running)
                    && running.Status == BackupJobStatus.Running
                    && string.Equals(running.FileName, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This backup is still being created.");
                }
            }

            File.Delete(path);
        }

        // ============================================================
        // Jobs
        // ============================================================

        public BackupJobDto StartManualBackup() => StartJob("manual");

        public BackupJobDto GetJob(string jobId)
        {
            if (_jobs.TryGetValue(jobId, out var job)) return Clone(job);
            throw new KeyNotFoundException("Backup job not found.");
        }

        public BackupJobDto GetCurrentJob()
        {
            lock (_jobLock)
            {
                if (_currentJobId != null
                    && _jobs.TryGetValue(_currentJobId, out var job)
                    && job.Status == BackupJobStatus.Running)
                {
                    return Clone(job);
                }
            }
            return new BackupJobDto { Status = BackupJobStatus.Idle };
        }

        private BackupJobDto StartJob(string type)
        {
            BackupJobDto job;

            lock (_jobLock)
            {
                if (_currentJobId != null
                    && _jobs.TryGetValue(_currentJobId, out var running)
                    && running.Status == BackupJobStatus.Running)
                {
                    throw new InvalidOperationException("A backup is already running. Please wait for it to finish.");
                }

                job = new BackupJobDto
                {
                    JobId = Guid.NewGuid().ToString("N"),
                    Status = BackupJobStatus.Running,
                    PercentComplete = 0,
                    Message = "Starting backup...",
                    StartedOn = DateTime.Now
                };

                _jobs[job.JobId] = job;
                _currentJobId = job.JobId;

                // keep the last 20 jobs only
                foreach (var oldId in _jobs.Values.OrderByDescending(j => j.StartedOn).Skip(20).Select(j => j.JobId).ToList())
                    _jobs.TryRemove(oldId, out _);
            }

            // Fire and forget: the backup must not be tied to the HTTP request that started it.
            _ = Task.Run(() => ExecuteBackupAsync(job, type));

            return Clone(job);
        }

        private async Task ExecuteBackupAsync(BackupJobDto job, string type)
        {
            string? fullPath = null;

            try
            {
                var folder = GetFolder();
                var dbName = DatabaseName;
                var fileName = $"{dbName}_{type}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                fullPath = Path.Combine(folder, fileName);
                job.FileName = fileName;

                await using var conn = new SqlConnection(ConnectionString);

                // STATS = 1 makes SQL Server report every percent as an info message.
                conn.InfoMessage += (_, e) =>
                {
                    foreach (SqlError err in e.Errors)
                    {
                        if (err.Number != PercentProcessedMessageNumber) continue;

                        var digits = new string(err.Message.TrimStart().TakeWhile(char.IsDigit).ToArray());
                        if (int.TryParse(digits, out var pct))
                        {
                            job.PercentComplete = Math.Clamp(pct, 0, 99); // 100 is reserved for "verified"
                            job.Message = "Backing up database...";
                        }
                    }
                };

                await conn.OpenAsync();

                var quotedDb = "[" + dbName.Replace("]", "]]") + "]";

                await using (var backup = new SqlCommand(
                    $"BACKUP DATABASE {quotedDb} TO DISK = @path WITH INIT, CHECKSUM, STATS = 1, NAME = @name", conn))
                {
                    backup.CommandTimeout = 0; // no timeout: large databases take a while
                    backup.Parameters.AddWithValue("@path", fullPath);
                    backup.Parameters.AddWithValue("@name", $"{dbName} {type} backup");
                    await backup.ExecuteNonQueryAsync();
                }

                job.PercentComplete = 99;
                job.Message = "Verifying backup...";

                await using (var verify = new SqlCommand("RESTORE VERIFYONLY FROM DISK = @path WITH CHECKSUM", conn))
                {
                    verify.CommandTimeout = 0;
                    verify.Parameters.AddWithValue("@path", fullPath);
                    await verify.ExecuteNonQueryAsync();
                }

                if (type == "auto") ApplyRetention();

                job.PercentComplete = 100;
                job.Message = "Backup completed successfully.";
                job.CompletedOn = DateTime.Now;
                job.Status = BackupJobStatus.Completed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database backup failed.");

                try { if (fullPath != null && File.Exists(fullPath)) File.Delete(fullPath); } catch { /* ignore */ }

                job.Error = ex.Message;
                job.Message = "Backup failed.";
                job.CompletedOn = DateTime.Now;
                job.Status = BackupJobStatus.Failed;
            }
            finally
            {
                lock (_jobLock)
                {
                    if (_currentJobId == job.JobId) _currentJobId = null;
                }
            }
        }

        // ============================================================
        // Automatic backup + retention
        // ============================================================

        public void TryRunAutoBackup()
        {
            var s = ReadSettingsFile();
            if (!s.AutoBackupEnabled) return;

            if (!TimeSpan.TryParseExact(s.BackupTime, @"hh\:mm", CultureInfo.InvariantCulture, out var timeOfDay))
                return;

            // Not time yet today
            if (DateTime.Now < DateTime.Today + timeOfDay) return;

            // Already have an automatic backup from today (also covers "app was off at 23:00, started later")
            var todayPrefix = $"{DatabaseName}_auto_{DateTime.Today:yyyyMMdd}_";
            if (Directory.GetFiles(GetFolder(), todayPrefix + "*.bak").Length > 0) return;

            // After a failure, wait before trying again instead of retrying every minute
            if (DateTime.Now - _lastAutoAttempt < TimeSpan.FromMinutes(30)) return;

            _lastAutoAttempt = DateTime.Now;
            try
            {
                StartJob("auto");
            }
            catch (InvalidOperationException)
            {
                // a backup is already running: try again on the next tick
                _lastAutoAttempt = DateTime.MinValue;
            }
        }

        /// <summary>Deletes AUTOMATIC backups older than the retention period. Manual backups are never touched.</summary>
        private void ApplyRetention()
        {
            try
            {
                var days = ReadSettingsFile().RetentionDays;
                var cutoff = DateTime.Now.AddDays(-days);

                foreach (var file in new DirectoryInfo(GetFolder())
                             .GetFiles($"{DatabaseName}_auto_*.bak")
                             .Where(f => f.LastWriteTime < cutoff))
                {
                    try { file.Delete(); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Could not delete old backup {File}", file.Name); }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Backup retention cleanup failed.");
            }
        }

        private static BackupJobDto Clone(BackupJobDto j) => new()
        {
            JobId = j.JobId,
            Status = j.Status,
            PercentComplete = j.PercentComplete,
            Message = j.Message,
            FileName = j.FileName,
            StartedOn = j.StartedOn,
            CompletedOn = j.CompletedOn,
            Error = j.Error
        };
    }
}