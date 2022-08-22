using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FzLib;
using Newtonsoft.Json;

namespace OffsiteBackupOfflineSync.Model
{
    public class SyncFile : INotifyPropertyChanged
    {
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
        private bool complete;

        private string message;

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public bool Checked { get; set; } = true;
        [JsonIgnore]
        public bool Complete
        {
            get => complete;
            set => this.SetValueAndNotify(ref complete, value, nameof(Complete));
        }

        public DateTime LastWriteTime { get; set; }
        public long Length { get; set; }
        [JsonIgnore]
        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        public string Name { get; set; }
        public string Path { get; set; }
        public string TempName { get; set; }
        public FileUpdateType UpdateType { get; set; }
    }
}
