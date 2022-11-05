using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.UI;
using OffsiteBackupOfflineSync.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffsiteBackupOfflineSync.WPF.UI
{
    public static class PanelHelper
    {
        public static void RegisterMessageAndProgressEvent<T>(UtilityBase utility,ViewModelBase<T> viewModel) where T:FileBase
        {
            utility.MessageReceived += (s, e) =>
            {
                viewModel.Message = e.Message;
            };
            utility.ProgressUpdated += (s, e) =>
            {
                if (e.MaxValue != viewModel.ProgressMax)
                {
                    viewModel.ProgressMax = e.MaxValue;
                }
                viewModel.Progress = e.Value;
            };
        }
    }
}
