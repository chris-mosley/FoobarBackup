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
// TODO: Figure out how to use the service logger here instead of logging only to console.
    public sealed class Backup
    {
        static void Main(string[] args)
        {
            IConfiguration config = Common.GetConfig();
            List<BackupGroup> backupGroups = config.GetSection("backupGroups").Get<List<BackupGroup>>();
            int sleep = config.GetValue<int>("interval");
            Backup backup = new Backup();

            // check to see if the service is installed, if not, ask if the user wants to install it
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
            IConfiguration config = Common.GetConfig();
            int sleep = config.GetValue<int>("interval");
            while (true)
            {
                Console.WriteLine("Starting backup");
                ExecuteBackup();
                Console.WriteLine("Backup complete");
                System.Threading.Thread.Sleep(sleep * 1000);
            }
        }
        public static void ExecuteBackup()
        {
            Console.WriteLine("Starting backup");
            List<BackupGroup> backupGroups = Common.GetConfig().GetSection("backupGroups").Get<List<BackupGroup>>();
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


                
                if (group.Scavenge.Enabled == true)
                {
                    // i'm 1000% sure this is the wrong way to do this, but one day i'll be smart enough to know what the right way is.
                    Backup backup = new Backup();
                    Task scavenging = backup.ScavengeBackups(group.Destination, group.Scavenge);
                }
            }
        }

        private async Task ScavengeBackups(string path, ScavengeSettings scavengeSettings)
        {
            //List<FileInfo> allbackups = 
            var files = Directory.GetFiles(path, "autobackup.*.zip").OrderBy(s => s).ToList();
            if (scavengeSettings.Type == "count")
            {
                int deleteCount = files.Count() - (int)scavengeSettings.Count;
                if (deleteCount > 0)
                {
                    for (int i = 0; i < deleteCount; i++)
                    {
                        FileInfo fileInfo = new FileInfo(files[i]);
                        Console.WriteLine("Deleting " + fileInfo.Name);
                        fileInfo.Delete();
                    }
                }
            }
            else if (scavengeSettings.Type == "days")
            {
                DateTime scavengeDate = DateTime.Now.AddDays((double)scavengeSettings.Age * -1);
                foreach (string file in files)
                {
                    DateTime fileAge = DateTime.Parse(file.Replace("autobackup.", "").Replace(".zip", ""));
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileAge < scavengeDate)
                    {
                        Console.WriteLine("Deleting " + file);
                        fileInfo.Delete();
                    }
                }
            }
            // TODO: implement total diskspace based scavenging



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