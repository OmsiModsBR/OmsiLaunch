# World Loading

Status: NORMATIVE

`NEW_MAP` is the runtime-validated canonical path. Map and entrypoint identity
are semantic values; a presented entrypoint index is a diagnostic/low-level
override, not a persistent identity.

Startup handoff v4 can carry an entrypoint selector and canonical saved-situation
identity to the native startup boundary. The entrypoint selector reaches the native
`Tform_setpos` presentation boundary. The adapter can match one exact unique
Unicode presented-list label, then delegates selection to the existing
`Button1Click` flow. It never falls back silently to an index. The public
canonical identity remains capability-gated: the raw `global.cfg` labels are
not necessarily the presented labels and can be duplicated, so their exact
runtime correlation is required before an identity becomes launchable.

Offline discovery exposes each `[entrypoints]` record as
`<map>#entrypoint:<sha256-of-normalized-record>`. This is a stable content
identity rather than an ordinal or label, but it remains discovery-only until
the profile closes its mapping to the native presented list. The validated
Grundorf baseline records `presented index 1 -> raw index 0 -> Nordspitze
Bauernhof`; the VCL presented-text representation is still unresolved.
correlation requires a structured identity before release.

`SAVED_SITUATION` represents a selected canonical `.osn` path and uses the
native Start-form situation branch. The profile records the evidence boundary:
`Tform_start+0x3E4` is the selected-situation control, `+0x3F8` is its ordered
native collection, and dispatch is `Tform_start.LoadSelectedSituation`
(`0x0064307C`) for `Omsi23004_692EBFBF`. The Current native bridge resolves the
canonical path in that live Unicode collection but never invokes that dispatcher
directly: doing so bypasses form-owned state. It selects the OMSI-owned radio
mode, sets the profiled selector through its native VMT setter, invokes the
selection synchronization handler, then executes `Button1Click`. This is
`RUNTIME_VALIDATED`: the installed Berlin-Spandau situation
`situations\\Baustelle Falkenseer Ch..osn` reached `gameplay.entered` through
the public session path. Its native load took about 54 seconds, so the Current
default startup timeout is 180 seconds; callers may provide a tighter explicit
bound when their content is known to load faster.

Before staging any runtime artifact, `PlanSession` resolves the map declared by
the selected `.osn` against the current installation. A missing map is rejected
as `OL_E_SITUATION_MAP_NOT_FOUND`; OmsiLaunch does not allow the native loader
to present a modal missing-content error and strand the headless startup path.

`LAST_MAP_STATE` means the native automatic last-map restore behavior. It is
not the newest `.osn`, a filesystem timestamp, or a directory-order heuristic.
`laststn.osn` and `laststn.osn.owt` are evidence only until the native branch is
closed. The capability is therefore unavailable for the current profile.
