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
    public partial class FilesGoHome : UserControl
    {
        private readonly FilesGoHomeUtility u = new FilesGoHomeUtility();
        public FilesGoHome(FilesGoHomeViewModel viewModel)
        {
            ViewModel = viewModel;
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
        public FilesGoHomeViewModel ViewModel { get; } 


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
                btnCreate.IsEnabled = false;
                btnStop.IsEnabled = true;
                ViewModel.Message = "正在分析";
                ViewModel.Working = true;
                ViewModel.ProgressIndeterminate = true;
                await Task.Run(() =>
                {
                 

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
                stkConfig.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnCreate.IsEnabled = false;
                ViewModel.Progress = 0;
                ViewModel.Working = true;
                await Task.Run(() =>
                {

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
            ViewModel.Files?.ForEach(p => p.Checked = true);
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Files?.ForEach(p => p.Checked = false);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            btnStop.IsEnabled = false;
            u.Stop();
        }
    }


    public class FilesGoHomeViewModel :ViewModelBase<FilesGoHomeFile>
    {
        private string destDir;
        private string filter;
        private bool filterIncludePath;
        private bool filterReverse;
        private string sourceDir;
        private string templateDir;
        public string DestDir
        {
            get => destDir;
            set => this.SetValueAndNotify(ref destDir, value, nameof(DestDir));
        }

        public string Filter
        {
            get => filter;
            set => this.SetValueAndNotify(ref filter, value, nameof(Filter));
        }

        public bool FilterIncludePath
        {
            get => filterIncludePath;
            set => this.SetValueAndNotify(ref filterIncludePath, value, nameof(FilterIncludePath));
        }

        public bool FilterReverse
        {
            get => filterReverse;
            set => this.SetValueAndNotify(ref filterReverse, value, nameof(FilterReverse));
        }

        public string SourceDir
        {
            get => sourceDir;
            set => this.SetValueAndNotify(ref sourceDir, value, nameof(SourceDir));
        }

        public string TemplateDir
        {
            get => templateDir;
            set => this.SetValueAndNotify(ref templateDir, value, nameof(TemplateDir));
        }
    }

}
