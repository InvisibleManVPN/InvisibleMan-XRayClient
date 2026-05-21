using System.IO;

namespace InvisibleGorillaXRay.Values
{
    public static class Path
    {
        public const string USER_SETTINGS = $"{Directory.ROOT}/Settings.json";
        public const string XRAY_CORE_DLL = $"{Directory.LIBRARIES}/XRayCore.dll";
        public const string INVISIBLEGORILLA_TUN_EXE = $"{Directory.TUN}/InvisibleGorilla-TUN.exe";
        public const string INVISIBLEMAN_TUN_EXE = $"{Directory.TUN}/InvisibleMan-TUN.exe";

        public static readonly string TUN_EXE = File.Exists(INVISIBLEGORILLA_TUN_EXE)
            ? INVISIBLEGORILLA_TUN_EXE
            : INVISIBLEMAN_TUN_EXE;
    }
}