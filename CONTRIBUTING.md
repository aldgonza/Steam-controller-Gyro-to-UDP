# Contributing

Bug reports, controller-compatibility tests and small focused pull requests are welcome.

## Reporting a bug

Include:

- Bridge version and Windows version.
- Controller model, connection type and firmware if known.
- Whether Steam is running and whether Request raw motion is checked.
- Whether the simulated-yaw test reaches OpenTrack.
- Whether bridge yaw/pitch values change with the real controller.
- Relevant OpenTrack input/output settings and the game's camera settings.

`--probe` can help identify device interfaces. Remove personal paths and device serials from diagnostics/screenshots before posting. Never include account tokens or other credentials.

## Development

Run `build.ps1` on Windows x64. It compiles using .NET Framework and runs core tests. Run `package.ps1` to produce a release ZIP. Read the source before running scripts.

Core tests cover parsing, timestamp rollover, calibration, known rotations, clutch behavior, keyboard bindings and settings migration. Live controller/Steam interactions and game-camera behavior require manual tests. `--udp-test` checks localhost delivery separately.

The code is kept compatible with the C# compiler shipped with .NET Framework; avoid syntax/dependencies that require a modern .NET SDK unless that requirement is intentionally changed.

For new controllers, keep report parsing and capture separate from the existing OpenTrack packet format. Do not silently claim support for untested devices or connections.

## Releases

Update `VERSION` and `RELEASE_NOTES.md` when preparing a new release. A successful push to `main` builds/tests and creates a tagged prerelease if that version does not already exist. Existing releases are not overwritten. The release's tag points to the source commit used by the build.

MIT licensing applies to contributions. Preserve relevant notices and credit protocol references.
