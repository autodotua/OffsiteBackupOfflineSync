using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffsiteBackupOfflineSync.Model
{
    public class Step1Model
    {
        public List<SyncFile> Files { get; set; }   
        public List<string> TopDirectories { get; set; }
    }
}
