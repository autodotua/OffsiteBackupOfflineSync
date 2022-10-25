using FzLib.DataStorage.Serialization;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OffsiteBackupOfflineSync
{
    public class Configs : IJsonSerializable
    {
        public static readonly double MaxTimeTolerance = 5;
        private static readonly string configPath = "configs.json";
        private static Configs instance;
        private Configs()
        {

        }

        public static Configs Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Configs();

                    try
                    {
                        instance.TryLoadFromJsonFile(configPath);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                return instance;
            }
        }

        public string Step1Dir { get; set; }
        public string Step2BlackList { get; set; } = "Thumbs.db";
        public bool Step2BlackListUseRegex { get; set; }
        public string Step2LocalDir { get; set; }
        public string Step2OffsiteSnapshot { get; set; }
        public string Step3DeletedDir { get; set; } = "被删除和替换的文件备份";
        public DeleteMode Step3DeleteMode { get; set; } = DeleteMode.MoveToDeletedFolder;
        public string Step3OffsiteDir { get; set; }
        public Dictionary<string, List<string>> SelectedDirectoriesHistory { get; set; }=new Dictionary<string, List<string>>();

        public string Step3PatchDir { get; set; }

        public void Save(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            this.Save(path, new JsonSerializerSettings().SetIndented());
        }

        public void Save()
        {
            try
            {
                Save(configPath);
            }
            catch
            {

            }
        }
    }

}
