# OMSI Interop Surface

Status: NORMATIVE

`OmsiLaunch.Interop` is the x86-only OMSI ABI boundary. It reconstructs the
reusable parts of OmsiHook without importing OmsiHook's external attach or RPC
architecture into the OmsiLaunch product.

## Rules

- All OMSI pointers are explicit unsigned 32-bit addresses inside Interop.
- Public API contracts never expose pointers, handles, DNNE types, or managed
  OMSI object wrappers.
- Reading Delphi strings and arrays is generic; writing or allocation requires
  an explicit profile-gated native allocator.
- A domain wrapper becomes executable only when the active `OmsiBuildProfile`
  provides the exact address/offset plus native byte guards.
- Generic Delphi collection replacement is forbidden. Native lifecycle
  operations own mutations to OMSI collections.

## Reconstructed Foundation

| Capability | State | Implementation |
| --- | --- | --- |
| Fixed-width x86 remote addresses | IMPLEMENTED | `OmsiRemoteAddress` |
| Scalar memory read/write | IMPLEMENTED | `OmsiMemoryPrimitives` |
| Delphi UnicodeString read | IMPLEMENTED | `ReadStringAsync` |
| Delphi ANSI string read | IMPLEMENTED | `ReadStringAsync` |
| Delphi string/pointer/struct arrays | IMPLEMENTED | `OmsiDelphiValues` |
| Remote string/array allocation | IMPLEMENTED, profile-native allocator required | `IOmsiRemoteAllocator` |
| Read-only object collection snapshots | IMPLEMENTED | `OmsiObjectCollection<T>` |
| `TList`/`OList` pointer collection snapshots | IMPLEMENTED | `OmsiPointerList<T>` with profile-supplied layout |
| `TList`/`OList` Delphi string snapshots | IMPLEMENTED | `OmsiStringList` with profile-supplied layout and encoding |
| Profile-validated object field access | IMPLEMENTED | `OmsiProfiledObject` validates VMT before each field access |
| Struct reflection marshalling | DEFERRED | Requires confirmed packing/layout and a concrete consumer |
| Process attach / external RPC | EXCLUDED | OmsiLaunch owns process lifecycle and startup handoff |

`OmsiRuntimeSurface` lists the semantic operations inherited from OmsiHook's
useful domains. A catalog entry is not a claim that the current profile permits
the operation. Availability is resolved by `OmsiBuildProfile` and native guards;
`OmsiRuntimeSurface.Resolve` makes that decision explicit for a profile adapter.
