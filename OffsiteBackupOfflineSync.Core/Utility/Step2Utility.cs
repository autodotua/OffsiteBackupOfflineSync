using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step2Utility : UtilityBase
    {
        public List<SyncFile> UpdateFiles { get; } = new List<SyncFile>();
        public List<string> LocalDirectories { get; } = new List<string>();
        private string localDir;
        private volatile int index = 0;

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="localDir">本地目录</param>
        /// <param name="offsiteSnapshotFile">异地快照文件</param>
        /// <param name="blackList">黑名单</param>
        /// <param name="blackListUseRegex">黑名单是否启用正则</param>
        /// <param name="maxTimeTolerance">对比时修改时间容差</param>
        public void Search(string localDir, string offsiteSnapshotFile, string blackList, bool blackListUseRegex, double maxTimeTolerance)
        {
            this.localDir = localDir;
            UpdateFiles.Clear();
            LocalDirectories.Clear();
            index = 0;
            string[] blacks = blackList?.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>(); ;
            List<Regex> blackRegexs = blacks.Select(p => new Regex(p, RegexOptions.IgnoreCase)).ToList();
            ConcurrentBag<SyncFile> tempUpdateFiles = new ConcurrentBag<SyncFile>(); //临时的多线程需要更新文件列表
            Step1Model offsite = JsonConvert.DeserializeObject<Step1Model>(File.ReadAllText(offsiteSnapshotFile));
            Dictionary<string, SyncFile> path2file = offsite.Files.ToDictionary(p => p.Path); //从路径寻找本地文件的字典
            ConcurrentDictionary<string, byte> localFiles = new ConcurrentDictionary<string, byte>(); //用于之后寻找差异文件的哈希表

            //枚举本地文件，寻找离线快照中是否存在相同文件
            foreach (var dir in new DirectoryInfo(localDir).EnumerateDirectories())
            {
                if (!offsite.TopDirectories.Contains(dir.Name))
                {
                    continue;
                }

                InvokeMessageReceivedEvent($"正在查找 {dir}");
                var localFileList = dir.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
                Parallel.ForEach(localFileList, file =>
                {
                    string relativePath = Path.GetRelativePath(localDir, file.FullName);
                    InvokeMessageReceivedEvent($"正在比对第 {++index} 个文件：{relativePath}");
                    localFiles.TryAdd(relativePath, 0);
                    if (IsInBlackList(file.Name, file.FullName, blacks, blackRegexs, blackListUseRegex))
                    {
                        return;
                    }
                    if (path2file.ContainsKey(relativePath))
                    {
                        var offsiteFile = path2file[relativePath];
                        if ((offsiteFile.LastWriteTime - file.LastWriteTime).Duration().TotalSeconds < maxTimeTolerance
                        && offsiteFile.Length == file.Length)//文件没有发生改动
                        {
                            return;
                        }

                        //文件发生改变
                        var newFile = new SyncFile()
                        {
                            Path = relativePath,
                            Name = file.Name,
                            Length = file.Length,
                            LastWriteTime = file.LastWriteTime,
                            UpdateType = FileUpdateType.Modify
                        };
                        if ((offsiteFile.LastWriteTime - file.LastWriteTime).TotalSeconds > maxTimeTolerance)
                        {
                            newFile.Message = "异地文件时间晚于本地文件时间";
                        }
                        tempUpdateFiles.Add(newFile);
                    }
                    else //新增文件
                    {
                        var newFile = new SyncFile()
                        {
                            Path = relativePath,
                            Name = file.Name,
                            Length = file.Length,
                            LastWriteTime = file.LastWriteTime,
                            UpdateType = FileUpdateType.Add
                        };
                        tempUpdateFiles.Add(newFile);
                    }
                });

                LocalDirectories.Add(dir.Name);
                foreach (var subDir in dir.EnumerateDirectories("*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(localDir, subDir.FullName);
                    LocalDirectories.Add(relativePath);
                }
            }
            UpdateFiles.AddRange(tempUpdateFiles);

            //枚举异地快照，查找本地文件中不存在的文件
            index = 0;
            foreach (var file in offsite.Files)
            {
                if (IsInBlackList(file.Name, file.Path, blacks, blackRegexs, blackListUseRegex))
                {
                    continue;
                }
                InvokeMessageReceivedEvent($"正在查找删除的文件：{++index} / {offsite.Files.Count}");
                if (!localFiles.ContainsKey(file.Path))
                {
                    file.UpdateType = FileUpdateType.Delete;
                    UpdateFiles.Add(file);
                }
            }
        }

        public void Export(string outputDir, bool hardLink)
        {
            stopping = false;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            var files = UpdateFiles.Where(p => p.Checked).ToList();
            long totalLength = files.Where(p => p.UpdateType != FileUpdateType.Delete).Sum(p => p.Length);
            long length = 0;
            foreach (var file in files)
            {
                if (file.UpdateType != FileUpdateType.Delete)
                {
                    string name = Guid.NewGuid().ToString();
                    file.TempName = name;
                    InvokeMessageReceivedEvent($"正在复制 {file.Path}");
                    string sourceFile = Path.Combine(localDir, file.Path);
                    string destFile = Path.Combine(outputDir, name);
                    if (hardLink)
                    {
                        CreateHardLink(destFile, sourceFile);
                    }
                    else
                    {
                        File.Copy(sourceFile, destFile);
                    }
                    InvokeProgressReceivedEvent(length += file.Length, totalLength);
                }
                file.Complete = true;
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
            }

            Step2Model model = new Step2Model()
            {
                Files = files,
                LocalDirectories = LocalDirectories
            };

            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText(Path.Combine(outputDir, "file.obos2"), json);
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        private void CreateHardLink(string link, string source)
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(source);
            }

            if (File.Exists(link))
            {
                File.Delete(link);
            }

            if (Path.GetPathRoot(link) != Path.GetPathRoot(source))
            {
                throw new IOException("硬链接的两者必须在同一个分区中");
            }

            bool value = CreateHardLink(link, source, IntPtr.Zero);
            if (!value)
            {
                throw new IOException("未知错误，无法创建硬链接");
            }
        }
    }

}


