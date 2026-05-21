# Project Brief

Invisible Gorilla XRay Client is a cross-platform XRay/VPN client for Android, Windows, and macOS.

## Goals
- Provide a consistent UI and feature set across desktop and Android.
- Support importing and managing server configs and links.
- Route traffic through XRay, including full-device Android VPN traffic.
- Support per-app routing rules and templates across platforms.

## Current Priority
- Stabilize Android app rules UX and runtime behavior.
- Produce tested Android release artifacts for emulator and real devices.
# Project Brief

## Project
`InvisibleGorilla-XRayClient` is a cross-platform Xray client with Windows, macOS, and Android heads built around shared runtime/configuration logic in `InvisibleGorilla.Core`.

## Primary Goals
- Provide a desktop-grade Xray client UX with shared config import, subscription handling, testing, and connect/disconnect flows.
- Keep Windows and macOS stable while bringing Android to functional parity where practical.
- Route Android device traffic through the selected Xray config by using `Android.Net.VpnService` plus a native mobile tunnel bridge.
- Keep the local TUN bridge secure so internal helper paths do not expose an unauthenticated localhost SOCKS surface.
- Ship Android builds for both emulator and device targets, especially `x86_64` and `arm64-v8a`.

## Current Android Direction
- Android UI is implemented in `InvisibleGorilla-XRay.Android` with Avalonia.
- Android runtime uses packaged native `libXRayCore.so` libraries from `Assets/Runtime/<abi>/`.
- Device-wide routing is handled by Android `VpnService`, not by porting the Windows TUN service model directly.

## Success Criteria
- Import config or subscription from file/link.
- Select a config and start routing successfully.
- Confirm browser IP changes while VPN is running and reverts after stop.
- Build signed or unsigned APKs for the supported Android ABIs without manual asset copying hacks.

# Project Brief

## Linux Direction
- Adds `InvisibleGorilla-XRay.Linux` as the GNOME-first desktop head built on top of the same `InvisibleGorilla.Core` runtime and the existing Avalonia views from the macOS head.
- Targets ALT Linux with GNOME first, but stays distro-agnostic: build script supports apt/dnf/zypper/pacman/apt-rpm.
- Reuses Mac XAML / code-behind by linking the files into the Linux project and exposing them to the Avalonia source generator via `<AdditionalFiles SourceItemGroup="AvaloniaXaml">`.
- `./build.sh` is the single entry point for producing a runnable Linux bundle (deps + Go wrapper + tun2socks + geo files + dotnet publish + tray-ready `.desktop` install).
