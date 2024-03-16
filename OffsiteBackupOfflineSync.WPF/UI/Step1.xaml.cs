using FzLib;
using FzLib.Collection;
using FzLib.WPF;
using Microsoft.Win32;
using ModernWpf.FzExtension.CommonDialog;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.Utility;
using OffsiteBackupOfflineSync.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CommonDialog = ModernWpf.FzExtension.CommonDialog.CommonDialog;

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
            PanelHelper.RegisterMessageAndProgressEvent(u, viewModel);
        }


        public Step1ViewModel ViewModel { get; }

        private async void BrowseDirButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            dialog.Multiselect = true;
            if (dialog.ShowDialog() == true)
            {
                var paths = dialog.FolderNames;
                if (paths.Length > 0)
                {
                    foreach (var path in paths)
                    {
                        try
                        {
                            ViewModel.AddSyncDir(path);
                        }
                        catch (Exception ex)
                        {
                            await CommonDialog.ShowErrorDialogAsync(ex.Message, null, "添加失败");
                        }
                    }
                }
            }
        }


        private void BrowseOutputFileButton_Click(object sender, RoutedEventArgs e)
        {
            GetObos1File();
        }

        private bool GetObos1File()
        {
            string name = $"{DateTime.Now:yyyyMMdd}-备份";
            var dialog = new SaveFileDialog().AddFilter("异地备份快照", "obos1");
            dialog.FileName = name;
            string path = dialog.GetPath(this.GetWindow());
            if (path != null)
            {
                ViewModel.OutputFile = path;
                return true;
            }
            return false;
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
                foreach (var dir2 in dirs.Where(p => p != dir1))
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
                if (!GetObos1File())
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

        private void RemoveAllSyncDirsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SyncDirs.Clear();
        }

        private void RemoveSelectedSyncDirsButton_Click(object sender, RoutedEventArgs e)
        {
            string dir = lvwSelectedDirs.SelectedItem as string;
            ViewModel.SyncDirs.Remove(dir);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateStatus(StatusType.Stopping);
            u.Stop();
        }

        private async void InputDirButton_Click(object sender, RoutedEventArgs e)
        {
            var paths = await CommonDialog.ShowInputDialogAsync("请输入目录，一行一个", multiLines: true, maxLines: int.MaxValue);
            if (paths != null)
            {
                foreach (var path in paths.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }
                    if (!Directory.Exists(path))
                    {
                        await CommonDialog.ShowErrorDialogAsync($"目录{path}不存在");
                        continue;
                    }
                    try
                    {
                        ViewModel.AddSyncDir(path);
                    }
                    catch (Exception ex)
                    {
                        await CommonDialog.ShowErrorDialogAsync(ex.Message,null, "添加失败");
                    }
                }
            }
        }
    }

    public class Step1ViewModel : ViewModelBase<FileBase>
    {
        private string outputFile;
        private ObservableCollection<string> selectedDirs = new ObservableCollection<string>();

        [JsonIgnore]
        public string OutputFile
        {
            get => outputFile;
            set => this.SetValueAndNotify(ref outputFile, value, nameof(OutputFile));
        }

        public ObservableCollection<string> SyncDirs
        {
            get => selectedDirs;
            set => this.SetValueAndNotify(ref selectedDirs, value, nameof(SyncDirs));
        }

        public void AddSyncDir(string path)
        {
            DirectoryInfo newDirInfo = new DirectoryInfo(path);

            // 检查新目录与现有目录是否相同
            foreach (string existingPath in SyncDirs)
            {
                DirectoryInfo existingDirInfo = new DirectoryInfo(existingPath);

                if (existingDirInfo.FullName.Equals(newDirInfo.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"目录 '{path}' 已经存在，不能重复添加。");
                }
            }

            // 检查新目录是否是现有目录的子目录或父目录
            foreach (string existingPath in SyncDirs)
            {
                DirectoryInfo existingDirInfo = new DirectoryInfo(existingPath);

                // 检查新目录是否是现有目录的子目录
                DirectoryInfo temp = newDirInfo;
                while (temp.Parent != null)
                {
                    if (temp.Parent.FullName.Equals(existingDirInfo.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"新目录 '{path}' 是现有目录 '{existingPath}' 的子目录，不能添加。");
                    }
                    temp = temp.Parent;
                }

                // 检查新目录是否是现有目录的父目录
                temp = existingDirInfo;
                while (temp.Parent != null)
                {
                    if (temp.Parent.FullName.Equals(newDirInfo.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"新目录 '{path}' 是现有目录 '{existingPath}' 的父目录，不能添加。");
                    }
                    temp = temp.Parent;
                }
            }

            SyncDirs.Add(path);

        }
    }
}