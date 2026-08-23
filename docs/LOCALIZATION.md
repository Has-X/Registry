# Localization

English is the canonical Registry source. The app uses Sensitivity's semantic JSON catalog model; installer language selection stays native Inno Setup.

- `src/Registry.App/Resources/locales/en/windows.json` is the source catalog.
- `src/Registry.App/Resources/locales/<language>/windows.json` is one complete reviewable app catalog per language.
- `installer/Registry.iss` contains the matching native Inno language list.

Supported Windows language identifiers: `en`, `hu`, `es`, `de`, `fr`, `it`, `pl`, `pt-BR`, `pt-PT`, `tr`, `id`, `ro`, `cs`, `sk`, `ru`, `uk`, `zh-CN`, `zh-TW`, `ar`, `vi`, `th`, `hi`, `ja`, `ko`, `nl`, `el`, `bg`, `hr`, `sr`, `sl`, `sv`, `da`, `fi`, and `nb`.

## Translator instructions

Translate intent, not isolated words. Keep `Registry`, `Chromatic`, `chromatic.hu`, `feedback@chromatic.hu`, registry hive names, `.reg`, command names, placeholders such as `{0}`, and Windows technology names unchanged. Safety text must remain explicit: importing and editing can modify Windows configuration.

English remains visible for a missing reviewed key. Never ship guessed machine translation as a fallback. Before accepting automated translations, preserve placeholders exactly and review narrow-window text fit.

## Adding copy

1. Add a stable semantic key to the English catalog before XAML or C# use.
2. Add that key to every released locale.
3. Use `Tag="prefix.semantic-key"` for static XAML and `LocalizationService.Get` or `LocalizationService.Format` for dynamic code text.
4. Run `pwsh -NoProfile -File tools/check-locales.ps1`, then build and test.

## Release checker

`tools/check-locales.ps1` is intentionally strict. It fails the release when a
supported language directory is missing, a key is absent or extra, a value is
empty, a placeholder changes, a non-technical translation is still identical
to English, or source code contains an unkeyed XAML/tooltip/C# status string.
It also checks that the Inno Setup language set has not silently shrunk.

During catalog bootstrapping only, `-AllowMissingLanguages -AllowEnglishFallbacks`
can be used to inspect source-copy findings without hiding them in CI. Those
switches must never be used by release workflows.
