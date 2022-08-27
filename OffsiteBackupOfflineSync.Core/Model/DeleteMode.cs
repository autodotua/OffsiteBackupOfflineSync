using System.ComponentModel;

namespace OffsiteBackupOfflineSync.Model
{
    public enum DeleteMode
    {
        [Description("直接删除")]
        Delete,
        [Description("移动到回收站")]
        MoveToRecycleBin,
        [Description("移动到删除文件夹")]
        MoveToDeletedFolder
    }
}
