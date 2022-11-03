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
using System.Collections.ObjectModel;
using OffsiteBackupOfflineSync.Model;

namespace OffsiteBackupOfflineSync.UI
{

    /// <summary>
    /// RebuildPanel.xaml 的交互逻辑
    /// </summary>
    public partial class Step2 : UserControl
    {
        private readonly Step2Utility u = new Step2Utility();
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
            if (ViewModel.UpdateFiles.Count == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("本地和异地没有差异");
            }
            var outputDir = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (outputDir != null)
            {
                try
                {
                    ViewModel.Working = true;
                    stkConfig.IsEnabled = false;
                    btnStop.IsEnabled = true;
                    btnPatch.IsEnabled = false;
                    ViewModel.Progress = 0;
                    await Task.Run(() =>
                    {
                        u.Export(outputDir,ViewModel.HardLink);
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
                    btnPatch.IsEnabled = true;
                    btnStop.IsEnabled = false;
                    ViewModel.Message = "就绪";
                    ViewModel.Working = false;
                    ViewModel.Progress = ViewModel.ProgressMax;
                }
            }
        }

        private async void SearchChangeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.OffsiteSnapshot))
            {
                await CommonDialog.ShowErrorDialogAsync("快照文件为空");
                return;
            }
            if (!File.Exists(ViewModel.OffsiteSnapshot))
            {
                await CommonDialog.ShowErrorDialogAsync("快照文件不存在");
                return;
            }
            if (string.IsNullOrEmpty(ViewModel.LocalDir))
            {
                await CommonDialog.ShowErrorDialogAsync("本地目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.LocalDir))
            {
                await CommonDialog.ShowErrorDialogAsync("本地目录不存在");
                return;
            }
            try
            {
                btnPatch.IsEnabled = false;
                ViewModel.Message = "正在查找更改";
                ViewModel.Working = true;
                await Task.Run(() =>
                {
                    u.Search(ViewModel.LocalDir, ViewModel.OffsiteSnapshot, ViewModel.BlackList, ViewModel.BlackListUseRegex, Configs.MaxTimeTolerance);
                    ViewModel.UpdateFiles = new ObservableCollection<SyncFile>(u.UpdateFiles);
                });
                if (ViewModel.UpdateFiles.Count == 0)
                {
                    await CommonDialog.ShowOkDialogAsync("查找完成", "本地和异地没有差异");
                }
                else
                {
                    btnPatch.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "查找失败");
            }
            finally
            {
                ViewModel.Working = false;
                ViewModel.Message = "就绪";
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
    public class Step2ViewModel : ViewModelBase
    {
        private string blackList;
        private bool blackListUseRegex;
        private string localDir;
        private string offsiteSnapshot;
        public string BlackList
        {
            get => blackList;
            set => this.SetValueAndNotify(ref blackList, value, nameof(BlackList));
        }

        public bool BlackListUseRegex
        {
            get => blackListUseRegex;
            set => this.SetValueAndNotify(ref blackListUseRegex, value, nameof(BlackListUseRegex));
        }

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

        private bool hardLink;
        public bool HardLink
        {
            get => hardLink;
            set => this.SetValueAndNotify(ref hardLink, value, nameof(HardLink));
        }

    }

}
