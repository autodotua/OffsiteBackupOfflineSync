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

namespace OffsiteBackupOfflineSync.UI
{
    /// <summary>
    /// RebuildPanel.xaml 的交互逻辑
    /// </summary>
    public partial class Step3 : UserControl
    {
        Step3Utility u = new Step3Utility();
        public Step3()
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
        public Step3ViewModel ViewModel { get; } = new Step3ViewModel();


        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnAnalyze.IsEnabled = btnRebuild.IsEnabled = false;
                ViewModel.Message = "正在重建分析";
                await Task.Run(() =>
                {
                    u.Analyze(ViewModel.PatchDir);
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


        private void BrowseOffsiteDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.OffsiteDir = path;
            }
        }

        private void BrowsePatchDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.PatchDir = path;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                stkConfig.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnRebuild.IsEnabled = false;
                ViewModel.Progress = 0;
                await Task.Run(() =>
                {
                    u.Update(ViewModel.OffsiteDir);
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
                btnRebuild.IsEnabled = true;
                btnStop.IsEnabled = false;
                ViewModel.Message = "就绪";
                ViewModel.Progress = ViewModel.ProgressMax;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            btnStop.IsEnabled = false;
            u.Stop();
        }
    }


    public class Step3ViewModel : PatchAndApplyViewModelBase
    {
        private string offsiteDir = @"C:\Users\autod\Desktop\test\remote2";
        private string patchDir = @"C:\Users\autod\Desktop\test\patch";

        public string OffsiteDir
        {
            get => offsiteDir;
            set => this.SetValueAndNotify(ref offsiteDir, value, nameof(OffsiteDir));
        }
        public string PatchDir
        {
            get => patchDir;
            set => this.SetValueAndNotify(ref patchDir, value, nameof(PatchDir));
        }


    }

}
