using FzLib.DataStorage.Serialization;
using Newtonsoft.Json;
using OffsiteBackupOfflineSync.Model;
using OffsiteBackupOfflineSync.UI;
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
        public static readonly int MaxTimeTolerance = 3;
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
        public string CurrentConfigName { get; set; } = "默认";
        public SingleConfig CurrentConfig
        {
            get
            {
                if (ConfigCollection.ContainsKey(CurrentConfigName))
                {
                    return ConfigCollection[CurrentConfigName];
                }
                else
                {
                    ConfigCollection.Add(CurrentConfigName, new SingleConfig());
                    return ConfigCollection[CurrentConfigName];
                }
            }
        }
        public Dictionary<string, SingleConfig> ConfigCollection { get; set; } = new Dictionary<string, SingleConfig>();

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

    public class SingleConfig
    {
        public Step1ViewModel Step1 { get; set; } = new Step1ViewModel();
        public Step2ViewModel Step2 { get; set; } = new Step2ViewModel();
        public Step3ViewModel Step3 { get; set; } = new Step3ViewModel();
        public CloneFileTreeViewModel CloneFileTree { get; set; } = new CloneFileTreeViewModel();
        public FilesGoHomeViewModel FilesGoHome { get; set; } = new FilesGoHomeViewModel();
    }
}
