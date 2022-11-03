using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.FzExtension;
using FzLib;
using System.ComponentModel;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using OffsiteBackupOfflineSync;
using System.Diagnostics;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.Utility;
using System.Collections.ObjectModel;
using System.Collections;

namespace OffsiteBackupOfflineSync.UI
{
    /// <summary>
    /// RebuildPanel.xaml 的交互逻辑
    /// </summary>
    public partial class CloneFileTree : UserControl
    {
        private readonly CloneFileTreeUtility u = new CloneFileTreeUtility();
        public CloneFileTree()
        {
            DataContext = ViewModel;
            InitializeComponent();
            u.MessageReceived += (s, e) =>
            {
                ViewModel.Message = e.Message;
            };
            u.ProgressUpdated += (s, e) =>
            {
                if (e.MaxValue != ViewModel.ProgressMax)
                {
                    ViewModel.ProgressMax = e.MaxValue;
                }
                ViewModel.Progress = e.Value;
            };
        }
        public CloneFileTreeViewModel ViewModel { get; } = new CloneFileTreeViewModel();


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
            if (string.IsNullOrEmpty(ViewModel.DistDir))
            {
                await CommonDialog.ShowErrorDialogAsync("目标目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.DistDir))
            {
                await CommonDialog.ShowErrorDialogAsync("目标目录为空");
                return;
            }
            try
            {
                btnCreate.IsEnabled = false;
                btnStop.IsEnabled = true;
                ViewModel.Message = "正在分析";
                ViewModel.Working = true;
                ViewModel.ProgressIndeterminate = true;
                await Task.Run(() =>
                {
                    u.EnumerateAllFiles(ViewModel.SourceDir);
                    ViewModel.UpdateFiles = new ObservableCollection<SyncFile>(u.Files); ;
                });
                btnCreate.IsEnabled = true;
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "分析失败");
            }
            finally
            {
                ViewModel.Message = "就绪";
                ViewModel.Working = false;
                ViewModel.ProgressIndeterminate = false;
                btnStop.IsEnabled = false;
            }
        }


        private void BrowseDistDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.DistDir = path;
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
                stkConfig.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnCreate.IsEnabled = false;
                ViewModel.Progress = 0;
                ViewModel.Working = true;
                await Task.Run(() =>
                {
                    u.CloneFiles(ViewModel.DistDir);
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
                stkConfig.IsEnabled = true;
                btnCreate.IsEnabled = true;
                btnStop.IsEnabled = false;
                ViewModel.Message = "就绪";
                ViewModel.Working = false;
                ViewModel.Progress = ViewModel.ProgressMax;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateFiles?.ForEach(p => p.Checked = true);
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateFiles?.ForEach(p => p.Checked = false);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            btnStop.IsEnabled = false;
            u.Stop();
        }
    }


    public class CloneFileTreeViewModel :ViewModelBase
    {
        private string sourceDir;
        private string distDir;

        public string SourceDir
        {
            get => sourceDir;
            set => this.SetValueAndNotify(ref sourceDir, value, nameof(SourceDir));
        }
        public string DistDir
        {
            get => distDir;
            set => this.SetValueAndNotify(ref distDir, value, nameof(DistDir));
        }
    }

}
