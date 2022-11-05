using FzLib.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffsiteBackupOfflineSync.Utility
{
    public static class TestUtility
    {
        public static void SleepInDebug()
        {
            Thread.Sleep(10);
        }
        public static Task CreateSyncTestFilesAsync(string dir)
        {
            return Task.Run(() =>
            {
                DateTime time = new DateTime(2000, 1, 1, 0, 0, 0);
                string fileName = null;
                var root = new DirectoryInfo(dir);
                if (root.Exists)
                {
                    root.Delete(true, true);
                }
                root.Create();
                var local = root.CreateSubdirectory("local");
                var remote = root.CreateSubdirectory("remote");

                var localDir = local.CreateSubdirectory("增加处理时间的目录");
                var remoteDir = remote.CreateSubdirectory("增加处理时间的目录");
                for (int i = 0; i < 10000; i++)
                {
                     fileName = Path.Combine(localDir.FullName, i.ToString());
                    Debug.WriteLine($"正在创建{fileName}");
                    File.Create(fileName).Dispose();
                    //File.SetLastWriteTime(fileName, time = time.AddHours(1));
                    File.Copy(fileName, Path.Combine(remoteDir.FullName, i.ToString()));
                }


                localDir = local.CreateSubdirectory("包含存在新建、修改、删除的文件的目录");
                remoteDir = remote.CreateSubdirectory("包含存在新建、修改、删除的文件的目录");

                for(int i=0;i<1000;i++)
                {
                     fileName = Path.Combine(localDir.FullName, $"新建的文件{i}");
                    Debug.WriteLine($"正在创建{fileName}");
                    File.Create(fileName).Dispose();
                     fileName = Path.Combine(remoteDir.FullName, $"删除的文件{i}");
                    Debug.WriteLine($"正在创建{fileName}");
                    File.Create(fileName).Dispose();
                    fileName = Path.Combine(localDir.FullName, $"修改的文件{i}");
                    Debug.WriteLine($"正在创建{fileName}");
                    File.Create(fileName).Dispose();
                    fileName = Path.Combine(remoteDir.FullName, $"修改的文件{i}");
                    Debug.WriteLine($"正在创建{fileName}");
                    File.Create(fileName).Dispose();
                    File.SetLastWriteTime(fileName, DateTime.Now.AddDays(-1));
                }
                remoteDir.CreateSubdirectory("空目录1");
                localDir.CreateSubdirectory("空目录1");
                remoteDir.CreateSubdirectory("空目录2");



            });
        }
    }
}
