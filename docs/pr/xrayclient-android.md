## Summary
- add an experimental `InvisibleGorilla-XRay.Android` Avalonia head and wire it into the shared `InvisibleGorilla.Core` flow
- make shared storage, settings, and diagnostic paths app-root aware so Android can use app-private storage instead of desktop-relative paths
- add `build-android.ps1` to auto-install missing Android prerequisites, prepare geo assets, build/package the Android native bridge runtime asset, and publish an APK
- update README with Android build instructions, runtime expectations, and current platform limitations

## Why
The project already had desktop clients and a reusable shared core, but no Android packaging or mobile entry point. This change establishes the Android app, APK build pipeline, and shared runtime groundwork so mobile support can evolve without duplicating config and Xray integration logic.

## Notes
- Android support is currently experimental.
- Android proxy mode currently runs a local Xray listener on `127.0.0.1:<port>`.
- Full `VpnService`-backed TUN routing still requires the mobile tunnel bridge runtime to be bundled in a follow-up step.

## Test plan
- run `.\build-android.ps1` on a clean Windows machine and verify it installs missing .NET, Android workload, JDK, SDK/NDK, and Go prerequisites
- verify the script downloads geo assets into `InvisibleGorilla-XRay.Android/Assets/Runtime`
- verify the Android native bridge runtime asset is produced and the Android project publishes an APK
- launch the APK on an arm64 device or emulator and confirm config import, config selection, settings persistence, and local proxy startup flow work
- confirm Windows and macOS projects still resolve shared settings/log/config paths correctly after the shared core changes
