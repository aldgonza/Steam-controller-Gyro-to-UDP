# Nuclear Option cockpit camera step test 0.1.0

Experimental offline camera-hook proof. This is not Gyro4All or the gyro-to-UDP app, and it does not read a controller. It uses BepInEx 5.4.23.6 and Harmony to patch the installed Mono game's camera method in memory. Original game assemblies are not rewritten.

## Install and test

1. Close Nuclear Option, the gyro bridge and OpenTrack. Save your game before testing.
2. Install the official BepInEx 5.4.23.6 Windows x64 loader from https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.6/BepInEx_win_x64_5.4.23.6.zip . Extract it into the Nuclear Option folder beside `NuclearOption.exe`. Its `winhttp.dll`, `doorstop_config.ini` and `BepInEx` folder must be beside the EXE. If those already exist, review the existing loader setup before copying; do not overwrite another installation. The camera-test ZIP does not bundle the loader.
3. Extract the test ZIP and copy the contents of `game-files` into the same game folder. This adds `BepInEx/plugins/NuclearOption.CameraStepTest/NuclearOption.CameraStepTest.dll`.
4. Launch normally through Steam and use a local single-player cockpit session.
5. Disable the game's TrackIR/head-tracking input, leave target padlock off, close menus and face approximately forward.
6. Look for the camera-test status box at the top left. It should say READY. Press F6 once: the cockpit view should turn 10 degrees right immediately and remain there. Repeated presses add another 10 degrees, within the normal look limit.
7. Move the aircraft and try normal look input. Verify aircraft-relative rotation, camera shake and look input still behave normally.
8. Press F7: restore the pan/tilt captured before the first test step and return to normal game look smoothing. Switching out of the cockpit also clears the active test.

Expected log: `BepInEx/LogOutput.log` contains `Camera hook installed`, then `STEP: pan ... -> ...` and `final pan=...`. A successful build/static check does not establish that the live hook works. If the loader or patch fails, send this log. No changes to antivirus exclusions are required by this package; investigate a block before running it.

## What changes during the test

The patch inserts one callback before the shared cockpit pan clamp in `CameraCockpitState.UpdateState`, after ordinary look input and padlock processing. It changes the controlling `panView` field, not a temporary view matrix. Two `viewSmoothing` reads are intercepted only while the test is active and the cockpit is usable. This bypass affects cockpit pan/tilt interpolation, including ordinary look input during the test; it does not alter the saved game smoothing setting, aircraft physics, FOV interpolation or camera position/shake.

To compare with the game's smoothing, close the game after its first run, set `BypassViewSmoothing = false` in `BepInEx/config/aldgonza.nuclearoption.camera-step-test.cfg`, then restart. Default is true.

The hook checks the expected camera-method layout and refuses an unexpected layout. Menus, TrackIR, target padlock, lack of focus and disabled flight controls block applying steps. No gyro, UDP, downloads, keyboard hook or process injection helper is included in the plugin. BepInEx loads it inside the game.

## Remove

Close the game. If this package installed the only BepInEx loader, remove the added `winhttp.dll` and `doorstop_config.ini` to prevent it loading. Remove the added `BepInEx` folder and `.doorstop_version` if present. Never delete an existing loader or other mods. If BepInEx was already installed, remove only `BepInEx/plugins/NuclearOption.CameraStepTest/NuclearOption.CameraStepTest.dll` and its config instead. No original game-file restoration is needed.

## Source and validation

Source is separate from the runtime ZIP. `build.ps1` compiles against Unity libraries from the local installation, without embedding game code. Place the official BepInEx 5.4.23.6 Windows x64 release under `dependencies/BepInEx` before building. `validate.ps1` reads IL metadata using BepInEx's bundled Mono.Cecil; it checks the installed method layout and compiled plugin. It does not launch or change the game.

References: https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.6 and https://github.com/gfgerard/Gyro4All/blob/main/src/core/engine.hpp . The intended next stage is integrating GyroLib after the fixed-angle hook passes live testing. This prototype has no Gyro4All integration yet.
