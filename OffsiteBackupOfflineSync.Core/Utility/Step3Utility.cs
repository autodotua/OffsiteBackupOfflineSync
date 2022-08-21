using FzLib.IO;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step3Utility : SyncUtilityBase
    {
        public List<SyncFile> UpdateFiles { get; private set; } = new List<SyncFile>();
        private string patchDir;
        public void Analyze(string patchDir)
        {
            this.patchDir = patchDir;
            var json = File.ReadAllText(Path.Combine(patchDir, "file.obos2"));
            UpdateFiles = JsonConvert.DeserializeObject<List<SyncFile>>(json);
            foreach (var file in UpdateFiles.Where(p => p.UpdateType != FileUpdateType.Delete))
            {
                if (!File.Exists(Path.Combine(patchDir, file.TempName)))
                {
                    file.Message = "补丁文件不存在";
                }
            }
        }

        public void Update(string offsiteDir)
        {
            stopping = false;
            long totalLength = UpdateFiles.Where(p => p.UpdateType != FileUpdateType.Delete).Sum(p => p.Length);
            long length = 0;
            foreach (var file in UpdateFiles)
            {
                InvokeMessageReceivedEvent($"正在处理{file.Path}");
                string patch = file.TempName == null ? null : Path.Combine(patchDir, file.TempName);
                if (file.UpdateType != FileUpdateType.Delete && !File.Exists(patch))
                {
                    continue;
                }
                string target = Path.Combine(offsiteDir, file.Path);
                if(!Directory.Exists(Path.GetDirectoryName(target)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                }
                switch (file.UpdateType)
                {
                    case FileUpdateType.Add:
                        if (File.Exists(target))
                        {
                            file.Message = "应当为新增文件，但文件已存在";
                            WindowsFileSystem.DeleteFileOrFolder(target, false, true);
                        }
                        File.Copy(patch, target);
                        InvokeProgressReceivedEvent(length += file.Length, totalLength);
                        break;
                    case FileUpdateType.Modify:
                        if (File.Exists(target))
                        {
                            WindowsFileSystem.DeleteFileOrFolder(target, false, true);
                        }
                        else
                        {
                            file.Message = "应当为修改后文件，但文件不存在";
                        }
                        File.Copy(patch, target);
                        InvokeProgressReceivedEvent(length += file.Length, totalLength);
                        break;
                    case FileUpdateType.Delete:
                        if (File.Exists(target))
                        {
                            WindowsFileSystem.DeleteFileOrFolder(target, false, true);
                        }
                        else
                        {
                            file.Message = "应当为待删除文件，但文件不存在";
                        }
                        break;
                }
                file.Complete = true;
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
            }
        }
    }

}


