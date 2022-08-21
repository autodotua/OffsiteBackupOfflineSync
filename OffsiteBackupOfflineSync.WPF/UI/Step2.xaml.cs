using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.FzExtension;
using FzLib;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using OffsiteBackupOfflineSync;
using System.Diagnostics;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Utility;

namespace OffsiteBackupOfflineSync.UI
{

    /// <summary>
    /// RebuildPanel.xaml 的交互逻辑
    /// </summary>
    public partial class Step2 : UserControl
    {
        Step2Utility u = new Step2Utility();
        public Step2()
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
        public Step2ViewModel ViewModel { get; } = new Step2ViewModel();

        private void BrowseLocalDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.LocalDir = path;
            }
        }

        private void BrowseOffsiteSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().Add("异地备份快照", "obos1").CreateOpenFileDialog().GetFilePath();
            if (path != null)
            {
                ViewModel.OffsiteSnapshot = path;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var outputDir = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (outputDir != null)
            {
                try
                {
                    stkConfig.IsEnabled = false;
                    btnStop.IsEnabled = true;
                    btnRebuild.IsEnabled = false;
                    ViewModel.Progress = 0;
                    await Task.Run(() =>
                    {
                        u.Export(outputDir);
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
                    stkConfig.IsEnabled = true;
                    btnRebuild.IsEnabled = true;
                    btnStop.IsEnabled = false;
                    ViewModel.Message = "就绪";
                    ViewModel.Progress = ViewModel.ProgressMax;
                }
            }
        }

        private async void SearchChangeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnAnalyze.IsEnabled = btnRebuild.IsEnabled = false;
                ViewModel.Message = "正在重建分析";
                await Task.Run(() =>
                {
                    u.Analyze(ViewModel.LocalDir, ViewModel.OffsiteSnapshot);
                    ViewModel.UpdateFiles = u.UpdateFiles;
                });
                btnRebuild.IsEnabled = true;
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "解析失败");
            }
            finally
            {
                btnAnalyze.IsEnabled = true;
                ViewModel.Message = "就绪";
            }
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            btnStop.IsEnabled = false;
            u.Stop();
        }
    }
    public class Step2ViewModel : PatchAndApplyViewModelBase
    {
        private string localDir = @"C:\Users\autod\Desktop\test\local";
        private string offsiteSnapshot = @"C:\Users\autod\Desktop\test\0821.obos1";
        public string LocalDir
        {
            get => localDir;
            set => this.SetValueAndNotify(ref localDir, value, nameof(LocalDir));
        }

        public string OffsiteSnapshot
        {
            get => offsiteSnapshot;
            set => this.SetValueAndNotify(ref offsiteSnapshot, value, nameof(OffsiteSnapshot));
        }
    }

}
