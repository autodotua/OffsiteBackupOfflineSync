using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step2Utility : SyncUtilityBase
    {
        public List<SyncFile> UpdateFiles { get; } = new List<SyncFile>();
        private string localDir;
        private volatile int index = 0;

        private bool IsInBlackList(string name,string path,IList<string> balckList,IList<Regex> blackRegexs, bool blackListUseRegex)
        {
            for (int i = 0; i < balckList.Count; i++)
            {
                if (blackListUseRegex) //正则
                {
                    if (balckList[i].Contains('\\') || balckList[i].Contains('/')) //目录
                    {
                        if (blackRegexs[i].IsMatch(path))
                        {
                            return true;
                        }
                    }
                    else //文件
                    {
                        if (blackRegexs[i].IsMatch(name))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (balckList[i].Contains('\\') || balckList[i].Contains('/')) //目录
                    {
                        if (path.Contains(balckList[i]))
                        {
                            return true;
                        }
                    }
                    else //文件
                    {

                        if (name.Contains(balckList[i]))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void Search(string localDir, string offsiteSnapshotFile,string blackList,bool blackListUseRegex,double maxTimeTolerance)
        {
            this.localDir = localDir;
            UpdateFiles.Clear();
            index = 0;
            string[] blacks = blackList.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            List<Regex> blackRegexs = blacks.Select(p => new Regex(p, RegexOptions.IgnoreCase)).ToList();
            ConcurrentBag<SyncFile> tempUpdateFiles = new ConcurrentBag<SyncFile>(); //临时的多线程需要更新文件列表
            Step1Model offsiteFiles = JsonConvert.DeserializeObject<Step1Model>(File.ReadAllText(offsiteSnapshotFile));
            Dictionary<string, SyncFile> path2file = offsiteFiles.Files.ToDictionary(p => p.Path); //从路径寻找本地文件的字典
            ConcurrentDictionary<string, byte> localFiles = new ConcurrentDictionary<string, byte>(); //用于之后寻找差异文件的哈希表
            foreach (var dir in new DirectoryInfo(localDir).EnumerateDirectories())
            {
                if (!offsiteFiles.TopDirectories.Any(p => p.Name == dir.Name))
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
                        if ((offsiteFile.LastWriteTime - file.LastWriteTime).TotalSeconds> maxTimeTolerance)
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


            }
            UpdateFiles.AddRange(tempUpdateFiles);
            index = 0;
            foreach (var file in offsiteFiles.Files)
            {
                if (IsInBlackList(file.Name, file.Path, blacks, blackRegexs, blackListUseRegex))
                {
                    continue;
                }
                InvokeMessageReceivedEvent($"正在查找删除的文件：{++index} / {offsiteFiles.Files.Count}");
                if (!localFiles.ContainsKey(file.Path))
                {
                    file.UpdateType = FileUpdateType.Delete;
                    UpdateFiles.Add(file);
                }
            }
        }

        public void Export(string outputDir)
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
                    File.Copy(Path.Combine(localDir, file.Path), Path.Combine(outputDir, name));
                    InvokeProgressReceivedEvent(length += file.Length, totalLength);
                }
                file.Complete = true;
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
            }

            var json = JsonConvert.SerializeObject(files, Formatting.Indented);
            File.WriteAllText(Path.Combine(outputDir, "file.obos2"), json);
        }
    }

}


