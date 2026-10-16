# Content Discovery

Status: NORMATIVE

Discovery is offline and read-only. Maps use `maps\...\global.cfg`; situations
use `situations\...\.osn`; vehicles use OMSI-relative `.bus` files; repaints are
vehicle-scoped CTI identities. HOF, fleet-number, registration, and add-on
results report only evidence available in installed content. Discovery order or
file timestamps never define runtime saved-state semantics.
## Entrypoints

`EnumerateEntrypoints(mapIdentity)` parses the profile-observed `[entrypoints]`
records in `global.cfg`. Its canonical identity is the map identity plus an
SHA-256 of the complete normalized raw record. Labels are display metadata
only, because OMSI maps can contain duplicate labels. Discovery does not claim
that this raw record has been correlated to the runtime `Tform_setpos`
presented list.
