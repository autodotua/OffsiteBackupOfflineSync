using System.ComponentModel;

namespace OffsiteBackupOfflineSync.Model
{
    public enum ExportMode
    {
        [Description("复制")]
        Copy,
        [Description("硬链接")]
        HardLink,
        [Description("硬链接优先")]
        PreferHardLink,
        [Description("脚本")]
        Script
    }
}
