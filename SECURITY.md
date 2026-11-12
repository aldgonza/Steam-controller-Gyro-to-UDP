# Security and antivirus detections

This is an unsigned, early Windows prototype. It was developed with AI assistance, has source available in the GitHub repository, and is distributed without a security certification or independent audit.

## Reported detections

On 12 November 2026, the user reported four VirusTotal detections for the earlier local version 0.2 executable:

| Engine | Reported label |
|---|---|
| Bkav Pro | `W32.Malware.C3CC73FC` |
| Elastic | `Malicious (high Confidence)` |
| McAfee Scanner | `Ti!89FBCA08D6A1` |
| SecureAge | `Malicious` |

SHA-256 of that local build:

```text
89fbca08d6a1e2cbb7007027822d0d1d78f87418314d7eb0ad835a42416f9209
```

[Original VirusTotal report](https://www.virustotal.com/gui/file/89fbca08d6a1e2cbb7007027822d0d1d78f87418314d7eb0ad835a42416f9209/detection)

The author considers these likely false positives. **No detecting-vendor confirmation has been obtained.** The report's sandbox behavior was not available for review in the development session. The detection count alone does not establish that a file is safe.

GitHub Actions builds are newly compiled binaries and may have different hashes from this local build. Compare a release against the checksums shipped with that release. A checksum verifies file identity, not safety.

## Intended behavior

- Open shared HID handles to compatible Valve controllers.
- Request raw IMU reports using a transient feature report when that option is enabled.
- Poll configured shortcut keys and modifier-key states with `GetAsyncKeyState`. Keys are not recorded or sent anywhere.
- Send OpenTrack pose packets to `127.0.0.1` on the configured UDP port.
- Read/write settings and diagnostic files beside the executable.

The source includes no external-network destination, downloader, process injection, startup registration, driver installation or firmware flashing. These are source-level observations, not an independent malware verdict. An unsigned binary, keyboard-state polling or device access may influence heuristic scanners; the actual detection trigger is not known.

## Verification

1. Review the source and `build.ps1`, or get help reviewing it.
2. Check the release's associated commit and Actions build log.
3. Check `SHA256SUMS.txt` and, after extraction, the executable's `EXE-SHA256.txt`.
4. Build from the reviewed source on your own machine if preferred. The .NET Framework compiler may produce a different binary hash between builds.
5. If your antivirus flags a build, keep the report and submit it to the detecting vendor for analysis. This project does not ask you to disable antivirus protection or add broad exclusions.

## Reporting a concern

For a generic detection, open an issue with the release version, SHA-256, exact vendor label and a public scan link. Remove personal device paths and usernames from diagnostics before posting.

For a suspected exploitable vulnerability, use GitHub's private vulnerability reporting if enabled. Otherwise contact the maintainer through their GitHub profile without posting exploit details publicly.
