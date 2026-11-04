// Put in: POS.Web/BackgroundServices/AutoBackupHostedService.cs
using POS.Entities.IServices.Backup;

namespace POS.Web.BackgroundServices
{
    /// <summary>Checks once a minute whether the daily automatic backup is due.</summary>
    public class AutoBackupHostedService : BackgroundService
    {
        private readonly IBackupService _backups;
        private readonly ILogger<AutoBackupHostedService> _logger;

        public AutoBackupHostedService(IBackupService backups, ILogger<AutoBackupHostedService> logger)
        {
            _backups = backups;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // let the app finish starting before the first check
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        _backups.TryRunAutoBackup();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Automatic backup check failed.");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // app is shutting down
            }
        }
    }
}