namespace OffsiteBackupOfflineSync.Model
{
    public class Step2Model
    {
        public List<SyncFile> Files { get; set; }

        /// <summary>
        /// 本地目录下的所有子目录，用于删除存在于异地但不存在于本地的空目录。
        /// </summary>
        /// <remarks>
        /// <see cref="KeyValuePair{TKey,TValue}.Key"/>表示异地的顶级目录，
        ///  <see cref="KeyValuePair{TKey,TValue}.Value"/>表示该目录对应的本地目录中包含的所有子目录的相对路径
        /// </remarks>
        public Dictionary<string, List<string>> LocalDirectories { get; set; }
    }
}
