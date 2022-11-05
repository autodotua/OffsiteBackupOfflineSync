using FzLib;
using Microsoft.WindowsAPICodePack.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.Utility;
using OffsiteBackupOfflineSync.WPF.UI;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OffsiteBackupOfflineSync.UI
{
    /// <summary>
    /// RebuildPanel.xaml 的交互逻辑
    /// </summary>
    public partial class CloneFileTree : UserControl
    {
        private readonly CloneFileTreeUtility u = new CloneFileTreeUtility();

        public CloneFileTree(CloneFileTreeViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
            PanelHelper.RegisterMessageAndProgressEvent(u, viewModel);
        }

        public CloneFileTreeViewModel ViewModel { get; }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.SourceDir))
            {
                await CommonDialog.ShowErrorDialogAsync("源目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.SourceDir))
            {
                await CommonDialog.ShowErrorDialogAsync("源目录不存在");
                return;
            }
            if (string.IsNullOrEmpty(ViewModel.DestDir))
            {
                await CommonDialog.ShowErrorDialogAsync("目标目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.DestDir))
            {
                await CommonDialog.ShowErrorDialogAsync("目标目录为空");
                return;
            }
            try
            {
                ViewModel.UpdateStatus(StatusType.Analyzing);
                await Task.Run(() =>
                {
                    u.EnumerateAllFiles(ViewModel.SourceDir);
                    ViewModel.Files = new ObservableCollection<FileTreeFile>(u.Files); ;
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

        private void BrowseDestDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.DestDir = path;
            }
        }

        private void BrowseSourceDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.SourceDir = path;
            }
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.UpdateStatus(StatusType.Processing);
                await Task.Run(() =>
                {
                    u.CloneFiles(ViewModel.DestDir);
                });
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

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateStatus(StatusType.Stopping);
            u.Stop();
        }
    }

    public class CloneFileTreeViewModel : ViewModelBase<FileTreeFile>
    {
        private string destDir;
        private string sourceDir;
        public string DestDir
        {
            get => destDir;
            set => this.SetValueAndNotify(ref destDir, value, nameof(DestDir));
        }

        public string SourceDir
        {
            get => sourceDir;
            set => this.SetValueAndNotify(ref sourceDir, value, nameof(SourceDir));
        }
    }
}