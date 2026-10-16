# Post-Release Backlog

Status: NORMATIVE

The first Current release must not expose unsafe generic pointer/list mutation.
The following work is intentionally post-release unless an exact profile-bound
semantic operation closes before the release gate:

- A distinct D3D9 device-loss transition. Device status, texture
  create/update/query/release and device Reset (resetting/restored, generation
  advance, stale-handle rejection, a request during Reset) are runtime-proven
  (runtime closure `D01`); OMSI went straight to `DEVICENOTRESET`, so `lost`
  was not produced.
- Cross-tile/spatial/ODE vehicle rebind. Local transforms are not a correct
  teleport or an authority-replication primitive.
- High-frequency authoritative vehicle puppet control.
- Generic pointer/list writes and low-level ODE APIs.
- OmsiHook RPC compatibility mode.
- Legacy NT6 and XP ports, after `legacy-port-base` is frozen.

Items recorded by the beta3 hardening round:

- Cooperative OMSI shutdown (`WM_CLOSE` with a forced-termination fallback).
  Today every stop path calls `TerminateProcess` so that OMSI cannot rewrite
  restored files; switching to a cooperative close is a product decision that
  requires runtime validation of the restore behaviour afterwards.
- Cross-logon installation lease. The named semaphore
  `Local\OmsiLaunch.Installation.<sha256(root)>` is per logon session; a
  second logon can start a competing owner. Accepted risk for beta3.
- Per-instance unique ids for handle fingerprints. The current fingerprint
  (VMT plus definition/model index) cannot distinguish an object of the same
  class and model recreated at the same address between two list reads.
- Native-speaker editorial review of the localized documentation. The
  `0.1.0-beta3` translations under `docs/localized/` were produced and
  mechanically validated against the canonical English pages; editorial review
  was not required for Beta 3 and may be done incrementally after publication.
