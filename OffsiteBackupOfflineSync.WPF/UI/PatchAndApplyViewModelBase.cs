using FzLib;
using System.ComponentModel;
using OffsiteBackupOfflineSync.Model;
using System.Collections.ObjectModel;

namespace OffsiteBackupOfflineSync.UI
{
    public class PatchAndApplyViewModelBase : INotifyPropertyChanged
    {
        private string message = "就绪";
        private double progress;
        private double progressMax;

        private ObservableCollection<SyncFile> updateFiles;
        private bool working;

        public event PropertyChangedEventHandler PropertyChanged;

        public long AddedFileLength => UpdateFiles?.Where(p => p.UpdateType == FileUpdateType.Add)?.Sum(p => p.Length) ?? 0;

        public long DeletedFileCount => UpdateFiles?.Where(p => p.UpdateType == FileUpdateType.Delete)?.Count() ?? 0;

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }
        public long ModifiedFileLength => UpdateFiles?.Where(p => p.UpdateType == FileUpdateType.Modify)?.Sum(p => p.Length) ?? 0;

        public double Progress
        {
            get => progress;
            set => this.SetValueAndNotify(ref progress, value, nameof(Progress));
        }
        public double ProgressMax
        {
            get => progressMax;
            set => this.SetValueAndNotify(ref progressMax, value, nameof(ProgressMax));
        }
        public ObservableCollection<SyncFile> UpdateFiles
        {
            get => updateFiles;
            set => this.SetValueAndNotify(ref updateFiles, value, nameof(UpdateFiles),nameof(AddedFileLength),nameof(ModifiedFileLength),nameof(DeletedFileCount));
        }
        public bool Working
        {
            get => working;
            set => this.SetValueAndNotify(ref working, value, nameof(Working));
        }

    }
}

