# `.omsilaunch` Installation State Directory

> Superseded by [Packaging / Installation Layout](../reference/packaging.md)
> and [Transactions and Recovery](transactions-and-recovery.md).
> This page is kept only so that existing links keep resolving; it is not
> normative.

Summary of the current behaviour, documented in full on the new pages:

- `.omsilaunch\` is OmsiLaunch-private state below the OMSI root: `assets\splash`
  (persistent), `diagnostics\` (50 newest sessions retained), `journal.json`
  and `backup\<sessionId>\` (transaction state, removed after restore),
  `session-profiles\<id>\` (user content, never removed).
- The permanent plugin closure lives under `plugins\OmsiLaunch.*` and is
  validated against `release-manifest.json`; it is never staged, snapshotted or
  restored by a session.
- Splash mode `Managed` overlays `GUI\NewSplashscreen_*.bmp` transactionally;
  `Native` / `Unset` preserves OMSI files.
