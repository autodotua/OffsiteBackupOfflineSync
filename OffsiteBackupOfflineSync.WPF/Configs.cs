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
                instance.Step1 = instance.Step1 ?? new Step1ViewModel();
                instance.Step2 = instance.Step2 ?? new Step2ViewModel();
                instance.Step3 = instance.Step3 ?? new Step3ViewModel();
                instance.CloneFileTree = instance.CloneFileTree ?? new CloneFileTreeViewModel();
                instance.FilesGoHome = instance.FilesGoHome ?? new FilesGoHomeViewModel();
                return instance;
            }
        }

        public Step1ViewModel Step1 { get; set; }
        public Step2ViewModel Step2 { get; set; }
        public Step3ViewModel Step3 { get; set; }
        public CloneFileTreeViewModel CloneFileTree { get; set; }
        public FilesGoHomeViewModel FilesGoHome { get; set; }

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
