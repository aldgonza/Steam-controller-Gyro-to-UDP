# Gyro to UDP 0.4.0

Keyboard-only shortcut mapping, tested successfully by the user before promotion to main.

- Click **Keyboard shortcuts...** and then an action's current shortcut.
- Capture prompt: **Press the keybind or Escape to cancel**.
- Supports key combinations with Ctrl, Alt and Shift. Clear disables an action; Save applies and persists the dialog changes; Cancel leaves the previous bindings unchanged.
- Includes Center view, Toggle pause, Recalibrate and Hold to pause. The hold shortcut starts disabled.
- Direct controller-button bindings are removed. Use Steam Input to map a paddle to the matching keyboard key; for the hold action, keep the key pressed while holding the paddle.
- Capture and mapping use focused window keyboard messages, without a global keyboard hook or reflection.
- Existing keyboard settings remain compatible. Older controller-binding XML fields are ignored and removed when settings are saved.

Punctuation shortcuts show symbols such as `-` and `Ctrl+-` instead of Windows key names. The Steam paddle hint has been removed from the shortcut dialog.

The release ZIP contains the EXE, settings example, documentation, licenses and checksum. Source code, tests and build scripts are available in the repository, rather than bundled in the ZIP.

Close the previous version before upgrading. Extract the ZIP, optionally copy your existing settings.xml beside the EXE, and run SteamControllerGyroToUDP.exe.

No antivirus-clearance claim is made for this release. Review SECURITY.md for the earlier scan history.
