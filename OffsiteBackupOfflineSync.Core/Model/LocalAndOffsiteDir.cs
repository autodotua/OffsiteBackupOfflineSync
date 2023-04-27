using FzLib;
using System.ComponentModel;

namespace OffsiteBackupOfflineSync.Model
{
    public class LocalAndOffsiteDir : INotifyPropertyChanged
    {
        private string localDir;
        private string offsiteDir;

        public event PropertyChangedEventHandler PropertyChanged;

        public string LocalDir
        {
            get => localDir;
            set => this.SetValueAndNotify(ref localDir, value, nameof(LocalDir));
        }
        public string OffsiteDir
        {
            get => offsiteDir;
            set => this.SetValueAndNotify(ref offsiteDir, value, nameof(OffsiteDir));
        }
    }
}