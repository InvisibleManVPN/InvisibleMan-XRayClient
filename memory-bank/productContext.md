# Product Context

## Why This Exists
Users need a simple way to run XRay-based connections on desktop and Android with the same mental model and similar visuals.

## User Problems
- Mobile UX lagged behind desktop UX.
- Android needed full-device VPN routing instead of proxy-only behavior.
- Users need easier config import, sharing, status visibility, and app-based routing.

## Experience Goals
- Desktop-like clarity on Android.
- Fast, reliable configuration management.
- Clear connection state and traffic feedback.
- Simple per-app routing with low-friction selection flows.
# Product Context

## Why This Exists
Users need a simple Xray client that hides raw Xray-core complexity behind a familiar UI for importing configs, managing subscriptions, and routing traffic.

## User Expectations
- Desktop and Android should feel like one product family.
- Android should behave like a real VPN client, not just expose a local proxy listener.
- TUN-mode helper paths should not leave an unauthenticated localhost SOCKS endpoint available to other local processes.
- Starting and stopping a connection must be obvious, fast, and reliable.
- Configs, subscriptions, sharing, deletion, and availability checks should work from the UI without manual file management.

## Android UX Priorities
- Match the Windows/macOS visual structure as closely as practical.
- Show connection state clearly on the home screen.
- Keep important runtime details visible through in-app status text and Android notifications.
- Make link/file import easy on mobile, including clipboard-based paste flows.

## High-Value Validation
- Real browser traffic must switch to the selected config when VPN is active.
- Stopping the VPN must restore the normal network path and must not trigger ANR dialogs.

# Product Context

## Linux UX Priorities
- ALT Linux + GNOME users get the same window layout, settings flow, and app-rules editor as macOS, because the Linux head reuses the macOS XAML.
- System integration must feel native: GNOME proxy via `gsettings org.gnome.system.proxy`, autostart via `~/.config/autostart/*.desktop`, deep links via `xdg-mime`, transient notifications via `notify-send`, and a tray icon via `StatusNotifierItem` (Avalonia `TrayIcon`).
- TUN mode must work without forcing the binary itself to run as root: privilege escalation only happens when needed (`pkexec` preferred, `sudo -n` fallback) for `ip tuntap`, `ip route`, and DNS overrides.
- `build.sh` is the single command an ALT Linux maintainer needs to run; everything else (dotnet, Go, tun2socks, geo files) is fetched on demand.
