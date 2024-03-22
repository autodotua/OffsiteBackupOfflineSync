using FzLib;
using FzLib.WPF;
using Microsoft.Win32;
using ModernWpf.FzExtension.CommonDialog;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.Utility;
using OffsiteBackupOfflineSync.Utils;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CommonDialog = ModernWpf.FzExtension.CommonDialog.CommonDialog;

namespace OffsiteBackupOfflineSync.UI
{
    /// <summary>
    /// RebuildPanel.xaml 的交互逻辑
    /// </summary>
    public partial class Step3 : UserControl
    {
        private Step3Utility u = null;

        public Step3(Step3ViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }

        public Step3ViewModel ViewModel { get; }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.PatchDir))
            {
                await CommonDialog.ShowErrorDialogAsync("未设置补丁目录");
                return;
            }
            if (!Directory.Exists(ViewModel.PatchDir))
            {
                await CommonDialog.ShowErrorDialogAsync("补丁目录不存在");
                return;
            }
            try
            {
                u = new Step3Utility();
                PanelHelper.RegisterMessageAndProgressEvent(u, ViewModel);
                ViewModel.UpdateStatus(StatusType.Analyzing);
                await Task.Run(() =>
                {
                    u.Analyze(ViewModel.PatchDir);
                    ViewModel.Files = new ObservableCollection<SyncFile>(u.UpdateFiles); ;
                });
                ViewModel.UpdateStatus(ViewModel.Files.Count > 0 ? StatusType.Analyzed : StatusType.Ready);
            }
            catch (OperationCanceledException)
            {
                ViewModel.UpdateStatus(StatusType.Ready);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "分析失败");
                ViewModel.UpdateStatus(StatusType.Ready);
            }
        }

        private void BrowsePatchDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new OpenFolderDialog().GetPath(this.GetWindow());
            if (path != null)
            {
                ViewModel.PatchDir = path;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Files?.ForEach(p => p.Checked = true);
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Files?.ForEach(p => p.Checked = false);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateStatus(StatusType.Stopping);
            u.Stop();
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.UpdateStatus(StatusType.Processing);
                await Task.Run(() =>
                {
                    u.Update(ViewModel.DeleteMode,Configs.Instance.DeleteDirName);
                    u.AnalyzeEmptyDirectories();
                });

                if (u.DeletingDirectories.Count != 0)
                {
                    if (await CommonDialog.ShowYesNoDialogAsync("删除空目录",
                        $"有{u.DeletingDirectories.Count}个已不存在于本地的空目录，是否删除？",
                        string.Join(Environment.NewLine, u.DeletingDirectories.Select(p => Path.Combine(p.TopDirectory, p.Path)))))
                    {
                        u.DeleteEmptyDirectories(ViewModel.DeleteMode, Configs.Instance.DeleteDirName);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "更新失败");
            }
            finally
            {
                ViewModel.UpdateStatus(StatusType.Analyzed);
            }
        }
    }

    public class Step3ViewModel : ViewModelBase<SyncFile>
    {
        private DeleteMode deleteMode = DeleteMode.MoveToDeletedFolder;
        private string patchDir;
        public string DeletedDir { get; set; } = "被删除和替换的文件备份";

        public DeleteMode DeleteMode
        {
            get => deleteMode;
            set => this.SetValueAndNotify(ref deleteMode, value, nameof(DeleteMode));
        }

        public IEnumerable DeleteModes => Enum.GetValues<DeleteMode>();



        public string PatchDir
        {
            get => patchDir;
            set => this.SetValueAndNotify(ref patchDir, value, nameof(PatchDir));
        }
    }
}