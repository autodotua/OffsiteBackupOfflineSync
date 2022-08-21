using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;

namespace OffsiteBackupOfflineSync.Utility
{
    public class Step1Utility : SyncUtilityBase
    {
        public void Enumerate(IEnumerable<string> dirs,string jsonPath)
        {
            List<SyncFile> files = new List<SyncFile>();
            List<SyncFile> topDirectories = new List<SyncFile>();
            foreach (var dir in dirs)
            {
                files.AddRange(new DirectoryInfo(dir)
                    .EnumerateFiles("*", SearchOption.AllDirectories)
                    .Select(file => new SyncFile()
                    {
                        Name = file.Name,
                        Path = Path.GetRelativePath(Path.GetDirectoryName(dir), file.FullName),
                        LastWriteTime = file.LastWriteTime,
                        Length = file.Length,
                    }));
                var dirInfo = new DirectoryInfo(dir);
                topDirectories.Add(new SyncFile()
                {
                    Name = dirInfo.Name,
                    Path = dirInfo.FullName,
                    LastWriteTime = dirInfo.LastWriteTime,
                });
            }
            Step1Model model = new Step1Model()
            {
                Files = files,
                TopDirectories = topDirectories,
            };
            var json = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText(jsonPath, json);
        }
    }

}


