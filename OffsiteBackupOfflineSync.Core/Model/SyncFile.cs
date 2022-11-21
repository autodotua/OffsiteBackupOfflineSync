using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using FzLib;
using Newtonsoft.Json;

namespace OffsiteBackupOfflineSync.Model
{
    public class SyncFile : FileBase
    {

        public SyncFile()
        {
            Checked = true;
        }
        public SyncFile(FileInfo file, string rootDir) : this()
        {
            Name = file.Name;
            Path = System.IO.Path.GetRelativePath(System.IO.Path.GetDirectoryName(rootDir), file.FullName);
            LastWriteTime = file.LastWriteTime;
            Length = file.Length;
        }

        public string TempName { get; set; }
        public FileUpdateType UpdateType { get; set; }
        /// <summary>
        /// 对于 <see cref="UpdateType"/>为<see cref="FileUpdateType.Move"/> 类型的对象，表示异地的相对路径
        /// </summary>
        public string OldPath { get; set; }
    }
}
