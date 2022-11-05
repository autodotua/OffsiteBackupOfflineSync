namespace OffsiteBackupOfflineSync.UI
{
    public enum StatusType
    {
        /// <summary>
        /// 就绪
        /// </summary>
        Ready,

        /// <summary>
        /// 正在分析
        /// </summary>
        Analyzing,

        /// <summary>
        /// 分析完毕
        /// </summary>
        Analyzed,

        /// <summary>
        /// 处理中
        /// </summary>
        Processing,

        /// <summary>
        /// 正在停止
        /// </summary>
        Stopping
    }
}