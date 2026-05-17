# Inspector Wiring Guide

Because **MathEdu builds every Canvas procedurally**, the Inspector wiring is
intentionally tiny. This page lists everything you might need to look at —
roughly in the order you'd open it after cloning the repo.

---

## 1. After first open

Unity will resolve packages and re‑import the project. **Accept the TMP
Essentials import prompt** when it appears (it adds the default TextMeshPro
shaders + a fallback font under `Assets/TextMesh Pro/`).

---

## 2. Build the data + scenes

Top menu bar:

1. `MathEdu → Build Default Database` (~4,800 questions, 20 levels per subject)
2. `MathEdu → Build Default Avatar Library` (10 emoji avatars)
3. `MathEdu → Build All Scenes` (13 scenes)

That populates `Assets/ScriptableObjects/` and `Assets/Scenes/`, and updates
`File → Build Settings` so Bootstrap is scene #0.

---

## 3. Scene contents

Every scene contains exactly **one** GameObject named `[SceneRoot]` with one
of the following MonoBehaviours attached:

| Scene file | Component on `[SceneRoot]` |
|---|---|
| `Bootstrap.unity` | `MathEdu.Modes.BootstrapManager` |
| `PlayerSetup.unity` | `MathEdu.Managers.PlayerSetupManager` |
| `MainMenu.unity` | `MathEdu.Modes.MainMenuManager` |
| `LevelSelect.unity` | `MathEdu.Modes.LevelSelectManager` |
| `ModeSelect.unity` | `MathEdu.Modes.ModeSelectManager` |
| `LearnMode.unity` | `MathEdu.Modes.LearnModeManager` |
| `PracticeMode.unity` | `MathEdu.Modes.PracticeModeManager` |
| `QuizMode.unity` | `MathEdu.Modes.QuizModeManager` |
| `StoryMode.unity` | `MathEdu.Modes.StoryModeManager` |
| `SpeedRound.unity` | `MathEdu.Modes.SpeedRoundManager` |
| `Results.unity` | `MathEdu.Modes.ResultsManager` |
| `Settings.unity` | `MathEdu.Managers.SettingsManager` |
| `ParentalDashboard.unity` | `MathEdu.Managers.ParentalDashboardManager` |

There are **no** other GameObjects you need to wire. Canvas, EventSystem,
buttons, text, layout groups, sliders, toggles, the avatar grid, the bar
chart, and the `[GameManager]` singleton are all created at runtime.

---

## 4. The `[GameManager]`

When the game runs, the first call to `GameManager.Instance` creates a
hidden `[GameManager]` GameObject (DontDestroyOnLoad) that holds:

- `MathDatabase database` — auto‑found from
  `Assets/Resources/MathDatabase.asset`. If you skipped
  `Build Default Database`, it's built in memory by `DatabaseBootstrapper`.
- `AvatarLibrary avatarLibrary` — auto‑found from
  `Assets/Resources/AvatarLibrary.asset`. Falls back to
  `AvatarLibrary.BuildDefault()`.
- `Audio / Progress / UI / VFX` managers — all auto‑added at boot.

You normally **never need to set anything in this Inspector**. The optional
public fields are surfaced only so you can drag in a custom database or
avatar library during Play if you want to A/B test content.

---

## 5. Plugging in your sprite art (UITheme)

`Assets/Sprites/UI/` already ships with the **Layer Lab UI** and **GUI**
sprite packs that came with the project. To make the entire UI use them:

1. **Create → MathEdu → UI Theme** in the Project window.
2. Drop your sprites onto the matching slots:
   - **Backgrounds:** `menuBackground`, `gameplayBackground`,
     `settingsBackground`, `parentalBackground`, `setupBackground`,
     `resultsBackground`.
     - Suggested backgrounds: `Assets/Sprites/Backgrounds/backgrounds/*.png`.
   - **Panels & Buttons:** `buttonSprite`, `panelSprite`, `cardSprite`,
     `pillSprite`, `headerSprite` — pick 9‑sliced variants from
     `Assets/Sprites/UI/UI asset/...`.
   - **Toggle Sprites:** `toggleOnSprite` =
     `Assets/Sprites/UI/Layer Lab UI Assets/on Toggle.png`,
     `toggleOffSprite` = `Layer Lab UI Assets/off Toggle.png`.
   - **Slider:** `sliderBackground`, `sliderFill`, `sliderHandle`.
   - **Icons:** `starFilled`, `starEmpty`, `lockIcon`, `settingsIcon`,
     `backArrow`, `chartIcon`, `coinIcon` (the gold coin PNG in
     `Assets/Sprites/UI/UI asset/` is a great fit).
3. **Move/copy** the configured asset to `Assets/Resources/UITheme.asset`.
4. Press Play — every button, panel, toggle, slider, and background uses
   your art automatically. The procedural defaults (`DefaultSprite.*`) are
   only used for slots that are still empty.

> Tip: you don't have to fill every slot at once. Each empty slot quietly
> falls back to the procedural placeholder so progress is incremental.

---

## 6. Plugging in Epic Toon FX (VFXLibrary)

The repository ships with the **Epic Toon FX** vendor pack under
`Assets/Epic Toon FX/`. To play these particles on answer feedback:

1. **Create → MathEdu → VFX Library** in the Project window.
2. Drop your favourite Epic Toon FX prefabs into the slots:
   - `correctVFX`, `wrongVFX`, `winVFX`, `loseVFX`, `tapVFX`,
     `starBurstVFX`, `ambientVFX`.
3. **Move/copy** the asset to `Assets/Resources/VFXLibrary.asset`.
4. The `VFXManager` (auto‑instantiated by `GameManager`) picks it up at boot
   and fires the matching prefab on every gameplay event. If the asset is
   missing, the manager silently no‑ops.

---

## 7. Customising the look (no code required)

- **Subject colours / icons** → `Assets/ScriptableObjects/Grades/Grade*/[Subject].asset`
  Change `themeColor`, `iconEmoji`, or assign `icon` (Sprite).
- **Star thresholds, XP, timers** → `LevelData.asset` per level
  (`oneStarPercent / twoStarPercent / threeStarPercent`,
  `quizSecondsPerQuestion`, `speedSecondsPerQuestion`, `xpReward`).
- **Question text / hints** → open any `Level_##.asset`, expand
  `questions`, edit `prompt`, `options`, `correctIndex`, `hint`.
- **Default parental PIN** → set on `PlayerProfile` (or change from inside
  the dashboard's "Change PIN" button). Defaults to `0000`.

---

## 8. Build for Android / iOS

`File → Build Profiles`:

- **Android:** select Android, click "Switch Platform" the first time, then
  "Build". Player Settings ship with `applicationIdentifier =
  com.mathedu.game` and `minSdkVersion = 22`.
- **iOS:** select iOS, build, then open the Xcode project Unity produces.
  Set your Apple Team ID and run.

That's everything. The point of the procedural‑UI architecture is that the
Inspector doesn't need babysitting.
