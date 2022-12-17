using System.ComponentModel;

namespace OffsiteBackupOfflineSync.Model
{
    public enum FileUpdateType
    {
        None=0,
        [Description("新增")]
        Add=1,
        [Description("修改")]
        Modify=2,
        [Description("删除")]
        Delete=3,
        [Description("移动")]
        Move=4
    }
}
