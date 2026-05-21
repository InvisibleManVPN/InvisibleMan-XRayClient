using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using AndroidX.Core.Content;
using AndroidUri = Android.Net.Uri;
using SystemEnvironment = System.Environment;

namespace InvisibleGorillaXRay.Android.Services
{
    using InvisibleGorillaXRay.Core;

    internal static class AndroidLogShareService
    {
        private const string FileProviderAuthority = "io.invisiblegorilla.xray.fileprovider";
        private const string ShareMimeType = "text/plain";
        private const string ShareSnapshotFolderName = "log-share";
        private const string DownloadsSubdirectory = "InvisibleGorilla-XRay";

        public enum SaveResultKind
        {
            SavedToMediaStore,
            SavedToFile,
            Failed
        }

        public sealed class SaveResult
        {
            public SaveResultKind Kind { get; init; }
            public string? Path { get; init; }
            public string? ErrorMessage { get; init; }

            public bool Succeeded => Kind != SaveResultKind.Failed;
        }

        public static async Task<bool> ShareDiagnosticLogAsync(Activity activity, string chooserTitle)
        {
            if (activity == null)
                return false;

            try
            {
                FileInfo? snapshot = await Task.Run(() => CreateShareSnapshot(activity));
                if (snapshot == null || !snapshot.Exists)
                {
                    DiagnosticLog.Write("AndroidLogShareService", "Share aborted: snapshot was not created");
                    return false;
                }

                global::Java.IO.File javaFile = new global::Java.IO.File(snapshot.FullName);
                AndroidUri uri = FileProvider.GetUriForFile(activity, FileProviderAuthority, javaFile);
                Intent sendIntent = new Intent(Intent.ActionSend)
                    .SetType(ShareMimeType)
                    .PutExtra(Intent.ExtraStream, uri)
                    .PutExtra(Intent.ExtraSubject, "Invisible Gorilla XRay diagnostic log")
                    .PutExtra(Intent.ExtraText, BuildShareBody())
                    .AddFlags(ActivityFlags.GrantReadUriPermission);

                Intent chooser = Intent.CreateChooser(sendIntent, chooserTitle)!
                    .AddFlags(ActivityFlags.GrantReadUriPermission);

                activity.StartActivity(chooser);
                DiagnosticLog.Write("AndroidLogShareService", $"Share chooser launched for {snapshot.FullName}");
                return true;
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidLogShareService.Share", ex);
                return false;
            }
        }

        public static async Task<SaveResult> SaveDiagnosticLogAsync(Context context)
        {
            if (context == null)
            {
                return new SaveResult
                {
                    Kind = SaveResultKind.Failed,
                    ErrorMessage = "Android context is unavailable."
                };
            }

            try
            {
                return await Task.Run(() => SaveDiagnosticLogCore(context));
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidLogShareService.Save", ex);
                return new SaveResult
                {
                    Kind = SaveResultKind.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        public static bool ClearDiagnosticLog()
        {
            try
            {
                DiagnosticLog.ClearAll();
                DiagnosticLog.Write("AndroidLogShareService", "Diagnostic log cleared by user request");
                return true;
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidLogShareService.Clear", ex);
                return false;
            }
        }

        public static string GetDiagnosticLogPath()
        {
            return DiagnosticLog.ActiveLogPath;
        }

        public static long GetDiagnosticLogSizeBytes()
        {
            long size = 0;
            try
            {
                string active = DiagnosticLog.ActiveLogPath;
                if (File.Exists(active))
                    size += new FileInfo(active).Length;

                string archived = DiagnosticLog.ArchivedLogPath;
                if (File.Exists(archived))
                    size += new FileInfo(archived).Length;
            }
            catch
            {
            }

            return size;
        }

        private static SaveResult SaveDiagnosticLogCore(Context context)
        {
            string snapshotName = BuildSnapshotFileName();
            byte[] payload = Encoding.UTF8.GetBytes(BuildLogBundle());

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
            {
                SaveResult mediaStoreResult = TrySaveToMediaStoreDownloads(context, snapshotName, payload);
                if (mediaStoreResult.Succeeded)
                    return mediaStoreResult;
            }

            return SaveToAppExternalFiles(context, snapshotName, payload);
        }

        private static SaveResult TrySaveToMediaStoreDownloads(Context context, string fileName, byte[] payload)
        {
            try
            {
                ContentResolver? resolver = context.ContentResolver;
                if (resolver == null)
                {
                    return new SaveResult
                    {
                        Kind = SaveResultKind.Failed,
                        ErrorMessage = "ContentResolver is unavailable."
                    };
                }

                ContentValues values = new ContentValues();
                values.Put(MediaStore.IMediaColumns.DisplayName, fileName);
                values.Put(MediaStore.IMediaColumns.MimeType, ShareMimeType);
                values.Put(
                    MediaStore.IMediaColumns.RelativePath,
                    System.IO.Path.Combine(global::Android.OS.Environment.DirectoryDownloads!, DownloadsSubdirectory));

                AndroidUri? collection = MediaStore.Downloads.GetContentUri(MediaStore.VolumeExternalPrimary);
                if (collection == null)
                {
                    return new SaveResult
                    {
                        Kind = SaveResultKind.Failed,
                        ErrorMessage = "MediaStore downloads URI is unavailable."
                    };
                }

                AndroidUri? itemUri = resolver.Insert(collection, values);
                if (itemUri == null)
                {
                    return new SaveResult
                    {
                        Kind = SaveResultKind.Failed,
                        ErrorMessage = "MediaStore insert returned null."
                    };
                }

                using (Stream? output = resolver.OpenOutputStream(itemUri))
                {
                    if (output == null)
                    {
                        return new SaveResult
                        {
                            Kind = SaveResultKind.Failed,
                            ErrorMessage = "MediaStore output stream is unavailable."
                        };
                    }

                    output.Write(payload, 0, payload.Length);
                    output.Flush();
                }

                string displayPath = System.IO.Path.Combine(
                    global::Android.OS.Environment.DirectoryDownloads,
                    DownloadsSubdirectory,
                    fileName);

                DiagnosticLog.Write("AndroidLogShareService", $"Saved log snapshot to MediaStore Downloads/{DownloadsSubdirectory}/{fileName}");

                return new SaveResult
                {
                    Kind = SaveResultKind.SavedToMediaStore,
                    Path = displayPath
                };
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidLogShareService.SaveMediaStore", ex);
                return new SaveResult
                {
                    Kind = SaveResultKind.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static SaveResult SaveToAppExternalFiles(Context context, string fileName, byte[] payload)
        {
            try
            {
                global::Java.IO.File? baseDir = context.GetExternalFilesDir(global::Android.OS.Environment.DirectoryDownloads);
                string directoryPath = baseDir?.AbsolutePath
                    ?? System.IO.Path.Combine(SystemEnvironment.GetFolderPath(SystemEnvironment.SpecialFolder.LocalApplicationData), "Logs");

                System.IO.Directory.CreateDirectory(directoryPath);
                string fullPath = System.IO.Path.Combine(directoryPath, fileName);
                File.WriteAllBytes(fullPath, payload);

                DiagnosticLog.Write("AndroidLogShareService", $"Saved log snapshot to {fullPath}");

                return new SaveResult
                {
                    Kind = SaveResultKind.SavedToFile,
                    Path = fullPath
                };
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidLogShareService.SaveExternalFiles", ex);
                return new SaveResult
                {
                    Kind = SaveResultKind.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static FileInfo? CreateShareSnapshot(Context context)
        {
            try
            {
                string? cacheRoot = context.CacheDir?.AbsolutePath;
                if (string.IsNullOrWhiteSpace(cacheRoot))
                    return null;

                string snapshotDir = System.IO.Path.Combine(cacheRoot, ShareSnapshotFolderName);
                System.IO.Directory.CreateDirectory(snapshotDir);

                foreach (string stale in System.IO.Directory.GetFiles(snapshotDir))
                {
                    try { File.Delete(stale); } catch { }
                }

                string snapshotPath = System.IO.Path.Combine(snapshotDir, BuildSnapshotFileName());
                File.WriteAllText(snapshotPath, BuildLogBundle(), Encoding.UTF8);
                return new FileInfo(snapshotPath);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidLogShareService.CreateShareSnapshot", ex);
                return null;
            }
        }

        private static string BuildSnapshotFileName()
        {
            return $"igxray-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        }

        private static string BuildLogBundle()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("=== Invisible Gorilla XRay Android diagnostic snapshot ===");
            builder.Append(BuildSystemInfoBlock());
            builder.AppendLine("=== Diagnostic log (oldest -> newest) ===");
            builder.AppendLine();
            builder.Append(DiagnosticLog.ReadAll());
            return builder.ToString();
        }

        private static string BuildSystemInfoBlock()
        {
            StringBuilder builder = new StringBuilder();
            try
            {
                builder.AppendLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}");
                builder.AppendLine($"Android SDK: {(int)Build.VERSION.SdkInt} ({Build.VERSION.Release})");
                builder.AppendLine($"Device: {Build.Manufacturer} {Build.Model} ({Build.Device})");
                builder.AppendLine($"Brand: {Build.Brand}");
                builder.AppendLine($"Hardware: {Build.Hardware}");
                builder.AppendLine($"Build fingerprint: {Build.Fingerprint}");
                builder.AppendLine($"Supported ABIs: {string.Join(", ", Build.SupportedAbis ?? Array.Empty<string>())}");
                builder.AppendLine($"Active log: {DiagnosticLog.ActiveLogPath}");
                builder.AppendLine($"Archived log: {DiagnosticLog.ArchivedLogPath}");
                builder.AppendLine($"Active log size: {GetDiagnosticLogSizeBytes()} bytes total");

                Context? context = global::Android.App.Application.Context;
                if (context != null)
                {
                    builder.AppendLine($"Package: {context.PackageName}");
                    try
                    {
                        global::Android.Content.PM.PackageInfo? info = context.PackageManager?.GetPackageInfo(context.PackageName!, 0);
                        if (info != null)
                        {
                            builder.AppendLine($"App version: {info.VersionName} (code {info.LongVersionCode})");
                        }
                    }
                    catch
                    {
                    }
                }

                try
                {
                    builder.AppendLine($"VPN running: {AndroidVpnServiceController.IsRunning}");
                    builder.AppendLine($"VPN stopping: {AndroidVpnServiceController.IsStopping}");
                    if (!string.IsNullOrWhiteSpace(AndroidVpnServiceController.LastError))
                        builder.AppendLine($"Last VPN error: {AndroidVpnServiceController.LastError}");
                }
                catch
                {
                }
            }
            catch
            {
                builder.AppendLine("Failed to collect full system info block.");
            }

            builder.AppendLine();
            return builder.ToString();
        }

        private static string BuildShareBody()
        {
            return new StringBuilder()
                .AppendLine("Invisible Gorilla XRay diagnostic log.")
                .AppendLine("Attached file contains device info and the recent diagnostic events.")
                .ToString();
        }
    }
}
