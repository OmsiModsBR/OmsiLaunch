# Build Profiles

Status: NORMATIVE

`OmsiBuildProfile` is independent of `RuntimePlatform`. It never encodes host
Windows-generation compatibility.

Beta 4 compatibility boundary: **OMSI 2 installed** (a 32-bit `Omsi.exe` with an
OMSI 2 version and the `maps\` and `Vehicles\` directories). No executable
hash, size or exact version is an acceptance criterion; they are diagnostics.

Every native assumption (function, patch site, class VMT, VMT slot, global,
field offset, object layout) is declared once, in the native target catalog of
the build pack (`src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`), with
its reference value, its guard and its build-independent resolution hints.
Guards and code signatures are derived from the reference executable by
`tools/OmsiLaunch.SignatureTool` into `NativeTargetCatalog.Signatures.g.cs`.
The native boundary contains no address: the plugin resolves and verifies the
catalog in process (`OmsiLaunch.NativeResolution`) before any hook and passes
the verified table to `OmsiLaunch.Native.x86.dll`.

- A known code identity (SHA-256 of the main code section; `Omsi23004`:
  `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`) selects the
  reference values as a fast path; each value still passes its guard.
- Otherwise each target is resolved by Delphi RTTI (classes, published methods,
  fields and properties), VMT slot value, or a masked code signature that must
  match exactly once. There is no fallback to a fixed address.
- A capability is available only when all its targets are verified; an
  unresolved target disables only the capabilities that need it.
- Object layouts read by the runtime readers have no structural validator yet
  and are verified only for a known code identity.

Recording a symbol is not an authorization to call it: each native ABI wrapper
requires its own guard and proven calling contract. The reference values below
are evidence for the `Omsi23004` layout.

The Current map global is `0x00861588`. The former candidate at `0x00859D94`
is not an `OmsiMap` object for this profile and is rejected as a map binding.
The profile also catalogs the internal AI configuration slot, raw-entrypoint
diagnostic index slot, and internet-texture native routine used by existing
native code. These remain profile-scoped implementation details, not public
pointer or address APIs.

The profile also records the Current D3D candidate slot `0x008627D0` as
`D3DDevice`. Runtime session `3cbd7bb6-d73f-4853-adc8-1213200527d2` proved
that the borrowed slot candidate supports `QueryInterface<IDirect3DDevice9>`,
`TestCooperativeLevel` returns `S_OK`, and all D3D operations execute on the
OMSI primary UI/main/render-owner thread. OmsiLaunch retains one QI reference;
the borrowed slot itself is never released. This entry is not generalized to
another executable fingerprint.

The matching static caller for `TProgMan.MakeVehicle` references candidate
globals at `0x00859DEC`, `0x008591DC`, and `0x00858D28`. Their exact list/index
roles remain unresolved, so they are deliberately not named BuildProfile
symbols. They are not public pointers and do not constitute a completed
player-vehicle implementation.
