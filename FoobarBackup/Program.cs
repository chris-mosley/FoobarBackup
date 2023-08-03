using App.WindowsService;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FoobarBackup;

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
    Backup backup = new Backup();
    IConfiguration config = Backup.GetConfig();
    if (config.GetValue<bool>("CheckServiceInstall"))
            {
                if (!backup.CheckService())
                {
                    Console.WriteLine("Service not installed.  Would you like to install it? (y/n): ");
                    string response = Console.ReadLine();
                    if (response == "y")
                    {
                        Console.WriteLine("Installing service...");
                        System.Diagnostics.Process.Start("sc.exe", "create FoobarBackup binPath= \"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\" start= auto");
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
            }
}