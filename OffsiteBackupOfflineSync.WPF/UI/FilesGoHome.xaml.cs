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
            ViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.StartsWith("Display"))
                {
                    UpdateList();
                }
            };
        }
        public FilesGoHomeViewModel ViewModel { get; }


        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.TemplateDir))
            {
                await CommonDialog.ShowErrorDialogAsync("模板目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.TemplateDir))
            {
                await CommonDialog.ShowErrorDialogAsync("模板目录不存在");
                return;
            }
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
            try
            {
                btnCopyOrMove.IsEnabled = false;
                btnStop.IsEnabled = true;
                ViewModel.Message = "正在分析";
                ViewModel.Working = true;
                ViewModel.ProgressIndeterminate = true;
                await Task.Run(() =>
                {
                    u.FindMatches(ViewModel.TemplateDir, ViewModel.SourceDir,
                        ViewModel.CompareName, ViewModel.CompareModifiedTime, ViewModel.CompareLength,
                        ViewModel.BlackList, ViewModel.BlackListUseRegex, Configs.MaxTimeTolerance);
                    UpdateList();
                });
                btnCopyOrMove.IsEnabled = true;
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

        private void BrowseTemplateDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.TemplateDir = path;
            }

        }

        private async void CopyOrMoveButton_Click(object sender, RoutedEventArgs e)
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
            try
            {
                stkConfig.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnCopyOrMove.IsEnabled = false;
                ViewModel.Progress = 0;
                ViewModel.Working = true;
                int copyMoveIndex = await CommonDialog.ShowSelectItemDialogAsync("请选择复制或是移动",
                       new SelectDialogItem[] {
                        new SelectDialogItem("复制"),
                        new SelectDialogItem("移动")
                       });
                if (copyMoveIndex >= 0)
                {
                    await Task.Run(() =>
                    {
                        u.CopyOrMove(ViewModel.Files.Where(p => p.Checked), ViewModel.SourceDir, ViewModel.DestDir, copyMoveIndex == 0);
                    });
                }
                else
                {
                    throw new OperationCanceledException();
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
                stkConfig.IsEnabled = true;
                btnCopyOrMove.IsEnabled = true;
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

        private void UpdateList()
        {
            if (u.WrongPositionFiles == null || u.RightPositionFiles == null)
            {
                ViewModel.Files = new ObservableCollection<GoHomeFile>();
                return;
            }
            IEnumerable<GoHomeFile> files = u.WrongPositionFiles;
            if (ViewModel.DisplayRightPositon)
            {
                files = files.Concat(u.RightPositionFiles);
            }
            if (!ViewModel.DisplayMultipleMatches)
            {
                files = files.Where(p => p.MultipleMatchs == false);
            }
            files = files.OrderBy(p => p.Path);
            ViewModel.Files = new ObservableCollection<GoHomeFile>(files);
        }
    }


    public class FilesGoHomeViewModel : ViewModelBase<GoHomeFile>
    {
        private string blackList = "Thumbs.db";
        private bool blackListUseRegex;
        private string destDir;
        private bool displayMultipleMatches = true;
        private bool displayRightPositon = false;
        private string sourceDir;
        private string templateDir;
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

        public bool CompareLength { get; set; } = true;

        public bool CompareModifiedTime { get; set; } = true;

        public bool CompareName { get; set; } = true;

        public string DestDir
        {
            get => destDir;
            set => this.SetValueAndNotify(ref destDir, value, nameof(DestDir));
        }

        public bool DisplayMultipleMatches
        {
            get => displayMultipleMatches;
            set => this.SetValueAndNotify(ref displayMultipleMatches, value, nameof(DisplayMultipleMatches));
        }

        public bool DisplayRightPositon
        {
            get => displayRightPositon;
            set => this.SetValueAndNotify(ref displayRightPositon, value, nameof(DisplayRightPositon));
        }

        public string SourceDir
        {
            get => sourceDir;
            set
            {
                if (string.IsNullOrWhiteSpace(DestDir) || sourceDir == DestDir)
                {
                    DestDir = value;
                }
                this.SetValueAndNotify(ref sourceDir, value, nameof(SourceDir));
            }
        }

        public string TemplateDir
        {
            get => templateDir;
            set => this.SetValueAndNotify(ref templateDir, value, nameof(TemplateDir));
        }
    }

}
