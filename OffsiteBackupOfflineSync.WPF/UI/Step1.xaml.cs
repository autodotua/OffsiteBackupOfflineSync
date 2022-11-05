using FzLib;
using FzLib.Collection;
using Microsoft.WindowsAPICodePack.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.Utility;
using OffsiteBackupOfflineSync.WPF.UI;
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
            ViewModel.Dirs = ViewModel.Dirs;
        }

        public Step1ViewModel ViewModel { get; }

        private void BrowseDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.Dir = path;
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
            var dirs = lst.SelectedItems.Cast<string>().ToList();
            if (dirs.Count == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("选择的目录为空");
                return;
            }
            if (string.IsNullOrWhiteSpace(ViewModel.OutputFile))
            {
                await CommonDialog.ShowErrorDialogAsync("未设置输出文件");
                return;
            }

            ViewModel.SelectedDirectoriesHistory.AddOrSetValue(ViewModel.Dir, dirs);

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
            if (drives.Any(p => ViewModel.Dir.StartsWith(p.Name)))
            {
                var label = drives.First(p => ViewModel.Dir.StartsWith(p.Name)).VolumeLabel;
                if (!string.IsNullOrEmpty(label))
                {
                    return label;
                }
            }
            return path[0].ToString();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            lst.SelectAll();
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            lst.UnselectAll();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateStatus(StatusType.Stopping);
            u.Stop();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Dirs))
            {
                if (ViewModel.Dirs.Count == 0)
                {
                    return;
                }
                try
                {
                    lst.SelectedItems.Clear();
                    if (!string.IsNullOrEmpty(ViewModel.Dir))
                    {
                        if (ViewModel.SelectedDirectoriesHistory.ContainsKey(ViewModel.Dir))
                        {
                            foreach (var item in ViewModel.SelectedDirectoriesHistory[ViewModel.Dir])
                            {
                                if (ViewModel.Dirs.Contains(item))
                                {
                                    lst.SelectedItems.Add(item);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    lst.SelectAll();
                }
            }
        }
    }

    public class Step1ViewModel : ViewModelBase<FileBase>
    {
        private string dir;
        private List<string> dirs = new List<string>();

        private string outputFile;

        public string Dir
        {
            get => dir;
            set
            {
                this.SetValueAndNotify(ref dir, value, nameof(Dir));
                if (Directory.Exists(value))
                {
                    Dirs = Directory.EnumerateDirectories(value)
                        .Where(p => !p.EndsWith("System Volume Information"))
                        .Where(p => !p.Contains('$'))
                        .ToList();
                }
                else
                {
                    Dirs = new List<string>();
                }
            }
        }

        [JsonIgnore]
        public List<string> Dirs
        {
            get => dirs;
            set => this.SetValueAndNotify(ref dirs, value, nameof(Dirs));
        }
        public string OutputFile
        {
            get => outputFile;
            set => this.SetValueAndNotify(ref outputFile, value, nameof(OutputFile));
        }


        public Dictionary<string, List<string>> SelectedDirectoriesHistory { get; set; } = new Dictionary<string, List<string>>();
    }
}