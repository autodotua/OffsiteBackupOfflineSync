﻿using FzLib.IO;
using OffsiteBackupOfflineSync.Model;
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
        private const int Count = 2;
        private const int CostTimeCount = 0;

        public static Task CreateSyncTestFilesAsync(string dir)
        {
            return Task.Run(() =>
            {
                DateTime time = new DateTime(2000, 1, 1, 0, 0, 0);
                var root = new DirectoryInfo(dir);
                if (root.Exists)
                {
                    root.Delete(true, true);
                }
                root.Create();

                var local = root.CreateSubdirectory("local");
                var remote = root.CreateSubdirectory("remote");

                CreateTestFiles(local.CreateSubdirectory("syncDir1"), remote.CreateSubdirectory("syncDir1"));
                CreateTestFiles(local.CreateSubdirectory("folder").CreateSubdirectory("syncDir2"), remote.CreateSubdirectory("folder").CreateSubdirectory("syncDir2"));
            });
        }

        public static void SleepInDebug()
        {
            // Thread.Sleep(1);
        }

        private static void CreateTestFiles(DirectoryInfo local, DirectoryInfo remote)
        {
            string fileName;
            var localDir = local.CreateSubdirectory("增加处理时间的目录");
            var remoteDir = remote.CreateSubdirectory("增加处理时间的目录");
            for (int i = 0; i < CostTimeCount; i++)
            {
                fileName = Path.Combine(localDir.FullName, i.ToString());
                Debug.WriteLine($"正在创建{fileName}");
                File.Create(fileName).Dispose();
                //File.SetLastWriteTime(fileName, time = time.AddHours(1));
                File.Copy(fileName, Path.Combine(remoteDir.FullName, i.ToString()));
            }


            localDir = local.CreateSubdirectory("包含存在新建、修改、删除的文件的目录");
            remoteDir = remote.CreateSubdirectory("包含存在新建、修改、删除的文件的目录");
            var localMovedDir = local.CreateSubdirectory("移动了的文件的目录");

            for (int i = 1; i <= Count; i++)
            {
                DateTime now = DateTime.Now;
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
                File.SetLastWriteTime(fileName, now.AddDays(-1));

                fileName = Path.Combine(localMovedDir.FullName, $"移动的文件{i}");
                Debug.WriteLine($"正在创建{fileName}");
                File.Create(fileName).Dispose();
                File.SetLastWriteTime(fileName, now);
                fileName = Path.Combine(remoteDir.FullName, $"移动的文件{i}");
                Debug.WriteLine($"正在创建{fileName}");
                File.Create(fileName).Dispose();
                File.SetLastWriteTime(fileName, now);



            }
            remoteDir.CreateSubdirectory("空目录1");
            localDir.CreateSubdirectory("空目录1");
            remoteDir.CreateSubdirectory("空目录2");

        }
    }
}
