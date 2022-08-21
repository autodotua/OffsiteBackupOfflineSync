using FzLib;
using System.ComponentModel;
using OffsiteBackupOfflineSync.Model;

namespace OffsiteBackupOfflineSync.UI
{
    public class PatchAndApplyViewModelBase : INotifyPropertyChanged
    {
        private string message = "就绪";
        private double progress;
        private double progressMax;

        private List<SyncFile> updateFiles;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }
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
        public List<SyncFile> UpdateFiles
        {
            get => updateFiles;
            set => this.SetValueAndNotify(ref updateFiles, value, nameof(UpdateFiles));
        }
    }

}
