using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.Collections.Concurrent;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step1Utility : UtilityBase
    {
        private volatile int index = 0;
        public static Step1Model ReadStep1Model(string jsonPath)
        {

            return JsonConvert.DeserializeObject<Step1Model>(File.ReadAllText(jsonPath));
        }

        public Step1Model Enumerate(IEnumerable<string> dirs, string jsonPath)
        {
            stopping = false;
            index = 0;
            List<SyncFile> syncFiles = new List<SyncFile>();
            foreach (var dir in dirs)
            {
                //为了加快速度，用了一些技巧
                InvokeMessageReceivedEvent($"正在搜索：{dir}，已加入 {syncFiles.Count} 个文件");
                List<FileInfo> files = new DirectoryInfo(dir)
                    .EnumerateFiles("*", SearchOption.AllDirectories).ToList();

                SyncFile[] tempFiles = new SyncFile[files.Count]; //临时使用数组以实现多线程并加快速度
                Parallel.For(0, files.Count, (i, state) =>
                {
                    if (stopping)
                    {
                        state.Stop();
                    }
                    tempFiles[i] = new SyncFile(files[i], dir); //使用索引，一对一映射文件和数组元素的关系
#if DEBUG
                    TestUtility.SleepInDebug();
#endif
                    InvokeMessageReceivedEvent($"正在加入：{dir}，已加入 {++index} 个文件");
                });
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
                syncFiles.AddRange(tempFiles); //加入临时的数组
                var dirInfo = new DirectoryInfo(dir);
            }
            Step1Model model = new Step1Model()
            {
                Files = syncFiles.ToList(),
            };
            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText(jsonPath, json);
            return model;
        }
    }

}


