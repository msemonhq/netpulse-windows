# NetPulse for Windows

A lightweight Windows system-tray internet traffic meter. NetPulse reads local network-interface counters; it does not run speed tests or send monitoring data to an external service.

## Version 1 features

- Updates download and upload traffic rates about once per second.
- Custom tray icon alternates between a compact download value and upload value.
- Hover over the tray icon to see both full readings.
- Right-click menu shows live rates, session totals, and network-adapter selection.
- Can optionally launch when the current Windows user signs in.
- Saves the selected adapter in the current user's local application data.
- Handles disconnected adapters and counter resets without treating them as traffic spikes.
- Includes unit tests for rate calculations and formatting.

## Requirements

- Windows 10 or later (x64).
- No administrator privileges are required for normal monitoring.

## Build and run locally

Install the .NET 10 SDK, then from the repository root run:

```powershell
dotnet test tests/NetPulse.Tests/NetPulse.Tests.csproj --configuration Release
dotnet run --project src/NetPulse/NetPulse.csproj --configuration Release
```

NetPulse runs in the notification area without a main window. Right-click its icon to choose a network adapter, enable launch-at-sign-in, review session totals, or exit.

## Download a build

The GitHub Actions workflow builds and tests the application on a Windows runner, then publishes a self-contained x64 build as a workflow artifact. Open the repository's **Actions** tab, select a successful **Windows build** run, and download the **NetPulse-win-x64** artifact.

## Measurement notes

- Rates are based on byte-counter deltas over the elapsed time between samples; a one-second timer is approximate, not a real-time guarantee.
- The default view aggregates active non-loopback, non-tunnel adapters. Some virtual networking setups may report overlapping traffic; select a single adapter if the aggregate does not match expectations.
- The tray icon alternates directions because Windows tray icons are small. The hover tooltip and context menu show download and upload rates together.
- Session totals begin when NetPulse starts and are not persisted between launches.
- The app performs no external speed test and cannot report the maximum speed your internet connection could achieve.

## Project structure

- `src/NetPulse/` — Windows Forms tray application.
- `tests/NetPulse.Tests/` — unit tests for traffic calculations and formatting.
- `.github/workflows/windows-build.yml` — automated Windows build, test, and publish workflow.
