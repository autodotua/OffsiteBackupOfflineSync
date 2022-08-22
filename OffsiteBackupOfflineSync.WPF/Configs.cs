using FzLib.DataStorage.Serialization;
using Newtonsoft.Json;
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
        public static readonly double MaxTimeTolerance = 1;
        public void Save(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            this.Save(path, new JsonSerializerSettings().SetIndented());
        }
        public string Step1Dir { get; set; }
        public string Step2OffsiteSnapshot { get; set; }
        public string Step2LocalDir { get; set; }
        public string Step3PatchDir { get; set; }
        public string Step3OffsiteDir { get; set; }
    }

}
