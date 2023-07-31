using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoobarBackup.Classes
{
    internal class BackupGroup
    {
        public string? Name { get; set; }
        public bool? Enabled { get; set; }
        public int ? Copies { get; set; }
        public string? RootFolder { get; set; }
        public List<string>? IncludeFolders { get; set; }
        public List<string>? IncludeFiles { get; set; }
        public List<string>? ExcludeFolders { get; set; }
        public List<string>? ExcludeFiles { get; set; }
        public required string Destination { get; set; }
    }
}
