# Player Vehicle

Status: IMPLEMENTATION IN PROGRESS

`PlayerVehicleSpec` uses canonical vehicle `.bus`, repaint, HOF, fleet-number,
and registration identities. Offline discovery resolves these identities
without assigning persistent OMSI list indices.

The Current profile records the static primitives needed for later native
creation (`TProgMan.MakeVehicle`, temporary vehicle list creation/copy, random
bus placement, and bus positioning). Their ABI-to-session wrapper is not yet
implemented, so each requested player-vehicle subcapability remains gated
independently. A saved situation must not be overlaid with a new player-vehicle
creation flow unless its native semantics explicitly require it.

The pinned OmsiHook invocation signature is evidence only until every required
global/list/critical-section input is reconciled to `Omsi23004_692EBFBF`.
The first targeted reconciliation found a concrete profile mismatch: upstream
`MakeVehicle` reads its `ProgMan` global at `0x00862F28`, while this profile's
existing semantic `ProgMan` global is `0x00858BDC`. Consequently no upstream
address is copied into the Current bridge. The remaining closure work is to
identify the exact Current owner/global used by the native creation path and
validate the temporary-list lifetime and post-create collection delta.
