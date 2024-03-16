using OffsiteBackupOfflineSync.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OffsiteBackupOfflineSync.Converters
{
    public class SyncFilePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            if(value is not SyncFile file)
            {
                throw new ArgumentException($"{nameof(value)}必须为{nameof(SyncFile)}类型");
            }

            string topDirName = Path.GetFileName(file.TopDirectory);
            string relativePath = file.Path;
            switch (file.UpdateType)
            {
                case FileUpdateType.None:
                case FileUpdateType.Add:
                case FileUpdateType.Modify:
                case FileUpdateType.Delete:
                    return Path.Combine(topDirName,relativePath);
                case FileUpdateType.Move:
                    return $"{Path.Combine(topDirName, file.OldPath)} -> {Path.Combine(topDirName, relativePath)}";
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
