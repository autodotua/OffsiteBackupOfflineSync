using FzLib.Collection;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step2Utility : UtilityBase
    {
        private volatile int index = 0;
        private IEnumerable<LocalAndOffsiteDir> localAndOffsiteDirs;
        public Dictionary<string, List<string>> LocalDirectories { get; } = new Dictionary<string, List<string>>();
        public List<SyncFile> UpdateFiles { get; } = new List<SyncFile>();
        public static IList<LocalAndOffsiteDir> MatchLocalAndOffsiteDirs(Step1Model step1, string[] localSearchingDirs)
        {
            var matchingDirs =
                 step1.Files
                 .Select(p => p.TopDirectory)
                 .Distinct()
                 .Select(p => new LocalAndOffsiteDir() { OffsiteDir = p, })
                 .ToList();
            var matchingDirsDic = matchingDirs.ToDictionary(p => Path.GetFileName(p.OffsiteDir), p => p);
            foreach (var localSearchingDir in localSearchingDirs)
            {
                foreach (var subLocalDir in new DirectoryInfo(localSearchingDir).EnumerateDirectories())
                {
                    if (matchingDirsDic.ContainsKey(subLocalDir.Name))
                    {
                        matchingDirsDic[subLocalDir.Name].LocalDir = subLocalDir.FullName;
                    }
                }
            }
            return matchingDirs;
        }

        public bool Export(string outputDir, ExportMode exportMode)
        {
            bool allOk = true;
            stopping = false;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            var files = UpdateFiles.Where(p => p.Checked).ToList();
            Dictionary<string, string> offsiteTopDir2LocalDir = localAndOffsiteDirs.ToDictionary(p => p.OffsiteDir, p => p.LocalDir);
            long totalLength = files.Where(p => p.UpdateType != FileUpdateType.Delete).Sum(p => p.Length);
            long length = 0;
            StringBuilder batScript = new StringBuilder();
            StringBuilder ps1Script = new StringBuilder();
            batScript.AppendLine("@echo off");
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
                    InvokeMessageReceivedEvent($"正在处理：{file.Path}");
                    string sourceFile = Path.Combine(offsiteTopDir2LocalDir[file.TopDirectory], file.Path);
                    string destFile = Path.Combine(outputDir, file.TempName);
                    if (File.Exists(destFile))
                    {
                        FileInfo existingFile = new FileInfo(destFile);
                        if (existingFile.Length == file.Length 
                            && existingFile.LastWriteTime == file.LastWriteTime
                            && exportMode!=ExportMode.Script)
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
                    switch (exportMode)
                    {
                        case ExportMode.PreferHardLink:
                            try
                            {
                                CreateHardLink(destFile, sourceFile);
                            }
                            catch (IOException)
                            {
                                goto copy;
                            }
                            catch (Exception ex)
                            {
                                allOk = false;
                                file.Message = ex.Message;
                            }
                            break;
                        case ExportMode.Copy:
                        copy:
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
                            break;
                        case ExportMode.HardLink:
                            try
                            {
                                CreateHardLink(destFile, sourceFile);
                            }
                            catch (IOException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                allOk = false;
                                file.Message = ex.Message;
                            }
                            break;
                        case ExportMode.Script:
                            batScript.AppendLine($"if exist \"{file.TempName}\" (");
                            batScript.AppendLine($"echo 文件 {sourceFile} 已存在");
                            batScript.AppendLine($") else (");
                            batScript.AppendLine($"echo 正在复制 {sourceFile}");
                            batScript.AppendLine($"copy \"{sourceFile}\" \"{file.TempName}\"");
                            batScript.AppendLine($")");

                            ps1Script.AppendLine($"if ([System.IO.File]::Exists(\"{file.TempName}\")){{");
                            ps1Script.AppendLine($"\"文件 {sourceFile} 已存在\"");
                            ps1Script.AppendLine($"}}else{{");
                            ps1Script.AppendLine($"\"正在复制 {sourceFile}\"");
                            ps1Script.AppendLine($"cp \"{sourceFile}\" \"{file.TempName}\"");
                            ps1Script.AppendLine($"}}");
                            break;
                        default:
                            break;
                    }

                    InvokeProgressReceivedEvent(length += file.Length, totalLength);
                }
                file.Complete = true;
            }

            if (exportMode == ExportMode.Script)
            {
                batScript.AppendLine("echo 复制完成");
                batScript.AppendLine("pause");
                var encoding = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
                File.WriteAllText(Path.Combine(outputDir, "CopyToHere.bat"), batScript.ToString(), encoding);

                ps1Script.AppendLine("\"复制完成\"");
                ps1Script.AppendLine("pause");
                File.WriteAllText(Path.Combine(outputDir, "CopyToHere.ps1"), ps1Script.ToString(), Encoding.UTF8);
            }

            Step2Model model = new Step2Model()
            {
                Files = files,
                LocalDirectories = LocalDirectories
            };

            WriteToZip(model, Path.Combine(outputDir, "file.obos2"));
            return allOk;
        }

        /// <summary>
        /// 搜索
        /// </summary>
        /// <param name="localDir">本地目录</param>
        /// <param name="offsiteSnapshotFile">异地快照文件</param>
        /// <param name="blackList">黑名单</param>
        /// <param name="blackListUseRegex">黑名单是否启用正则</param>
        /// <param name="maxTimeTolerance">对比时修改时间容差</param>
        public void Search(IEnumerable<LocalAndOffsiteDir> localAndOffsiteDirs, Step1Model offsite, string blackList, bool blackListUseRegex, double maxTimeTolerance, bool checkMoveIgnoreFileName)
        {
            stopping = false;
            UpdateFiles.Clear();
            LocalDirectories.Clear();
            index = 0;
            this.localAndOffsiteDirs = localAndOffsiteDirs;

            InvokeMessageReceivedEvent($"正在初始化");
            InitializeBlackList(blackList, blackListUseRegex, out string[] blacks, out Regex[] blackRegexs);
            //将异地文件根据顶级目录
            var offsiteTopDir2Files = offsite.Files.GroupBy(p => p.TopDirectory).ToDictionary(p => p.Key, p => p.ToList());
            //用于之后寻找差异文件的哈希表
            Dictionary<string, byte> localFiles = new Dictionary<string, byte>();
            HashSet<string> offsiteTopDirs = localAndOffsiteDirs.Select(p => p.OffsiteDir).ToHashSet();
            if (offsiteTopDirs.Count != localAndOffsiteDirs.Count())
            {
                throw new ArgumentException("异地顶级目录存在重复", nameof(localAndOffsiteDirs));
            }
            foreach (var file in offsiteTopDir2Files)
            {
                if (!offsiteTopDirs.Contains(file.Key))
                {
                    throw new ArgumentException($"快照中存在顶级目录{file.Key}但{nameof(localAndOffsiteDirs)}未提供", nameof(localAndOffsiteDirs));
                }
            }

            //枚举本地文件，寻找离线快照中是否存在相同文件
            foreach (var localAndOffsiteDir in localAndOffsiteDirs)
            {
                var localDir = new DirectoryInfo(localAndOffsiteDir.LocalDir);
                var offsiteDir = new DirectoryInfo(localAndOffsiteDir.OffsiteDir);
                InvokeMessageReceivedEvent($"正在查找：{localDir}");
                var localFileList = localDir.EnumerateFiles("*", SearchOption.AllDirectories).ToList();

                //从路径、文件名、时间、长度寻找本地文件的字典
                string offsiteTopDirectory = localAndOffsiteDir.OffsiteDir;
                Dictionary<string, SyncFile> offsitePath2File = offsiteTopDir2Files[offsiteTopDirectory].ToDictionary(p => p.Path);
                Dictionary<string, List<SyncFile>> offsiteName2File = offsiteTopDir2Files[offsiteTopDirectory].GroupBy(p => p.Name).ToDictionary(p => p.Key, p => p.ToList());
                Dictionary<DateTime, List<SyncFile>> offsiteTime2File = offsiteTopDir2Files[offsiteTopDirectory].GroupBy(p => p.LastWriteTime).ToDictionary(p => p.Key, p => p.ToList());
                Dictionary<long, List<SyncFile>> offsiteLength2File = offsiteTopDir2Files[offsiteTopDirectory].GroupBy(p => p.Length).ToDictionary(p => p.Key, p => p.ToList());


                foreach (var file in localFileList)
                {

#if DEBUG
                    TestUtility.SleepInDebug();
                    Debug.WriteLine(file);
#endif
                    if (stopping)
                    {
                        break;
                    }
                    string relativePath = Path.GetRelativePath(localDir.FullName, file.FullName);
                    InvokeMessageReceivedEvent($"正在比对第 {++index} 个文件：{relativePath}");
                    localFiles.Add(Path.Combine(localDir.Name, relativePath), 0);
                    if (IsInBlackList(file.Name, file.FullName, blacks, blackRegexs, blackListUseRegex))
                    {
                        continue;
                    }

                    if (offsitePath2File.ContainsKey(relativePath))//路径相同，说明是没有变化或者文件被修改
                    {
                        var offsiteFile = offsitePath2File[relativePath];
                        if ((offsiteFile.LastWriteTime - file.LastWriteTime).Duration().TotalSeconds < maxTimeTolerance
                        && offsiteFile.Length == file.Length)//文件没有发生改动
                        {
                            continue;
                        }

                        //文件发生改变
                        var newFile = new SyncFile()
                        {
                            Path = relativePath,
                            Name = file.Name,
                            Length = file.Length,
                            LastWriteTime = file.LastWriteTime,
                            UpdateType = FileUpdateType.Modify,
                            TopDirectory = offsiteTopDirectory,
                        };
                        if ((offsiteFile.LastWriteTime - file.LastWriteTime).TotalSeconds > maxTimeTolerance)
                        {
                            newFile.Message = "异地文件时间晚于本地文件时间";
                        }
                        UpdateFiles.Add(newFile);
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
                                TopDirectory = offsiteTopDirectory,
                            };
                            UpdateFiles.Add(movedFile);
                            localFiles.Add(Path.Combine(offsiteDir.Name, offsiteMovedFile.Path), 0);//如果被移动了，那么不需要进行删除判断，所以要把异地的文件地址也加入进去。
                        }
                        else//新增文件
                        {
                            var newFile = new SyncFile()
                            {
                                Path = relativePath,
                                Name = file.Name,
                                Length = file.Length,
                                LastWriteTime = file.LastWriteTime,
                                UpdateType = FileUpdateType.Add,
                                TopDirectory = offsiteTopDirectory,
                            };
                            UpdateFiles.Add(newFile);
                        }
                    }
                }

                if (stopping)
                {
                    throw new OperationCanceledException();
                }
                List<string> localSubDirs = new List<string>();
                foreach (var subDir in localDir.EnumerateDirectories("*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(localDir.FullName, subDir.FullName);
                    localSubDirs.Add(relativePath);
                }
                LocalDirectories.Add(localAndOffsiteDir.OffsiteDir, localSubDirs);

                //枚举异地快照，查找本地文件中不存在的文件
                index = 0;
                foreach (var file in offsiteTopDir2Files[offsiteTopDirectory])
                {
                    var offsitePathWithTopDir = Path.Combine(Path.GetFileName(file.TopDirectory), file.Path);
                    if (IsInBlackList(file.Name, offsitePathWithTopDir, blacks, blackRegexs, blackListUseRegex))
                    {
                        continue;
                    }
                    InvokeMessageReceivedEvent($"正在查找删除的文件：{++index} / {offsite.Files.Count}");
                    if (!localFiles.ContainsKey(offsitePathWithTopDir))
                    {
                        file.UpdateType = FileUpdateType.Delete;
                        UpdateFiles.Add(file);
                    }
                }

            }

        }
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        private static void CreateHardLink(string link, string source)
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

        private static string GetTempFileName(SyncFile file, SHA256 sha256)
        {
            string featureCode = $"{file.TopDirectory}{file.Path}{file.LastWriteTime}{file.Length}";

            var bytes = Encoding.UTF8.GetBytes(featureCode);
            var code = sha256.ComputeHash(bytes);
            return Convert.ToHexString(code);
        }
    }

}


