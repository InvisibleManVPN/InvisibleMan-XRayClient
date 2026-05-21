## Summary
- clarify that `InvisibleGorilla-TUN` remains the Windows-only tunnel companion service
- document that Android support is being implemented inside the client repository through an Android head and `VpnService` groundwork, not by porting the Windows TUN service as-is
- align repository docs so platform responsibilities stay explicit during Android rollout

## Why
The existing `InvisibleGorilla-TUN` codebase is tightly coupled to Windows APIs, Wintun, and the desktop companion process model. Android needs a different networking model based on `VpnService`, so the cleanest path is to keep this repository Windows-focused and move mobile VPN evolution into the Android client.

## Notes
- this PR is documentation and positioning only
- no Windows TUN behavior should change
- Android VPN/TUN work should continue in `InvisibleGorilla-XRayClient`

## Test plan
- verify README or release notes clearly state that `InvisibleGorilla-TUN` is Windows-only
- verify references to Android now point to the client repository and mobile `VpnService` path
- confirm no Windows build, packaging, or runtime assets changed
