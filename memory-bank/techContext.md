# Tech Context

## Stack
- C# / .NET 8
- Avalonia UI
- Android VpnService
- XRay runtime and native Android bridge

## Build Notes
- `global.json` pins the SDK to .NET 8.
- Android publish targets used in this repo:
- `android-x64` for emulator
- `android-arm64` for real devices
- Release publish is preferred over Debug fast deployment.

## Important Constraints
- Android runtime changes may require rebuilding native libraries for both `arm64-v8a` and `x86_64`.
- Android overlay controls should not rely on generated `x:Name` fields in complex views.
# Tech Context

## Languages And Frameworks
- C# / .NET 8 for shared core and Android app code.
- Avalonia 11 for the Android UI layer.
- Go with cgo for the native `XRayCore` bridge and Android `tun2socks` glue.

## Important Android Components
- `InvisibleGorilla-XRay.Android/Services/AndroidVpnService.cs`
- `InvisibleGorilla-XRay.Android/Services/AndroidVpnServiceController.cs`
- `InvisibleGorilla-XRay.Android/Handlers/Tunnels/AndroidTunnel.cs`
- `InvisibleGorilla-XRay.Android/Views/MainView.axaml.cs`
- `XRay-Wrapper/xray/android_tun2socks.go`

## Tooling
- Local .NET SDK path used in this workspace: `C:\Users\hex\AppData\Local\Microsoft\dotnet\dotnet.exe`
- Android SDK root: `C:\Users\hex\AppData\Local\Android\Sdk`
- Android NDK version in use: `26.3.11579264`
- JDK path used for Android publish: `C:\Program Files\Eclipse Adoptium\jdk-17.0.18.8-hotspot`

## Build Notes
- `global.json` pins the SDK to `8.0.419`; Windows `build.ps1` must check and install that exact SDK with `dotnet-install.ps1 -Version 8.0.419`, not treat SDK 7.x as sufficient.
- Windows `build.ps1` targets `InvisibleGorilla-XRay/InvisibleGorilla-XRay.csproj` for restore/build/publish. Do not restore the full `.sln` in the default Windows desktop build, because that requires Android workload on machines that only need the WPF client.
- Windows `build.ps1` preflights a running output `Invisible Gorilla XRay.exe` before `.NET` build/publish. Default behavior is to close the matching process to avoid MSBuild file-lock errors; use `-NoStopRunningApp` when validating without terminating a local app instance.
- macOS `build-macos.sh` must also read `global.json` and install/resolve the exact pinned SDK (`8.0.419`) instead of accepting or installing SDK 7.x. Its fallback install dir is repo-local `.dotnet-sdk/`; `DOTNET_CLI_HOME` and `NUGET_PACKAGES` point to repo-local `.dotnet-home/` and `.nuget/packages/`.
- macOS raw publish output lives under `publish-macos/<rid>/`; the runnable deliverable lives under `dist-macos/<rid>/` with `InvisibleGorilla-XRay.app`, `run-igxray`, `README-MACOS.txt`, and `InvisibleGorilla-XRay-macOS-<rid>-v<version>.tar.gz`. The internal app binary is `InvisibleGorilla-XRay.app/Contents/MacOS/InvisibleGorilla-XRay.Mac`.
- Shared `InvisibleGorilla.Core` now distinguishes writable data root from runtime root. `Directory.ROOT`/`DATA_ROOT`, configs, logs, settings, and data-side TUN descriptors are user-writable; `Directory.RUNTIME_ROOT`, `LIBRARIES`, `TUN`, and `ASSETS` point to the installed bundle/publish output. macOS configures data root to `~/Library/Application Support/InvisibleGorilla-XRay`; Linux configures it to `$XDG_DATA_HOME/InvisibleGorilla-XRay` or `~/.local/share/InvisibleGorilla-XRay`.
- macOS startup crash diagnostics are written to `~/Library/Application Support/InvisibleGorilla-XRay/Logs/startup-crash.log`; normal diagnostic logs remain at `~/Library/Application Support/InvisibleGorilla-XRay/diagnostic.log`.
- Rebuild both Android native libraries after Go bridge changes.
- Rebuild the Windows `XRayCore.dll` after shared Go wrapper changes that affect the local listener contract.
- Publish emulator APK with RID `android-x64`.
- Publish device APK with RID `android-arm64`.
- The Android project currently disables trimming and AOT for build stability.

## Runtime Constraints
- Android uses app-private storage and packaged native libs from the APK.
- Complex `vless://` links can be awkward through `adb shell am start` because shell parsing may corrupt `&` and `#`.
- Mobile validation sometimes needs a mix of Mobile MCP and direct `adb shell input tap`.
- Desktop proxy mode and TUN mode now intentionally diverge for the local listener contract: TUN paths can require SOCKS auth, while the current Windows/macOS system-proxy integrations still use the temporary legacy listener path.

# Tech Context

## Linux Stack
- C# / .NET 7 (`net7.0` for parity with macOS head) with Avalonia 11 desktop.
- Linux RIDs published: `linux-x64`, `linux-arm64` (self-contained, single-file).
- Native runtime: shared `XRay-Wrapper` Go bridge built as `libXRayCore.so` plus pre-built `tun2socks` (xjasonlyu) binary fetched from GitHub releases.
- System integration: `gsettings`, `notify-send`, `xdg-mime`, `pkexec`/`sudo`, `iproute2`, `resolvectl` (with `/etc/resolv.conf` fallback).

## Linux Project Layout
- `InvisibleGorilla-XRay.Linux/InvisibleGorilla-XRay.Linux.csproj` — Avalonia `<OutputType>Exe</OutputType>`; XAML resources and code-behind are linked from `InvisibleGorilla-XRay.Mac/` and exposed to the Avalonia name source generator via `<AdditionalFiles ... SourceItemGroup="AvaloniaXaml" />`. Without `SourceItemGroup="AvaloniaXaml"`, generated `x:Name` field accessors for linked views are missing and CS0103 errors appear.
- `Program.cs` / `App.axaml(.cs)` — standard Avalonia desktop bootstrap, wires `LinuxAppManager` and registers `LinuxWindowFactory`.
- `Managers/` — `LinuxAppManager`, `LinuxHandlersInitializer`, `LinuxPipeManager` (single-instance + deep-link IPC over `~/.config/.../single-instance.sock`).
- `Handlers/` — `LinuxLocalizationHandler`, `LinuxNotifyHandler` (TrayIcon + `notify-send`), `Proxies/LinuxProxy` (gsettings), `Settings/LinuxStartup` (`~/.config/autostart/*.desktop`), `DeepLinks/LinuxDeepLink` (`xdg-mime`), `Tunnels/LinuxTunnel` (tun2socks via pkexec), `Tunnels/LinuxAppRulesBridge` (JSON bridge file).
- `Services/MacInstalledAppDiscovery.cs` — implemented inside the Linux project but kept under the `InvisibleGorillaXRay.Mac.Services` namespace so the linked Mac UI reuses it without source edits. Parses `/usr/share/applications`, `/usr/local/share/applications`, `~/.local/share/applications`, and Flatpak/Snap dirs.

## Linux Build Notes
- Single entry point: `./build.sh` at repo root.
- Detects ALT (apt-rpm), Debian/Ubuntu (apt), Fedora/RHEL (dnf), openSUSE (zypper), Arch (pacman). Asks for sudo before installing build deps.
- Installs the .NET SDK pinned by `global.json` (currently `8.0.419`, via Microsoft script when distro doesn't ship it), `golang`, `wget`, `curl`, `git`, `tar`, plus runtime libs Avalonia needs (`libfontconfig`, `libice`, `libsm`, `libx11`, `libxrandr`, `libxcursor`, `libxi`, `libgl`, etc.). Package groups can fail on ALT when one optional package is absent, so `build.sh` retries package installs individually and uses ALT's `notify-send` package name.
- On ALT Linux, apt-rpm may not provide the exact pinned .NET SDK patch and `$HOME/.dotnet` can be unwritable if an earlier run created it through root/sudo. `build.sh` must read `global.json`, resolve the matching executable into `DOTNET_CMD`, and install the Microsoft-script fallback into repo-local `.dotnet-sdk/` instead of `$HOME/.dotnet`. It must also set `DOTNET_CLI_HOME` to `.dotnet-home/` and `NUGET_PACKAGES` to `.nuget/packages/` so first-run sentinel files and package caches never need write access to home. Installing only SDK 7 or ALT's `8.0.1xx` SDK is insufficient because repo-level `global.json` requests SDK `8.0.419`.
- Builds Go wrapper as `libXRayCore.so` for the host arch (not `XRayCore.so`; the Linux DllImport resolver looks for `Libraries/libXRayCore.so`), downloads matching `tun2socks-linux-<arch>` from the upstream GitHub release, and pulls `geosite.dat` / `geoip.dat`.
- `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` (or `linux-arm64`) into `publish-linux/<rid>/`, producing the executable `InvisibleGorilla-XRay.Linux`.
- Bundles output into `dist-linux/<rid>/` with `install.sh` that drops `Invisible Gorilla XRay.desktop` into `~/.local/share/applications/` and links the binary into `~/.local/bin/`.

## Linux Runtime Constraints
- TUN mode requires CAP_NET_ADMIN; the app does not self-elevate and prompts via `pkexec` only when activating TUN. Failure is fail-closed.
- `gsettings` only exists on GNOME-based shells. On non-GNOME desktops, proxy mode falls back to a no-op and logs a warning; TUN mode is unaffected.
- Tray icon is provided by Avalonia's `TrayIcon`, which uses `StatusNotifierItem` over D-Bus. On GNOME this requires the AppIndicator extension; otherwise notifications still work via `notify-send`.
- Linux app-rules enforcement is currently a JSON bridge only (`LinuxAppRulesBridge`); kernel-level per-app routing (cgroups + `iptables -m owner`) is intentionally deferred to keep the first port reviewable.
- `LinuxTunnel` must launch `tun2socks` through `Values.Path.TUN_EXE` (`AppContext.BaseDirectory/TUN/tun2socks`), not `./TUN/tun2socks`, because `.desktop` launches do not guarantee the current working directory is the app's `bin/` folder.
