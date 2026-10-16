# Session Profiles

> Superseded by [Session Profiles Reference](../reference/session-profiles.md).
> This page is kept only so that existing links keep resolving; it is not
> normative. (The previous body of this page was Portuguese; normative
> documentation is English only.)

Summary of the current behaviour, documented in full on the new page:

- A session profile is `<root>\.omsilaunch\session-profiles\<id>\profile.yaml`,
  schema `omsilaunch.session-profile/v1`, at most 256 KiB, no YAML anchors,
  unknown keys rejected, `id` equal to the directory name, 1 to 99 presets with
  unique `index`.
- Select it with `/predefined-profile:<id> /predefined-profile-index:<n>`; the
  `new:` block applies only in NEW_MAP mode, `compatibility.maps` is enforced
  for NEW_MAP and SAVED_SITUATION.
- Explicit CLI arguments that conflict with a profile-owned field are rejected
  with `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`; asset paths that escape the
  package, lexically or through a junction, are rejected with
  `OL_E_SESSION_PROFILE_PATH_ESCAPE`.
