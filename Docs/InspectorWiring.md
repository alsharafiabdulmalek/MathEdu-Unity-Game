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

1. `MathEdu → Build Default Database`
2. `MathEdu → Build All Scenes`

That populates `Assets/ScriptableObjects/` and `Assets/Scenes/`, and updates
`File → Build Settings` so Bootstrap is scene #0.

---

## 3. Scene contents

Every scene contains exactly **one** GameObject named `[SceneRoot]` with one
of the following MonoBehaviours attached:

| Scene file | Component on `[SceneRoot]` |
|---|---|
| `Bootstrap.unity` | `MathEdu.Modes.BootstrapManager` |
| `MainMenu.unity` | `MathEdu.Modes.MainMenuManager` |
| `LevelSelect.unity` | `MathEdu.Modes.LevelSelectManager` |
| `ModeSelect.unity` | `MathEdu.Modes.ModeSelectManager` |
| `LearnMode.unity` | `MathEdu.Modes.LearnModeManager` |
| `PracticeMode.unity` | `MathEdu.Modes.PracticeModeManager` |
| `QuizMode.unity` | `MathEdu.Modes.QuizModeManager` |
| `StoryMode.unity` | `MathEdu.Modes.StoryModeManager` |
| `SpeedRound.unity` | `MathEdu.Modes.SpeedRoundManager` |
| `Results.unity` | `MathEdu.Modes.ResultsManager` |

There are **no** other GameObjects you need to wire. Canvas, EventSystem,
buttons, text, layout groups, and the `[GameManager]` singleton are all
created at runtime.

---

## 4. The `[GameManager]`

When the game runs, the first call to `GameManager.Instance` creates a
hidden `[GameManager]` GameObject (DontDestroyOnLoad) that holds:

- `MathDatabase database` — auto‑found from `Assets/Resources/MathDatabase.asset`.
  If you skipped `Build Default Database`, it's built in memory by
  `DatabaseBootstrapper`. You only need to assign a custom database
  manually if you want to override the bundled one — drag it onto the
  `GameManager` component during Play.

---

## 5. Customising the look (no code required)

- **Subject colours / icons** → `Assets/ScriptableObjects/Grades/Grade*/[Subject].asset`
  Change `themeColor`, `iconEmoji`, or assign `icon` (Sprite).
- **Star thresholds, XP, timers** → `LevelData.asset` per level
  `oneStarPercent / twoStarPercent / threeStarPercent`,
  `quizSecondsPerQuestion`, `speedSecondsPerQuestion`, `xpReward`.
- **Question text / hints** → open any `Level_##.asset`, expand
  `questions`, edit `prompt`, `options`, `correctIndex`, `hint`.

---

## 6. When sprite art lands

The single switch you need to flip is in `Assets/Scripts/UI/UIFactory.cs`
inside `CreateGradientBackground` / `CreateButton` / `CreatePanel`. Replace
the calls to `DefaultSprite.RoundedRect(...)` and
`DefaultSprite.Gradient(...)` with `Resources.Load<Sprite>("UI/...")`. See
the "Adding artwork" section in the top‑level `README.md` for the full
plan.

---

## 7. Build for Android / iOS

`File → Build Profiles`:

- **Android:** select Android, click "Switch Platform" the first time, then
  "Build". Player Settings ship with `applicationIdentifier =
  com.mathedu.game` and `minSdkVersion = 22`.
- **iOS:** select iOS, build, then open the Xcode project Unity produces.
  Set your Apple Team ID and run.

That's everything. The point of the procedural‑UI architecture is that the
Inspector doesn't need babysitting.
