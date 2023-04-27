﻿using FzLib;
using Microsoft.WindowsAPICodePack.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.Utility;
using OffsiteBackupOfflineSync.WPF.UI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace OffsiteBackupOfflineSync.UI
{

    /// <summary>
    /// RebuildPanel.xaml 的交互逻辑
    /// </summary>
    public partial class Step2 : UserControl
    {
        private readonly Step2Utility u = new Step2Utility();

        public Step2(Step2ViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
            PanelHelper.RegisterMessageAndProgressEvent(u, viewModel);
        }

        public Step2ViewModel ViewModel { get; }

        private void BrowseLocalDirButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FileFilterCollection().CreateOpenFileDialog();
            dialog.Multiselect = true;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                string path = string.Join(Environment.NewLine, dialog.FileNames);
                if (string.IsNullOrEmpty(ViewModel.LocalDir))
                {
                    ViewModel.LocalDir = path;
                }
                else
                {
                    ViewModel.LocalDir += Environment.NewLine + path;
                }
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

        private void BrowsePatchDirButton_Click(object sender, RoutedEventArgs e)
        {
            var outputDir = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (outputDir != null)
            {
                ViewModel.PatchDir = outputDir;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Files.Count == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("本地和异地没有差异");
                return;
            }
            if (string.IsNullOrWhiteSpace(ViewModel.PatchDir))
            {
                await CommonDialog.ShowErrorDialogAsync("未设置导出补丁目录");
                return;
            }
            try
            {
                ViewModel.UpdateStatus(StatusType.Processing);
                bool allOk = true;
                await Task.Run(() =>
                {
                    allOk = u.Export(ViewModel.PatchDir, ViewModel.HardLink);
                });
                if (!allOk)
                {
                    await CommonDialog.ShowErrorDialogAsync("导出完成，但部分文件出现错误");
                }
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
                ViewModel.UpdateStatus(StatusType.Analyzed);
            }

        }
        Step1Model step1;
        private async void MatchDirsButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ViewModel.OffsiteSnapshot))
            {
                await CommonDialog.ShowErrorDialogAsync("未设置快照文件");
                return;
            }
            if (!File.Exists(ViewModel.OffsiteSnapshot))
            {
                await CommonDialog.ShowErrorDialogAsync("快照文件不存在");
                return;
            }
            if (string.IsNullOrEmpty(ViewModel.LocalDir))
            {
                await CommonDialog.ShowErrorDialogAsync("未设置本地目录");
                return;
            }

            try
            {
                ViewModel.UpdateStatus(StatusType.Analyzing);
                await Task.Run(() =>
                {

                    string[] localSearchingDirs = ViewModel.LocalDir.Split(new char[] { '|', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    step1 = Step1Utility.ReadStep1Model(ViewModel.OffsiteSnapshot);
                    ViewModel.MatchingDirs = new ObservableCollection<LocalAndOffsiteDir>(
                        step1.TopDirectories.Select(p => new LocalAndOffsiteDir() { OffsiteDir = p }));
                    var matchingDirsDic = ViewModel.MatchingDirs.ToDictionary(p => Path.GetFileName(p.OffsiteDir), p => p);
                    foreach (var localSearchingDir in localSearchingDirs)
                    {
                        foreach (var subLocalDir in new DirectoryInfo(localSearchingDir).EnumerateDirectories())
                        {
                            if (matchingDirsDic.ContainsKey(subLocalDir.Name))
                            {
                                matchingDirsDic[subLocalDir.Name].LocalDir = subLocalDir.FullName;
                            }
                        }
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "匹配失败");
            }
            finally
            {
                ViewModel.UpdateStatus(StatusType.Ready);
            }
        }

        private async void SearchChangeButton_Click(object sender, RoutedEventArgs e)
        {

            bool needProcess = false;
            try
            {
                ViewModel.UpdateStatus(StatusType.Analyzing);
                await Task.Run(() =>
                {
                    u.Search(ViewModel.MatchingDirs, step1, ViewModel.BlackList,
                        ViewModel.BlackListUseRegex, Configs.MaxTimeTolerance,
                        ViewModel.MoveFileIgnoreName);
                    ViewModel.Files = new ObservableCollection<SyncFile>(u.UpdateFiles);
                });
                if (ViewModel.Files.Count == 0)
                {
                    await CommonDialog.ShowOkDialogAsync("查找完成", "本地和异地没有差异");
                }
                else
                {
                    needProcess = true;
                }
                ViewModel.UpdateStatus(needProcess ? StatusType.Analyzed : StatusType.Ready);
            }
            catch (OperationCanceledException)
            {
                ViewModel.UpdateStatus(StatusType.Ready);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "查找失败");
                ViewModel.UpdateStatus(StatusType.Ready);
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
    }
    public class Step2ViewModel : ViewModelBase<SyncFile>
    {
        private string blackList = "Thumbs.db";
        private bool blackListUseRegex;
        private bool hardLink;
        private string localDir;
        private ObservableCollection<LocalAndOffsiteDir> matchingDirs;
        private bool moveFileIgnoreName = true;
        private string offsiteSnapshot;

        private string patchDir;
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

        public bool HardLink
        {
            get => hardLink;
            set => this.SetValueAndNotify(ref hardLink, value, nameof(HardLink));
        }

        public string LocalDir
        {
            get => localDir;
            set => this.SetValueAndNotify(ref localDir, value, nameof(LocalDir));
        }

        [JsonIgnore]
        public ObservableCollection<LocalAndOffsiteDir> MatchingDirs
        {
            get => matchingDirs;
            set => this.SetValueAndNotify(ref matchingDirs, value, nameof(MatchingDirs));
        }

        public bool MoveFileIgnoreName
        {
            get => moveFileIgnoreName;
            set => this.SetValueAndNotify(ref moveFileIgnoreName, value, nameof(MoveFileIgnoreName));
        }

        public string OffsiteSnapshot
        {
            get => offsiteSnapshot;
            set => this.SetValueAndNotify(ref offsiteSnapshot, value, nameof(OffsiteSnapshot));
        }

        public string PatchDir
        {
            get => patchDir;
            set => this.SetValueAndNotify(ref patchDir, value, nameof(PatchDir));
        }
    }
}