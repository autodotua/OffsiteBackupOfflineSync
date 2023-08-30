using FzLib.IO;
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

        public static async Task TestAll()
        {
            string dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string localDir = Path.Combine(dir, "local");
            string remoteDir = Path.Combine(dir, "remote");
            Debug.WriteLine(dir);
            await CreateSyncTestFilesAsync(dir);
            await Task.Run(() =>
            {
                try
                {
                    Step1Utility u1 = new Step1Utility();
                    string[] syncDirs = new[]
                    {
                        Path.Combine(remoteDir,"syncDir1"),
                        Path.Combine(remoteDir,"folder","syncDir2"),
                    };
                    string step1JSON = Path.GetRandomFileName();
                    u1.Enumerate(syncDirs, step1JSON);

                    Step1Model s1m = Step1Utility.ReadStep1Model(step1JSON);

                    string[] searchingDirs = new string[]
                    {
                        localDir,
                        Path.Combine(localDir,"folder"),
                    };
                    var match = Step2Utility.MatchLocalAndOffsiteDirs(s1m, searchingDirs);
                    Step2Utility u2 = new Step2Utility();
                    u2.Search(match, s1m, $"黑名单文件.+{Environment.NewLine}黑名单目录/", true, 2, false);
                    Debug.Assert(u2.UpdateFiles != null);
                    Debug.Assert(u2.UpdateFiles.Count == Count*8);
                    Debug.Assert(!u2.UpdateFiles.Any(p=>p.Name.Contains('黑')));
                    Debug.Assert(u2.UpdateFiles.Where(p => p.Name.Contains("新建")).All(p => p.UpdateType == FileUpdateType.Add));
                    Debug.Assert(u2.UpdateFiles.Where(p => p.Name.Contains("新建")).Count() == Count*2);
                    Debug.Assert(u2.UpdateFiles.Where(p => p.Name.Contains("删除")).All(p => p.UpdateType == FileUpdateType.Delete));
                    Debug.Assert(u2.UpdateFiles.Where(p => p.Name.Contains("删除")).Count() == Count * 2);
                    Debug.Assert(u2.UpdateFiles.Where(p => p.Name.Contains("移动")).All(p => p.UpdateType == FileUpdateType.Move));
                    Debug.Assert(u2.UpdateFiles.Where(p => p.Name.Contains("移动")).Count() == Count * 2);
                    Debug.Assert(u2.UpdateFiles.Where(p => p.Name.Contains("修改")).All(p => p.UpdateType == FileUpdateType.Modify));
                    Debug.Assert(u2.UpdateFiles.Where(p => p.Name.Contains("修改")).Count() == Count * 2);

                    string patchDir = Path.Combine(dir, "patch");
                    u2.Export(patchDir, ExportMode.Copy);

                    Step3Utility u3 = new Step3Utility();
                    u3.Analyze(patchDir);
                    u3.Update(DeleteMode.Delete);
                    u3.AnalyzeEmptyDirectories();
                    u3.DeleteEmptyDirectories(DeleteMode.Delete);

                    var localFiles = Directory.EnumerateFiles(localDir, "*", SearchOption.AllDirectories)
                        .Select(p => Path.GetRelativePath(localDir, p))
                        .Where(p => !p.Contains("黑"))
                        .OrderBy(p=>p)
                        .ToList();
                    var remoteFiles = Directory.EnumerateFiles(remoteDir, "*", SearchOption.AllDirectories)
                        .Select(p => Path.GetRelativePath(remoteDir, p))
                        .Where(p => !p.Contains("黑"))
                        .OrderBy(p => p)
                        .ToList();

                    Debug.Assert(localFiles.SequenceEqual(remoteFiles));

                     localFiles = Directory.EnumerateFiles(localDir, "*", SearchOption.AllDirectories)
                        .Select(p => Path.GetRelativePath(localDir, p))
                        .Where(p => p.Contains("黑"))
                        .OrderBy(p=>p)
                        .ToList();
                     remoteFiles = Directory.EnumerateFiles(remoteDir, "*", SearchOption.AllDirectories)
                        .Select(p => Path.GetRelativePath(remoteDir, p))
                        .Where(p => p.Contains("黑"))
                        .OrderBy(p => p)
                        .ToList();

                    Debug.Assert(localFiles.Count==remoteFiles.Count);

                    var localDirs=Directory.EnumerateDirectories(localDir,"*", SearchOption.AllDirectories)
                        .Select(p => Path.GetRelativePath(localDir, p))
                        .OrderBy(p => p)
                        .ToList();
                    var remoteDirs=Directory.EnumerateDirectories(remoteDir,"*", SearchOption.AllDirectories)
                        .Select(p => Path.GetRelativePath(remoteDir, p))
                        .OrderBy(p => p)
                        .ToList();

                    Debug.Assert(localDirs.SequenceEqual(remoteDirs));
                }
                finally
                {
                    Directory.Delete(dir, true);
                }
            });
        }

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

        private static Random random = new Random();

        private static void CreateRandomFile(string path)
        {
            using FileStream file = File.Create(path);
            byte[] buffer = new byte[random.Next(4096) + 1024];
            random.NextBytes(buffer);
            file.Write(buffer, 0, buffer.Length);
            file.Dispose();
        }

        private static void CreateTestFiles(DirectoryInfo local, DirectoryInfo remote)
        {
            string fileName, fileName2;
            var localDir = local.CreateSubdirectory("增加处理时间的目录");
            var remoteDir = remote.CreateSubdirectory("增加处理时间的目录");
            for (int i = 0; i < CostTimeCount; i++)
            {
                fileName = Path.Combine(localDir.FullName, i.ToString());
                CreateRandomFile(fileName);
                File.Copy(fileName, Path.Combine(remoteDir.FullName, i.ToString()));
            }


            localDir = local.CreateSubdirectory("测试目录");
            remoteDir = remote.CreateSubdirectory("测试目录");
            var localBlackDir = localDir.CreateSubdirectory("黑名单目录");
            var remoteBlackDir = remoteDir.CreateSubdirectory("黑名单目录");
            var localMovedDir = local.CreateSubdirectory("已移动文件的目录");

            for (int i = 1; i <= Count; i++)
            {
                DateTime now = DateTime.Now;

                fileName = Path.Combine(localDir.FullName, $"未修改的文件{i}");
                CreateRandomFile(fileName);
                fileName2 = Path.Combine(remoteDir.FullName, $"未修改的文件{i}");
                File.Copy(fileName, fileName2);

                fileName = Path.Combine(localDir.FullName, $"新建的文件{i}");
                CreateRandomFile(fileName);

                fileName = Path.Combine(remoteDir.FullName, $"删除的文件{i}");
                CreateRandomFile(fileName);

                fileName = Path.Combine(localDir.FullName, $"修改的文件{i}");
                CreateRandomFile(fileName);
                fileName = Path.Combine(remoteDir.FullName, $"修改的文件{i}");
                CreateRandomFile(fileName);
                File.SetLastWriteTime(fileName, now.AddDays(-1));

                fileName = Path.Combine(localMovedDir.FullName, $"移动的文件{i}");
                CreateRandomFile(fileName);
                File.SetLastWriteTime(fileName, now);

                fileName2 = Path.Combine(remoteDir.FullName, $"移动的文件{i}");
                File.Copy(fileName, fileName2);


                fileName = Path.Combine(localMovedDir.FullName, $"黑名单文件{i}");
                CreateRandomFile(fileName);
                fileName = Path.Combine(remoteDir.FullName, $"黑名单文件{i}");
                CreateRandomFile(fileName);
                fileName = Path.Combine(localBlackDir.FullName, $"黑名单目录中的文件{i}");
                CreateRandomFile(fileName);
                fileName = Path.Combine(remoteBlackDir.FullName, $"黑名单文件{i}");
                CreateRandomFile(fileName);
                File.SetLastWriteTime(fileName, now.AddDays(-1));
            }
            remoteDir.CreateSubdirectory("空目录1");
            localDir.CreateSubdirectory("空目录1");
            remoteDir.CreateSubdirectory("空目录2").CreateSubdirectory("子空目录3");

        }
    }
}
