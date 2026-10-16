# Platform Support

Status: NORMATIVE

## OmsiLaunch Current

Official Current support is intentionally narrow: final/latest serviced
Windows 10 x86-64 and current supported Windows 11 x86-64. The host OS and
outer Current host are AMD64/x86-64; OMSI, its in-process plugin, and native
OMSI interop remain x86.

Current does not support 32-bit Windows, ARM64, Vista, Windows 7, Windows 8,
Windows 8.1, Windows XP, Wine, Proton, Linux, or macOS. Unsupported platforms
are rejected during validation before any installation mutation.

## Runtime Dependencies

Final Current distribution should be self-contained where practical. Users do
not need Visual Studio, the .NET SDK, Git, Python, Ghidra, or CMake. Normal
operation does not require elevation when the OMSI installation is writable by
the current user.

## Legacy Families

Legacy families are separate historical/research ports, not Current support.

### Legacy NT6

Intended targets are final patched Windows Vista SP2 x64, Windows 7 SP1 x64,
Windows 8 x64, and Windows 8.1 x64.

### Legacy XP

The intended target is Windows XP SP3 x86. Windows XP x64 is not a target.

## Security Positioning

Legacy releases are for offline systems, historical compatibility, controlled
benchmarks, and reproducibility research. They are not recommended for normal
internet-connected use.
