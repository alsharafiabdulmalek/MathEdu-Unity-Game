# MathEdu — Unity 6 Mobile Math Game

> **Grades 1‑3 · 11 math subjects · 5 learning modes · 20 levels each · ScriptableObject‑driven**
>
> A complete Unity **6000.4.4f1** mobile project (Android + iOS) that teaches
> kids math through games. The entire UI is built procedurally from C# +
> TextMeshPro, every piece of content lives in **ScriptableObjects**, and the
> project ships with a runtime fallback so it works the moment you press
> Play — even before any sprite art is added.

---

## Table of Contents

1. [What's in the box](#whats-in-the-box)
2. [Quick start](#quick-start)
3. [Project structure](#project-structure)
4. [Math curriculum](#math-curriculum)
5. [Learning modes](#learning-modes)
6. [New screens](#new-screens)
7. [Architecture overview](#architecture-overview)
8. [Scene flow](#scene-flow)
9. [Adding artwork (sprites you'll provide)](#adding-artwork)
10. [Epic Toon FX integration](#epic-toon-fx-integration)
11. [Adding / editing questions](#adding--editing-questions)
12. [Mobile build settings](#mobile-build-settings)
13. [Editor menu reference](#editor-menu-reference)
14. [Roadmap](#roadmap)

---

## What's in the box

| Feature | Status |
|---|---|
| Grades 1–3, 11 subjects, **20 levels × 10 questions** each | ✅ Procedurally generated, ~4,800+ questions |
| ScriptableObjects: `MathDatabase`, `GradeData`, `SubjectData`, `LevelData`, `MathQuestion`, `AvatarData`, `AvatarLibrary`, `UITheme`, `VFXLibrary` | ✅ |
| Player profile (JSON save + PlayerPrefs backup) with per‑subject stats | ✅ |
| **Player Setup scene** (name + avatar + grade) | ✅ |
| **Settings scene** (music/SFX toggles + volume sliders + haptics + reset) | ✅ |
| **Parental Dashboard scene** (PIN‑gated, per‑subject accuracy + time) | ✅ |
| Procedural UI (TextMeshPro + uGUI Canvas), safe‑area aware | ✅ |
| 5 learning modes: Learn, Practice, Quiz, Story, Speed Round (all 20 levels) | ✅ |
| Star ratings (1–3), XP, badges, level unlocks | ✅ |
| Procedural audio + master mixer (music ON/OFF, SFX ON/OFF) | ✅ |
| **Epic Toon FX hooks** (VFXLibrary → correct / wrong / win / star burst) | ✅ |
| **Theme‑aware sprite loading** via `UITheme.asset` | ✅ |
| Scene transitions with fade | ✅ |
| Android + iOS player settings | ✅ |

---

## Quick start

1. **Clone:**
   ```bash
   git clone https://github.com/alsharafiabdulmalek/MathEdu-Unity-Game.git
   ```
2. **Open in Unity Hub** with Unity **6000.4.4f1** (or any 6000.x). On first
   load Unity will resolve the packages listed in `Packages/manifest.json` —
   notably TextMeshPro. Accept the TMP Essentials import prompt if it appears.
3. **Build the database & scenes** from the editor menu:
   - `MathEdu → Build Default Database` (creates ~4,800 questions across all
     grades / subjects / **20 levels** and a `MathDatabase.asset` in
     `Assets/Resources` so it's loaded automatically at runtime).
   - `MathEdu → Build Default Avatar Library` (creates the 10 default
     emoji avatars under `Assets/ScriptableObjects/Avatars` + a Resources copy).
   - `MathEdu → Build All Scenes` (creates the **13 scenes** under
     `Assets/Scenes/` and registers them in `EditorBuildSettings`).
4. **Open `Assets/Scenes/Bootstrap.unity` and press ▶**. On first launch the
   game routes you to `PlayerSetup`, then `MainMenu`. Subsequent launches go
   straight to `MainMenu` because `PlayerProfile.setupComplete` is now `true`.

> If you skip step 3, the game still runs — `GameManager` builds a runtime
> database in memory via `DatabaseBootstrapper` and a fallback `AvatarLibrary`
> through `AvatarLibrary.BuildDefault()`. The editor menu is only needed if
> you want the question data visible in the Project window for tuning.

---

## Project structure

```
Assets/
├── Scripts/
│   ├── Data/                          # ScriptableObject definitions
│   │   ├── MathQuestion.cs
│   │   ├── LevelData.cs
│   │   ├── SubjectData.cs
│   │   ├── GradeData.cs
│   │   ├── MathDatabase.cs
│   │   ├── PlayerProfile.cs           # + SubjectStats roll-up
│   │   ├── GameSession.cs
│   │   ├── AvatarData.cs              # NEW
│   │   ├── AvatarLibrary.cs           # NEW
│   │   ├── UITheme.cs                 # NEW
│   │   └── VFXLibrary.cs              # NEW
│   ├── Utility/
│   │   ├── QuestionGenerator.cs       # 20 levels per subject
│   │   ├── DatabaseBootstrapper.cs    # 20-level timer curves
│   │   └── SaveSystem.cs              # JSON + PlayerPrefs persistence
│   ├── Managers/
│   │   ├── GameManager.cs             # Root singleton, also owns VFX + Avatars
│   │   ├── AudioManager.cs            # Music/SFX with master toggles
│   │   ├── ProgressManager.cs         # Stars, XP, unlocks, badges, subject stats
│   │   ├── UIManager.cs               # Scene transitions, new scene names
│   │   ├── VFXManager.cs              # NEW – Epic Toon FX hooks
│   │   ├── PlayerSetupManager.cs      # NEW – first launch screen
│   │   ├── SettingsManager.cs         # NEW – music/SFX/volume/haptics
│   │   └── ParentalDashboardManager.cs# NEW – per-subject stats
│   ├── UI/
│   │   ├── UIFactory.cs               # Theme-aware procedural Canvas/TMP builder
│   │   ├── UIThemeService.cs          # NEW – sprite library accessor
│   │   ├── DefaultSprite.cs           # Procedural rounded-rect, gradient, circle
│   │   ├── SafeAreaHandler.cs
│   │   ├── FadeOverlay.cs
│   │   ├── AnswerButton.cs
│   │   ├── StarRating.cs
│   │   ├── ProgressBar.cs
│   │   ├── Timer.cs
│   │   ├── AnimatedFeedback.cs
│   │   ├── QuestionVisualRenderer.cs  # Clock, dots, fractions, etc.
│   │   ├── ToggleSwitch.cs            # NEW – sprite-aware on/off toggle
│   │   ├── AvatarTile.cs              # NEW – avatar grid tile
│   │   ├── AccuracyBarChart.cs        # NEW – per-subject horizontal bar chart
│   │   └── PasswordDialog.cs          # NEW – parental PIN modal
│   ├── Gameplay/
│   │   └── GameplayManagerBase.cs     # Shared MCQ loop + VFX + timing
│   ├── Modes/
│   │   ├── BootstrapManager.cs        # First-launch detection
│   │   ├── MainMenuManager.cs         # Avatar + Settings + Parental buttons
│   │   ├── LevelSelectManager.cs      # 20 level tiles
│   │   ├── ModeSelectManager.cs
│   │   ├── LearnModeManager.cs
│   │   ├── PracticeModeManager.cs
│   │   ├── QuizModeManager.cs
│   │   ├── StoryModeManager.cs
│   │   ├── SpeedRoundManager.cs
│   │   └── ResultsManager.cs          # VFX star bursts
│   ├── Editor/
│   │   ├── DatabaseBuilderMenu.cs     # MathEdu / Build Default Database
│   │   ├── DatabaseBuilderMenu.cs     # MathEdu / Build Default Avatar Library
│   │   └── SceneBuilderMenu.cs        # MathEdu / Build All Scenes
│   ├── MathEdu.Runtime.asmdef
│   └── Editor/MathEdu.Editor.asmdef
├── ScriptableObjects/                 # Generated by the editor menu
│   ├── MathDatabase.asset
│   ├── AvatarLibrary.asset
│   ├── Grades/Grade1..3/
│   ├── Subjects/
│   ├── Levels/
│   └── Avatars/
├── Scenes/                            # Generated by the editor menu (13 scenes)
│   ├── Bootstrap.unity
│   ├── PlayerSetup.unity              # NEW
│   ├── MainMenu.unity
│   ├── LevelSelect.unity
│   ├── ModeSelect.unity
│   ├── LearnMode.unity
│   ├── PracticeMode.unity
│   ├── QuizMode.unity
│   ├── StoryMode.unity
│   ├── SpeedRound.unity
│   ├── Results.unity
│   ├── Settings.unity                 # NEW
│   └── ParentalDashboard.unity        # NEW
├── Resources/
│   ├── MathDatabase.asset             # Auto-found by GameManager at runtime
│   ├── AvatarLibrary.asset            # Auto-found by GameManager at runtime
│   ├── UITheme.asset                  # Optional – drop sprites here
│   └── VFXLibrary.asset               # Optional – drop Epic Toon FX prefabs here
├── Sprites/
│   ├── Backgrounds/                   # bg PNGs the UITheme references
│   ├── UI/                            # Layer Lab + game GUI assets
│   └── Characters/                    # avatar / character sprites
├── Epic Toon FX/                      # vendor pack (used by VFXLibrary)
└── Prefabs/                           # Empty by design — UI is procedural
```

---

## Math curriculum

The procedural curriculum (in `QuestionGenerator.cs`) is grade‑appropriate
and roughly Common‑Core‑flavoured. **Every subject ships 20 levels with 10
questions each (200 questions per subject).**

| Subject | Grade 1 | Grade 2 | Grade 3 |
|---|---|---|---|
| Counting | ✅ 1–30 | ✅ skip 2/5/10/25 | – |
| Addition | ✅ within 25 | ✅ within 100 | ✅ 3‑digit |
| Subtraction | ✅ within 25 | ✅ within 100 | ✅ 3‑digit |
| Multiplication | – | ✅ x2/5/10 → x1‑10 | ✅ tables 1‑12 |
| Division | – | – | ✅ within 144 |
| Shapes | ✅ 2‑D | ✅ 2‑D → 3‑D (L11+) | ✅ perimeter / area |
| Patterns | ✅ AB / ABB / ABBC | – | – |
| Fractions | – | ✅ halves / thirds / fifths | ✅ equivalent fractions |
| Measurement | ✅ compare | ✅ pick the unit | ✅ pick the unit |
| Time | ✅ to the hour | ✅ ¼ / 5‑min / odd | ✅ to the minute |
| Money | ✅ coin recognition | ✅ totals (more coins) | ✅ making change |

Each question is a `MathQuestion` with `prompt`, `options[4]`, `correctIndex`,
`hint`, `explanation`, `difficulty`, and an optional `visual` payload (clock
hands, fraction numerator/denominator, dot counts, etc.).

---

## Learning modes

| Mode | Behaviour |
|---|---|
| **Learn** | Guided lesson with intro / example / tip, then 3 try‑it questions with hints always visible. No scoring, no timer. |
| **Practice** | Untimed run through all 10 questions of the chosen level. Hints available. Mistakes do not penalise. |
| **Quiz** | Timed challenge — `LevelData.quizSecondsPerQuestion` per question. Curve: **30 s → 10 s** across the 20 levels. Score = base + time bonus. No hints. |
| **Story** | Same MCQ loop as Practice, wrapped in a narrative banner. Order preserved (not shuffled). |
| **Speed Round** | Rapid‑fire — `LevelData.speedSecondsPerQuestion` per question. Curve: **8 s → 2.5 s**. One wrong answer ends the run. |

All five modes route to the same `Results` scene which:
- Animates a 0→N star count‑up using `LevelData.ComputeStars(correct, total)`.
- Fires a `VFXLibrary.starBurstVFX` particle per star.
- Records the result via `ProgressManager.CompleteLevel(...)` (which awards
  XP, stars, badges, unlocks the next level, **and updates per‑subject
  stats** that the Parental Dashboard reads).

---

## New screens

### Player Setup (`Assets/Scenes/PlayerSetup.unity`)

- TextMeshPro **name** input (16 char max).
- **Avatar grid** populated from `AvatarLibrary` — 10 emoji‑on‑colour
  defaults, each replaceable with a real sprite via `AvatarData.sprite`.
- **Grade picker** (1 / 2 / 3) — coloured highlight on the chosen grade.
- "Start Playing" button saves `playerName`, `avatarId`, `selectedGrade`,
  flips `setupComplete = true`, and transitions to Main Menu.

The Bootstrap scene routes here on first launch (i.e. whenever
`setupComplete` is `false`).

### Settings (`Assets/Scenes/Settings.unity`)

- **Music** ON/OFF toggle + volume slider.
- **Sound effects** ON/OFF toggle + volume slider.
- **Haptics** toggle.
- **Language** placeholder (English by default; ready for i18n).
- **Reset Player Progress** button — protected by the Parental PIN dialog.
- All changes are written to `PlayerProfile`, flushed to JSON, and the live
  `AudioManager` volumes are updated immediately.

### Parental Dashboard (`Assets/Scenes/ParentalDashboard.unity`)

- PIN‑gated (default PIN `0000`; configurable from within the dashboard).
- Summary tiles: **Stars · XP · Badges · Levels played · Total time · Grade**.
- **Accuracy by Subject** horizontal bar chart (read from
  `PlayerProfile.subjectStats`).
- **Subject details table**: questions answered, correct%, stars, levels
  completed, time spent.
- **Grade completion** bar chart (% of all levels with at least 1 ★).
- "Change PIN" and "Reset Progress" actions (both PIN‑gated).

---

## Architecture overview

```
[GameManager] (DontDestroyOnLoad)
   ├── MathDatabase         (from Resources OR built procedurally)
   ├── AvatarLibrary        (from Resources OR built procedurally)
   ├── PlayerProfile        (loaded by SaveSystem)
   ├── GameSession          (grade / subject / level / mode selections)
   ├── AudioManager         (auto-added; music + SFX with master toggles)
   ├── ProgressManager      (auto-added; records subject stats)
   ├── UIManager            (auto-added; scene transitions)
   └── VFXManager           (auto-added; Epic Toon FX hooks)
```

- **Self‑bootstrapping singletons.** Every scene contains exactly **one**
  `[SceneRoot]` GameObject with the relevant `*Manager` script. Touching
  `GameManager.Instance` lazily creates the root if it's missing, so any
  scene can run standalone in the editor without manual wiring.
- **Procedural UI.** No scene contains hand‑authored Canvas hierarchies —
  every manager calls `UIFactory.CreateCanvas(...)` in `Start()` and builds
  buttons, panels, text, layouts, and animations from code. This keeps the
  YAML scene files tiny and merge‑friendly.
- **Theme‑aware factories.** `UIFactory` consults `UIThemeService` for every
  Sprite slot (buttons, panels, sliders, toggles, backgrounds). Drop in a
  `UITheme.asset` (Resources/UITheme.asset) with your sprites and the whole
  UI re‑skins automatically.
- **Procedural fallback content.** If the `MathDatabase.asset` is missing
  (or empty), `DatabaseBootstrapper.BuildInMemory()` constructs the entire
  ~4,800‑question tree at runtime. Same story for `AvatarLibrary` via
  `AvatarLibrary.BuildDefault()`.
- **Single save file** at `Application.persistentDataPath/player_profile.json`
  with a redundant copy in PlayerPrefs for platforms with finicky file I/O.

---

## Scene flow

```
Bootstrap
    │  (1.2 s splash)
    │  if (!profile.setupComplete)
    ▼
PlayerSetup ─────────────► Name + Avatar + Grade
    │
    ▼ (Start Playing)
MainMenu  ────────────────► Grade buttons (1, 2, 3)
                            Subject grid (per‑grade)
                            ⚙ Settings  /  👪 Parental Dashboard
    │
    ▼ (tap subject)
LevelSelect ──────────────► 20 level tiles (unlocked / 🔒)
    │
    ▼ (tap unlocked level)
ModeSelect ───────────────► Learn / Practice / Quiz / Story / Speed
    │
    ▼ (tap mode)
[Mode scene]  ────────────► MCQ loop with mode‑specific rules + VFX
    │
    ▼ (last question or fail)
Results ──────────────────► Stars + score + Menu/Retry/Next + star bursts
```

`Settings` and `ParentalDashboard` are reachable from the Main Menu's bottom
toolbar (`⚙` and `👪` icon buttons respectively).

---

## Adding artwork (sprites you'll provide)

The repository now ships with a UI/Backgrounds sprite bundle (the
`Layer Lab UI Assets` toggles and `UI asset` GUI packs under
`Assets/Sprites/UI/`) and a `backgrounds*` folder of backgrounds. To wire
these into the entire UI in one step:

1. **Create → MathEdu → UI Theme** in the Project window.
2. Drag your sprites into the matching slots on the new `UITheme.asset`:
   - `Backgrounds` group → `menuBackground`, `gameplayBackground`, etc.
   - `Panels & Buttons` → 9‑sliced `buttonSprite`, `panelSprite`, etc.
   - `Toggle Sprites` → `on Toggle.png` and `off Toggle.png` from
     `Layer Lab UI Assets/`.
   - `Icons` → `starFilled`, `starEmpty`, `coinIcon` (the gold coin PNG is
     already in the repo) etc.
3. Move (or copy) the configured `UITheme.asset` into `Assets/Resources/`.
4. Press Play — the entire UI now uses your artwork.

If you don't want to wire a `UITheme`, the procedural defaults
(`DefaultSprite.RoundedRect`, `Gradient`, `Circle`) keep every screen
functional with zero artwork.

---

## Epic Toon FX integration

The repository includes the **Epic Toon FX** pack under `Assets/Epic Toon FX/`.
To wire its particle prefabs into the gameplay:

1. **Create → MathEdu → VFX Library** in the Project window.
2. Drag your favourite Epic Toon FX prefabs into the slots, e.g.
   - `correctVFX` → a sparkle / star burst prefab from
     `Epic Toon FX/Prefabs/Other/`.
   - `wrongVFX` → a smoke or short puff prefab.
   - `winVFX` → a fireworks prefab.
   - `starBurstVFX` → a small star‑shower prefab (fires once per star on
     the Results screen).
   - `tapVFX` → a tiny sparkle (plays on each answer tap).
   - `ambientVFX` → e.g. `SnowStorm.prefab` for atmospheric scenes.
3. Move the configured `VFXLibrary.asset` into `Assets/Resources/`.
4. Press Play — answer feedback now spawns particles via `VFXManager`.

`VFXManager` calls `Instantiate(prefab, position, identity, [VFXRoot])` and
destroys the instance after `defaultLifetime` seconds (configurable on the
`[GameManager]` runtime object during Play). If `VFXLibrary` is missing or a
slot is empty, the manager **silently no‑ops** — gameplay is never blocked
by missing VFX assets.

---

## Adding / editing questions

You have **three** options, from easiest to most powerful:

1. **Tweak a single level by hand** after running
   `MathEdu → Build Default Database`. Each `LevelData.asset` is fully
   editable — change a question prompt or option directly in the Inspector.
2. **Add a new question type** — extend `MathQuestion.QuestionVisual`,
   render the new visual in `QuestionVisualRenderer.Show()`, and emit it
   from `QuestionGenerator`.
3. **Add a new subject** — add a value to `MathSubject`, write a generator
   method in `QuestionGenerator`, list it in `SubjectsFor(grade)`, and
   rebuild the database.

All edits survive Unity restarts because the data lives in `.asset` files
under `Assets/ScriptableObjects/`.

---

## Mobile build settings

The included `ProjectSettings/ProjectSettings.asset` ships with:

- Default orientation: **Portrait** (`defaultScreenOrientation: 0`)
- Reference resolution: **1080 × 1920**
- `applicationIdentifier`: `com.mathedu.game` (Android, iOS, Standalone)
- Android: **min SDK 22** (Android 5.1)
- Safe‑area aware UI via `SafeAreaHandler`

**To build for Android:** `File → Build Profiles → Android → Build`.

**To build for iOS:** `File → Build Profiles → iOS → Build` (requires a Mac
host with Xcode 15+).

---

## Editor menu reference

| Menu | What it does |
|---|---|
| `MathEdu / Build Default Database` | Generates `MathDatabase.asset` + all `GradeData / SubjectData / LevelData` (20 levels per subject) under `Assets/ScriptableObjects/` and a copy under `Assets/Resources/`. |
| `MathEdu / Build Default Avatar Library` | Generates 10 emoji avatars + an `AvatarLibrary.asset` under `Assets/ScriptableObjects/Avatars/` and a Resources copy. |
| `MathEdu / Build All Scenes` | Creates **13** `.unity` scenes under `Assets/Scenes/` (including PlayerSetup, Settings, ParentalDashboard) and registers them in Build Settings. |
| `MathEdu / Wipe Generated Database` | Removes the generated folder + Resources copies. |
| `MathEdu / Reset Player Progress` | Wipes the save file + PlayerPrefs. |

---

## Roadmap

- 🌍 Localisation (i18n keys already isolated in `QuestionGenerator`)
- 👤 Multi‑profile support (file naming already partitioned)
- 🏅 Daily streak rewards + push notifications
- 🧪 Adaptive Practice Mode that re‑surfaces previously missed questions
- 🎨 More avatar art (drop sprites onto `AvatarData.sprite`)

---

Built for kids who deserve great math games. PRs welcome.
