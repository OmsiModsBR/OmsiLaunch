# Configuration

Status: NORMATIVE

All LaunchSpec configuration overrides are session-scoped. OmsiLaunch snapshots
each touched `options.cfg`, `Inputs\keyboard.cfg`, or `Inputs\gamectrler.cfg`
file before applying an overlay, records it in the durable transaction journal,
and restores the original bytes and SHA-256 after normal exit, `StopSession`,
startup failure, or stale-journal recovery. There is no permanent-edit API.

`UNSET` is preserve: it produces no mutation. Unknown tokens, ordering,
encoding, newlines, vector tails, and unrelated values survive lossless patches.

## Implemented Options Surface

`ConfigurationCatalog` currently supports proven session overlays for general,
view, controls, collision, ticket, autosave, graphics distance/complexity,
stencil/rain reflection, traffic, sound, and the four-value `smokesystems`
block. Public negative semantics are inverted at the codec boundary; for
example `simulation.collisionTerrain=false` writes `no_collision_terrain`.

`advanced.reducedMultithreading` is one semantic setting and synchronizes both
native reduced-multithreading flags. `AIMaxCountRandom` patches only the road
traffic or human component and preserves the other seven vector values.

`graphics.realTimeReflections` currently accepts only `economy` and `full`.
The native representation of the UI's Disabled selection remains deliberately
non-writable until static evidence closes it. `graphics.texture` and
`graphics.textureFilter` are known but non-writable for the same reason.

`graphics.particles` is a compound value:
`enabled,maxPerEmitter,playerVehicleOnly,inReflections`. It translates the
native fourth field (`disableInReflections`) without exposing that negative name.
