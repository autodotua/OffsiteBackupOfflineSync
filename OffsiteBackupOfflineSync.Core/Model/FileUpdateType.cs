using System.ComponentModel;

namespace OffsiteBackupOfflineSync.Model
{
    public enum FileUpdateType
    {
        [Description("新增")]
        Add,
        [Description("修改")]
        Modify,
        [Description("删除")]
        Delete,
    }
}
