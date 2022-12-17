using FzLib;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace OffsiteBackupOfflineSync.UI
{
    public class ViewModelBase<T> : INotifyPropertyChanged where T : FileBase
    {
        private bool canAnalyze = true;
        private bool canEditConfigs = true;
        private bool canProcess = false;
        private bool canStop = false;
        private ObservableCollection<T> files = new ObservableCollection<T>();
        private string message = "就绪";
        private double progress;
        private bool progressIndeterminate;
        private double progressMax;

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonIgnore]
        public long AddedFileCount => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Add && p.Checked)?.Count() ?? 0;

        [JsonIgnore]
        public long AddedFileLength => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Add && p.Checked)?.Sum(p => p.Length) ?? 0;
        [JsonIgnore]
        public bool CanAnalyze
        {
            get => canAnalyze;
            set => this.SetValueAndNotify(ref canAnalyze, value, nameof(CanAnalyze));
        }

        [JsonIgnore]
        public bool CanEditConfigs
        {
            get => canEditConfigs;
            set => this.SetValueAndNotify(ref canEditConfigs, value, nameof(CanEditConfigs));
        }

        [JsonIgnore]
        public bool CanProcess
        {
            get => canProcess;
            set => this.SetValueAndNotify(ref canProcess, value, nameof(CanProcess));
        }

        [JsonIgnore]
        public bool CanStop
        {
            get => canStop;
            set => this.SetValueAndNotify(ref canStop, value, nameof(CanStop));
        }

        [JsonIgnore]
        public int CheckedFileCount => Files?.Where(p => p.Checked)?.Count() ?? 0;

        [JsonIgnore]
        public int DeletedFileCount => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Delete && p.Checked)?.Count() ?? 0;

        [JsonIgnore]
        public int MovedFileCount => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Move && p.Checked)?.Count() ?? 0;

        [JsonIgnore]
        public ObservableCollection<T> Files
        {
            get => files;
            set
            {
                this.SetValueAndNotify(ref files, value,
                nameof(Files),
                nameof(AddedFileLength),
                nameof(AddedFileCount),
                nameof(ModifiedFileCount),
                nameof(ModifiedFileLength),
                nameof(DeletedFileCount),
                nameof(MovedFileCount),
                nameof(CheckedFileCount));

                value.ForEach(p => AddFileCheckedNotify(p));
                value.CollectionChanged += (s, e) => throw new NotSupportedException("不允许对集合进行修改");
            }
        }

        [JsonIgnore]
        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        [JsonIgnore]
        public long ModifiedFileCount => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Modify && p.Checked)?.Count() ?? 0;

        [JsonIgnore]
        public long ModifiedFileLength => Files?.Cast<SyncFile>().Where(p => p.UpdateType == FileUpdateType.Modify && p.Checked)?.Sum(p => p.Length) ?? 0;
        [JsonIgnore]
        public double Progress
        {
            get => progress;
            set
            {
                this.SetValueAndNotify(ref progress, value, nameof(Progress));
                ProgressIndeterminate = false;
            }
        }

        [JsonIgnore]
        public bool ProgressIndeterminate
        {
            get => progressIndeterminate;
            set => this.SetValueAndNotify(ref progressIndeterminate, value, nameof(ProgressIndeterminate));
        }

        [JsonIgnore]
        public double ProgressMax
        {
            get => progressMax;
            set => this.SetValueAndNotify(ref progressMax, value, nameof(ProgressMax));
        }

        public void UpdateStatus(StatusType status)
        {
            CanStop = status is StatusType.Analyzing or StatusType.Processing;
            CanAnalyze = status is StatusType.Ready or StatusType.Analyzed;
            CanProcess = status is StatusType.Analyzed;
            CanEditConfigs = status is StatusType.Ready or StatusType.Analyzed;
            Message = status is StatusType.Ready or StatusType.Analyzed ? "就绪" : "处理中";
            Progress = 0;
            ProgressIndeterminate = status is StatusType.Analyzing or StatusType.Processing or StatusType.Stopping;
        }

        private void AddFileCheckedNotify(FileBase file)
        {
            file.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FileBase.Checked))
                {
                    this.Notify(nameof(CheckedFileCount));
                    if (s is SyncFile syncFile)
                    {
                        switch (syncFile.UpdateType)
                        {
                            case FileUpdateType.Add:
                                this.Notify(nameof(AddedFileCount), nameof(AddedFileLength));
                                break;

                            case FileUpdateType.Modify:
                                this.Notify(nameof(ModifiedFileCount), nameof(ModifiedFileLength));
                                break;

                            case FileUpdateType.Delete:
                                this.Notify(nameof(DeletedFileCount));
                                break;

                            case FileUpdateType.Move:
                                this.Notify(nameof(MovedFileCount));
                                break;

                            default:
                                break;
                        }
                    }
                }
            };
        }
    }
}