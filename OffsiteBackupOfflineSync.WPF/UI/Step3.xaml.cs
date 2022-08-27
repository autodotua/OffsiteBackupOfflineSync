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
    public partial class Step3 : UserControl
    {
        private readonly Step3Utility u = new Step3Utility();
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
            if (string.IsNullOrEmpty(ViewModel.OffsiteDir))
            {
                await CommonDialog.ShowErrorDialogAsync("异地目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.OffsiteDir))
            {
                await CommonDialog.ShowErrorDialogAsync("异地目录不存在");
                return;
            }
            if (string.IsNullOrEmpty(ViewModel.PatchDir))
            {
                await CommonDialog.ShowErrorDialogAsync("补丁目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.PatchDir))
            {
                await CommonDialog.ShowErrorDialogAsync("补丁目录不存在");
                return;
            }
            try
            {
                btnAnalyze.IsEnabled = btnRebuild.IsEnabled = false;
                ViewModel.Message = "正在分析";
                await Task.Run(() =>
                {
                    u.Analyze(ViewModel.PatchDir,ViewModel.OffsiteDir);
                    ViewModel.UpdateFiles = new ObservableCollection<SyncFile>(u.UpdateFiles); ;
                });
                btnRebuild.IsEnabled = true;
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "分析失败");
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
                    u.Update(ViewModel.OffsiteDir,Configs.Instance.DeletedDir,ViewModel.DeleteMode);
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
        private string offsiteDir;
        private string patchDir;

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

        private DeleteMode deleteMode = DeleteMode.MoveToDeletedFolder;
        public DeleteMode DeleteMode
        {
            get => deleteMode;
            set => this.SetValueAndNotify(ref deleteMode, value, nameof(DeleteMode));
        }

        public IEnumerable DeleteModes => Enum.GetValues<DeleteMode>();
    }

}
