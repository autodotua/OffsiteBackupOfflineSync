using FzLib;
using System.ComponentModel;
using OffsiteBackupOfflineSync.Model;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace OffsiteBackupOfflineSync.UI
{
    public class ViewModelBase<T> : INotifyPropertyChanged where T:FileBase
    {
        private string message = "就绪";
        private double progress;
        private bool progressIndeterminate;
        private double progressMax;
        private ObservableCollection<T> files;

        private bool working;

        public event PropertyChangedEventHandler PropertyChanged;

        public long AddedFileLength => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Add && p.Checked)?.Sum(p => p.Length) ?? 0;

        public long DeletedFileCount => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Delete && p.Checked)?.Count() ?? 0;
        [JsonIgnore]
        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }
        [JsonIgnore]
        public long ModifiedFileLength => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Modify && p.Checked)?.Sum(p => p.Length) ?? 0;
        [JsonIgnore]
        public double Progress
        {
            get => progress;
            set => this.SetValueAndNotify(ref progress, value, nameof(Progress));
        }
        [JsonIgnore]
        public bool ProgressIndeterminate
        {
            get => progressIndeterminate;
            set => this.SetValueAndNotify(ref progressIndeterminate, value, nameof(ProgressIndeterminate));
        }
        public double ProgressMax
        {
            get => progressMax;
            set => this.SetValueAndNotify(ref progressMax, value, nameof(ProgressMax));
        }
        [JsonIgnore]
        public ObservableCollection<T> Files
        {
            get => files;
            set
            {
                this.SetValueAndNotify(ref files, value,
                nameof(Files), nameof(AddedFileLength), nameof(ModifiedFileLength), nameof(DeletedFileCount));
                if (typeof(T).Equals(typeof(SyncFile)))
                {
                    value.Cast<SyncFile>().ForEach(p => AddFileCheckedNotify(p));
                }
                //value.CollectionChanged += (s, e) =>
                //{
                //    switch (e.Action)
                //    {
                //        case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                //            foreach (SyncFile item in e.NewItems)
                //            {
                //                AddFileCheckedNotify(item);
                //            }
                //            break;
                //        default:
                //            break;
                //    }
                //};
            }
        }
        [JsonIgnore]
        public bool Working
        {
            get => working;
            set => this.SetValueAndNotify(ref working, value, nameof(Working));
        }

        private void AddFileCheckedNotify(SyncFile file)
        {
            file.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SyncFile.Checked))
                {
                    switch ((s as SyncFile).UpdateType)
                    {
                        case FileUpdateType.Add:
                            this.Notify(nameof(AddedFileLength));
                            break;
                        case FileUpdateType.Modify:
                            this.Notify(nameof(ModifiedFileLength));
                            break;
                        case FileUpdateType.Delete:
                            this.Notify(nameof(DeletedFileCount));
                            break;
                        default:
                            break;
                    }
                }
            };
        }

    }
}

