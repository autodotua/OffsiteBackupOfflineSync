using FzLib;
using ModernWpf.FzExtension.CommonDialog;
using OffsiteBackupOfflineSync.WPF.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace OffsiteBackupOfflineSync.UI
{
    public partial class MainWindow : Window
    {
        private CloneFileTree cloneFileTree;
        private FilesGoHome filesGoHome;
        private Step1 step1;
        private Step2 step2;
        private Step3 step3;
        PeriodicTimer saveConfigTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        private void LoadFromConfigs()
        {
            var config = Configs.Instance.CurrentConfig;
            step1 = new Step1(config.Step1);
            step2 = new Step2(config.Step2);
            step3 = new Step3(config.Step3);
            cloneFileTree = new CloneFileTree(config.CloneFileTree);
            filesGoHome = new FilesGoHome(config.FilesGoHome);
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
            LoadFromConfigs();

            frame.Navigate(step1);
            StartSaveConfigTimer().ConfigureAwait(false);
        }

        private MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();

        private void NavigationView_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                frame.Navigate(typeof(AboutPage));
                ViewModel.NavigationViewHeader = Title;
            }
            else if (sender.MenuItems.Contains(args.SelectedItem))
            {
                switch (sender.MenuItems.IndexOf(args.SelectedItem))
                {
                    case 0:
                        frame.Navigate(step1);
                        ViewModel.NavigationViewHeader = "请使用异地磁盘完成这一步";
                        break;

                    case 1:
                        frame.Navigate(step2);
                        ViewModel.NavigationViewHeader = "请使用本地磁盘完成这一步";
                        break;

                    case 2:
                        frame.Navigate(step3);
                        ViewModel.NavigationViewHeader = "请使用异地磁盘完成这一步";
                        break;
                }
            }
            else
            {
                switch (sender.FooterMenuItems.IndexOf(args.SelectedItem))
                {
                    case 0:
                        frame.Navigate(cloneFileTree);
                        ViewModel.NavigationViewHeader = "创建保留文件大小和修改时间，但不占用空间的文件结构";
                        break;

                    case 1:
                        frame.Navigate(filesGoHome);
                        ViewModel.NavigationViewHeader = "将源目录中的文件结构匹配为模板目录中的文件结构";
                        break;
                }
            }
            SaveConfig();
        }

        private void SaveConfig()
        {
            var config = Configs.Instance;

            config.Save();
        }
        private async Task StartSaveConfigTimer()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            while (await saveConfigTimer.WaitForNextTickAsync())
            {
                SaveConfig();
            }
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            saveConfigTimer.Dispose();
            SaveConfig();
        }

        private void SelectConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string name = (e.OriginalSource as System.Windows.Controls.MenuItem).DataContext as string;
            if (name == Configs.Instance.CurrentConfigName)
            {
                return;
            }
            ChangeConfigName(name);

        }

        private void ChangeConfigName(string name)
        {
            Configs.Instance.CurrentConfigName = name;
            ViewModel.CurrentConfigName = name;

            LoadFromConfigs();
            object currentContent = (nav.Content as Frame).Content;
            if (currentContent is Step1)
            {
                frame.Navigate(step1);
                ViewModel.NavigationViewHeader = "请使用异地磁盘完成这一步";
            }
            else if (currentContent is Step2)
            {
                frame.Navigate(step2);
                ViewModel.NavigationViewHeader = "请使用本地磁盘完成这一步";
            }
            else if (currentContent is Step3)
            {
                frame.Navigate(step3);
                ViewModel.NavigationViewHeader = "请使用异地磁盘完成这一步";
            }
            else if (currentContent is CloneFileTree)
            {
                frame.Navigate(cloneFileTree);
                ViewModel.NavigationViewHeader = "创建保留文件大小和修改时间，但不占用空间的文件结构";
            }
            else if (currentContent is FilesGoHome)
            {
                frame.Navigate(filesGoHome);
                ViewModel.NavigationViewHeader = "将源目录中的文件结构匹配为模板目录中的文件结构";
            }
            else
            {
                //Debug.Assert(false);
            }

            Configs.Instance.Save();
        }

        private async void RemoveConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Configs.Instance.ConfigCollection.Count == 1)
            {
                await CommonDialog.ShowErrorDialogAsync("必须保留至少一个配置");
                return;
            }

            if (await CommonDialog.ShowYesNoDialogAsync("是否删除当前配置？"))
            {
                Configs.Instance.ConfigCollection.Remove(ViewModel.CurrentConfigName);
                ViewModel.ConfigNames.Add(ViewModel.CurrentConfigName);
                ChangeConfigName(ViewModel.ConfigNames.First());
                Configs.Instance.Save();
            }
        }

        private async void AddConfigMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string name = await CommonDialog.ShowInputDialogAsync("请输入配置名", "新配置");
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (Configs.Instance.ConfigCollection.ContainsKey(name))
                {
                    await CommonDialog.ShowErrorDialogAsync("该配置名已存在");
                    return;
                }
                Configs.Instance.ConfigCollection.Add(name, new SingleConfig());
                ViewModel.ConfigNames.Add(name);
                ChangeConfigName(name);
                Configs.Instance.Save();
            }
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string navigationViewHeader = "请使用异地磁盘完成这一步";

        public event PropertyChangedEventHandler PropertyChanged;

        public string NavigationViewHeader
        {
            get => navigationViewHeader;
            set => this.SetValueAndNotify(ref navigationViewHeader, value, nameof(NavigationViewHeader));
        }

        private string currentConfigName = Configs.Instance.CurrentConfigName;

        public string CurrentConfigName
        {
            get => currentConfigName;
            set => this.SetValueAndNotify(ref currentConfigName, value, nameof(CurrentConfigName));
        }


        public ObservableCollection<string> ConfigNames { get; set; } = new ObservableCollection<string>(Configs.Instance.ConfigCollection.Keys);
    }
}