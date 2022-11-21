using System.ComponentModel;

namespace OffsiteBackupOfflineSync.Model
{
    public enum FileUpdateType
    {
        None,
        [Description("新增")]
        Add,
        [Description("修改")]
        Modify,
        [Description("删除")]
        Delete,
        [Description("移动")]
        Move
    }
}
