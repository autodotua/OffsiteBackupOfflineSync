using FzLib;
using FzLib.Collection;
using Microsoft.WindowsAPICodePack.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.Utility;
using OffsiteBackupOfflineSync.WPF.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OffsiteBackupOfflineSync.UI
{
    /// <summary>
    /// UpdatePanel.xaml 的交互逻辑
    /// </summary>
    public partial class Step1 : UserControl
    {
        private readonly Step1Utility u = new Step1Utility();

        public Step1(Step1ViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            PanelHelper.RegisterMessageAndProgressEvent(u, viewModel);
            ViewModel.SearchingDirs = ViewModel.SearchingDirs;
        }


        public Step1ViewModel ViewModel { get; }

        private void BrowseDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.SearchingDir = path;
            }
        }

        private void BrowseOutputFileButton_Click(object sender, RoutedEventArgs e)
        {
            string name = $"{DateTime.Now:yyyyMMdd}-";
            name += GetVolumeName(name);
            string path = new FileFilterCollection().Add("异地备份快照", "obos1")
               .CreateSaveFileDialog()
               .SetDefault(name)
               .GetFilePath();
            if (path != null)
            {
                ViewModel.OutputFile = path;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dirs = ViewModel.SyncDirs.ToHashSet();
            if (dirs.Count == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("选择的目录为空");
                return;
            }
            foreach (var dir1 in dirs)
            {
                foreach (var dir2 in dirs.Where(p=>p!=dir1))
                {
                    if (dir1.StartsWith(dir2))
                    {
                        await CommonDialog.ShowErrorDialogAsync($"目录存在嵌套：{dir1}是{dir2}的子目录");
                        return;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(ViewModel.OutputFile))
            {
                string name = $"{DateTime.Now:yyyyMMdd}-";
                name += GetVolumeName(name);
                string path = new FileFilterCollection().Add("异地备份快照", "obos1")
                   .CreateSaveFileDialog()
                   .SetDefault(name)
                   .GetFilePath();
                if (path != null)
                {
                    ViewModel.OutputFile = path;
                }
                else
                {
                    return;
                }
            }


            ViewModel.UpdateStatus(StatusType.Processing);
            try
            {
                await Task.Run(() =>
                {
                    u.Enumerate(dirs, ViewModel.OutputFile);
                });
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
            }
            finally
            {
                ViewModel.UpdateStatus(StatusType.Ready);
            }

        }

        private string GetVolumeName(string path)
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            if (drives.Any(p => ViewModel.SearchingDir.StartsWith(p.Name)))
            {
                var label = drives.First(p => ViewModel.SearchingDir.StartsWith(p.Name)).VolumeLabel;
                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }
            }
            return path[0].ToString();
        }

        private void RemoveAllSyncDirsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SyncDirs.Clear();
            lvwSearchingDirs.UnselectAll();
        }

        private void RemoveSelectedSyncDirsButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = lvwSelectedDirs.SelectedItem as string;
            ViewModel.SyncDirs.Remove(dir);
            lvwSearchingDirs.SelectedItems.Remove(dir);
        }

        private void SearchingDirList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                foreach (string dir in e.AddedItems)
                {
                    if (!ViewModel.SyncDirs.Contains(dir))
                    {
                        ViewModel.SyncDirs.Add(dir);
                    }
                }
            }
            if (e.RemovedItems.Count > 0)
            {
                foreach (string dir in e.RemovedItems)
                {
                    if (ViewModel.SyncDirs.Contains(dir) && ViewModel.SearchingDirs.Contains(dir))
                    {
                        ViewModel.SyncDirs.Remove(dir);
                    }
                }
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            lvwSearchingDirs.SelectAll();
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            lvwSearchingDirs.UnselectAll();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateStatus(StatusType.Stopping);
            u.Stop();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SearchingDirs))
            {
                if (ViewModel.SearchingDirs.Count == 0)
                {
                    return;
                }
                foreach (var dir in ViewModel.SearchingDirs.Where(p => ViewModel.SyncDirs.Contains(p)))
                {
                    lvwSearchingDirs.SelectedItems.Add(dir);
                }
            }
        }
    }

    public class Step1ViewModel : ViewModelBase<FileBase>
    {
        private string outputFile;
        private string searchingDir;
        private List<string> searchingDirs = new List<string>();
        private ObservableCollection<string> selectedDirs = new ObservableCollection<string>();

        [JsonIgnore]
        public string OutputFile
        {
            get => outputFile;
            set => this.SetValueAndNotify(ref outputFile, value, nameof(OutputFile));
        }

        public string SearchingDir
        {
            get => searchingDir;
            set
            {
                this.SetValueAndNotify(ref searchingDir, value, nameof(SearchingDir));
                if (Directory.Exists(value))
                {
                    SearchingDirs = Directory.EnumerateDirectories(value)
                        .Where(p => !p.EndsWith("System Volume Information"))
                        .Where(p => !p.Contains('$'))
                        .Where(p => !SyncDirs.Contains(p))
                        .ToList();
                }
                else if(string.IsNullOrEmpty(value))
                {
                    SearchingDirs= DriveInfo.GetDrives().Select(p=>p.RootDirectory.FullName).ToList();
                }
                else
                {
                    SearchingDirs = new List<string>();
                }
            }
        }
        [JsonIgnore]
        public List<string> SearchingDirs
        {
            get => searchingDirs;
            set => this.SetValueAndNotify(ref searchingDirs, value, nameof(SearchingDirs));
        }

        public ObservableCollection<string> SyncDirs
        {
            get => selectedDirs;
            set => this.SetValueAndNotify(ref selectedDirs, value, nameof(SyncDirs));
        }
    }
}