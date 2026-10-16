# OmsiLaunch 0.1.0-beta4 Localization Manifest

The English pages under `docs/` are the canonical (`en-US`) and normative documentation of OmsiLaunch `0.1.0-beta4`. Each directory under `docs/localized/` is a complete translation of the page set below, written from the final English Beta 4 pages. Translations are not alternative specifications: where a translation disagrees with the English page, the English page and the code are authoritative.

## Locales

| Locale | Language | Root | Website route | Product Windows UI in that language |
| --- | --- | --- | --- | --- |
| `en-US` | English (United States), canonical source | `docs/` | `/en/` | yes |
| `pt-BR` | Português (Brasil) | `docs/localized/pt-BR/` | `/` | yes |
| `pt-PT` | Português (Portugal) | `docs/localized/pt-PT/` | `/pt-pt/` | no (English UI) |
| `en-GB` | English (United Kingdom) | `docs/localized/en-GB/` | `/en-gb/` | yes (English UI) |
| `fr-FR` | Français | `docs/localized/fr-FR/` | `/fr/` | yes |
| `de-DE` | Deutsch | `docs/localized/de-DE/` | `/de/` | yes |
| `es-ES` | Español (España) | `docs/localized/es-ES/` | `/es/` | no (English UI) |
| `es-LATAM` | Español (Latinoamérica) | `docs/localized/es-LATAM/` | `/es-latam/` | no (English UI) |
| `it-IT` | Italiano | `docs/localized/it-IT/` | `/it/` | no (English UI) |
| `pl-PL` | Polski | `docs/localized/pl-PL/` | `/pl/` | yes |
| `nl-NL` | Nederlands | `docs/localized/nl-NL/` | `/nl/` | no (English UI) |
| `ru-RU` | Русский | `docs/localized/ru-RU/` | `/ru/` | no (English UI) |
| `zh-CN` | 简体中文 | `docs/localized/zh-CN/` | `/zh-cn/` | no (English UI) |
| `zh-TW` | 繁體中文（台灣） | `docs/localized/zh-TW/` | `/zh-tw/` | no (English UI) |
| `ja-JP` | 日本語 | `docs/localized/ja-JP/` | `/ja/` | no (English UI) |

The locale identifier and the website route are separate: the locale (for example `ja-JP`) names the translation and its directory, while the website route (for example `/ja/`) is where the public OmsiLaunch site presents that language. Routes are recorded as `website_route` in `tools/Localization/locales.json` (`source_website_route` for the canonical `en-US` pages) for site integration; documentation paths never depend on them.

Regional variants are separate translations: `pt-PT` is not `pt-BR`, `es-LATAM` is not `es-ES`, `zh-TW` is not `zh-CN`, and `en-GB` is a British English edition, not a copy of the canonical pages. `es-LATAM` corresponds to the BCP 47 tag `es-419`.

The last column refers to the tray indicator, status window and dialogs of the product (`tools/OmsiLaunch.Cli/WindowsUiStrings.cs`), which follow the Windows display language. Where the product has no strings for a language it shows English, so translations keep the English UI strings as literals and explain them in the translated text.

## Localized page set

Every locale contains exactly these pages, with the same relative paths as under `docs/` (machine-readable list: `tools/Localization/locales.json`):

```text
README.md
getting-started/installation.md
getting-started/first-session.md
getting-started/api-quick-start.md
reference/cli.md
reference/cli-examples.md
reference/exit-codes.md
reference/local-control.md
reference/omsilaunchw.md
reference/windows-tray.md
reference/packaging.md
reference/public-api.md
reference/public-api-inventory.md
reference/launchspec.md
reference/errors.md
concepts/session-lifecycle.md
reference/session-profiles.md
concepts/transactions-and-recovery.md
concepts/permanent-plugin.md
reference/capabilities.md
reference/runtime-control.md
status/runtime-validation-status.md
reference/compatibility.md
reference/known-limitations.md
reference/hof-management.md
reference/runtime-modules.md
```

`reference/public-api-inventory.md` is generated from the generated English inventory by `python tools/Localization/l10n.py inventory <locale>`: only its fixed phrases are translated, every signature is copied.

## English-only pages

These English pages ship with the package but are not translated, and links to them from a translated page lead to the English page:

| Page | Reason |
| --- | --- |
| `DOCUMENTATION-MANIFEST.md` | Maintainer source map of the English documentation (document → authoritative code). |
| `api/local-control.md`, `concepts/omsilaunch-directory.md`, `concepts/windows-session-indicator.md`, `guides/session-profiles.md`, `reference/beta-0.1-capabilities.md` | Legacy redirect pages kept only so that old links resolve; each points to a translated page. |
| `status/release-0.1.0-beta1-notes.md`, `status/release-0.1.0-beta2-notes.md`, `windows-ui-localization.md`, `adr/*.md` | Historical records and decision records. |
| `examples/session-profiles/rmg-leste/profile.yaml` | Example asset, not documentation prose. |

## Review status

The localized documentation was produced and mechanically validated against the canonical EN-US documentation. Native-speaker editorial review was not required for the Beta 3 release and may be performed incrementally after publication.

## Translation rules

- Fenced code blocks are identical to the English page; commands, flags, identifiers, capability and operation ids, error codes, JSON and YAML keys, paths, hashes and example values are never translated.
- Headings, tables and lists keep the structure of the English page. Each translated heading is preceded by an `<a id="…">` anchor carrying the English anchor, so links such as `cli.md#dispatch-order-cliprogramrunasync` work in every language.
- Each translated page starts with a notice that links to the English page it translates.
- Product UI strings are quoted as they appear in the English UI; `pt-BR`, `de-DE`, `fr-FR` and `pl-PL` may also quote the product's own string for that language.

## Validation

The `docs.localization` gate (`tests/OmsiLaunch.DocumentationTests/LocalizationGate.cs`, run by `tools/Invoke-OfflineValidation.ps1`) checks for every locale: the complete page set and no other files; the translation notice; the English heading levels; identical code blocks; the same table rows and cells; every inline code span of the English page; the same link destinations; relative links and anchors that resolve; and that prose is not left in English. It does not compare prose. `python tools/Localization/l10n.py check|leak <locale>` applies the same checks and adds a heuristic report of untranslated or cross-regional wording.

## Packaging

`tools/New-ReleasePackage.ps1` copies this manifest and every locale listed above to `.omsilaunch/docs/localized/` in the release package, with the same structure. A listed locale that is missing or incomplete stops the packaging, so a partial locale cannot ship as a Beta 3 translation.
