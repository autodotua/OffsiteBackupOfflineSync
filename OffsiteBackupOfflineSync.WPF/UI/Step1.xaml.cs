using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.FzExtension;
using FzLib;
using System.ComponentModel;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using OffsiteBackupOfflineSync;
using System.Diagnostics;
using FzLib.WPF.Converters;
using FzLib.WPF;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.Utility;

namespace OffsiteBackupOfflineSync.UI
{
    /// <summary>
    /// UpdatePanel.xaml 的交互逻辑
    /// </summary>
    public partial class Step1 : UserControl
    {
        Step1Utility u=new Step1Utility();
        public Step1()
        {
            DataContext = ViewModel;
            InitializeComponent();

        }
        public Step1ViewModel ViewModel { get; } = new Step1ViewModel();

        private async void AddDirButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FileFilterCollection().CreateOpenFileDialog();
            dialog.Multiselect = true;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog(this.GetWindow()) == CommonFileDialogResult.Ok)
            {
                foreach (var dir in dialog.FileNames)
                {
                    if (!ViewModel.Dirs.Contains(dir))
                    {
                        if (Path.GetDirectoryName(dir) == null)
                        {
                            await CommonDialog.ShowErrorDialogAsync("不可选择磁盘根目录");
                            return;
                        }
                        ViewModel.Dirs.Add(dir);
                    }
                }
            }
        }

        private void RemoveDirButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedDir != null && ViewModel.Dirs.Contains(ViewModel.SelectedDir))
            {
                ViewModel.Dirs.Remove(ViewModel.SelectedDir);
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Dirs.Count == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("目录为空");
                return;
            }
            string path = new FileFilterCollection().Add("异地备份快照", "obos1").CreateSaveFileDialog().GetFilePath();
            if (path != null)
            {
               
                ViewModel.Working = true;
                btnExport.IsEnabled = false;
                try
                {
                    await Task.Run(() =>
                    {
                        u.Enumerate(ViewModel.Dirs, path);
                    });
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "导出失败");
                }
                finally
                {
                    ViewModel.Working = false;
                    btnExport.IsEnabled = true;
                }
            }
        }
    }


    public class Step1ViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<string> dirs = new ObservableCollection<string>();
        public ObservableCollection<string> Dirs
        {
            get => dirs;
            set => this.SetValueAndNotify(ref dirs, value, nameof(Dirs));
        }
        private string selectedDir;
        public string SelectedDir
        {
            get => selectedDir;
            set => this.SetValueAndNotify(ref selectedDir, value, nameof(SelectedDir));
        }
        private bool working;
        public bool Working
        {
            get => working;
            set => this.SetValueAndNotify(ref working, value, nameof(Working));
        }


        public event PropertyChangedEventHandler PropertyChanged;
    }

}
