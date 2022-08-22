using Microsoft.WindowsAPICodePack.FzExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FzLib;
using System.ComponentModel;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using Mapster;
using FzLib.DataStorage.Serialization;
using OffsiteBackupOfflineSync.WPF.UI;

namespace OffsiteBackupOfflineSync.UI
{
    public partial class MainWindow : Window
    {
        private readonly Step1 step1 = new Step1();
        private readonly Step2 step2 = new Step2();
        private readonly Step3 step3 = new Step3();
        private readonly Configs config = new Configs();
        private readonly string configPath = "configs.json";
        public MainWindow()
        {
            try
            {
                config.TryLoadFromJsonFile(configPath);
            }
            catch (Exception ex)
            {

            }
            InitializeComponent();
            DataContext = ViewModel;
            step1.ViewModel.Dir = config.Step1Dir;
            step2.ViewModel.OffsiteSnapshot = config.Step2OffsiteSnapshot;
            step2.ViewModel.LocalDir = config.Step2LocalDir;
            step3.ViewModel.PatchDir = config.Step3PatchDir;
            step3.ViewModel.OffsiteDir = config.Step3OffsiteDir;
            frame.Navigate(step1);
        }

        MainWindowViewModel ViewModel { get; } = new MainWindowViewModel();
        private void NavigationView_SelectionChanged(ModernWpf.Controls.NavigationView sender, ModernWpf.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                frame.Navigate(typeof(AboutPage));
                ViewModel.NavigationViewHeader = Title;
            }
            else
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
            SaveConfig();
        }

        private void SaveConfig()
        {
            config.Step1Dir = step1.ViewModel.Dir;
            config.Step2OffsiteSnapshot = step2.ViewModel.OffsiteSnapshot;
            config.Step2LocalDir = step2.ViewModel.LocalDir;
            config.Step3PatchDir = step3.ViewModel.PatchDir;
            config.Step3OffsiteDir = step3.ViewModel.OffsiteDir;

            config.Save(configPath);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
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
