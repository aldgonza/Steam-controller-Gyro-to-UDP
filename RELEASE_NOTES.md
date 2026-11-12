# Gyro to UDP 0.3.0 — simpler interface, config-based bindings

- Window title simplified to **Gyro to UDP**.
- Removed the **Controller motion → cockpit view** heading and the Nuclear Option/TrackIR reminder from the window.
- Removed the keybind editor and controller remapping dropdown.
- Configure keyboard and controller bindings in `settings.xml` with the app closed. Named shortcuts such as `F8`, `Ctrl+F9` and `Ctrl+Alt+K` are supported. Existing numeric keyboard settings still load.
- Added `settings.example.xml` and a config reference in the README. Existing `settings.xml` files are not included or overwritten by the release package.
- Removed key-capture code and its reflection-based tests. Modifier states are checked only while a configured key is held.

Defaults remain F8 (center), F9 (toggle pause), F10 (recalibrate), and R3 held (clutch).

The controller reader, orientation integration and OpenTrack UDP format are unchanged. Copy your existing `settings.xml` beside the new EXE to keep your configuration, then restart the app after any manual edits.

This release has not been cleared by antivirus vendors; removing the editor is not a guarantee that detections will disappear. See SECURITY.md for the earlier scan and intended application behavior.

## Quick start

1. Extract the ZIP and run `SteamControllerGyroToUDP.exe`.
2. Configure OpenTrack input as UDP over network on port 4242; select its appropriate game output.
3. Start OpenTrack, start the bridge, and leave the controller still for two seconds to calibrate.

Windows x64 and the **2026 Steam Controller** are supported. The original 2015 controller and other brands are not implemented. Gyro drift requires occasional recentering; no positional tracking is provided.
