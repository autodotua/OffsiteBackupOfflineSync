using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.Collections.Concurrent;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step1Utility : UtilityBase
    {
        private volatile int index = 0;
        public void Enumerate(IEnumerable<string> dirs, string jsonPath)
        {
            index = 0;
            List<SyncFile> syncFiles = new List<SyncFile>();
            List<string> topDirectories = new List<string>();
            foreach (var dir in dirs)
            {
                //为了加快速度，用了一些技巧
                InvokeMessageReceivedEvent($"正在搜索 {dir}，已加入 {syncFiles.Count} 个文件");
                List<FileInfo> files = new DirectoryInfo(dir)
                    .EnumerateFiles("*", SearchOption.AllDirectories).ToList();

                SyncFile[] tempFiles = new SyncFile[files.Count]; //临时使用数组以实现多线程并加快速度
                Parallel.For(0, files.Count, i =>
                {
                    tempFiles[i] = new SyncFile(files[i], dir); //使用索引，一对一映射文件和数组元素的关系

                    InvokeMessageReceivedEvent($"正在加入 {dir}，已加入 {++index} 个文件");
                });
                syncFiles.AddRange(tempFiles); //加入临时的数组
                //syncFiles.AddRange(new DirectoryInfo(dir)
                //    .EnumerateFiles("*", SearchOption.AllDirectories)
                //    .Select(file => new SyncFile()
                //    {
                //        Name = file.Name,
                //        Path = Path.GetRelativePath(Path.GetDirectoryName(dir), file.FullName),
                //        LastWriteTime = file.LastWriteTime,
                //        Length = file.Length,
                //    }));
                var dirInfo = new DirectoryInfo(dir);
                topDirectories.Add(dirInfo.Name);
            }
            Step1Model model = new Step1Model()
            {
                Files = syncFiles.ToList(),
                TopDirectories = topDirectories,
            };
            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText(jsonPath, json);
        }
    }

}


