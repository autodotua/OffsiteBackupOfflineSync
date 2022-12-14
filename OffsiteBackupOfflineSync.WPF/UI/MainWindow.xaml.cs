using FzLib;
using OffsiteBackupOfflineSync.WPF.UI;
using System.ComponentModel;
using System.Windows;

namespace OffsiteBackupOfflineSync.UI
{
    public partial class MainWindow : Window
    {
        private readonly CloneFileTree cloneFileTree;
        private readonly FilesGoHome filesGoHome;
        private readonly Step1 step1;
        private readonly Step2 step2;
        private readonly Step3 step3;
        PeriodicTimer saveConfigTimer = new PeriodicTimer(TimeSpan.FromSeconds(10));

        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
            var config = Configs.Instance;

            step1 = new Step1(config.Step1);
            step2 = new Step2(config.Step2);
            step3 = new Step3(config.Step3);
            cloneFileTree = new CloneFileTree(config.CloneFileTree);
            filesGoHome = new FilesGoHome(config.FilesGoHome);

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
    }
}