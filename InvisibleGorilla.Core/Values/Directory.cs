namespace InvisibleGorillaXRay.Values
{
    public static class Directory
    {
        private static string dataRoot = System.AppContext.BaseDirectory;
        private static string runtimeRoot = System.AppContext.BaseDirectory;

        public static string ROOT => dataRoot;
        public static string DATA_ROOT => dataRoot;
        public static string RUNTIME_ROOT => runtimeRoot;
        public static string LIBRARIES => System.IO.Path.Combine(RUNTIME_ROOT, "Libraries");
        public static string TUN => System.IO.Path.Combine(RUNTIME_ROOT, "TUN");
        public static string DATA_TUN => System.IO.Path.Combine(DATA_ROOT, "TUN");
        public static string CONFIGS => System.IO.Path.Combine(DATA_ROOT, "Configs");
        public static string LOGS => System.IO.Path.Combine(DATA_ROOT, "Logs");
        public static string ASSETS => System.IO.Path.Combine(RUNTIME_ROOT, "Assets");
        public static string LOCALIZATION => System.IO.Path.Combine(ASSETS, "Localization");

        public static void SetRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                return;

            dataRoot = System.IO.Path.GetFullPath(rootPath);
        }

        public static void SetRuntimeRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                return;

            runtimeRoot = System.IO.Path.GetFullPath(rootPath);
        }

        public static void ConfigureRoots(string dataRootPath, string runtimeRootPath)
        {
            SetRoot(dataRootPath);
            SetRuntimeRoot(runtimeRootPath);
        }

        public static void EnsureWritableDirectories()
        {
            TryCreate(DATA_ROOT);
            TryCreate(CONFIGS);
            TryCreate(LOGS);
            TryCreate(DATA_TUN);

            static void TryCreate(string path)
            {
                try
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                catch
                {
                }
            }
        }
    }
}