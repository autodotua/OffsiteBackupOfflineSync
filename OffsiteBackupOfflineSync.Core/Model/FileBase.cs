﻿using System.ComponentModel;
using System.Diagnostics;
using FzLib;
using Newtonsoft.Json;

namespace OffsiteBackupOfflineSync.Model
{
    [DebuggerDisplay("{Name}")]
    public class FileBase : INotifyPropertyChanged
    {
        private bool complete;
        private bool isChecked = true;
        private string message;
        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public bool Checked
        {
            get => isChecked;
            set => this.SetValueAndNotify(ref isChecked, value, nameof(Checked));
        }

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
    }
}
