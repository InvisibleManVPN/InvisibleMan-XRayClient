using System.IO;
using System.Linq;

namespace InvisibleGorillaXRay.Handlers.Configs
{
    using Models;
    using Utilities;
    using Values;

    public class GeneralConfig : BaseConfig
    {
        public GeneralConfig() : base()
        {
            LoadFiles();
        }

        public void CreateConfig(string remark, string data)
        {
            string destinationPath = $"{Directory.CONFIGS}/{remark}.json";
            SaveToDirectory();
            SetFileTime(destinationPath);
            AddConfigToList(CreateConfigModel(destinationPath));

            void SaveToDirectory()
            {
                System.IO.Directory.CreateDirectory(Directory.CONFIGS);
                File.WriteAllText(destinationPath, JsonUtility.SanitizeRuntimeManagedSections(data));
            }
        }

        public void CopyConfig(string path)
        {
            string destinationPath = $"{Directory.CONFIGS}/{GetFileName(path)}";
            CopyToConfigsDirectory();
            SetFileTime(destinationPath);
            AddConfigToList(CreateConfigModel(destinationPath));

            void CopyToConfigsDirectory()
            {
                System.IO.Directory.CreateDirectory(Directory.CONFIGS);
                string rawConfig = File.ReadAllText(path);
                File.WriteAllText(destinationPath, JsonUtility.SanitizeRuntimeManagedSections(rawConfig));
            }
        }

        public override void LoadFiles(string path = null)
        {
            configs.Clear();

            DirectoryInfo directoryInfo = new DirectoryInfo(Directory.CONFIGS);
            if (!directoryInfo.Exists)
                return;

            FileInfo[] files = directoryInfo.GetFiles().OrderBy(file => file.CreationTime).ToArray();
            foreach(FileInfo file in files)
            {
                AddConfigToList(CreateConfigModel(file.FullName));
            }
        }

        public override Config CreateConfigModel(string path)
        {
            return new Config(
                path: $"{Directory.CONFIGS}/{GetFileName(path)}",
                name: GetFileName(path),
                type: ConfigType.FILE,
                group: GroupType.GENERAL,
                updateTime: GetFileUpdateTime(path)
            );
        }
    }
}