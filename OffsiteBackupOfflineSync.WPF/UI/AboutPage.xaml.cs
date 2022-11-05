using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using OffsiteBackupOfflineSync.Utility;
using System.Windows.Controls;

namespace OffsiteBackupOfflineSync.WPF.UI
{
    /// <summary>
    /// AboutPage.xaml 的交互逻辑
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private async void TestButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string path = new CommonOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                IsEnabled = false;
                try
                {
                    await TestUtility.CreateSyncTestFilesAsync(path);
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex);
                }
                IsEnabled = true;
            }
        }
    }
}