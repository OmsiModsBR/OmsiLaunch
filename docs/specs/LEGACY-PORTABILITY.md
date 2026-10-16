# Legacy Portability

Status: NORMATIVE
Implementation status: FUTURE

Legacy portability is a design constraint, not a Current implementation target.

## Objective

After Current is stable, and before Current leaves its initial .NET baseline,
freeze a functional `legacy-port-base` for two explicit backports: Legacy NT6
and Legacy XP.

## Principle

Current is not made old to support old Windows. LaunchSpec semantics, public
results/errors, BuildProfile data, content identities, configuration semantics,
startup handoff wire protocol, and native operation identities remain portable;
platform implementations vary underneath.

`OmsiBuildProfile` is independent from `RuntimePlatform`. DNNE is a Current
plugin-host adapter, not a protocol requirement.

## Legacy Targets

Legacy NT6 targets Vista SP2 x64, Windows 7 SP1 x64, Windows 8 x64, and
Windows 8.1 x64. Legacy XP targets Windows XP SP3 x86 only. The actual legacy
toolchains are selected in their dedicated port milestones.

## Wire Compatibility

Startup/session wire structures are versioned; use fixed-width fields, UTF-8
strings with explicit lengths, and documented packing/alignment/endian rules.
They never use CLR object serialization or pointer-size-dependent layout.

## Reproducibility

Where OMSI build/content support it, the same semantic LaunchSpec should be
replayable across Current and Legacy platform implementations for benchmark and
optimization research.

## Development Sequence

1. Complete and stabilize Current.
2. Freeze/tag `legacy-port-base`.
3. Create `legacy/nt6` and `legacy/xp`.
4. Perform explicit backports and preserve those branches.
5. Modernize Current only after that baseline exists.

Legacy implementation does not begin during the initial Current build.
