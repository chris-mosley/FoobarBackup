using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoobarBackup.Classes
{
    public class ScavengeSettings
    {
        public bool? Enabled { get; set; }
        public string? Type { get; set; }
        public int? Age { get; set; }
        public int? Count { get; set; }
    }
}
