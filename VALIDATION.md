# Validation — 12 November 2026

## Keyboard-only development preview

- Separate development branch; no merge or main-branch release.
- Tests cover focused key capture, Escape cancellation, modifiers, Enter/Tab/Backspace capture, duplicate bindings, independent dialog working copies, keyboard clutch, settings round trips and migration of older XML.
- Verified legacy direct controller mappings are not serialized and raw controller buttons no longer trigger pause/center/calibration.
- No reflection-based keyboard tests or global keyboard hooks are used by the capture dialog.
- Existing motion/packet, calibration and timestamp tests remain in place.
- Live typing and Steam paddle-to-key behavior still require user testing.

## Version 0.3

- Removed the keybind dialog, key-capture control, mapping dropdown and their reflection-based tests.
- Window title is Gyro to UDP; removed the in-window heading and Nuclear Option message.
- Tests cover named key parsing, modifier combinations, disabled and invalid shortcuts, controller config indices, settings persistence, missing fields and migration from numeric 0.2 settings.
- Motion/packet, calibration, timestamp rollover and controller action tests remain in place.
- Keyboard polling and the .NET runtime remain; no antivirus-clearance claim is made for this release.

## Version 0.2

- Keyboard binding capture, exact modifier matching, duplicate detection, and cleared bindings tested.
- Controller toggle edge detection and suppression while editing bindings tested.
- Settings serialization and migration from 0.1 tested.
- Main window and keybind dialog rendered and inspected.
- User reports version 0.1 worked successfully with the live controller/OpenTrack setup. The new keybinds still need a live controller check; motion decoding and UDP transmission are unchanged.

## Passed

- Built a Windows x64 WinForms EXE with the .NET Framework C# compiler.
- Parsed synthetic reports for IDs 0x42, 0x45 and 0x47 against SDL's documented packed structures.
- Checked accelerometer/gyro scaling and SDL-style axis remapping.
- Rejected short and non-state reports.
- Tested 32-bit and 16-bit timestamp rollover and duplicate suppression.
- Tested stationary calibration, bias subtraction, known yaw/pitch/roll rotations, recenter, both pause paths, and rejection of long time gaps.
- Verified OpenTrack packet size (48 bytes), double ordering, and non-finite-value rejection.
- Instantiated the WinForms interface successfully for a UI smoke check.

## Not yet passed / requires a live PC test

- Local UDP loopback timed out in the restricted execution session. The explicit `--udp-test` remains a failing test in that session; core tests do not claim to verify network delivery.
- No Valve HID interfaces were visible to the probe in that session. Live shared access with Steam, sensor activation, button masks, axis signs and calibration on the user's controller remain unverified.
- No live OpenTrack preview or Nuclear Option camera test has been performed.
- Bluetooth transport, UI display/layout at different DPI scales, and long-session drift remain unverified.

Use the app's five-second simulated-yaw test first, then test live controller data in OpenTrack, then test cockpit movement in Nuclear Option. If the simulated test succeeds but live angles do not change, investigate the HID/Steam capture side rather than the game's TrackIR settings.
