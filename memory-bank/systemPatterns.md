# System Patterns

## Architecture
- `InvisibleGorilla.Core` contains shared models, handlers, and settings logic.
- Platform projects implement UI and runtime integration:
- `InvisibleGorilla-XRay.Android`
- `InvisibleGorilla-XRay`
- `InvisibleGorilla-XRay.Mac`

## Key Patterns
- Shared `UserSettings` model for cross-platform behavior.
- Platform-specific tunnel services/controllers for VPN/TUN integration.
- Android UI uses Avalonia code-behind with explicit `GetRequiredControl(...)` access for complex overlays.
- App rules are template-driven and bound per configuration.

## Android-Specific Notes
- Release APKs are used for emulator/runtime verification.
- Android app picker renders discovered launchable apps and now includes icons.
- VPN routing and app rule enforcement require runtime verification, not only build validation.
- On Android, `VpnService.Builder.addDisallowedApplication(...)` changes routing for excluded apps but does not guarantee that third-party apps cannot detect that a system VPN session is active.
# System Patterns

## Architecture
- `InvisibleGorilla.Core` owns shared config loading, runtime settings, Xray startup/shutdown, and proxy/tunnel abstraction.
- Platform heads provide UI, storage, and OS-specific routing integrations.
- Android-specific VPN control lives in `InvisibleGorilla-XRay.Android/Services`.

## App Rules Pattern
- Shared `AppRule` and `AppRulesMode` live in the common settings model so all heads serialize the same bypass contract.
- Android uses a real exclusion model through `VpnService.Builder.AddDisallowedApplication(...)`.
- Windows currently forwards normalized executable paths into `InvisibleGorilla-TUN` through `-bypassApps=<base64-lines>` and stages the accepted set for downstream native filtering.
- macOS stages a transparent-proxy bridge config file plus helper scaffolding so a signed Network Extension can consume the same excluded-app contract.
- Desktop settings windows discover locally installed apps at runtime and render them as toggleable cards in the Basic settings panel instead of relying on manual path entry.
- Android no longer keeps the app-rules picker inline in the settings scroll tree; it uses a dedicated overlay editor with shared templates, current-config binding, three modes (`ALL_APPS`, `BYPASS_SELECTED_APPS`, `ONLY_SELECTED_APPS`), search, and a compact summary on Home/Settings.
- In the Android editor, filtered searches must preserve selections that are temporarily hidden by the current search text; capture the previous template selection set first, then only replace the visible package subset from the current toggle states.

## Local Bridge Security Pattern
- TUN mode now creates session-scoped local SOCKS credentials inside `InvisibleGorillaXRayCore` and passes them through `XRayCoreWrapper` into the native `XRay-Wrapper`.
- The Go wrapper only enables mandatory SOCKS password auth when that TUN-session auth is present, which keeps Android and desktop TUN paths secure while desktop proxy mode stays on a temporary legacy listener path for OS compatibility.
- Android `tun2socks` no longer relies on the upstream unauthenticated handlers; it uses local auth-aware TCP and UDP handlers so the bridge can authenticate against the protected listener.
- Windows and macOS TUN/helper paths now build auth-capable `socks5://user:pass@127.0.0.1:<port>` proxy URIs for their internal clients.
- Imported raw JSON configs are sanitized before storage so users cannot persist runtime-managed top-level sections like `api`, `stats`, or custom `inbounds` that would recreate unmanaged local surfaces.

## Android Routing Flow
1. `MainView` saves settings and requests VPN permission through `MainActivity`.
2. `InvisibleGorillaXRayCore.Run()` creates session-scoped local SOCKS credentials for TUN mode, starts `XRayCoreWrapper.StartServer()`, and waits for the protected local SOCKS listener to become active.
3. In TUN mode, `AndroidTunnel.Enable()` delegates to `AndroidVpnServiceController.Start(...)` and passes the same credentials via service extras.
4. `AndroidVpnService` establishes the TUN interface and calls `XRayCoreWrapper.StartAndroidTunnel(...)` with the session credentials.
5. Native Go code in `XRay-Wrapper/xray/android_tun2socks.go` authenticates against the protected local SOCKS listener before bridging TUN packets.

## Android Stop Pattern
- Stop must not block the service main thread.
- `AndroidVpnService` handles stop work asynchronously.
- Native tunnel shutdown must not hold the global tunnel mutex while closing the TUN FD or LWIP stack, otherwise Android service ANRs can occur.
- Closing the TUN file before LWIP teardown helps unblock the packet reader cleanly.
- Do not nil out the active `androidTunBridge` file/LWIP references before the reader goroutine exits. Signal stop, close the TUN FD, wait for `bridge.done`, and only then close LWIP, otherwise `runAndroidTunLoop` can dereference a nil `lwip` during `STOP` and abort the Android process.
- Android notification state should only move to `Stopped` from the real VPN teardown path; startup cleanup of stale tunnel state must not reuse the same stop-notification routine, or the foreground notification will briefly regress to `Stopped` during `START`.

## Packaging Pattern
- Android native runtime is packaged as `AndroidNativeLibrary`, not copied from assets at runtime.
- ABI outputs live under `InvisibleGorilla-XRay.Android/Assets/Runtime/arm64-v8a/` and `.../x86_64/`.

## Verification Pattern
- Do not trust UI state alone for Android VPN work.
- Validate with emulator/device browser IP before and after `STOP`.
- Use `diagnostic.log`, `logcat`, and screenshots together when debugging lifecycle issues.

# System Patterns

## Linux Head Composition Pattern
- The Linux project does not duplicate UI: it links the existing macOS Avalonia views and only contributes platform handlers, managers, factories, and the entry point.
- Linked `.axaml` resources must be exposed both through `<AvaloniaResource>` (so XAML loads at runtime) and through `<AdditionalFiles SourceItemGroup="AvaloniaXaml">` (so the Avalonia name source generator emits strongly-typed `x:Name` field accessors for the linked code-behind partial classes). Without the `SourceItemGroup="AvaloniaXaml"` metadata, the generator silently skips linked files and the build fails with `CS0103: Имя "<controlName>" не существует в текущем контексте`.
- Code-behind files keep their original `InvisibleGorillaXRay.Mac.Views` namespace inside the Linux assembly. This avoids forking views and keeps Linux a pure additive head.
- `MacInstalledAppDiscovery` is implemented in the Linux project but registered under the `InvisibleGorillaXRay.Mac.Services` namespace so the linked App Rules UI binds to Linux `.desktop` parsing without further abstraction.

## Linux System Integration Pattern
- Proxy: GNOME `gsettings org.gnome.system.proxy*` schema reads/writes; restored to system defaults on app exit.
- Notifications: Avalonia `TrayIcon` for persistent state, `notify-send` (libnotify) for transient toasts; both gracefully no-op when the binary is missing.
- Autostart: `~/.config/autostart/Invisible Gorilla XRay.desktop` written/removed by `LinuxStartup`.
- Deep links: `xdg-mime default` is invoked on app start to take ownership of `vless://`, `vmess://`, and `ig-xray://` schemes; a single-instance Unix domain socket forwards links from a freshly-launched secondary process to the running primary.
- TUN: privilege-elevated steps (`ip tuntap`, `ip route`, `resolvectl`) run via `pkexec` (preferred) or `sudo -n`; the unprivileged Linux app supervises the `tun2socks` child process directly.
- App rules: `LinuxAppRulesBridge` writes a JSON manifest the user can later consume from a system-side helper (cgroups + iptables); kernel-level enforcement is intentionally not part of the first port.

## Linux Build Pattern
- A single `build.sh` is the supported build path. It is idempotent, detects distro family, asks once for sudo, and prints what it is about to do before each phase.
- Build artifacts go to `publish-linux/<rid>/` (raw dotnet publish) and `dist-linux/<rid>/` (bundle with `install.sh` and `.desktop` file). Both are gitignored.
