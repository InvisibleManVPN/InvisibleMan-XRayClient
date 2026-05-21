# Active Context

## Current Focus
- Android app rules picker usability and correctness.
- Android release builds for emulator and real devices.

## Recent Changes
- Split Android app rules flow into a template editor and a separate app picker overlay.
- Added async app discovery and app icons in the Android picker.
- Fixed accidental app selection during scrolling by using movement-aware tap handling.
- Investigated excluded-app VPN detection reports: Android routing bypass can work while apps still detect an active system VPN; current desktop split tunneling still exposes platform-level tunnel state.

## Current Task
- Latest Android arm64 release artifact has been published for a real mobile device.

## Next Likely Steps
- If requested, publish both emulator and device builds into dedicated folders.
- Continue runtime verification of app rules and VPN bypass behavior.
- Hand off the signed APK path for installation on the phone.
# Active Context

## Current Focus
Roll out the secure local-bridge contract so TUN mode no longer exposes an unauthenticated localhost SOCKS listener while preserving the stabilized Android VPN lifecycle and the existing cross-platform app-rules work.

## Recent Changes
- Added shared `AppRule` / `AppRulesMode` persistence to the common settings model so Android, Windows, and macOS all store the same bypass rules contract.
- Android settings now expose an app-rules selector populated from installed launchable packages, and `AndroidVpnService` applies the selected packages through `Builder.AddDisallowedApplication(...)`.
- Windows TUN startup now forwards enabled bypass app paths to `InvisibleGorilla-TUN` through `-bypassApps=<base64-lines>`, and the companion service stages accepted paths in `active-bypass-apps.txt`.
- Windows and macOS settings windows now include desktop app discovery plus a dedicated app-rules editor inside the existing Basic tab.
- macOS TUN startup now stages `TUN/macos-transparent-proxy-config.json` for a future signed transparent-proxy helper and includes helper scaffolding under `InvisibleGorilla-XRay.Mac/Native/TransparentProxyExtension/`.
- Confirmed Android browser traffic routes through the selected config using `VpnService` plus the mobile `tun2socks` bridge.
- Fixed Android stop-path ANR by moving VPN stop work off the service main thread in `AndroidVpnService`.
- Fixed a native shutdown deadlock risk by releasing the Go tunnel mutex before closing the TUN FD and LWIP stack, and by closing the TUN file first.
- Hardened server shutdown in `XRay-Wrapper/xray/wrapper.go` so duplicate `StopServer()` calls do not leave stale stop signals or block later runs.
- Removed the eager Android UI-side `DisableMode()` stop race and added a stop guard in `MainView` plus `AndroidVpnServiceController`.
- Switched Android config checking from raw `TcpClient.ConnectAsync()` to the shared native `core.Test(...)` path, so the check actually validates the Xray config.
- Reworked Android config sharing into a two-option flow: copy source link when available or export the full config text, with source-link metadata stored for link imports.
- Restored Android top-notification details for running and stopped states by keeping the session metadata alive across app re-entry and by publishing a non-ongoing `Stopped` notification after VPN teardown.
- Split Android VPN start cleanup from the real stop path so startup no longer emits a false `Stopped` notification before the new foreground session is established.
- Prevented the UI thread from briefly forcing notification state back to `Running` while the Android VPN service is already processing `STOP_VPN`.
- Fixed the remaining Android native `STOP` crash in `XRay-Wrapper/xray/android_tun2socks.go` by keeping the bridge references alive until the TUN reader exits, checking the stop signal before `lwip.Write(...)`, and only closing LWIP after the reader loop is unblocked.
- Rebuilt native `libXRayCore.so` for `arm64-v8a` and `x86_64`.
- Published fresh APKs for `android-arm64` and `android-x64` under `publish-android/bugfix-arm64/` and `publish-android/bugfix-x64/`.
- Published fresh post-fix APKs under `publish-android/stopfix-race-arm64/` and `publish-android/stopfix-race-x64/`.
- Replaced the heavy inline Android app-rules list with a dedicated overlay editor that manages shared templates, three routing modes, search, and config-bound summaries from both Home and Settings.
- Synchronized the new app-rules localization keys across Windows WPF resources and the macOS/Avalonia dictionaries that Android reuses, so the new template editor and summaries render actual translated strings instead of raw `Lang.*` keys.
- Hardened Android app-rules editor opening so `Manage` no longer takes down the app when launcher discovery hits a bad OEM/system package: discovery now skips/logs per-package failures and the editor open/refresh path reports a status instead of crashing.
- Fixed the remaining Android `Manage` failure for app rules: the overlay editor was still touching Avalonia NameGenerator-backed fields that were `null` at runtime on Android, so the app-rules editor controls now go through explicit `GetRequiredControl(...)` getters instead.
- Added session-scoped local SOCKS credentials in both the shared runtime and the Windows desktop duplicate so TUN sessions can pass auth through `InvisibleGorillaXRayCore`, `XRayCoreWrapper`, Android `VpnService`, and desktop helper paths without persisting secrets in user settings.
- Updated `XRay-Wrapper` so the local SOCKS inbound requires password auth whenever the runtime passes TUN session credentials; Android now uses auth-aware TCP and UDP tun2socks handlers, while Windows/macOS TUN helper paths build `socks5://user:pass@127.0.0.1:<port>` URIs. Desktop proxy mode intentionally stays on the legacy listener path for now because the OS system-proxy integrations do not yet support mandatory auth.
- Centralized raw JSON sanitization in `GeneralConfig` so imported configs strip runtime-managed top-level sections such as `api`, `stats`, and `inbounds` before they are stored on disk.

## Latest Validation Snapshot
- `dotnet build` succeeded for `InvisibleGorilla-XRay.Android`, `InvisibleGorilla-XRay`, `InvisibleGorilla-XRay.Mac`, and `InvisibleGorilla-TUN` after the app-rules changes.
- IDE linting for the newly edited desktop/macOS app-rules files returned clean.
- On Windows, the local `Invisible Gorilla XRay.exe` from `InvisibleGorilla-XRay/bin/Debug/net7.0-windows/` launched and stayed alive during a basic smoke-start check.
- A non-invasive Windows runtime validator executed the real `WindowsTunnel.BuildBypassAppsCommandSuffix()` path against a live `Settings.json`, confirmed that only existing selected apps were encoded into `-bypassApps`, and then fed that payload into the real `InvisibleGorilla-TUN` `AppRulesHandler`, which staged the same paths into `active-bypass-apps.txt` and cleaned them up correctly.
- The same validator executed the real macOS `.NET` bridge path through `MacAppRulesBridge.Prepare()`, confirmed that `mac-transparent-proxy-config.json` was generated with the expected excluded bundle identifiers and then removed by `Clear()`.
- A freshly installed Android `Release` x64 APK launched correctly on the emulator, entered `Running`, and both `Running` and `Stopped` notifications no longer showed `Endpoint` or `Protocol`.
- A freshly installed Android `Debug publish` APK was not suitable for emulator install validation because it aborted at startup with the Mono fast-deployment error `No assemblies found ... Assuming this is part of Fast Deployment`.
- With VPN active, emulator Chrome resolved `https://api.ipify.org` to `158.160.104.107`.
- After `STOP`, the same emulator resolved a fresh request to `95.24.237.142`.
- No `ANR in io.invisiblegorilla.xray` or `Timeout executing service` remained in the final validation pass.
- After the latest Android UI fix, tapping the Wi-Fi/check icon on the emulator reported a real latency (`278 ms`) instead of always timing out.
- The new share flow was rebuilt successfully, but final tap-driven validation of the tiny share icon remains partially blocked by flaky emulator coordinate automation.
- After the notification fix, emulator `dumpsys notification --noredact` showed `Running - WhiteBlade.json - RX 0 B/s TX 0 B/s` while the VPN service was active.
- After tapping `STOP`, the same notification entry remained visible as `Stopped - WhiteBlade.json` instead of disappearing, and the stop transition no longer bounced back to `Running`.
- After the Go bridge shutdown fix, a freshly installed x64 `Release` APK from `publish-android/stopfix-race-x64/` completed two consecutive `RUN -> STOP` cycles on the emulator with no `panic`, no `SIGABRT`, and the app process still alive afterward.
- After the Android app-rules overlay and localization pass, `dotnet build` succeeded for `InvisibleGorilla-XRay.Android`, `InvisibleGorilla-XRay`, and `InvisibleGorilla-XRay.Mac`; the only issue hit during the pass was an initial Android `CS0102` conflict caused by naming helper properties the same as generated Avalonia `x:Name` fields, which was then fixed.
- After replacing the remaining app-rules overlay field accesses with explicit control lookups, emulator validation confirmed that tapping `Manage` now opens the Android app-rules editor instead of falling back to `Lang.AppRules.LoadFailed`. The failure was first reproduced on a debuggable x64 build, diagnosed via `files/InvisibleGorilla-XRay/diagnostic.log`, then re-verified on a fresh `Release` x64 APK.
- `go build -buildmode=c-shared` succeeded for the updated Windows `XRayCore.dll` and for Android `libXRayCore.so` under both `arm64-v8a` and `x86_64`; `dotnet build InvisibleGorilla-XRay.sln -c Debug` also succeeded after the auth-contract changes.
- Fresh `Release` APKs were published under `publish-android/secure-local-bridge-x64/` and `publish-android/secure-local-bridge-arm64/`.
- On the emulator, the fresh x64 APK installed cleanly, `WhiteBlade.json` still launched, `RUN` entered `Running`, an unauthenticated localhost SOCKS5 hello on forwarded port `10801` returned `05-FF`, Chrome opened `https://api.ipify.org` and showed `158.160.104.107`, and `STOP` returned the app to `Stopped` without a crash.

## Next Likely Work
- Run full host-level validation on Windows with the actual Wintun service active so the new auth-capable TUN proxy URI is exercised end to end.
- Validate the Android app-rules selector on a physical Samsung device together with the existing stop/start/share flow.
- Exercise the new Android app-rules overlay on a real device or emulator session to validate template switching, filtered selection persistence, and VPN restart behavior for all three modes.
- Re-run the Android app-rules overlay on a physical Samsung device after the latest `Manage` fix, because the current pass validated the editor open path on the emulator but not yet on the OEM device that originally reproduced the issue.
- Rebuild and validate the macOS native `XRayCore.dylib` plus the transparent-proxy path on a real macOS host; this Windows pass updated the source contract but could not perform a native macOS runtime check.
- Decide how to migrate desktop proxy mode off the legacy unauthenticated listener path once Windows/macOS system-proxy integrations can consume auth-capable proxy settings.

# Active Context

## Current Focus
Add a Linux head (ALT Linux + GNOME first, distro-agnostic in practice) that reuses the macOS Avalonia views and exposes a single `./build.sh` entry point.

## Recent Changes
- Added `InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj` targeting `net7.0` for `linux-x64` / `linux-arm64`, registered it in `InvisibleGorilla-XRay.sln`, and updated `.gitignore` to exclude `publish-linux/` and `dist-linux/` plus the project's `bin/`, `obj/`, and `TUN/` outputs.
- Linked the macOS Avalonia views (`MainWindow`, `ServerWindow`, `SettingsWindow`, `AppRulesWindow`, `AboutWindow`, `UpdateWindow`, `PolicyWindow`) and their styles/icons/localization dictionaries into the Linux project. Each linked `.axaml` file is exposed both as `<AvaloniaResource>` (runtime XAML loading) and as `<AdditionalFiles>` with `<SourceItemGroup>AvaloniaXaml</SourceItemGroup>` so the Avalonia name source generator emits the strongly-typed `x:Name` field accessors that the linked code-behind partial classes depend on.
- Implemented `LinuxAppManager`, `LinuxHandlersInitializer`, `LinuxPipeManager` (single-instance + deep-link IPC over a Unix domain socket), and `LinuxWindowFactory` to build the linked Mac windows from the Linux head.
- Implemented Linux system handlers: `LinuxLocalizationHandler` (avares://InvisibleGorilla-XRay.Linux), `LinuxNotifyHandler` (Avalonia `TrayIcon` + `notify-send` via libnotify), `LinuxProxy` (GNOME `gsettings org.gnome.system.proxy*`), `LinuxStartup` (`~/.config/autostart/*.desktop`), `LinuxDeepLink` (`xdg-mime` default for `vless://`, `vmess://`, `ig-xray://`), `LinuxAppRulesBridge` (JSON manifest for future kernel enforcement), and `LinuxTunnel` (xjasonlyu `tun2socks` + `ip tuntap` / `ip route` / `resolvectl`, privileged steps via `pkexec`/`sudo -n`, fail-closed on TUN failure).
- Added `Services/MacInstalledAppDiscovery.cs` inside the Linux project but kept it in the `InvisibleGorillaXRay.Mac.Services` namespace so the linked Mac App Rules UI binds to Linux `.desktop` parsing without any view-side changes.
- Authored `build.sh` at the repo root: detects distro family (ALT/apt-rpm, Debian/apt, Fedora/dnf, openSUSE/zypper, Arch/pacman), installs build deps and Avalonia runtime libs, ensures .NET SDK 7 + Go, builds `libXRayCore.so`, fetches `tun2socks` and geo files, runs `dotnet publish` for the chosen Linux RID, and bundles the result into `dist-linux/<rid>/` together with an `install.sh` and a `.desktop` file.

## Validation Snapshot
- `dotnet build InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj -c Release` succeeds on Windows after adding `<SourceItemGroup>AvaloniaXaml</SourceItemGroup>` to every linked `<AdditionalFiles>` entry. Without that metadata the Avalonia name generator skipped linked XAML and produced 227 `CS0103: Имя "<controlName>" не существует в текущем контексте` errors.
- Solution-wide `dotnet build InvisibleGorilla-XRay.sln -c Release` builds Mac, Core, Android, and Linux cleanly. The only error is `MSB3027` on `InvisibleGorilla-XRay\bin\Release\net7.0-windows\Invisible Gorilla XRay.exe`, caused by the WPF Windows app being currently running locally (PID 14156); it is unrelated to the Linux work and goes away once the running instance is closed.
- Runtime validation on a real ALT Linux + GNOME host is still pending — this iteration was a build-side port.
- Follow-up Linux build audit found and fixed three packaging/runtime issues: `build.sh` now publishes a real single-file `InvisibleGorilla-XRay.Linux` binary under `publish-linux/<rid>/`, builds/copies the native bridge as `Libraries/libXRayCore.so` (matching `XRayCoreWrapper`), and `LinuxTunnel` resolves `tun2socks` from `AppContext.BaseDirectory/TUN/tun2socks` instead of `./TUN/tun2socks`.
- Validation after the audit: `bash -n build.sh` succeeds; `dotnet build InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj -c Release` succeeds; direct `dotnet publish` with `PublishSingleFile=true` produces an 86 MB `InvisibleGorilla-XRay.Linux` executable; `build.sh --step bundle` was exercised against that single-file output and produced a tarball successfully.
- Windows build follow-up: a clean test machine had only `.NET SDK 7.0.410`, while `global.json` pins `8.0.419`; `build.ps1` incorrectly treated 7.x as OK and then `dotnet restore` failed. `Ensure-DotNet` now reads `global.json`, installs the exact pinned SDK with `dotnet-install.ps1 -Version`, and validates that `dotnet --list-sdks` sees it before restore/build.
- Windows build follow-up 2: after SDK bootstrap succeeded, clean Windows machines failed on `NETSDK1147` because `build.ps1` restored the full solution and pulled in `InvisibleGorilla-XRay.Android.csproj` without Android workload installed. `Build-DotNetApp` now restores/builds/publishes only `InvisibleGorilla-XRay/InvisibleGorilla-XRay.csproj` for normal Windows desktop builds.
- Validation after the Windows build fixes: PowerShell parse check succeeds; direct `dotnet restore InvisibleGorilla-XRay/InvisibleGorilla-XRay.csproj` succeeds without Android workload; `build.ps1 -Step DotNet` reaches the Windows project build on SDK `8.0.419`. Local validation only fails because a running `Invisible Gorilla XRay.exe` process locks the output exe, which is unrelated to SDK/workload resolution.

## Next Likely Steps
- Run `./build.sh` on an actual ALT Linux + GNOME box, confirm gsettings proxy mode toggles reachability, exercise TUN mode end-to-end, and verify tray + notify-send + autostart + xdg-mime deep-link handoff.
- Decide whether to add cgroups + `iptables -m owner` enforcement for the Linux app-rules bridge once the JSON contract has shipped.
- Consider promoting the linked-view + `<AdditionalFiles SourceItemGroup="AvaloniaXaml">` pattern into a shared build prop file so future heads (e.g. additional desktops) do not have to re-discover it.

# Active Context

## Current Focus
Improve desktop configuration export so users can choose QR code, configuration file, or an import link instead of being forced into QR-only sharing.

## Recent Changes
- Reworked the Windows `ServerWindow` share panel into a three-option export chooser: QR code rendering, `.json` save dialog, and clipboard copy of a self-contained `invxray://config-data/...` import link.
- Added shared parsing for exported `data:application/json;name=...;base64,...` configuration links in both the Windows duplicate and `InvisibleGorilla.Core`, so a generated import link can round-trip back through the existing link import flow.
- Added `DeepLink.CONFIG_DATA` and taught Windows/Core deep-link handlers to decode the new import-link payload before forwarding it to the config import UI.
- Fixed manual import of exported links: `ConfigTemplate` now accepts the full `invxray://config-data/...` wrapper as well as the decoded `data:application/json...` payload, so users can paste exported links into the regular Add Config Link field on another machine.
- Android external share/deep-link dispatch now recognizes `invxray://config-data/...` and forwards it into the same config import path.

## Validation Snapshot
- `dotnet build InvisibleGorilla-XRay/InvisibleGorilla-XRay.csproj -c Debug` succeeds after the share panel changes.
- `dotnet build InvisibleGorilla.Core/InvisibleGorilla.Core.csproj -c Debug` succeeds for both target frameworks after the shared parser changes.
- `dotnet build InvisibleGorilla-XRay.Android/InvisibleGorilla-XRay.Android.csproj -c Debug` succeeds after adding Android dispatch for exported config-data links.

# Active Context

## Current Focus
Harden release/build scripts so remote Windows and Linux builders get actionable behavior instead of ambiguous packaging, PATH, or file-lock failures.

## Recent Changes
- Updated `build.sh` packaging so every Linux archive contains an obvious top-level `run-igxray` launcher plus a `bin/invisible-gorilla-xray` command wrapper that execs the real `bin/InvisibleGorilla-XRay.Linux` binary.
- `install.sh` now removes stale installed `bin`/`share` folders before copying the new bundle, installs `/usr/local/bin/invisible-gorilla-xray`, and adds an `igxray` symlink.
- The generated `.desktop` file now launches the command wrapper and advertises the `invxray://` scheme along with `vless://`, `vmess://`, and the legacy `ig-xray://` handler.
- Linux runtime deep-link registration now also binds `x-scheme-handler/invxray`.
- Windows `build.ps1` now detects a running build-output `Invisible Gorilla XRay.exe` before `.NET` restore/build/publish. By default it closes that matching process so MSBuild can replace the exe; `-NoStopRunningApp` keeps the old safe behavior and fails early with the blocking PID/path.
- Linux `build.sh` now reads the required SDK from `global.json`, resolves that SDK into `DOTNET_CMD`, and calls restore/publish through the resolved executable. This fixes ALT Linux cases where only SDK 7 is installed while repo-level `global.json` requires SDK `8.0.419`.
- Linux dependency installation now retries packages one by one after a grouped package install fails, and ALT uses `notify-send` instead of the missing `libnotify-tools` package. The publish step also verifies and prints the exact `InvisibleGorilla-XRay.Linux` binary path.
- The pinned Linux .NET fallback no longer installs into `$HOME/.dotnet`; it uses repo-local `.dotnet-sdk/` so a root-owned or otherwise unwritable home SDK cache cannot block ALT/Simply Linux builds.
- Linux `build.sh` now also sets `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, and first-run/telemetry flags to repo-local `.dotnet-home/` and `.nuget/`, preventing `dotnet restore` from writing sentinel files into an unwritable `$HOME/.dotnet`.
- macOS `build-macos.sh` now mirrors the hardened SDK/bootstrap behavior: reads the exact SDK from `global.json`, installs fallback SDKs into repo-local `.dotnet-sdk/`, isolates `.NET` CLI/NuGet state into `.dotnet-home/` and `.nuget/`, and always invokes restore/publish through the resolved `DOTNET_CMD`.
- macOS build outputs now land in a single runnable folder `dist-macos/<runtime>/` containing `InvisibleGorilla-XRay.app`, a top-level `run-igxray` launcher, `README-MACOS.txt`, and a tarball. The script prints the raw publish path, app bundle path, internal binary path, launcher path, and archive path; missing runtime files fail the build explicitly.
- macOS startup now separates runtime files from writable user data. Runtime assets still load from `.app/Contents/MacOS`, while settings/configs/logs/diagnostics write under `~/Library/Application Support/InvisibleGorilla-XRay`; Linux uses `$XDG_DATA_HOME/InvisibleGorilla-XRay` or `~/.local/share/InvisibleGorilla-XRay`. This avoids first-launch crashes when a bundle was built or unpacked with root-owned files.
- macOS startup now writes unhandled startup exceptions to `~/Library/Application Support/InvisibleGorilla-XRay/Logs/startup-crash.log` in addition to `diagnostic.log`.

## Validation Snapshot
- `bash -n build.sh` succeeds after the launcher/install changes.
- `dotnet build InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj -c Release` succeeds on Windows.
- PowerShell parse validation for `build.ps1` succeeds.
- `.\build.ps1 -Step DotNet -Configuration Debug -NoStopRunningApp` succeeds; only existing project warnings remain.
- `bash -n build.sh` still succeeds after the `DOTNET_CMD` resolver change.
- `bash -n build.sh` succeeds after the ALT package fallback and publish-output verification changes.
- `bash -n build.sh` succeeds after switching Linux SDK bootstrap from hard-coded SDK 7 to the `global.json` SDK.
- `bash -n build.sh` succeeds after moving the pinned SDK fallback install dir to `.dotnet-sdk/`.
- `bash -n build.sh` succeeds after moving .NET CLI home and NuGet packages to repo-local folders.
- `bash -lc "tr -d '\\r' < build-macos.sh | bash -n"` succeeds after the macOS SDK/output hardening. The local Windows checkout still reports CRLF in the working tree, but repository `.gitattributes` already enforces LF for `*.sh` when committed/pulled.
- `dotnet build InvisibleGorilla-XRay.Mac/InvisibleGorilla-XRay.Mac.csproj -c Debug`, `dotnet build InvisibleGorilla.Core/InvisibleGorilla.Core.csproj -c Debug`, and `dotnet build InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj -c Debug` succeed after the runtime/data root split. The only remaining Linux/Mac warnings are existing `net7.0` EOL warnings.

# Active Context

## Current Focus
Diagnose and prevent the Android black-screen / freeze that reproduced when App Rules whitelist (`ONLY_SELECTED_APPS`) was active, and give end users a way to capture and ship diagnostic data without developer tooling.

## Recent Changes
- Hardened `InvisibleGorilla.Core/Core/DiagnosticLog.cs` with size-capped rotation (active `diagnostic.log` plus rotated `diagnostic.log.1` at 1 MB), `ReadAll` / `ClearAll` helpers, and deeper exception walking (up to 5 nested `InnerException` levels).
- Added Android `FileProvider` plumbing: a `<provider>` entry in `AndroidManifest.xml` with authority `io.invisiblegorilla.xray.fileprovider` plus `Resources/xml/file_paths.xml` exposing the relevant files/cache paths.
- New `Services/AndroidLogShareService.cs` provides three first-class actions: `ShareDiagnosticLogAsync` (bundles current + rotated logs + device/system info into the cache and launches `Intent.ActionSend` via the FileProvider), `SaveDiagnosticLogAsync` (writes to `MediaStore.Downloads/InvisibleGorillaXRay/` on Android Q+ or app-private external files as fallback), and `ClearDiagnosticLog`.
- Updated Android `Views/MainView.axaml(.cs)` with a dedicated `Logs and diagnostics` panel in Settings: description, live status (size/path), and `Share log` / `Save log to device` / `Clear log` buttons. `RefreshLogsStatus` plus `FormatLogSize` keep the status text human-readable.
- Added matching `Lang.Android.Logs.*` keys to `InvisibleGorilla-XRay.Mac/Assets/Localization/{ru-RU,en-US}.axaml` (description, status, share/save/clear, in-progress + success/failure messages).
- Installed global Android exception handlers in `MainActivity.cs`: `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`, and a Java `IUncaughtExceptionHandler` all route into `DiagnosticLog`. `OnCreate`'s `base.OnCreate` is wrapped in try/catch, and a new `internal static MainActivity? CurrentActivity` property lets the log share service grab an `Activity` context.
- Wrapped `AvaloniaXamlLoader.Load(this)` in `App.axaml.cs` with try/catch + `DiagnosticLog.WriteException` so a XAML init failure no longer manifests as an opaque black screen.
- Defensive whitelist fix in `Services/AndroidVpnService.cs`: in `ONLY_SELECTED_APPS` mode, if zero packages are selected OR if none of the selected packages can actually be added (own package, missing package, etc.), `ApplyApplicationRules` now throws a user-friendly `InvalidOperationException` instead of letting Android silently fall back to routing everything. `TryAllowUserSelectedApplications` returns the count of accepted packages and logs every skip. `builder.Establish()` is bracketed by diagnostic writes.

## Latest Validation Snapshot
- `dotnet build` succeeded for `InvisibleGorilla.Core`, `InvisibleGorilla-XRay.Mac`, `InvisibleGorilla-XRay.Linux`, and `InvisibleGorilla-XRay.Android` (only pre-existing warnings remain).
- The Android publish errors that initially showed up were resolved before re-running: `CS1503` on `FileProvider.GetUriForFile` (now passes `new global::Java.IO.File(snapshot.FullName)`) and `CS0117` on `MediaStore.IDownloadColumns.DisplayName` (now `MediaStore.IMediaColumns.DisplayName`).
- Fresh signed APKs published to `publish-android/logging-arm64/io.invisiblegorilla.xray-Signed.apk` (~67.6 MB) and `publish-android/logging-x64/io.invisiblegorilla.xray-Signed.apk` (~69.6 MB).

## Next Likely Steps
- Install the new `logging-arm64` APK on the original Samsung device that hit the black screen, reproduce the whitelist flow, and confirm either (a) the new `InvalidOperationException` surfaces with a readable error and the app keeps running, or (b) the share log feature ships a usable `diagnostic.log` if anything else fails.
- If a new failure mode appears, ask the user to use the new `Share log` button so the rotated + active log + device info arrive in one shot.

