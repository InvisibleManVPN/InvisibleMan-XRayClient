# Progress

## Working
- Android app rules editor/picker split is implemented.
- Async installed-app loading is implemented.
- App icons are shown in the Android picker.
- Accidental checkbox toggles during scroll were fixed.
- Android builds succeed in Release configuration.
- Latest device APK was published to `publish-android/mobile-arm64/io.invisiblegorilla.xray-Signed.apk`.

## In Progress
- Ongoing runtime verification of Android app rule enforcement.
- Investigated reports that excluded apps still detect VPN/TUN/proxy presence: current evidence points to platform-visible VPN/TUN state rather than a simple routing leak in the Android path.

## Remaining
- Publish and hand off current Android device artifact when requested.
- Continue validating per-app routing behavior across platforms.
# Progress

## Working
- Shared Xray config loading and runtime startup in `InvisibleGorilla.Core`.
- Shared cross-platform storage for app-routing rules in `UserSettings` / `SettingsHandler`.
- Android config import from links/files and subscription management UI.
- Android per-app bypass selection and `VpnService` disallowed-app application.
- Android app-rules overlay editor with template switching, three routing modes, search, and config-aware summaries in Home/Settings.
- Windows desktop app discovery plus settings-side app-rules editor.
- Windows TUN app-rules CLI contract and accepted-rule staging in `active-bypass-apps.txt`.
- macOS desktop app discovery plus settings-side app-rules editor.
- macOS app-rules bridge that stages `macos-transparent-proxy-config.json` for the native transparent-proxy helper.
- Windows app-rules runtime command generation and TUN-side staging contract validated on the local Windows host without altering system routes.
- macOS app-rules bridge generation and cleanup validated through the live `.NET` runtime path.
- Android config check now uses the shared native connection test path instead of a raw endpoint socket probe.
- Android config share now offers a choice between copying the source link and exporting the full config text.
- Android native library packaging through `AndroidNativeLibrary`.
- Android `VpnService` startup with the local mobile bridge to the SOCKS listener.
- Android connection notification now keeps detailed state for both active and recently stopped sessions.
- Browser IP switching on the emulator when VPN is started and restored after stop.
- Android `RUN -> STOP` no longer crashes on the emulator after the Go bridge shutdown fix.
- APK publishing for both `android-x64` and `android-arm64`.
- TUN mode local bridge auth contract across shared runtime, Android, Windows, macOS, and `XRay-Wrapper`.
- Raw JSON config imports sanitized before storage to strip runtime-managed listener/API sections.

## Recently Fixed
- Native `DllNotFoundException` path for Android packaged libraries.
- x86_64 Android runtime packaging and publish flow.
- Device-wide Android routing through `VpnService`.
- Stop-path ANR during `AndroidVpnService` teardown.
- Duplicate/stale `StopServer()` signal handling in the Go wrapper.
- Android config check always reporting timeout.
- Android config share no longer forcing the full JSON into the clipboard by default.
- Android notification no longer loses connection details when the app is reopened or when the VPN transitions from running to stopped.
- Android startup no longer flashes a false `Stopped` notification because stale-tunnel cleanup is separated from the real stop flow.
- Android stop no longer hits the Go-side `nil pointer dereference` in `runAndroidTunLoop` after the bridge shutdown ordering fix.
- Android app-rules localization now resolves correctly through the shared Avalonia/macOS dictionaries, and the new overlay/settings/server summaries show translated text instead of raw localization keys.
- Android `Manage` for app rules is now guarded against discovery/runtime exceptions, so a single problematic installed app should no longer crash the whole editor open path.
- Android `Manage` for app rules now also survives the remaining Avalonia Android runtime nulls: the editor opens correctly on the emulator after switching the overlay controls from generated `x:Name` fields to explicit `GetRequiredControl(...)` lookups.
- The local SOCKS listener no longer accepts the old unauthenticated Android TUN handshake; the latest emulator proof returned `05-FF` for a no-auth SOCKS hello on forwarded port `10801`.
- Android `tun2socks` now authenticates over both TCP and UDP against the protected local listener instead of relying on the upstream unauthenticated SOCKS handlers.
- Fresh secure-bridge release APKs were published under `publish-android/secure-local-bridge-x64/` and `publish-android/secure-local-bridge-arm64/`.
- The latest x64 emulator proof still reached `158.160.104.107` in Chrome while `Running`, then returned to `Stopped` cleanly after `STOP`.

## Remaining Risks
- Windows process-aware bypass still needs full end-to-end validation with the actual Wintun driver and route changes enabled; the current pass validated the command/staging contract but deliberately avoided invasive host networking changes.
- macOS native enforcement is only scaffolded so far; the signed Network Extension still needs to be built and exercised on a real macOS host. The current pass only validated bridge-config generation from the `.NET` client side.
- The macOS source path now carries the new auth-capable local SOCKS contract, but this Windows session did not rebuild or runtime-check a native macOS `XRayCore.dylib`.
- Physical-device validation is still needed for the new Android app-rules flow after the latest settings changes.
- The new three-mode Android app-rules overlay still needs real tap-driven validation for template creation/deletion, filtered app selection persistence, and restart behavior on a device or emulator session.
- The latest `Manage` fix is live-validated on the emulator, but still needs confirmation on the physical Samsung device that originally reproduced the issue.
- Physical-device validation is still needed after the latest stop/share fixes.
- `dotnet publish -c Debug` produces an APK that is not valid for standalone emulator installation because it expects fast-deployment assemblies at runtime; use `Release` for installable validation builds.
- Android analyzer/nullability warnings remain in notification and service code.
- Deep-link testing through raw `adb shell am start` can still be misleading because of shell escaping issues.
- Emulator UI automation is still flaky for the small icon-only buttons, so share-sheet validation was only partial on the latest pass.

## Artifacts
- Emulator APK output: `publish-android/secure-local-bridge-x64/io.invisiblegorilla.xray-Signed.apk`
- Device APK output: `publish-android/secure-local-bridge-arm64/io.invisiblegorilla.xray-Signed.apk`
- Windows TUN diagnostics artifact for app rules: `active-bypass-apps.txt`
- macOS app-rules bridge artifact: `TUN/macos-transparent-proxy-config.json`

# Progress

## Working (Linux head)
- `InvisibleGorilla-XRay.Linux` builds cleanly under `dotnet build -c Release` on Windows.
- Linked Mac Avalonia views compile against the Linux project via `<AvaloniaResource>` + `<AdditionalFiles SourceItemGroup="AvaloniaXaml">`.
- Linux platform handlers cover tray (`TrayIcon`), notifications (`notify-send`), GNOME proxy (`gsettings`), autostart (`.desktop`), deep links (`xdg-mime`), TUN tunnel (xjasonlyu `tun2socks` via `pkexec`/`sudo -n`), app-rules bridge (JSON manifest), and `.desktop`-based installed-app discovery for the App Rules UI.
- Single-instance + deep-link IPC via Unix domain socket in `LinuxPipeManager`.
- `build.sh` covers ALT Linux (apt-rpm), Debian/Ubuntu (apt), Fedora/RHEL (dnf), openSUSE (zypper), and Arch (pacman).

## In Progress (Linux head)
- Real ALT Linux + GNOME runtime validation of TUN mode, GNOME proxy, tray icon, autostart, and deep links.

## Remaining (Linux head)
- Optional kernel-level Linux app-rules enforcement (cgroups + `iptables -m owner`); current `LinuxAppRulesBridge` only persists the JSON manifest.
- Optional CI matrix entry for `linux-x64` / `linux-arm64`.
- Optional `.deb` / `.rpm` / Flatpak packaging on top of the current `dist-linux/<rid>/` tarball-style bundle.

## Recently Fixed (Linux head)
- 227 × `CS0103: Имя "<controlName>" не существует в текущем контексте` errors when building the Linux project against linked Mac code-behind. Root cause: Avalonia name source generator only processes `<AdditionalFiles>` whose `SourceItemGroup` metadata equals `AvaloniaXaml`. Fixed by adding `<SourceItemGroup>AvaloniaXaml</SourceItemGroup>` to every linked `<AdditionalFiles>` entry in `InvisibleGorilla-XRay.Linux.csproj`.
- Linux build script now produces the expected single-file GUI binary at `publish-linux/<rid>/InvisibleGorilla-XRay.Linux` instead of a small apphost plus a large DLL tree.
- Linux native bridge packaging now uses the correct filename/path `Libraries/libXRayCore.so`; the previous `XRayCore.so` output name did not match the Linux `XRayCoreWrapper` resolver.
- Linux TUN startup now resolves `tun2socks` from `AppContext.BaseDirectory/TUN/tun2socks` via `Values.Path.TUN_EXE`, so `.desktop` launches no longer depend on the current working directory.

## Recently Fixed (Windows build)
- `build.ps1` now reads `global.json` and requires the pinned .NET SDK (`8.0.419`) instead of accepting any installed SDK 7.x. This fixes clean Windows machines where `dotnet restore` failed with `A compatible .NET SDK was not found` after dependency checks incorrectly reported `.NET SDK 7.0.410` as OK.
- `build.ps1` now restores/builds/publishes `InvisibleGorilla-XRay/InvisibleGorilla-XRay.csproj` for the Windows desktop app instead of the full solution. This prevents a clean Windows desktop build from requiring the Android workload because of `InvisibleGorilla-XRay.Android.csproj`.

## Remaining Risks (Linux head)
- Tray icon depends on `StatusNotifierItem` over D-Bus; on stock GNOME this requires the AppIndicator extension. Without it, Avalonia falls back to no tray, but `notify-send` notifications and the main window keep working.
- `LinuxTunnel` shells out to `pkexec`/`sudo`. On non-systemd or non-`resolvectl` distros the DNS override path will fall back to `/etc/resolv.conf` and may need extra packaging on locked-down hosts.
- The Linux head currently reuses Mac XAML verbatim. Any future Mac-only UI tweak that relies on AppKit or Cocoa-specific behaviors will need a guard or a Linux fork of that view.

## Artifacts (Linux head)
- Source build script: `build.sh`
- Linux project: `InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj`
- Per-RID publish output (gitignored): `publish-linux/<rid>/`
- Distributable bundle (gitignored): `dist-linux/<rid>/`

# Progress

## Recently Fixed (config export)
- Windows desktop config sharing is no longer QR-only. The server config export panel now lets the user choose QR code, `.json` configuration file, or a clipboard import link.
- Exported links use `invxray://config-data/...` with a base64-encoded JSON config payload and can be imported back through the existing config-link import flow.
- Full exported `invxray://config-data/...` links can now be pasted directly into the normal config-link import field; previously only the decoded inner `data:application/json...` payload was accepted.
- Android external import dispatch now recognizes the same exported config-data link format.

## Validation
- Windows desktop project build succeeds after the UI/export changes.
- Shared Core build succeeds after adding exported data-config link parsing.
- Android project build succeeds after the exported config-data dispatch update.

# Progress

## Recently Fixed (Linux bundle)
- Linux archives now include a top-level `run-igxray` launcher so users can run the app immediately after unpacking without hunting for `bin/InvisibleGorilla-XRay.Linux`.
- Installed Linux bundles now expose `/usr/local/bin/invisible-gorilla-xray` and `igxray`, while the application menu launcher points to the same command wrapper.
- Linux deep-link metadata now includes `invxray://`, matching the shared app deep-link scheme used by exported config links.
- Linux `build.sh` no longer depends on `dotnet` being visible in the current shell PATH after fallback installation. It now reads the required SDK from `global.json`, installs/resolves that SDK from system locations, `$HOME/.dotnet/dotnet`, and `/root/.dotnet/dotnet`, then uses that executable for restore/publish.
- ALT/Simply Linux dependency installation no longer treats a missing package in a group as a black box: `build.sh` retries packages individually, uses `notify-send` instead of the missing `libnotify-tools`, and verifies that `publish-linux/<rid>/InvisibleGorilla-XRay.Linux` exists immediately after publish.
- Pinned .NET fallback installs now go into repo-local `.dotnet-sdk/` instead of `$HOME/.dotnet`, avoiding permission failures when the home SDK cache was previously created by root.
- `dotnet restore` no longer writes first-run sentinel files or NuGet packages into `$HOME/.dotnet` / `$HOME/.nuget`; `build.sh` points `DOTNET_CLI_HOME` and `NUGET_PACKAGES` at repo-local `.dotnet-home/` and `.nuget/`.

## Recently Fixed (Windows build)
- `build.ps1` now handles the common `MSB3026` / `MSB3027` failure where `Invisible Gorilla XRay.exe` is already running from the project's output folder and blocks MSBuild from replacing the apphost. The script closes the matching running process before restore/build/publish by default, or reports the blocking PID/path early when `-NoStopRunningApp` is used.

## Recently Fixed (macOS build)
- `build-macos.sh` no longer hard-codes .NET SDK 7.0. It reads the exact SDK from `global.json`, resolves an existing matching `dotnet`, or installs the pinned SDK into repo-local `.dotnet-sdk/`.
- macOS `dotnet restore` / `publish` now use repo-local `.dotnet-home/` and `.nuget/packages/`, avoiding hidden writes to a broken or root-owned home SDK/cache.
- macOS build artifacts now have clear locations: raw publish output in `publish-macos/<rid>/`, and the runnable bundle in `dist-macos/<rid>/` with `InvisibleGorilla-XRay.app`, `run-igxray`, `README-MACOS.txt`, and a tarball archive. The script prints those paths and fails explicitly if required runtime files are missing.
- macOS first launch no longer writes settings/configs/logs into `.app/Contents/MacOS`. Runtime files stay in the app bundle, while user data goes to `~/Library/Application Support/InvisibleGorilla-XRay`; this addresses the observed `SIGABRT` crash pattern when a bundle was produced by `sudo` and became root-owned.
- Startup exceptions on macOS are now captured in `~/Library/Application Support/InvisibleGorilla-XRay/Logs/startup-crash.log`.

## Validation
- `bash -n build.sh` succeeds.
- `dotnet build InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj -c Release` succeeds.
- PowerShell parse validation for `build.ps1` succeeds.
- `.\build.ps1 -Step DotNet -Configuration Debug -NoStopRunningApp` succeeds; only existing warnings remain.
- `bash -n build.sh` succeeds after the Linux `DOTNET_CMD` resolver change.
- `bash -n build.sh` succeeds after the ALT package fallback and publish-output verification changes.
- `bash -n build.sh` succeeds after switching Linux SDK bootstrap from hard-coded SDK 7 to the `global.json` SDK.
- `bash -n build.sh` succeeds after moving the pinned SDK fallback install dir to `.dotnet-sdk/`.
- `bash -n build.sh` succeeds after moving .NET CLI home and NuGet packages to repo-local folders.
- `bash -lc "tr -d '\\r' < build-macos.sh | bash -n"` succeeds for the macOS script after SDK/output hardening.
- `dotnet build InvisibleGorilla-XRay.Mac/InvisibleGorilla-XRay.Mac.csproj -c Debug`, `dotnet build InvisibleGorilla.Core/InvisibleGorilla.Core.csproj -c Debug`, and `dotnet build InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj -c Debug` succeed after the macOS launch crash fix. Existing `net7.0` EOL warnings remain.
