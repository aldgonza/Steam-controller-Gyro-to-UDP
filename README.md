# Gyro to UDP

Use a **2026 Steam Controller's gyro as cockpit head tracking** through [OpenTrack](https://github.com/opentrack/opentrack). Turn and tilt the controller to look around while keeping your regular flight controls in Steam Input.

**Controller > this bridge > localhost UDP > OpenTrack > game's head-tracking camera**

## Download

Get the Windows x64 ZIP from [Releases](https://github.com/aldgonza/Steam-controller-Gyro-to-UDP/releases). Extract it into a writable folder and run `SteamControllerGyroToUDP.exe`. Keep `settings.xml` when upgrading to preserve your settings.

The release ZIP contains the application, settings example, documentation, licenses and executable checksum. Source code, tests and build scripts are available by cloning the repository; they are not included in the release ZIP.

The executable is unsigned. Some antivirus engines flagged the earlier local 0.2 build with generic detections. **See [SECURITY.md](SECURITY.md)** for the report, known hash, intended behavior and verification options. Vendor confirmation of a false positive has not been obtained.

## Supported hardware

| Device / connection | Status |
|---|---|
| Steam Controller **2026**, USB or receiver | Implemented; user reports working |
| Steam Controller 2026, Bluetooth | Report parser present; transport unverified |
| Original Steam Controller (2015) | Not implemented |
| DualSense, DualShock 4, Switch Pro, other gyro pads | Not implemented |

Requires Windows x64, .NET Framework 4.x (4.8 recommended), OpenTrack and a game with compatible head-tracking support. This bridge recognizes Valve device IDs `28DE:1302` through `28DE:1305`.

## Setup

### 1. Configure OpenTrack

1. Install [OpenTrack](https://github.com/opentrack/opentrack/releases).
2. Select **Input > UDP over network**, port **4242**.
3. Select **Output > freetrack 2.0 Enhanced** and enable its TrackIR interface. If available, choose **Use TrackIR, disable FreeTrack**.
4. Start with linear 1:1 yaw/pitch mapping and no filter. Disable positional X/Y/Z mapping; leave roll disabled initially.
5. Click **Start** in OpenTrack.

### 2. Test the connection

1. Open the bridge and click **Test OpenTrack with 5 seconds of simulated yaw**.
2. Check that OpenTrack's preview turns left/right. This tests UDP output independently of real sensor movement, although it also starts the bridge's controller reader.
3. Click **Start bridge** if stopped. Leave the controller still on a surface for two seconds until calibration finishes.
4. Turn it left/right and tilt it up/down. Check the bridge's angle display and OpenTrack's preview.

Use **Invert yaw / Invert pitch** if needed. Start with gains at 1.0; increase either the bridge's gain or OpenTrack's mapping curve to taste.

The bridge uses the game's existing head-tracking interface.
## Controls and keybinds

| Default | Action |
|---|---|
| **F8** | Center the view at the current controller orientation |
| **F9** | Toggle pause; motion made while paused is discarded |
| **F10** | Recalibrate sensor bias and center |
| **Hold to pause: None** | Assign a keyboard key to use as a clutch |

Click **Keyboard shortcuts...**, then click the shortcut beside the action you want to change. The capture prompt says **Press the keybind or Escape to cancel**. Press a key, optionally with Ctrl, Alt and/or Shift. Escape cancels just that capture and keeps the previous shortcut. Clear disables an action; Defaults restores F8/F9/F10 with no hold shortcut. Click Save to apply all changes and save them to `settings.xml`; Cancel discards the dialog's changes.

While the mapper is open, camera motion is held and action shortcuts are suspended so the key you are assigning cannot trigger an action. Capture uses keyboard messages delivered to its focused window, without a keyboard hook or reflection. Duplicate shortcuts are rejected. Escape is reserved for cancellation, so it cannot be assigned.

There are **no direct controller-button bindings**, including in the config. To use a Steam Controller paddle, assign it to your chosen keyboard shortcut in Steam Input. For example, bind Hold to pause to F7 here, then map a paddle to F7 in Steam. Keep the key held for as long as the paddle is held; use a normal press rather than a toggle/turbo binding.

You can also edit **`settings.xml` beside the EXE** while the app is closed, then restart. Run and close the app once to create the file, or copy `settings.example.xml` to `settings.xml` while the app is closed.

For example, inside the existing `<Settings>` element:

```xml
<CenterKey>F8</CenterKey>
<PauseKey>Ctrl+F9</PauseKey>
<CalibrateKey>F10</CalibrateKey>
<HoldPauseKey>F7</HoldPauseKey>
```

`CenterKey`, `PauseKey`, `CalibrateKey` and `HoldPauseKey` accept Windows key names such as `F8`, `F13` or `K`, optionally combined with `Ctrl`, `Alt` and/or `Shift` (for example `Ctrl+Alt+K`). Use `None` or `0` to disable a shortcut. Numeric keyboard values from previous versions remain supported. Old controller-binding XML fields are ignored and disappear when settings are saved. Invalid shortcuts are disabled; the window shows a notice when they are loaded. Malformed XML falls back to defaults and is preserved rather than overwritten.

Keyboard shortcuts require the exact modifiers: Ctrl+F8 does not also trigger an F8 binding. The app polls the configured shortcuts while open; avoid conflicts with the game and OpenTrack. No keystrokes are recorded.

## Steam Input and raw motion

Keep Steam running for your normal game controls. The bridge uses shared HID handles alongside Steam.

**Request raw motion** sends a transient IMU-enable command every 250 ms while the device is open. It does not change mappings, disable lizard mode, flash firmware or install a driver. Steam may contend with that setting on some firmware/configurations. If normal controls misbehave, stop the bridge, uncheck this option, reconnect the controller, and test with a Steam layout that keeps the sensor awake. Avoid also mapping gyro to the game's mouse/camera, which can duplicate movement.

## Limitations

- Orientation comes from bias-calibrated gyro integration with quaternions. The accelerometer checks calibration stability; it does not correct orientation continuously.
- There is no compass heading or positional tracking. Yaw, pitch and roll can drift; recenter or recalibrate when needed.
- Output is approximately 60 packets/s, driven by the UI timer. Device timestamps govern motion integration.
- This is a small prototype, not an established device driver. Broader firmware, Bluetooth and game compatibility need testing.
- Stopping the bridge leaves OpenTrack holding its last pose. Stop/center OpenTrack to reset the game view.

## Build from source

Clone/download the source and run in PowerShell:

```powershell
.\build.ps1
```

This uses Windows' .NET Framework C# compiler and runs the core tests. No .NET SDK, NuGet package, downloaded library or installation step is required. The executable appears beside the source files. The compiler must exist at `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.

To build a distributable ZIP and SHA-256 checksums:

```powershell
.\package.ps1
```

The GitHub Actions workflow builds/tests the source and uploads build artifacts. The first successful build of a new `VERSION` on `main` also creates a tagged **prerelease** with the ZIP and checksum file. Existing releases are left untouched.

## Troubleshooting

| Symptom | Check |
|---|---|
| Simulated yaw does not reach OpenTrack | Start OpenTrack with UDP input on the same port; check local firewall rules |
| Simulated yaw works, real angles do not change | Check controller connection, raw-motion setting and Steam's sensor configuration |
| Calibration keeps restarting | Place the controller on a still surface for at least two seconds |
| Camera moves twice | Remove duplicate Steam gyro-to-camera/mouse output |
| OpenTrack preview works, game view does not | Enable the game's TrackIR input and use cockpit view; restart with tracking running |

Diagnostic commands write results beside the EXE:

```powershell
.\SteamControllerGyroToUDP.exe --probe
.\SteamControllerGyroToUDP.exe --self-test
.\SteamControllerGyroToUDP.exe --udp-test
```

`--probe` enumerates Valve HID interfaces without sending commands. `--self-test` checks packet decoding, orientation, calibration and keybind behavior. `--udp-test` separately checks local UDP delivery. Wait for the process to exit before reading `diagnostics.txt`, `self-test.txt` or `udp-test.txt`.

## Contributing and credits

See [CONTRIBUTING.md](CONTRIBUTING.md) for bug reports and development. Controller support contributions are welcome.

- [SDL Steam Triton driver](https://github.com/libsdl-org/SDL/blob/main/src/joystick/hidapi/SDL_hidapi_steam_triton.c) and [wire structures](https://github.com/libsdl-org/SDL/blob/main/src/joystick/hidapi/steam/controller_structs.h): motion units, report layouts and coordinate conventions.
- [SteamController2GyroDSU-Win](https://github.com/Visualtaggy/SteamController2GyroDSU-Win): IMU command format and button-mask cross-checks.
- [OpenTrack's UDP receiver](https://github.com/opentrack/opentrack/blob/master/tracker-udp/ftnoir_tracker_udp.cpp): packet format, six little-endian doubles ordered X/Y/Z/yaw/pitch/roll.

The C# implementation is new. No SDL or OpenTrack binary is bundled. This project is unofficial and is not affiliated with Valve, OpenTrack or Shockfront Studios.

MIT license. See [LICENSE.txt](LICENSE.txt) and [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).
