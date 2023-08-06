using App.WindowsService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FoobarBackup;
using System.ServiceProcess;

if (!Environment.UserInteractive)
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "FoobarBackup";
    });

    LoggerProviderOptions.RegisterProviderOptions<
        EventLogSettings, EventLogLoggerProvider>(builder.Services);

    builder.Services.AddSingleton<FoobarBackup.Backup>();
    builder.Services.AddHostedService<WindowsBackgroundService>();

    // See: https://github.com/dotnet/runtime/issues/47303
    builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

    IHost host = builder.Build();
    host.Run();
}
else
{
    //Backup backup = new Backup();
    Common common = new Common();
    IConfiguration config = Common.GetConfig();
    // TODO: bypass all this shit after a timer so that users don't have to interact if they don't want to install the service.
    if (config.GetValue<bool>("CheckServiceInstall"))
    {
        var services = ServiceController.GetServices();
        var backupService = services.FirstOrDefault(s => s.ServiceName == "FoobarBackup");
        // TODO: Check service path and ask if user wants to reinstall if it doesn't match
        if (backupService == null)
        {
            Console.WriteLine("Service not installed.  Would you like to install it?  No will backup but the console will remain open. (y/n): ");
            string response = Console.ReadLine();
            if (response == "y")
            {
                Console.WriteLine("Installing service...");
                System.Diagnostics.Process.Start("sc.exe", "create FoobarBackup binPath= \"" + System.AppDomain.CurrentDomain.BaseDirectory + System.AppDomain.CurrentDomain.FriendlyName + "\" start= auto");
                System.Diagnostics.Process.Start("sc.exe", "description FoobarBackup " + "Backup Service for foobar configuration directory");
                Console.WriteLine("Service installed.  Would you like to start it? (y/n): ");
                response = Console.ReadLine();
                if (response == "y")
                {
                    Console.WriteLine("Starting service...");
                    System.Diagnostics.Process.Start("sc.exe", "start FoobarBackup");
                    Console.WriteLine("Service started.");
                }
            }
            else
            {
                Console.WriteLine("Service not installed.  Backup will run in user environment until exited.");
            }
        }
        else
        {
            Console.WriteLine("Service installed.  Would you like to uninstall it? (y/n): ");
            string response = Console.ReadLine();
            if (response == "y")
            {
                Console.WriteLine("Uninstalling service...");
                System.Diagnostics.Process.Start("sc.exe", "stop FoobarBackup");
                System.Diagnostics.Process.Start("sc.exe", "delete FoobarBackup");
                Console.WriteLine("Service uninstalled.");
            }
        }
    }
    Backup.BackupLoop();
}