# Steam controller Gyro to UDP 0.2.0 — community preview

Turn a 2026 Steam Controller's gyro into cockpit head tracking through OpenTrack. Originally built for Nuclear Option on Windows; the user reports a working live setup.

Includes gyro calibration, yaw/pitch gain and inversion, optional roll, controller clutch, configurable keyboard/controller bindings and a simulated OpenTrack connection test.

## Quick start

1. Extract the ZIP and run `SteamControllerGyroToUDP.exe`.
2. Configure OpenTrack input as UDP over network, port 4242; output as freetrack 2.0 Enhanced with TrackIR enabled.
3. Start tracking in OpenTrack, start the bridge, and keep the controller still for two seconds.
4. Enable Nuclear Option's TrackIR cockpit camera input.

The ZIP also includes the C# source and build script. No .NET SDK or NuGet packages are needed to build.

## Scope

- 2026 Steam Controller only; start with wired USB or its receiver.
- Bluetooth transport is unverified. Original 2015 Steam Controller and other brands are not implemented.
- Gyro integration can drift; use center/recalibrate as needed. No positional tracking.
- Unsigned executable. An earlier local 0.2 build received four generic VirusTotal detections; vendor confirmation of a false positive has not been obtained. Read SECURITY.md and the repository's source/build logs before deciding to run it.

This is an early prerelease, not an independently audited driver.
