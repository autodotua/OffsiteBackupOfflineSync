using FzLib.IO;
using OffsiteBackupOfflineSync.Model;
using System.ComponentModel;
using System.Diagnostics;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step3Utility : UtilityBase
    {
        public const string DeleteDirName = "异地备份离线同步-删除的文件";
        private readonly DateTime createTime = DateTime.Now;
        private string patchDir;
        public List<SyncFile> DeletingDirectories { get; private set; }
        public Dictionary<string, List<string>> LocalDirectories { get; private set; }
        public List<SyncFile> UpdateFiles { get; private set; }
        public static string GetNoDuplicateDirectory(string path, string suffixFormat = " ({i})")
        {
            if (!Directory.Exists(path))
            {
                return path;
            }

            if (!suffixFormat.Contains("{i}"))
            {
                throw new ArgumentException("后缀应包含“{i}”以表示索引");
            }

            int num = 2;
            string directoryName = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string text;
            while (true)
            {
                text = Path.Combine(directoryName, fileNameWithoutExtension + suffixFormat.Replace("{i}", num.ToString()) + extension);
                if (!Directory.Exists(text))
                {
                    break;
                }

                num++;
            }

            return text;
        }

        public static string GetNoDuplicateFile(string path, string suffixFormat = " ({i})")
        {
            if (!File.Exists(path))
            {
                return path;
            }

            if (!suffixFormat.Contains("{i}"))
            {
                throw new ArgumentException("后缀应包含“{i}”以表示索引");
            }

            int num = 2;
            string directoryName = Path.GetDirectoryName(path);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            string text;
            while (true)
            {
                text = Path.Combine(directoryName, fileNameWithoutExtension + suffixFormat.Replace("{i}", num.ToString()) + extension);
                if (!File.Exists(text))
                {
                    break;
                }

                num++;
            }

            return text;
        }

        public void Analyze(string patchDir)
        {
            stopping = false;
            this.patchDir = patchDir;
            var step2 = ReadFromZip<Step2Model>(Path.Combine(patchDir, "file.obos2"));

            UpdateFiles = step2.Files;
            LocalDirectories = step2.LocalDirectories;

            //检查文件
            int index = 0;
            foreach (var file in UpdateFiles)
            {
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
#if DEBUG
                TestUtility.SleepInDebug();
#endif
                string patch = file.TempName == null ? null : Path.Combine(patchDir, file.TempName);
                string target = Path.Combine(file.TopDirectory, file.Path);
                string oldPath = file.OldPath == null ? null : Path.Combine(file.TopDirectory, file.OldPath);
                if (file.UpdateType is not (FileUpdateType.Delete or FileUpdateType.Move) && !File.Exists(patch))
                {
                    file.Message = "补丁文件不存在";
                    file.Checked = false;
                }
                else
                {
                    InvokeMessageReceivedEvent($"正在处理：{file.Path}");
                    switch (file.UpdateType)
                    {
                        case FileUpdateType.Add:
                            if (File.Exists(target))
                            {
                                file.Message = "应当为新增文件，但文件已存在";
                            }
                            break;
                        case FileUpdateType.Modify:
                            if (!File.Exists(target))
                            {
                                file.Message = "应当为修改后文件，但文件不存在";
                            }
                            break;
                        case FileUpdateType.Delete:
                            if (!File.Exists(target))
                            {
                                file.Message = "应当为待删除文件，但文件不存在";
                                file.Checked = false;
                            }
                            break;
                        case FileUpdateType.Move:
                            if (!File.Exists(oldPath))
                            {
                                file.Message = "应当为移动后文件，但源文件不存在";
                                file.Checked = false;
                            }
                            else if (File.Exists(target))
                            {
                                file.Message = "应当为移动后文件，但目标文件已存在";
                                file.Checked = false;
                            }
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }
                InvokeProgressReceivedEvent(++index, UpdateFiles.Count);
            }

        }

        public void AnalyzeEmptyDirectories()
        {
            InvokeMessageReceivedEvent($"正在查找空目录");
            DeletingDirectories = new List<SyncFile>();
            //清理空目录
            //HashSet<string> shouldKeepDirs = LocalDirectories
            //    .Select(p => p.Value.Select(q => Path.Combine(p.Key, q)))
            //    .SelectMany(p => p)
            //    .ToHashSet();

            foreach (var topDir in LocalDirectories.Keys)
            {
                HashSet<string> deletingDirsInThisTopDir = new HashSet<string>();
                foreach (var offsiteSubDir in Directory.EnumerateDirectories(topDir, "*", SearchOption.AllDirectories).ToList())
                {
                    if (stopping)
                    {
                        throw new OperationCanceledException();
                    }
                    if (!LocalDirectories[topDir].Contains(Path.GetRelativePath(topDir, offsiteSubDir)))//本地已经没有远程的这个目录了
                    {
                        if (!Directory.EnumerateFiles(offsiteSubDir).Any())//并且远程的这个目录是空的
                        {
                            deletingDirsInThisTopDir.Add(offsiteSubDir);
                        }
                    }
                }



                //通过两层循环，删除位于空目录下的空目录
                foreach (var dir1 in deletingDirsInThisTopDir.ToList())//外层循环，dir1为内层空目录
                {
                    foreach (var dir2 in deletingDirsInThisTopDir)//内存循环，dir2为外层空目录
                    {
                        if (dir1 == dir2)
                        {
                            continue;
                        }
                        if (dir1.StartsWith(dir2))//如果dir2位于dir1的外层，那么dir1就不需要单独删除
                        {
                            deletingDirsInThisTopDir.Remove(dir1);
                            break;
                        }
                    }
                }

                DeletingDirectories.AddRange(deletingDirsInThisTopDir.Select(p => new SyncFile() { Path = p, TopDirectory = topDir }));
            }
        }

        public void DeleteEmptyDirectories(DeleteMode deleteMode)
        {
            foreach (var dir in DeletingDirectories)
            {
                Delete(dir.TopDirectory, dir.Path, deleteMode);
            }
        }

        public void Update(DeleteMode deleteMode)
        {
            stopping = false;
            var updateFiles = UpdateFiles.Where(p => p.Checked).ToList();
            long totalLength = updateFiles.Where(p => p.UpdateType != FileUpdateType.Delete).Sum(p => p.Length);
            long length = 0;

            InvokeProgressReceivedEvent(0, totalLength);
            //更新文件
            foreach (var file in updateFiles.OrderByDescending(p => p.UpdateType))
            {
                //先处理移动，然后处理修改，这样能避免一些问题（2022-12-17）
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
#if DEBUG
                TestUtility.SleepInDebug();
#endif
                InvokeMessageReceivedEvent($"正在处理：{file.Path}");

                try
                {
                    string patch = file.TempName == null ? null : Path.Combine(patchDir, file.TempName);
                    if (file.UpdateType is not (FileUpdateType.Delete or FileUpdateType.Move) && !File.Exists(patch))
                    {
                        throw new Exception("补丁文件不存在");
                    }
                    string target = Path.Combine(file.TopDirectory, file.Path);
                    string oldPath = file.OldPath == null ? null : Path.Combine(file.TopDirectory, file.OldPath);
                    if (!Directory.Exists(Path.GetDirectoryName(target)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                    }
                    switch (file.UpdateType)
                    {
                        case FileUpdateType.Add:
                            if (File.Exists(target))
                            {
                                Delete(file.TopDirectory, target, deleteMode);
                            }
                            File.Copy(patch, target);
                            File.SetLastWriteTime(target, file.LastWriteTime);
                            InvokeProgressReceivedEvent(length += file.Length, totalLength);
                            break;
                        case FileUpdateType.Modify:
                            if (File.Exists(target))
                            {
                                Delete(file.TopDirectory, target, deleteMode);
                            }
                            File.Copy(patch, target);
                            File.SetLastWriteTime(target, file.LastWriteTime);
                            InvokeProgressReceivedEvent(length += file.Length, totalLength);
                            break;
                        case FileUpdateType.Delete:
                            if (!File.Exists(target))
                            {
                                throw new Exception("应当为待删除文件，但文件不存在");
                            }
                            Delete(file.TopDirectory, target, deleteMode);
                            break;

                        case FileUpdateType.Move:
                            if (!File.Exists(oldPath))
                            {
                                throw new Exception("应当为移动后文件，但源文件不存在");
                            }
                            else if (File.Exists(target))
                            {
                                throw new Exception("应当为移动后文件，但目标文件已存在");
                            }
                            File.Move(oldPath, target);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                    file.Complete = true;
                }
                catch (Exception ex)
                {
                    file.Message = $"错误：{ex.Message}";
                }
            }

        }
        private static bool IsDirectory(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.Directory);
        }

        private void Delete(string rootDir, string filePath, DeleteMode deleteMode)
        {
            Debug.Assert(IsDirectory(filePath) || true);
            if (!filePath.StartsWith(rootDir))
            {
                throw new ArgumentException("文件不在目录中");
            }
            switch (deleteMode)
            {
                case DeleteMode.Delete:
                    if (IsDirectory(filePath))
                    {
                        Directory.Delete(filePath, true);
                    }
                    else
                    {
                        File.Delete(filePath);
                    }
                    break;
                case DeleteMode.MoveToRecycleBin:
                    WindowsFileSystem.DeleteFileOrFolder(filePath, false, true);
                    break;
                case DeleteMode.MoveToDeletedFolder:
                    string relative = Path.GetRelativePath(rootDir, filePath);
                    string deletedFolder = Path.Combine(Path.GetPathRoot(filePath), DeleteDirName, createTime.ToString("yyyyMMdd-HHmmss"));
                    string target = Path.Combine(deletedFolder, relative);
                    string dir = Path.GetDirectoryName(target);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                    if (IsDirectory(filePath))
                    {
                        Directory.Move(filePath, GetNoDuplicateDirectory(target));
                    }
                    else
                    {
                        File.Move(filePath, GetNoDuplicateFile(target));
                    }
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }

        }
    }

}


