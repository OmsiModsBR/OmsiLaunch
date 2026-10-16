# LaunchSpec

Status: NORMATIVE

`LaunchSpec` is portable semantic data. `UNSET` means preserve the existing
OMSI setting and must never be collapsed into `false`, zero, or an invented
default. The JSON representation is the same model used by `/spec`; explicit
CLI switches override its file values.

Supported structural groups are installation, world, entrypoint identity or
presented index, date, time, year, weather, player vehicle, configuration,
input, diagnostics and runtime behavior. `NEW_MAP`, `SAVED_SITUATION` and
`LAST_MAP_STATE` are semantic modes. `LAST_MAP_STATE` means OMSI's native
automatic last-map restoration branch; it is never inferred from `.osn` file
timestamps or directory order. Numeric native list indices are not
persistent content identities.

For `NEW_MAP`, `World.EntrypointIdentity` is reserved for a future structured
canonical identity. A raw `global.cfg` label is not sufficient because it can
be duplicated and can differ from the native presented label. Until the
profile closes that correlation, `PresentedEntrypointIndex` is the supported
diagnostic/low-level selector. Startup handoff v3 carries an identity field
without pointer-sized values, but public use remains capability-gated.

Date and time use explicit `year/month/day` and `hour/minute/second` records.
The schema does not include OS versions, Win32 handles, CLR runtime objects,
DNNE details, memory mappings or native addresses.

PlanSession is the compiler for this data. A plan is non-runnable only when a
requested required capability is unavailable. Unrequested optional capability
gates are informational.
