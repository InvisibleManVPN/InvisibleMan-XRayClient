# macOS Transparent Proxy Helper

This folder contains the bridge contract for the future native macOS per-app bypass layer.

## Runtime handoff

When the macOS client starts TUN mode with `AppRulesMode.BYPASS_SELECTED_APPS`, it writes the current rule set to:

- `TUN/macos-transparent-proxy-config.json`

The JSON payload contains:

- local SOCKS port exposed by XRay
- tunnel address and DNS used by the current session
- excluded bundle identifiers selected by the user

## Intended native pieces

- `TransparentProxyProvider.swift`
  - `NETransparentProxyProvider` scaffold that should proxy included app flows to the local SOCKS listener
- `TransparentProxyConfigurator.swift`
  - helper/configurator scaffold that should install or refresh the `NETransparentProxyManager` profile and translate the excluded bundle list into the effective included-app ruleset

## Current status

The .NET client now stages and clears the app-rules runtime config automatically on start/stop. The Swift sources here are scaffolding for the native Network Extension layer that must be built and signed on macOS with the proper entitlements.
