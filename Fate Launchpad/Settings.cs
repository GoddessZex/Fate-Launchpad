using Newtonsoft.Json;
using System.IO;

namespace FateLaunchpad
{
    public class Settings
    {
        public bool AdvancedMode = false;

        public void Save(string file)
        {
            string str = JsonConvert.SerializeObject(this);
            File.WriteAllText(file, str);
        }

        public static Settings ReadFile(string file)
        {
            if (!File.Exists(file))
                new Settings().Save(file);

            return JsonConvert.DeserializeObject<Settings>(
                File.ReadAllText(file)
            );
        }
    }
}