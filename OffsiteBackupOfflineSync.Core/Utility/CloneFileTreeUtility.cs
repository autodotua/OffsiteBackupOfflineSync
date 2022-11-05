using Microsoft.Win32.SafeHandles;
using OffsiteBackupOfflineSync.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OffsiteBackupOfflineSync.Utility
{
    public class CloneFileTreeUtility : UtilityBase
    {
        public FileTreeFile[] Files { get; private set; }
        public string SourceDir { get; private set; }
        public void CloneFiles(string destDir)
        {
            stopping = false;
            int index = 0;
            foreach (var file in Files)
            {
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
                string relativePath = Path.GetRelativePath(SourceDir, file.Path);
                string newPath = Path.Combine(destDir, relativePath);
                FileInfo newFile = new FileInfo(newPath);
                if (!newFile.Directory.Exists)
                {
                    newFile.Directory.Create();
                }
                InvokeMessageReceivedEvent($"正在创建：{relativePath}");
                InvokeProgressReceivedEvent(++index, Files.Length);
                try
                {
                    using FileStream fs = File.Create(newPath);
                    MarkAsSparseFile(fs.SafeFileHandle);
                    fs.SetLength(file.Length);
                    fs.Seek(-1, SeekOrigin.End);
                    file.Complete = true;
                }
                catch (Exception ex)
                {
                    file.Message = ex.Message;
                }
            }
        }

        public void EnumerateAllFiles(string dir)
        {
            stopping=false;
            SourceDir = dir;
            var fileInfos = new DirectoryInfo(dir)
             .EnumerateFiles("*", new EnumerationOptions()
             {
                 IgnoreInaccessible = true,
                 AttributesToSkip = 0,
                 RecurseSubdirectories = true,
             });
            List<FileTreeFile> files = new List<FileTreeFile>();
            foreach (var file in fileInfos)
            {
                if (stopping)
                {
                    throw new OperationCanceledException();
                }
                InvokeMessageReceivedEvent($"正在处理：{Path.GetRelativePath(dir,file.FullName)}");
                files.Add(new FileTreeFile()
                {
                    Name = file.Name,
                    Path = file.FullName,
                    LastWriteTime = file.LastWriteTime,
                    Length = file.Length,
                });
            }
            Files = files.ToArray();
        }
        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DeviceIoControl(
SafeFileHandle hDevice,
int dwIoControlCode,
IntPtr InBuffer,
int nInBufferSize,
IntPtr OutBuffer,
int nOutBufferSize,
ref int pBytesReturned,
[In] ref NativeOverlapped lpOverlapped
);
        private static void MarkAsSparseFile(SafeFileHandle fileHandle)
        {
            int bytesReturned = 0;
            NativeOverlapped lpOverlapped = new NativeOverlapped();
            bool result =
                DeviceIoControl(
                    fileHandle,
                    590020, //FSCTL_SET_SPARSE,
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero,
                    0,
                    ref bytesReturned,
                    ref lpOverlapped);
            if (result == false)
                throw new Win32Exception();
        }
    }
}
