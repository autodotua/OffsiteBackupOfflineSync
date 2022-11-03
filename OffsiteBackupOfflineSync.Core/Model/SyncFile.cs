using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using FzLib;
using Newtonsoft.Json;

namespace OffsiteBackupOfflineSync.Model
{
    public class SyncFile : FileBase
    {
        private bool isChecked = true;


        public SyncFile()
        {

        }
        public SyncFile(FileInfo file, string rootDir)
        {
            Name = file.Name;
            Path = System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(rootDir), file.FullName);
            LastWriteTime = file.LastWriteTime;
            Length = file.Length;
        }
        [JsonIgnore]
        public bool Checked
        {
            get => isChecked;
            set => this.SetValueAndNotify(ref isChecked, value, nameof(Checked));
        }


        public string TempName { get; set; }
        public FileUpdateType UpdateType { get; set; }
    }
}
