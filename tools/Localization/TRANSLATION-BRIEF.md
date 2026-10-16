# OmsiLaunch Beta 4 documentation translation brief

This is the brief every translation of the 0.1.0-beta4 round followed. The locale glossaries it refers to are kept in `tools/Localization/glossary/<locale>.md`.

Run commands from the repository root.

You translate canonical English documentation pages of OmsiLaunch `0.1.0-beta4` into ONE locale. The English page under `docs/<page>` is the only source of truth. Write the translation to `docs/localized/<locale>/<page>` (same relative path). Read the whole English page before you start and translate it completely: this is a full, faithful, professional translation, not a summary and not an adaptation.

Old translations of an earlier beta (the `0.1.0-beta2` trees, available in the repository history) are outdated and technically wrong in places. They may be consulted ONLY for established terminology; never copy sentences or facts from them.

Your locale glossary is `tools/Localization/glossary/<locale>.md`. Use its terms consistently; other translators work on the other pages of your locale with the same glossary.

## What to translate

All human prose: titles, headings, paragraphs, list items, table header cells and prose table cells, notes, warnings, blockquotes, link texts, explanations of examples, error explanations, capability descriptions, lifecycle and packaging explanations, known limitations, UI descriptions. The result must read as documentation originally written for developers and users of the locale. Keep established English technical terms where the local technical community normally does (for example runtime, API, CLI, handle, hash, plugin, thread, build, timeout, pipe, overlay, snapshot) — follow the glossary.

## What must stay exactly as in English (mechanically checked)

1. **Fenced code blocks**: copy every ``` block byte for byte, including its language tag and any comments inside it. Do not translate, reformat or re-indent code.
2. **Inline code spans**: every `` `...` `` span of the English page must appear unchanged in your page (executables, flags, commands, routes, capability and operation ids, error codes, JSON/YAML keys, property/type/method names, enum members, file names, paths, environment variables, hashes, session ids, values, UI literals). Never translate text inside backticks. Never remove a span. Do not wrap translated words in backticks.
3. **Markdown structure**: the same headings in the same order with the same levels (`#`, `##`, …); the same tables with the same number of rows and the same number of columns per row; the same lists. Do not add, remove, merge or split sections, rows, notes or paragraphs. A heading that is only code (for example ``### `PlanSessionAsync` ``) stays identical. Keep `|` table syntax intact; if a translated cell needs a literal pipe character, do not add one.
4. **Links**: keep every link target (the part in parentheses) exactly as in the English page, including `#anchors`; translate only the link text. Do not add or drop links. A post-processing step rewrites targets that leave the translated page set and inserts anchors, so do not "fix" targets yourself.
5. **Literal table values**: a table cell whose English value is an enum or boolean literal written without backticks (`Read`, `Write`, `Action`, `Event`, `true`, `false`) stays exactly as in English; translate only the column header. The identical-lines check ignores such rows.
6. **Literal tokens outside backticks**: classification and status words (`STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL`, `UNAVAILABLE`, `RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `RUNTIME_PASS`, `PublicStableBeta`, `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`, `WRITTEN_THIS_ROUND` and similar all-caps tokens), evidence ids (`T01`, `CAM01`, `D01`, `RV-002`, `RA-019`, `BUG-05`, `DOC-01`, `S-08` …), version numbers, hashes, session ids, numbers and units (`64 KiB`, `250 ms`, `2 s`) stay unchanged even where they are not in backticks.
7. Do not add the translation banner, notes of your own, translator comments or front matter. Do not change the English page or any other file outside your assigned pages.

## Meaning that must survive translation exactly

Translate faithfully; do not add, remove, soften or strengthen any claim, qualifier, number or status. In particular keep precisely:

- **Bounded runtime lists (BUG-05)**: when a bounded list result does not fit the 64 KiB runtime slot, the runtime returns the largest prefix that fits, `returned_count` reports how many rows were returned and `truncated=true` says rows were left out. This is a successful reply, not a failure. `OL_E_RUNTIME_RESPONSE_TOO_LARGE` remains only for results that are not bounded lists, exactly as the English page says.
- **DOC-01**: OmsiLaunch restores the files it owns in the session transaction; it does not promise to revert OMSI's own normal writes (for example `[last_map]` in `options.cfg`, caches, logs).
- **Open items**: Steam LAA on a genuine Steam installation is not runtime-validated (`PARTIAL`, `pending_beta_field_validation`) — not "unsupported", not "broken". RV-002 (RoadVehicle/Human stale handles after natural removal) has no safe runtime producer and is covered offline — not "fixed", not "a defect".
- **Self-reported evidence lag**: the registry/`GetCapabilitiesAsync` strings for `camera.lock` and `runtime.d3d.lifecycle.reset` are older than the runtime evidence; the documentation explains the mismatch and does not change the product strings.
- **OmsiLaunchW (BUG-03, BUG-06)**: `/silent` delegation through `ShellExecute` without handle inheritance; a non-runnable launch shows a dialog with the error code and exits `1` without starting OMSI.
- `UNAVAILABLE`, `PARTIAL`, `EXPERIMENTAL`, "not runtime validated", "offline only", "not implemented" and similar qualifiers must be translated with the same strength.

## Windows tray and UI strings

The product's Windows UI (tray tooltip, menu, status window, confirmation dialog) is localized by the product itself only for English, Brazilian Portuguese (`pt-BR`), German (`de`), French (`fr`) and Polish (`pl`); every other Windows language (including `pt-PT`) shows the English strings. The English UI strings appear in the pages as inline code (for example `End session`, `Session is running`); they are UI literals and stay unchanged (rule 2). Explain their meaning in your language in the surrounding prose.

- Locales `pt-BR`, `de-DE`, `fr-FR`, `pl-PL`: where helpful you may add, right after an English UI literal, the product's own string for your language in parentheses and backticks, taken exactly from `tools/OmsiLaunch.Cli/WindowsUiStrings.cs` (never invent a UI translation).
- All other locales: never present a translated label as if it were text shown by the product.

## Regional style

- `pt-BR`: natural Brazilian technical Portuguese (arquivo, usuário, tela, registro, salvar, padrão, "você" or impersonal).
- `pt-PT`: genuine European Portuguese (ficheiro, utilizador, ecrã, registo, guardar, predefinição, "está a executar", impersonal constructions or "o utilizador"); never Brazilian forms (você, arquivo, usuário, tela, gerenciar, registro).
- `en-GB`: British English spelling and usage (behaviour, colour, catalogue, initialise, normalise, recognise, organise, analyse, finalise, cancelled, licence as a noun, "per cent" only in prose); technical identifiers unchanged. It is a real edition, but do not change meaning or structure.
- `fr-FR`: professional French technical documentation, "vous", French typography (non-breaking space before `:` `;` `?` `!` is optional but be consistent).
- `de-DE`: professional German technical documentation, "Sie", established German IT terms.
- `es-ES`: Spanish as used in Spain ("tú" or impersonal; ordenador, fichero/archivo as glossary says).
- `es-LATAM`: neutral Latin-American technical Spanish; never vosotros forms, ordenador, fichero, coger, vale.
- `it-IT`: professional Italian technical documentation.
- `pl-PL`: professional Polish technical documentation (impersonal forms preferred).
- `nl-NL`: professional Dutch technical documentation ("je"/"u" per glossary, consistently).
- `ru-RU`: professional Russian technical documentation (impersonal forms, "вы" when needed).
- `zh-CN`: natural Simplified Chinese technical documentation (文件, 进程, 线程, 默认, 设置, 用户, 程序集). Use full-width Chinese punctuation in Chinese sentences.
- `zh-TW`: natural Traditional Chinese for Taiwan, written with Taiwan terminology (檔案, 處理程序, 執行緒, 預設, 設定, 使用者, 組件, 記憶體, 程式碼), not converted Simplified Chinese.
- `ja-JP`: natural Japanese technical documentation in です・ます style, standard katakana IT terms.

## Workflow and self-check (mandatory)

1. Read the English page(s) and your glossary.
2. Write each translated page with the Write tool (UTF-8) to `docs/localized/<locale>/<page>`. Long pages may be written in several parts, but the final file must be complete.
3. Run: `python tools/Localization/l10n.py finalize <locale> <page> [<page> ...]`
4. Run: `python tools/Localization/l10n.py check <locale> <page> [<page> ...] --allow-pending` and fix every reported problem in your page (re-run finalize after edits) until it prints `problems=0`.
5. Run: `python tools/Localization/l10n.py leak <locale>` and fix every finding that concerns your pages (ignore findings for pages you were not assigned). A finding on a product message the English page quotes verbatim (for example an error or status text in quotes) is acceptable: keep the message in English and do not paraphrase it away.
6. Final answer (short, at most 12 lines): pages written, the final `check` result, any English sentence you could not translate and why, and any terminology decision not in the glossary.
