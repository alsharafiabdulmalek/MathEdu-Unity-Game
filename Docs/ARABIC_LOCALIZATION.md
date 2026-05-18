# Arabic Localization Guide

MathEdu now ships with two languages: **English** (default) and **Arabic** (العربية).

## How a user switches language

1. MainMenu → ⚙ Settings.
2. Tap the language row: **English** or **العربية**.
3. The Settings scene reloads instantly in the new language; every text in every subsequent screen renders Arabic.

The choice is persisted in `PlayerProfile.language` (`"en"` or `"ar"`) and applied on every app launch via `GameManager.Initialize()`.

## What is in this PR

| Layer | Status |
|---|---|
| Bootstrap splash | ✅ Arabic |
| Player Setup (name / avatar / grade) | ✅ Arabic |
| Main Menu (greeting, grade strip, subject cards) | ✅ Arabic |
| Subject names (Addition / Subtraction / …) | ✅ Arabic |
| Level Select (header, level tiles, footer hint) | ✅ Arabic |
| Mode Select (Learn / Practice / Quiz / Story / Speed) | ✅ Arabic |
| Results (titles, score, retry / next / menu, badges) | ✅ Arabic |
| Settings (all rows + language selector + PIN flow + reset dialog) | ✅ Arabic |
| Badge pretty names (First Step, Speed Demon, …) | ✅ Arabic |
| **Math question prompts** | ⏳ next PR — still English |
| Parental Dashboard text | ⏳ next PR — keys exist, manager not yet wired |
| Pause / Quit overlay during gameplay | ⏳ next PR |

## Architecture

### `Localization` (Assets/Scripts/Utility/LocalizationManager.cs)

Static translator. Two parallel `Dictionary<string,string>` tables hold the English fallback and Arabic translations (~150 keys). API:

```csharp
Localization.SetLanguage(Localization.Lang.Arabic);
Localization.T("setup.welcome");                    // -> Arabic 'mrhban!'
Localization.T("menu.hi", profile.playerName);      // -> 'mrhban {name}!'
Localization.Apply(tmpComponent);                    // -> swap font + RTL flag
```

Keys are namespaced (`setup.*`, `menu.*`, `subj.*`, `gp.*`, etc.). Missing keys silently fall back to English, then to the raw key string — useful for spotting un-localized strings during development.

### Font + RTL

`Localization.ArabicFont` lazy-loads `Resources/Fonts/Arabic SDF` if present, else generates a TMP_FontAsset at runtime from a system font candidate list (`Arial`, `Tahoma`, `Geeza Pro`, `Helvetica`, `Noto Sans Arabic`). On Android "Arial" maps to Roboto (full Arabic), on iOS to Helvetica/Geeza Pro, on macOS to Geeza Pro.

`UIFactory.CreateText()` automatically calls `Localization.Apply()` on every text it creates, so every screen built through `UIFactory` is automatically RTL-aware when language=Arabic.

### Persistence

`PlayerProfile.language` is `"en"` by default. `GameManager.Initialize()` reads it and calls `Localization.SetFromCode(...)` immediately after the profile is loaded, so the first UI text the player sees is already in the right language.

## Adding more translations

### A new UI string

1. Pick a namespaced key: `parental.subject_details`.
2. Add it to both `En` and `Ar` dictionaries in `LocalizationManager.cs`.
3. Use `Localization.T("parental.subject_details")` in the manager.

### A new language

1. Add a new enum value: `Lang.French`.
2. Add a parallel `Fr` dictionary.
3. Extend `SetLanguage`, `CurrentCode`, and the `T()` dispatch to handle the new value.
4. Add the language to the Settings selector (3rd button).

## Notes for translators

- Format placeholders use `{0}`, `{1}`, … — keep the same order/positions in translations.
- Strings like `"{0} \u2022 {1} \u2022 Level {2}"` can be reordered freely in Arabic (e.g. `"Level {2} \u2022 {1} \u2022 {0}"`) since `string.Format` picks by index.
- Words like `English` stay in English even in the Arabic table (so the user can always find their native language button).
- Emoji-prefixed strings (`"🌱 First Step"`) keep the emoji on the original side; Arabic readers don't expect it mirrored.
