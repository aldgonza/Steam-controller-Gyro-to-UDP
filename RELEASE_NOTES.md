# Gyro to UDP — keyboard-only mapper preview

Local test build from `feature/keyboard-only-mapper`. Not merged to main and not an official release.

- Click **Keyboard shortcuts...** and then an action's current shortcut.
- Capture prompt: **Press the keybind or Escape to cancel**.
- Supports key combinations with Ctrl, Alt and Shift. Clear disables an action; Save applies and persists the dialog changes; Cancel leaves the previous bindings unchanged.
- Includes Center view, Toggle pause, Recalibrate and Hold to pause. The hold shortcut starts disabled.
- Direct controller-button bindings are removed. Use Steam Input to map a paddle to the matching keyboard key; for the hold action, keep the key pressed while holding the paddle.
- Capture and mapping use focused window keyboard messages, without a global keyboard hook or reflection.
- Existing keyboard settings remain compatible. Older controller-binding XML fields are ignored and removed when settings are saved.

Close the previous version before testing. Extract the ZIP, optionally copy your existing settings.xml beside the EXE, and run SteamControllerGyroToUDP.exe. Main and its v0.3.0 release remain unchanged until this preview is tested and merged.

No antivirus-clearance claim is made for this preview. Review SECURITY.md for the earlier scan history.
