using FoobarBackup.Classes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System.Runtime.ExceptionServices;
using System.IO.Compression;
using System.ServiceProcess;
using System.Diagnostics;
using App.WindowsService;
using Microsoft.Extensions.Logging;

namespace FoobarBackup
{
    public sealed class Backup
    {
        static void Main(string[] args)
        {
            IConfiguration config = GetConfig();
            List<BackupGroup> backupGroups = config.GetSection("backupGroups").Get<List<BackupGroup>>();
            int sleep = config.GetValue<int>("interval");
            Backup servicecheck = new Backup();

            // check to see if the service is installed, if not, ask if the user wants to install it
            if (config.GetValue<bool>("CheckServiceInstall"))
            {
                if (!servicecheck.CheckService())
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
            if (config.GetValue<bool>("RunOnce"))
            {
                Console.WriteLine("Running single backup");
                ExecuteBackup();
                Console.WriteLine("Backup complete, exiting");
                Environment.Exit(0);
            }

            while (true)
            {
                Console.WriteLine("Starting backup");
                ExecuteBackup();
                Console.WriteLine("Backup complete");
                System.Threading.Thread.Sleep(sleep * 1000);
            }
        }

        public static void BackupLoop()
        {
            IConfiguration config = GetConfig();
            int sleep = config.GetValue<int>("interval");
            while (true)
            {
                Console.WriteLine("Starting backup");
                ExecuteBackup();
                Console.WriteLine("Backup complete");
                System.Threading.Thread.Sleep(sleep * 1000);
            }
        }
        public static IConfiguration GetConfig()
        {
            try
            {
                IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
                return config;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
        }
        public static void ExecuteBackup()
        {
            Console.WriteLine("Starting backup");
            List<BackupGroup> backupGroups = GetConfig().GetSection("backupGroups").Get<List<BackupGroup>>();
            foreach (BackupGroup group in backupGroups)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                if (group.Enabled != true)
                {
                    //bail out if the group is disabled or misconfigured etc
                    Console.WriteLine("Skipping group " + group.Name);
                    continue;
                }
                List<FileInfo> files = new List<FileInfo>();
                List<DirectoryInfo> directoryInfos = new List<DirectoryInfo>();
                string backupRoot = Path.Combine((Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)), "foobar2000-v2");
                foreach (string file in group.IncludeFiles)
                {
                    files.Add(new FileInfo(Path.Combine(backupRoot, file)));
                    Debug.WriteLine(Path.Combine(backupRoot, file));
                }
                foreach (string folder in group.IncludeFolders)
                {
                    directoryInfos.Add(new DirectoryInfo(Path.Combine(backupRoot, folder)));
                    Debug.WriteLine(Path.Combine(backupRoot, folder));
                }
                if (Directory.Exists(group.Destination) != true)
                {
                    Directory.CreateDirectory(group.Destination);
                }
                ZipArchive zip = ZipFile.Open(Path.Combine(group.Destination + "\\" + "autobackup." + timestamp + ".zip"), ZipArchiveMode.Create);

                foreach (FileInfo file in files)
                {
                    var readFile = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var zipfile = zip.CreateEntry(file.Name);
                    var entryStream = zipfile.Open();
                    readFile.CopyTo(entryStream);
                    entryStream.Close();
                    readFile.Close();
                }
                foreach (DirectoryInfo directory in directoryInfos)
                {
                    zip.CreateEntry(directory.Name + "\\");
                    foreach (FileInfo file in directory.GetFiles("*", SearchOption.AllDirectories))
                    {
                        var readFile = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        var zipfile = zip.CreateEntry(directory.Name + "\\" + file.Name);
                        //zip.CreateEntryFromFile(file.FullName, directory.Name + "\\" + file.Name);
                        var entryStream = zipfile.Open();
                        readFile.CopyTo(entryStream);
                        entryStream.Close();
                        readFile.Close();
                    }
                }
                Console.WriteLine("Writing Zip File");
                zip.Dispose();
            }
        }

        public bool CheckService()
        {
            try
            {
                var services = ServiceController.GetServices();
                if (services.FirstOrDefault(s => s.ServiceName == "FoobarBackup") != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

    }
}