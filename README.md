# MathEdu — Unity 6 Mobile Math Game

> **Grades 1‑3 · 11 math subjects · 5 learning modes · 20 levels each · ScriptableObject‑driven**
>
> A complete Unity **6000.4.4f1** mobile project (Android + iOS) that teaches
> kids math through games. The entire UI is built procedurally from C# +
> TextMeshPro, every piece of content lives in **ScriptableObjects**, and the
> project ships with a runtime fallback so it works the moment you press
> Play — even before any sprite art is added.

---

## ✅ What works right now

The repo is **end‑to‑end playable** on a fresh clone. A developer can do
**exactly** this:

```bash
git clone https://github.com/alsharafiabdulmalek/MathEdu-Unity-Game.git
# Open in Unity Hub with Unity 6000.4.4f1
# MathEdu → Build Default Database
# MathEdu → Build Default Avatar Library
# MathEdu → Build All Scenes
# Open Assets/Scenes/Bootstrap.unity, press ▶ Play
```

And without touching any other editor control:

| Flow | Outcome |
|---|---|
| **First launch** | Splash → PlayerSetup. Type name, pick avatar, pick grade, "Start Playing!" |
| **Main Menu** | Avatar mini + name + 11 subject cards visible, grade strip top, stars/XP/badges in header |
| **Pick a subject** | LevelSelect: 20 tiles, Level 1 always unlocked, others 🔒 |
| **Pick Level 1 + Quiz** | 10 questions, 30 s timer per question with colour + pulse, "Time's up!" if expired |
| **Finish a level** | Results: animated star pop‑ins, badges (if any), XP, Score, Menu / Retry / Next |
| **Earn ≥ 1 star** | Level 2 unlocks; tap "Next Level" to play it immediately |
| **Speed Round wrong answer** | Run ends immediately, Results shows "Survived X questions" |
| **Return to LevelSelect** | Level 1 shows the earned star count, Level 2 unlocked, Level 3+ still 🔒 |
| **Settings / Parental Dashboard** | Reachable from Main Menu, PIN gate uses keypad, slides up on success |

No null reference exceptions in Console during any of the above. All 5
learning modes (Learn / Practice / Quiz / Story / Speed Round) are playable
from Level 1 of every subject across grades 1–3.

---

## Table of Contents

1. [Quick start](#quick-start)
2. [How to test each mode](#how-to-test-each-mode)
3. [Project structure](#project-structure)
4. [Math curriculum](#math-curriculum)
5. [Learning modes](#learning-modes)
6. [Badges](#badges)
7. [Architecture overview](#architecture-overview)
8. [Scene flow](#scene-flow)
9. [Adding artwork (sprites you'll provide)](#adding-artwork)
10. [Epic Toon FX integration](#epic-toon-fx-integration)
11. [Editor menu reference](#editor-menu-reference)
12. [Mobile build settings](#mobile-build-settings)
13. [Known issues](#known-issues)
14. [Remaining manual steps](#remaining-manual-steps)
15. [Roadmap](#roadmap)

---

## Quick start

1. **Clone:**
   ```bash
   git clone https://github.com/alsharafiabdulmalek/MathEdu-Unity-Game.git
   ```
2. **Open in Unity Hub** with Unity **6000.4.4f1** (or any 6000.x). On first
   load Unity will resolve the packages listed in `Packages/manifest.json` —
   notably TextMeshPro. Accept the TMP Essentials import prompt if it
   appears.
3. **Build database & scenes** — three editor menu items, one click each:
   - `MathEdu → Build Default Database` — generates **6,600 questions**
     (3 grades × 11 subjects × 20 levels × 10 questions, where applicable)
     and writes a `MathDatabase.asset` copy to `Assets/Resources/` so
     GameManager finds it without any inspector wiring.
   - `MathEdu → Build Default Avatar Library` — 10 emoji avatars under
     `Assets/ScriptableObjects/Avatars` + a Resources copy.
   - `MathEdu → Build All Scenes` — creates the **13 scenes** under
     `Assets/Scenes/` and registers them in `EditorBuildSettings`.
4. **Open `Assets/Scenes/Bootstrap.unity` and press ▶**.

> If you skip step 3, the game **still runs** — `GameManager` builds a
> runtime database in memory via `DatabaseBootstrapper` and a fallback
> `AvatarLibrary` through `AvatarLibrary.BuildDefault()`. The editor menu is
> only needed if you want the question data visible in the Project window.

---

## How to test each mode

After running through PlayerSetup once, every mode is reachable from
MainMenu → Subject → Level → ModeSelect → \[mode\]:

| Mode | What to check |
|---|---|
| **Learn** | 3 example questions auto‑reveal the correct answer (1.5 s show, then highlight green + hint, then 2.5 s pause). After "Now it's YOUR turn!" 7 practice questions follow with hints visible. Back returns to ModeSelect. |
| **Practice** | 10 untimed questions, hints available via the 💡 button. Mistakes don't penalise. Pause button (top‑right) freezes Time.timeScale. |
| **Quiz** | 10 timed questions with a Timer that turns green → yellow → red, pulses below 20 % fill, ticks every second below 5 s, and plays the alarm SFX on expiry. Score gets a small time bonus per fast answer. |
| **Story** | Same MCQ loop as Practice with a themed banner. Per‑subject intros & outros (Farmer Jenny / 🍕 pizza / Architect Aria etc.). |
| **Speed Round** | Up to 50 questions; a single wrong answer ends the run. Pause is disabled by design. Results shows "Survived X questions". A 25‑in‑a‑row streak unlocks the Speed Demon badge. |

To verify the unlock chain:

1. Play Quiz Level 1 of Addition with at least 1 star (50 % correct).
2. Results → tap **Next Level** → confirms Level 2 loads.
3. Back to MainMenu → Addition → LevelSelect → Level 2 tile is now unlocked
   with its star count.

To verify Settings persistence:

1. MainMenu → ⚙ Settings.
2. Toggle music or SFX, slide a volume bar — change persists immediately
   (no need to back out).
3. Toggle haptics, change PIN (Settings → 🔐 Change Parental PIN → 3‑step
   flow).
4. Restart Bootstrap — settings still applied.

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
│   │   ├── PlayerProfile.cs           # + SubjectStats roll-up, play-day streak
│   │   ├── GameSession.cs             # + SessionResult snapshot
│   │   ├── AvatarData.cs
│   │   ├── AvatarLibrary.cs
│   │   ├── UITheme.cs
│   │   └── VFXLibrary.cs
│   ├── Utility/
│   │   ├── QuestionGenerator.cs       # L1–10 single-step, L11–20 word problems
│   │   ├── DatabaseBootstrapper.cs    # Subject-themed Story templates
│   │   └── SaveSystem.cs              # JSON + PlayerPrefs persistence
│   ├── Managers/
│   │   ├── GameManager.cs             # Root singleton (standalone-scene safe)
│   │   ├── AudioManager.cs            # Named PlaySFX, 10 procedural clips
│   │   ├── HapticManager.cs           # Static wrapper around Handheld.Vibrate
│   │   ├── ProgressManager.cs         # Full badge taxonomy + SessionResult
│   │   ├── UIManager.cs               # Scene transitions with fade
│   │   ├── VFXManager.cs              # Epic Toon FX hooks
│   │   ├── PlayerSetupManager.cs      # First-launch screen
│   │   ├── SettingsManager.cs         # Music/SFX/Haptics + PIN change flow
│   │   └── ParentalDashboardManager.cs# 10-key PIN gate + slide-up reveal
│   ├── UI/
│   │   ├── UIFactory.cs               # Theme-aware procedural Canvas/TMP builder
│   │   ├── UIThemeService.cs
│   │   ├── DefaultSprite.cs           # Procedural rounded-rect, gradient, circle
│   │   ├── SafeAreaHandler.cs
│   │   ├── FadeOverlay.cs
│   │   ├── AnswerButton.cs
│   │   ├── StarRating.cs
│   │   ├── ProgressBar.cs
│   │   ├── Timer.cs                   # Threshold colours + pulse + tick SFX
│   │   ├── AnimatedFeedback.cs
│   │   ├── QuestionVisualRenderer.cs  # Clock, dots, fractions, etc.
│   │   ├── ToggleSwitch.cs
│   │   ├── AvatarTile.cs
│   │   ├── AccuracyBarChart.cs
│   │   └── PasswordDialog.cs
│   ├── Gameplay/
│   │   └── GameplayManagerBase.cs     # Shared MCQ loop + pause + quit confirm
│   ├── Modes/
│   │   ├── BootstrapManager.cs
│   │   ├── MainMenuManager.cs         # Subject progress bars + badge strip
│   │   ├── LevelSelectManager.cs      # 20 level tiles
│   │   ├── ModeSelectManager.cs
│   │   ├── LearnModeManager.cs        # 3 examples + 7 practice questions
│   │   ├── PracticeModeManager.cs
│   │   ├── QuizModeManager.cs         # Timer-driven
│   │   ├── StoryModeManager.cs        # Themed narrative banner
│   │   ├── SpeedRoundManager.cs       # 50-pool, stop-on-first-wrong
│   │   └── ResultsManager.cs          # Per-star pop animation + badges
│   ├── Editor/
│   │   ├── DatabaseBuilderMenu.cs
│   │   └── SceneBuilderMenu.cs
│   ├── MathEdu.Runtime.asmdef
│   └── Editor/MathEdu.Editor.asmdef
├── ScriptableObjects/                 # Generated by the editor menu
├── Scenes/                            # Generated by the editor menu (13 scenes)
├── Resources/                         # MathDatabase + AvatarLibrary copies
├── Sprites/                           # UI/Backgrounds (Layer Lab + custom)
└── Epic Toon FX/                      # vendor pack (optional)
```

---

## Math curriculum

Every subject ships 20 levels with 10 questions each. **L11+ are word
problems** with named characters and scaffolded multi-step hints — the
hints walk the player through the operations rather than just stating the
formula.

| Subject | Grade 1 | Grade 2 | Grade 3 |
|---|---|---|---|
| Counting | ✅ 1–30 | ✅ skip 2/5/10/25 | – |
| Addition | ✅ within 100 (L20 max) | ✅ within 999 | ✅ within 9999 |
| Subtraction | ✅ within 100 | ✅ within 999 | ✅ within 9999 |
| Multiplication | – | ✅ x2/5/10 → x1‑10 | ✅ tables 1‑12 + word |
| Division | – | – | ✅ within 144, word problems L11+ |
| Shapes | ✅ 2‑D | ✅ 2‑D → 3‑D (L11+) | ✅ perimeter / area |
| Patterns | ✅ AB / ABB / ABBC (emoji) | ✅ longer patterns | ✅ number patterns (+N, ×N) |
| Fractions | – | ✅ halves / thirds / fifths | ✅ equivalent fractions |
| Measurement | ✅ compare | ✅ pick the unit | ✅ unit conversions (cm→m, m→km, …) |
| Time | ✅ to the hour | ✅ ¼ / 5‑min / odd | ✅ to the minute, **elapsed time L11+** |
| Money | ✅ coin recognition | ✅ totals (more coins) | ✅ making change, multi-step purchases |

L11–L15 = single-step word problems, L16–L19 = two-step,
L20 = three-step "challenge" at the grade's max range.

---

## Learning modes

| Mode | Behaviour |
|---|---|
| **Learn** | Guided lesson. Intro card → 3 auto‑reveal examples (each: show 1.5 s → highlight green + hint → 2.5 s pause → fade) → "Now it's YOUR turn!" → 7 practice questions with hints always visible. No scoring. |
| **Practice** | Untimed run through all 10 questions of the chosen level. Hints available. Mistakes don't penalise. |
| **Quiz** | Timed challenge — `LevelData.quizSecondsPerQuestion` per question. Curve: **30 s → 10 s** across the 20 levels. Score = base + time bonus. No hints. |
| **Story** | Same MCQ loop as Practice, wrapped in a subject-themed narrative banner (Farmer Jenny / 🍕 pizza / Architect Aria / etc.). |
| **Speed Round** | Rapid‑fire — `LevelData.speedSecondsPerQuestion` per question. Curve: **8 s → 2.5 s**. One wrong answer ends the run. Up to 50 questions per session. |

All five modes route to the same `Results` scene which:
- Reads `GameSession.lastResult` exclusively (populated by Finish() before
  the scene transition, so Results survives backgrounding / scene reloads).
- Animates 3 individual star widgets that pop 0 → 1.3 → 1.0 over 0.25 s
  with 0.15 s inter-star delay. Plays "starReveal" SFX per pop.
- Lists any newly earned badges with their pretty names + emoji.
- Disables **Next Level** unless the player earned ≥ 1 star AND the next
  level was actually unlocked by this run.

---

## Badges

Earned automatically by `ProgressManager.MaybeAwardMetaBadges()`:

| Badge | Trigger |
|---|---|
| 🌱 **First Step** | Complete any level with ≥1 star for the first time |
| 🎓 **{Subject} Apprentice (G1/2/3)** | Clear Level 5 of a subject (per grade) |
| 🏆 **{Subject} Master (G1/2/3)** | Clear Level 20 with 3 stars |
| 🛤 **Half Way There** | Complete any subject's Level 10 |
| ⚡ **Speed Demon** | Survive 25 correct‑in‑a‑row in Speed Round |
| 💯 **Perfect Score** | Get 10/10 in Quiz Mode |
| 🌅 **Early Bird** | Complete a level before 8 AM local time |
| 📅 **Dedicated** | Play on 3 consecutive days (tracked via `PlayerProfile.playDays`) |

The Parental Dashboard renders the full badge wall and the Main Menu
header shows a 🏅 count.

---

## Architecture overview

```
[GameManager] (DontDestroyOnLoad)
   ├── MathDatabase         (from Resources OR built procedurally)
   ├── AvatarLibrary        (from Resources OR built procedurally)
   ├── PlayerProfile        (loaded by SaveSystem)
   ├── GameSession          (grade / subject / level / mode + lastResult)
   ├── AudioManager         (named PlaySFX with 10 procedural clip fallbacks)
   ├── ProgressManager      (records subject stats, awards badges)
   ├── UIManager            (scene transitions with fade + page-transition SFX)
   └── VFXManager           (Epic Toon FX hooks)
```

- **Self‑bootstrapping singletons.** Every scene contains exactly **one**
  `[SceneRoot]` GameObject with the relevant `*Manager` script. Touching
  `GameManager.Instance` lazily creates the root if it's missing, so any
  scene can run standalone in the editor without manual wiring. The
  Initialize() path picks sensible defaults on a fresh profile so the
  gameplay managers never crash on null lookups.
- **Procedural UI.** No scene contains hand‑authored Canvas hierarchies —
  every manager calls `UIFactory.CreateCanvas(...)` in `Start()` and builds
  buttons, panels, text, layouts, and animations from code.
- **Theme‑aware factories.** `UIFactory` consults `UIThemeService` for
  every Sprite slot (buttons, panels, sliders, toggles, backgrounds). Drop
  in a `UITheme.asset` and the whole UI re‑skins automatically.
- **Procedural fallback content + audio.** If no `MathDatabase.asset` is
  present, `DatabaseBootstrapper.BuildInMemory()` constructs ~4,800
  questions at runtime. AudioManager generates pleasant SFX procedurally
  for **all 10 named clips** (correct, wrong, tap, levelComplete,
  starReveal, timerTick, timerExpire, pageTransition, badgeUnlocked, lose).
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
                            Subject grid (per-grade) — progress bar +
                            "Level X / 20" / stars, or "Tap to start!"
                            ⚙ Settings  /  👪 Parental Dashboard
    │
    ▼ (tap subject)
LevelSelect ──────────────► 20 level tiles, unlocked tiles tappable,
                            locked tiles show 🔒
    │
    ▼ (tap unlocked level)
ModeSelect ───────────────► Learn / Practice / Quiz / Story / Speed
    │
    ▼ (tap mode)
[Mode scene]  ────────────► MCQ loop with mode-specific rules
                            Pause button (top-right) freezes Time.timeScale
                            Back button asks "Quit this level?"
    │
    ▼ (last question or fail)
Results ──────────────────► Stars + score + XP + badges
                            Menu / Retry / Next (Next gated on unlock+stars)
```

Every scene change is wrapped in a fade-to-black + page-transition SFX.

---

## Adding artwork (sprites you'll provide)

The repository ships with a UI/Backgrounds sprite bundle (the
`Layer Lab UI Assets` toggles and `UI asset` GUI packs under
`Assets/Sprites/UI/`) and a `backgrounds*` folder of backgrounds. To wire
these into the entire UI in one step:

1. **Create → MathEdu → UI Theme** in the Project window.
2. Drag your sprites into the matching slots on the new `UITheme.asset`.
3. Move (or copy) the configured `UITheme.asset` into `Assets/Resources/`.
4. Press Play — the entire UI now uses your artwork.

If you don't want to wire a `UITheme`, the procedural defaults
(`DefaultSprite.RoundedRect`, `Gradient`, `Circle`) keep every screen
functional with zero artwork.

---

## Epic Toon FX integration

The repository includes the **Epic Toon FX** pack under `Assets/Epic Toon FX/`.

1. **Create → MathEdu → VFX Library** in the Project window.
2. Drag your favourite Epic Toon FX prefabs into the slots
   (`correctVFX`, `wrongVFX`, `winVFX`, `loseVFX`, `tapVFX`, `starBurstVFX`,
   `ambientVFX`).
3. Move the configured `VFXLibrary.asset` into `Assets/Resources/`.
4. Press Play — answer feedback now spawns particles via `VFXManager`.

If `VFXLibrary` is missing or a slot is empty, the manager silently
no‑ops — gameplay is never blocked by missing VFX assets.

---

## Editor menu reference

| Menu | What it does |
|---|---|
| `MathEdu / Build Default Database` | Generates `MathDatabase.asset` + all `GradeData / SubjectData / LevelData` (20 levels per subject) under `Assets/ScriptableObjects/` and a copy under `Assets/Resources/`. Subject-themed `storyIntro` / `storyOutro` on every level. |
| `MathEdu / Build Default Avatar Library` | Generates 10 emoji avatars + an `AvatarLibrary.asset`. |
| `MathEdu / Build All Scenes` | Creates **13** `.unity` scenes and registers them in Build Settings. |
| `MathEdu / Wipe Generated Database` | Removes the generated folder + Resources copies. |
| `MathEdu / Reset Player Progress` | Wipes the save file + PlayerPrefs. |

---

## Mobile build settings

- Default orientation: **Portrait** (`defaultScreenOrientation: 0`)
- Reference resolution: **1080 × 1920**
- `applicationIdentifier`: `com.mathedu.game` (Android, iOS, Standalone)
- Android: **min SDK 22** (Android 5.1)
- Safe‑area aware UI via `SafeAreaHandler`
- Build for Android: `File → Build Profiles → Android → Build`
- Build for iOS: `File → Build Profiles → iOS → Build` (Mac + Xcode 15+)

---

## Known issues

- **Haptics on iOS** use `Handheld.Vibrate()` because the project must ship
  without third‑party packages. That maps to a "peek/pop" notification on
  iOS rather than the modern UIImpactFeedbackGenerator. A native plugin
  bridge can be wired in by replacing `HapticManager.Light/Medium/Heavy()`.
- **Language toggle** in Settings is a placeholder — the i18n strings
  inside `QuestionGenerator` and the mode managers are already isolated so
  the swap‑in is straightforward.
- **Editor scene authoring is intentionally minimal**: each scene contains
  only one GameObject. Everything visible is built at runtime from
  `UIFactory`. If you want hand-authored prefabs, build them under
  `Assets/Prefabs/` and reference them from a manager script.

---

## Remaining manual steps

After cloning, run these editor menu items **once**:

1. `MathEdu / Build Default Database` — populates the .asset files. (The
   game runs without this thanks to the procedural fallback, but the
   Project window stays empty until you click.)
2. `MathEdu / Build Default Avatar Library` — writes the 10 avatars.
3. `MathEdu / Build All Scenes` — creates the 13 `.unity` files and
   registers them in `EditorBuildSettings`.

Then open `Assets/Scenes/Bootstrap.unity` and press **▶ Play**.

---

## Roadmap

- 🌍 Localisation (i18n keys already isolated in `QuestionGenerator`)
- 👤 Multi‑profile support (file naming already partitioned)
- 🏅 Daily streak rewards + push notifications (foundation in
  `PlayerProfile.playDays` + `consecutiveDayStreak`)
- 🧪 Adaptive Practice Mode that re‑surfaces previously missed questions
- 🎨 More avatar art (drop sprites onto `AvatarData.sprite`)
- 📐 Drawing-based answers for shape questions

---

Built for kids who deserve great math games. PRs welcome.
