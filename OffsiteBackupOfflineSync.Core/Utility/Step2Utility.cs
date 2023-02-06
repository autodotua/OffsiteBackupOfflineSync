using FzLib.Collection;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

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
        public void Search(string localDir, string offsiteSnapshotFile, string blackList, bool blackListUseRegex, double maxTimeTolerance, bool checkMoveIgnoreFileName)
        {
            stopping = false;
            this.localDir = localDir;
            UpdateFiles.Clear();
            LocalDirectories.Clear();
            index = 0;
            InitializeBlackList(blackList, blackListUseRegex, out string[] blacks, out Regex[] blackRegexs);
            ConcurrentBag<SyncFile> tempUpdateFiles = new ConcurrentBag<SyncFile>(); //临时的多线程需要更新文件列表
            Step1Model offsite = JsonConvert.DeserializeObject<Step1Model>(File.ReadAllText(offsiteSnapshotFile));
            Dictionary<string, SyncFile> offsitePath2File = offsite.Files.ToDictionary(p => p.Path); //从路径寻找本地文件的字典
            Dictionary<string, List<SyncFile>> offsiteName2File = offsite.Files.GroupBy(p => p.Name).ToDictionary(p => p.Key, p => p.ToList());
            Dictionary<DateTime, List<SyncFile>> offsiteTime2File = offsite.Files.GroupBy(p => p.LastWriteTime).ToDictionary(p => p.Key, p => p.ToList());
            Dictionary<long, List<SyncFile>> offsiteLength2File = offsite.Files.GroupBy(p => p.Length).ToDictionary(p => p.Key, p => p.ToList());
            ConcurrentDictionary<string, byte> localFiles = new ConcurrentDictionary<string, byte>(); //用于之后寻找差异文件的哈希表


            //枚举本地文件，寻找离线快照中是否存在相同文件
            foreach (var dir in new DirectoryInfo(localDir).EnumerateDirectories())
            {
                if (!offsite.TopDirectories.Contains(dir.Name))
                {
                    continue;
                }

                InvokeMessageReceivedEvent($"正在查找：{dir}");
                var localFileList = dir.EnumerateFiles("*", SearchOption.AllDirectories).ToList();
                Parallel.ForEach(localFileList, (file, state) =>
                {
#if DEBUG
                    TestUtility.SleepInDebug();
#endif
                    if (stopping)
                    {
                        state.Stop();
                    }
                    string relativePath = Path.GetRelativePath(localDir, file.FullName);
                    InvokeMessageReceivedEvent($"正在比对第 {++index} 个文件：{relativePath}");
                    localFiles.TryAdd(relativePath, 0);
                    if (IsInBlackList(file.Name, file.FullName, blacks, blackRegexs, blackListUseRegex))
                    {
                        return;
                    }
                    if (offsitePath2File.ContainsKey(relativePath))//路径相同，说明是没有变化或者文件被修改
                    {
                        var offsiteFile = offsitePath2File[relativePath];
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
                    else //新增文件或文件被移动或重命名
                    {
                        var sameFiles = !checkMoveIgnoreFileName ?
                            (offsiteTime2File.GetOrDefault(file.LastWriteTime) ?? Enumerable.Empty<SyncFile>())
                            .Intersect(offsiteLength2File.GetOrDefault(file.Length) ?? Enumerable.Empty<SyncFile>()) :
                            (offsiteName2File.GetOrDefault(file.Name) ?? Enumerable.Empty<SyncFile>())
                             .Intersect(offsiteTime2File.GetOrDefault(file.LastWriteTime) ?? Enumerable.Empty<SyncFile>())
                             .Intersect(offsiteLength2File.GetOrDefault(file.Length) ?? Enumerable.Empty<SyncFile>());
                        if (sameFiles.Count() == 1)//存在被移动或重命名的文件，并且为一对一关系
                        {
                            var offsiteMovedFile = sameFiles.First();
                            var movedFile = new SyncFile()
                            {
                                Path = relativePath,
                                OldPath = offsiteMovedFile.Path,
                                Name = file.Name,
                                Length = file.Length,
                                LastWriteTime = file.LastWriteTime,
                                UpdateType = FileUpdateType.Move,
                            };
                            tempUpdateFiles.Add(movedFile);
                            localFiles.TryAdd(offsiteMovedFile.Path, 0);//如果被移动了，那么不需要进行删除判断，所以要把异地的文件地址也加入进去。
                        }
                        else//新增文件
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
                    }
                });

                if (stopping)
                {
                    throw new OperationCanceledException();
                }
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

        public bool Export(string outputDir, bool hardLink)
        {
            bool allOk = true;
            stopping = false;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            var files = UpdateFiles.Where(p => p.Checked).ToList();
            long totalLength = files.Where(p => p.UpdateType != FileUpdateType.Delete).Sum(p => p.Length);
            long length = 0;
            using var sha256 = SHA256.Create();
            foreach (var file in files)
            {
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
#if DEBUG
                TestUtility.SleepInDebug();
#endif
                if (file.UpdateType is not (FileUpdateType.Delete or FileUpdateType.Move))
                {
                    file.TempName = GetTempFileName(file, sha256);
                    InvokeMessageReceivedEvent($"正在复制：{file.Path}");
                    string sourceFile = Path.Combine(localDir, file.Path);
                    string destFile = Path.Combine(outputDir, file.TempName);
                    if (File.Exists(destFile))
                    {
                        FileInfo existingFile = new FileInfo(destFile);
                        if (existingFile.Length == file.Length && existingFile.LastWriteTime == file.LastWriteTime)
                        {
                            InvokeProgressReceivedEvent(length += file.Length, totalLength);
                            continue;
                        }
                        else
                        {
                            try
                            {
                                File.Delete(destFile);
                            }
                            catch (IOException ex)
                            {
                                throw new IOException($"修改时间或长度与待写入文件{file.Path}不同的目标补丁文件{destFile}已存在，但无法删除：{ex.Message}", ex);
                            }
                        }
                    }
                    if (hardLink)
                    {
                        try
                        {
                            CreateHardLink(destFile, sourceFile);
                        }
                        catch (IOException ex)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            allOk = false;
                            file.Message = ex.Message;
                        }
                    }
                    else
                    {
                        int tryCount = 10;

                        while (--tryCount > 0)
                        {
                            if (tryCount < 9 && File.Exists(destFile))
                            {
                                File.Delete(destFile);
                            }
                            try
                            {
                                File.Copy(sourceFile, destFile);
                                tryCount = 0;
                            }
                            catch (IOException ex)
                            {
                                Debug.WriteLine($"复制文件{sourceFile}到{destFile}失败：{ex.Message}，剩余{tryCount}次重试");
                                if (tryCount == 0)
                                {
                                    allOk = false;
                                    file.Message = ex.Message;
                                }
                                Thread.Sleep(1000);
                            }
                        }
                    }
                    InvokeProgressReceivedEvent(length += file.Length, totalLength);
                }
                file.Complete = true;
            }

            Step2Model model = new Step2Model()
            {
                Files = files,
                LocalDirectories = LocalDirectories
            };

            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText(Path.Combine(outputDir, "file.obos2"), json);
            return allOk;
        }

        private string GetTempFileName(SyncFile file, SHA256 sha256)
        {
            string featureCode = $"{file.Path}{file.LastWriteTime}{file.Length}";

            var bytes = Encoding.UTF8.GetBytes(featureCode);
            var code = sha256.ComputeHash(bytes);
            return Convert.ToHexString(code);
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
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
                throw new Exception($"未知错误，无法创建硬链接：" + Marshal.GetLastWin32Error());
            }
        }
    }

}


