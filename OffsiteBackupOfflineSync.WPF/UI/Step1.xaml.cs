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
       private readonly Step1Utility u=new Step1Utility();
        public Step1()
        {
            DataContext = ViewModel;
            InitializeComponent();
            ViewModel.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName==nameof(ViewModel.Dirs))
                {
                    lst.SelectAll();
                }
            };
            u.MessageReceived += (s, e) =>
            {
                ViewModel.Message = e.Message;
            };

        }
        public Step1ViewModel ViewModel { get; } = new Step1ViewModel();

        private void BrowseDirButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.Dir = path;
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dirs = lst.SelectedItems.Cast<string>().ToList();
            if (dirs.Count == 0)
            {
                await CommonDialog.ShowErrorDialogAsync("选择的目录为空");
                return;
            }
            string path = new FileFilterCollection().Add("异地备份快照", "obos1")
                .CreateSaveFileDialog()
                .SetDefault($"{ Environment.MachineName} - {Path.GetFileName(ViewModel.Dir)}")
                .GetFilePath();
            if (path != null)
            {
               
                ViewModel.Working = true;
                btnExport.IsEnabled = false;
                try
                {
                    ViewModel.Message = "正在查找文件";
                    await Task.Run(() =>
                    {
                        u.Enumerate(dirs, path);
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
                    ViewModel.Message = "就绪";
                }
            }
        }
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            lst.SelectAll();
        }

        private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
        {
            lst.UnselectAll();
        }
    }


    public class Step1ViewModel : INotifyPropertyChanged
    {
        private string dir;
        private List<string> dirs = new List<string>();
        private string message = "就绪";
        private bool working;
        public event PropertyChangedEventHandler PropertyChanged;

        public string Dir
        {
            get => dir;
            set
            {
                this.SetValueAndNotify(ref dir, value, nameof(Dir));
                if (Directory.Exists(value))
                {
                    Dirs = Directory.EnumerateDirectories(value)
                        .Where(p=>!p.EndsWith("System Volume Information"))
                        .Where(p=>!p.Contains('$'))
                        .ToList();
                }
                else
                {
                    Dirs = new List<string>();
                }
            }
        }

        public List<string> Dirs
        {
            get => dirs;
            set => this.SetValueAndNotify(ref dirs, value, nameof(Dirs));
        }

        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }
        public bool Working
        {
            get => working;
            set => this.SetValueAndNotify(ref working, value, nameof(Working));
        }
    }

}
