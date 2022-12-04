using FzLib.IO;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System.ComponentModel;
using System.Diagnostics;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step3Utility : UtilityBase
    {
        public List<SyncFile> UpdateFiles { get; private set; }
        public List<string> LocalDirectories { get; private set; }
        public List<string> DeletingDirectories { get; private set; }
        private string patchDir;
        public void Analyze(string patchDir, string offsiteDir)
        {
            stopping = false;
            this.patchDir = patchDir;
            var json = File.ReadAllText(Path.Combine(patchDir, "file.obos2"));
            var step2 = JsonConvert.DeserializeObject<Step2Model>(json);

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
                string target = Path.Combine(offsiteDir, file.Path);
                string oldPath = file.OldPath == null ? null : Path.Combine(offsiteDir, file.OldPath);
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

        public void Update(string offsiteDir, string deletedDir, DeleteMode deleteMode)
        {
            stopping = false;
            var updateFiles = UpdateFiles.Where(p => p.Checked).ToList();
            long totalLength = updateFiles.Where(p => p.UpdateType != FileUpdateType.Delete).Sum(p => p.Length);
            long length = 0;

            InvokeProgressReceivedEvent(0, totalLength);
            //更新文件
            foreach (var file in updateFiles)
            {
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
                    string target = Path.Combine(offsiteDir, file.Path);
                    string oldPath = file.OldPath == null ? null : Path.Combine(offsiteDir, file.OldPath);
                    if (!Directory.Exists(Path.GetDirectoryName(target)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                    }
                    switch (file.UpdateType)
                    {
                        case FileUpdateType.Add:
                            if (File.Exists(target))
                            {
                                Delete(offsiteDir, target, deletedDir, deleteMode);
                            }
                            File.Copy(patch, target);
                            File.SetLastWriteTime(target, file.LastWriteTime);
                            InvokeProgressReceivedEvent(length += file.Length, totalLength);
                            break;
                        case FileUpdateType.Modify:
                            Delete(offsiteDir, target, deletedDir, deleteMode);
                            File.Copy(patch, target);
                            File.SetLastWriteTime(target, file.LastWriteTime);
                            InvokeProgressReceivedEvent(length += file.Length, totalLength);
                            break;
                        case FileUpdateType.Delete:
                            if (!File.Exists(target))
                            {
                                throw new Exception("应当为待删除文件，但文件不存在");
                            }
                            Delete(offsiteDir, target, deletedDir, deleteMode);
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

        public void AnalyzeEmptyDirectories(string offsiteDir, DeleteMode deleteMode)
        {
            InvokeMessageReceivedEvent($"正在查找空目录");
            DeletingDirectories = new List<string>();
            //清理空目录
            foreach (var offsiteTopDir in Directory.EnumerateDirectories(offsiteDir).ToList())
            {
                //本地不存在远程的顶级目录，跳过
                if (!LocalDirectories.Contains(Path.GetRelativePath(offsiteDir, offsiteTopDir)))
                {
                    continue;
                }

                foreach (var offsiteSubDir in Directory.EnumerateDirectories(offsiteTopDir, "*", SearchOption.AllDirectories).ToList())
                {
                    if (stopping)
                    {
                        throw new OperationCanceledException();
                    }
                    if (!LocalDirectories.Contains(Path.GetRelativePath(offsiteDir, offsiteSubDir)))//本地已经没有远程的这个目录了
                    {
                        if (!Directory.EnumerateFiles(offsiteSubDir).Any())//并且远程的这个目录是空的
                        {
                            DeletingDirectories.Add(offsiteSubDir);
                        }
                    }
                }
            }


            //通过两层循环，删除位于空目录下的空目录
            foreach (var dir1 in DeletingDirectories.ToList())//外层循环，dir1为内层空目录
            {
                foreach (var dir2 in DeletingDirectories)//内存循环，dir2为外层空目录
                {
                    if (dir1 == dir2)
                    {
                        continue;
                    }
                    if (dir1.StartsWith(dir2))//如果dir2位于dir1的外层，那么dir1就不需要单独删除
                    {
                        DeletingDirectories.Remove(dir1);
                        break;
                    }
                }
            }
        }
        public void DeleteEmptyDirectories(string offsiteDir, string deletedDir, DeleteMode deleteMode)
        {
            foreach (var dir in DeletingDirectories)
            {
                Delete(offsiteDir, dir, deletedDir, deleteMode);
            }
        }
        private static bool IsDirectory(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            return attr.HasFlag(FileAttributes.Directory);
        }
        private static void Delete(string rootDir, string filePath, string deletedFolder, DeleteMode deleteMode)
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
    }

}


