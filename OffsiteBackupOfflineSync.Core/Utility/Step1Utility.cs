using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step1Utility : UtilityBase
    {
        private volatile int index = 0;
        public static Step1Model ReadStep1Model(string outputPath)
        {
            return ReadFromZip<Step1Model>(outputPath);
        }

        public Step1Model Enumerate(IEnumerable<string> dirs, string outputPath)
        {
            stopping = false;
            index = 0;
            List<SyncFile> syncFiles = new List<SyncFile>();
            var groups = dirs.GroupBy(p => Path.GetFileName(p));
            foreach (var group in groups)
            {
                if(group.Count()>1)
                {
                    throw new ArgumentException("存在重复的顶级目录名：" + group.Key);
                }
            }

            foreach (var dir in dirs)
            {
                foreach (var file in new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (stopping)
                    {
                        throw new OperationCanceledException();
                    }
                    syncFiles.Add(new SyncFile(file, dir));
#if DEBUG
                    TestUtility.SleepInDebug();
#endif
                    InvokeMessageReceivedEvent($"正在搜索：{dir}，已加入 {++index} 个文件");
                }
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
            }
            InvokeMessageReceivedEvent($"正在保存快照");

            Step1Model model = new Step1Model()
            {
                Files = syncFiles.ToList(),
            };
            WriteToZip(model, outputPath);
            return model;
        }

    }
}


