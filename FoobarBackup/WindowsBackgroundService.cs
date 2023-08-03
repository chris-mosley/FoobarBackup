using FoobarBackup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace App.WindowsService;

public sealed class WindowsBackgroundService : BackgroundService
{
    private readonly Backup _backupService;
    private readonly ILogger<WindowsBackgroundService> _logger;
    int sleep = Backup.GetConfig().GetValue<int>("interval");
    public WindowsBackgroundService(
        Backup backup,
        ILogger<WindowsBackgroundService> logger) =>
        (_backupService, _logger) = (backup, logger);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting Backup.");
                Backup.ExecuteBackup();
                _logger.LogInformation("Sleeping for " + sleep + " seconds.");
                await Task.Delay(sleep * 1000, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}