# MathEdu — Unity 6 Mobile Math Game

> **Grades 1‑5 · 11 math subjects · 5 learning modes · 20 levels each · Bilingual (English + Arabic) · ScriptableObject‑driven**
>
> A complete Unity **6000.4.4f1** mobile project (Android + iOS) that teaches
> kids math through games. The entire UI is built procedurally from C# +
> TextMeshPro, every piece of content lives in **ScriptableObjects**, and the
> project ships with a runtime fallback so it works the moment you press
> Play — even before any sprite art is added.

---

## ✅ What works right now

The repo is **end‑to‑end playable** on a fresh clone with **one** menu
click:

```bash
git clone https://github.com/alsharafiabdulmalek/MathEdu-Unity-Game.git
# Open in Unity Hub with Unity 6000.4.4f1
# MathEdu → Polish → ✨ Run Full Polish Setup (scenes + theme + icons)
# Open Assets/Scenes/Bootstrap.unity, press ▶ Play
```

That's it — the database, avatars, scenes, the polished UITheme.asset and
IconLibrary.asset are all generated in well under a minute. (If you prefer
running the steps individually, see the [Quick start](#quick-start) section.)

> **Recovering from a Unity hang?**
> If a previous attempt to run `MathEdu → Build Default Database` froze
> Unity (the version before the performance fix), follow the recovery
> steps in [`Docs/RESCUE_FROM_DB_HANG.md`](Docs/RESCUE_FROM_DB_HANG.md).

| Flow | Outcome |
|---|---|
| **First launch** | Animated splash 📚 → PlayerSetup. Type name, pick avatar (with GUI Pro character art), pick grade, "Start Playing!" |
| **Main Menu** | Avatar mini + name + 11 subject cards visible, grade strip top, stars/XP/badges in header. Settings & Parental buttons show real Pictoicons. |
| **Pick a subject** | LevelSelect: 20 tiles, polished star icons with glow halo on unlocked tiles, lock sprite on locked ones |
| **Pick Level 1 + Quiz** | 10 questions, animated reaction face puck reacts to every answer, streak counter on the pill, emoji confetti puffs on correct |
| **Finish a level** | Results: trophy icon, animated star pop‑ins with glow halos, badges, XP, Score, full-screen confetti rain on win, delayed badge sprinkle |
| **Earn ≥ 1 star** | Level 2 unlocks; tap "Next Level" to play it immediately |
| **Speed Round wrong answer** | Run ends immediately, Results shows "Survived X questions" |
| **Return to LevelSelect** | Level 1 shows the earned star count, Level 2 unlocked, Level 3+ still 🔒 |
| **Settings / Parental Dashboard** | Reachable from Main Menu, icon-led rows (🎵 music / 🔊 sfx / 📳 haptics), PIN gate keypad slides up on success |

No null reference exceptions in Console during any of the above. All 5
learning modes (Learn / Practice / Quiz / Story / Speed Round) are playable
from Level 1 of every subject across **grades 1–5**, in both **English and
Arabic** (Settings → 🌐 Language toggles instantly; every question, hint,
lesson example and story prompt re-renders in the chosen language without a
restart).

---

## Table of Contents

1. [Quick start](#quick-start)
2. [How to test each mode](#how-to-test-each-mode)
3. [Polish pass — engagement visuals](#polish-pass--engagement-visuals)
4. [Project structure](#project-structure)
5. [Math curriculum](#math-curriculum)
6. [Learning modes](#learning-modes)
7. [Badges](#badges)
8. [Architecture overview](#architecture-overview)
9. [Scene flow](#scene-flow)
10. [Adding artwork (sprites you'll provide)](#adding-artwork)
11. [Epic Toon FX integration](#epic-toon-fx-integration)
12. [Editor menu reference](#editor-menu-reference)
13. [Mobile build settings](#mobile-build-settings)
14. [Known issues](#known-issues)
15. [Remaining manual steps](#remaining-manual-steps)
16. [Roadmap](#roadmap)

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
3. **Run setup — pick one of three paths:**

   #### Path A — one click (recommended)
   ```
   MathEdu → Polish → ✨ Run Full Polish Setup (scenes + theme + icons)
   ```
   Runs Quick Start (avatars + scenes) **plus** builds the polished
   `UITheme.asset` + `IconLibrary.asset` so every screen automatically picks
   up the GUI Pro - Casual Game sprites. Total time: well under a minute
   even on a constrained machine.

   #### Path B — three clicks (same result, more visible)
   - `MathEdu → Build Default Database` — generates ~9,400 questions (5
     grades × ~47 subject-tracks × 20 levels × 10 questions) as a single
     consolidated `MathDatabase.asset` in `Assets/Resources/` with every
     Grade / Subject / Level as a nested sub-asset (typically a few seconds
     on a modern Mac).
   - `MathEdu → Build Default Avatar Library` — 10 emoji avatars under
     `Assets/ScriptableObjects/Avatars` + a Resources copy.
   - `MathEdu → Build All Scenes` — creates the **13 scenes** under
     `Assets/Scenes/` and registers them in `EditorBuildSettings`.

   #### Path C — no database build at all
   The game runs without any of the build menus. `GameManager.EnsureDatabase()`
   builds a ~9,400-question content tree in memory at startup via
   `DatabaseBootstrapper.BuildInMemory()`. You only need:
   - `MathEdu → Build All Scenes` (~5 s; required because Unity needs registered scenes)
   - Optional: `MathEdu → Build Default Avatar Library` (also has a runtime fallback)

   The `MathEdu → Advanced → Use Runtime Database Only (no build)` menu
   item surfaces this information as an in-Editor popup.

4. **Open `Assets/Scenes/Bootstrap.unity` and press ▶**.

> **Performance note.** The old `Build Default Database` created ~570
> separate `.asset` files and could take 30+ minutes (or hang) on macOS.
> The current build writes ONE consolidated asset and finishes in
> seconds. See [`Docs/RESCUE_FROM_DB_HANG.md`](Docs/RESCUE_FROM_DB_HANG.md)
> if you ran the old version and Unity is still recovering.

---

## How to test each mode

After running through PlayerSetup once, every mode is reachable from
MainMenu → Subject → Level → ModeSelect → \[mode\]:

| Mode | What to check |
|---|---|
| **Learn** | A MascotHost in the bottom-left "talks" the player through 3 auto-reveal examples and 7 practice questions. Correct answers fire an emoji burst; wrong ones make the mascot frown and say "Try again — you can do it!" |
| **Practice** | 10 untimed questions, hints available via the 💡 button. Mistakes don't penalise. Pause button (top‑right) freezes Time.timeScale. ReactionFace puck reacts to every answer. |
| **Quiz** | 10 timed questions with a Timer that turns green → yellow → red, pulses below 20 % fill, ticks every second below 5 s, and plays the alarm SFX on expiry. Timer expiry triggers a *surprised* reaction face + "Time's up!" pill. Score gets a small time bonus per fast answer. |
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

## Polish pass — engagement visuals

The "Polish Pass" layer ships a set of small but high-impact upgrades:

| System | What it does |
|---|---|
| **`MathEdu / Polish / ✨ Run Full Polish Setup`** | One-click: builds avatars, scenes, **and** wires the GUI Pro - Casual Game sprite pack into `UITheme.asset` + `IconLibrary.asset` under `Assets/Resources/`. After this, every screen uses GUI Pro buttons, frames, popups, sliders, toggles, and pictoicons automatically. |
| **`IconLibrary` ScriptableObject** | Maps named keys (`gear`, `star`, `bulb`, `heart`, `emojiSmile`, …) to sprites. Resolved at runtime through `IconService`. Drop in your own art at any time; missing slots fall back to emoji glyphs so nothing ever breaks. |
| **`ReactionFace` widget** | A small bouncing face puck that lives next to the question card. Reacts in real-time: 😄 happy on correct, 🤩 cheer on streaks ≥ 3, 😢 sad on wrong, 😮 surprised on timer expiry. Sprite-or-glyph; uses `Pictoicon_Emoji_*` from GUI Pro when present. |
| **`MascotHost`** | A larger cartoon mascot with body, head, blush, speech bubble. Used in Learn Mode to "talk" the player through the lesson. Speaks praise on correct answers, encouragement on wrong, and a final cheer when the lesson ends. |
| **`EmojiBurst`** | Procedural confetti / emoji puffs spawned at any anchored position: `Correct` (⭐ ✨), `Cheer` (🎉 🔥 ⭐ on streaks), `Win` (full-screen 🎊 🥳 🏆 confetti rain on Results), `Badge` (🏅 sprinkle when a new badge is earned), and a discreet `Wrong` (💧). |
| **`PolishSprites`** | Procedural star, glow halo, ring, and shadowed-rounded-rect generators. Used by Results star widgets, the level-select tiles, the answer buttons, and the AnimatedFeedback pill. |
| **`AnimatedFeedback` 2.0** | The big "✓ Correct!" pill now ships with a reaction face, a streak counter (`x3 Streak!`), ease-out-back scale-in, and an emoji burst behind it. Wrong answers play a horizontal shake and stamp a red ✗ on the chosen button. |
| **Answer button feedback** | Correct flash → green colour + scale-punch + green ✓ stamp icon. Wrong flash → red colour + shake + red ✗ stamp icon. Always uses sprite-or-glyph from the IconLibrary. |
| **Bootstrap splash** | The splash scene now scale-in animates the wordmark + 📚 mark with ease-out-back and a gentle idle bob. |
| **Level Select / Results stars** | Use the polished 5-point star sprite (or the GUI Pro `Pictoicon_Star` when wired) with a soft glow halo on filled stars. |
| **Settings rows** | Music, SFX, and Haptics rows now show their icon (sprite or glyph) on the left for instant readability. |
| **Main Menu icons** | The ⚙ Settings and 👪 Parental Controls buttons auto-upgrade to real `Pictoicon_Setting` / `Pictoicon_Account` art when the IconLibrary is present. |
| **Polished gradient background** | Every gradient background now has a soft radial glow overlay that mimics a cheap post-process bloom — no URP or Post-Processing dependencies. |

### Asset sourcing

Every additional visual is built from one of three sources, all of which are
already in the repo or generated procedurally:

| Source | Used for | Licence |
|---|---|---|
| **GUI Pro - Casual Game** (Layer Lab) | Buttons, frames, popups, sliders, toggles, ~640 PictoIcons, ~20 cartoon characters used as avatar art. Already imported under `Assets/Sprites/UI/Layer Lab UI Assets/GUI Pro-CasualGame/`. | Bundled with project under Layer Lab's user-asset guide (see `Assets/Sprites/UI/Layer Lab UI Assets/GUI Pro-CasualGame/+README+/LayerLab_UserAssetGuide.txt`). |
| **Unicode emoji glyphs** | Engagement visuals — used as the live fallback whenever a sprite slot is empty, so the game is fully expressive even with no art. | Public domain — Unicode standard glyphs rendered by the device font. |
| **Procedural (PolishSprites)** | Star, glow halo, ring, shadowed rounded rect. Built once at runtime, cached. | All-original code in this repo. |

No third-party packages were added — everything works against the same
`com.unity.textmeshpro` + `com.unity.ugui` packages already in `manifest.json`.

### Quick start (polish)

```
MathEdu → Polish → ✨ Run Full Polish Setup (scenes + theme + icons)
```

That's it. Open `Assets/Scenes/Bootstrap.unity` and press **▶ Play**.

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
│   │   ├── IconLibrary.cs             # POLISH: named sprite mapping
│   │   └── VFXLibrary.cs
│   ├── Utility/
│   │   ├── QuestionGenerator.cs       # L1–10 single-step, L11–20 word problems
│   │   ├── DatabaseBootstrapper.cs    # Subject-themed Story templates
│   │   └── SaveSystem.cs              # JSON + PlayerPrefs persistence
│   ├── Managers/
│   │   ├── GameManager.cs             # Root singleton (standalone-scene safe)
│   │   ├── AudioManager.cs            # Named PlaySFX, 13 stereo procedural clips + ambient loop
│   │   ├── HapticManager.cs           # Static wrapper around Handheld.Vibrate
│   │   ├── ProgressManager.cs         # Full badge taxonomy + SessionResult
│   │   ├── UIManager.cs               # Scene transitions with fade + per-scene music swap
│   │   ├── VFXManager.cs              # Epic Toon FX hooks
│   │   ├── PlayerSetupManager.cs      # First-launch screen
│   │   ├── SettingsManager.cs         # Music/SFX/Haptics + PIN change flow + language switch
│   │   └── ParentalDashboardManager.cs# 10-key PIN gate + slide-up reveal
│   ├── UI/
│   │   ├── UIFactory.cs               # Theme-aware procedural Canvas/TMP builder + auto Arabic shaping
│   │   ├── UIThemeService.cs
│   │   ├── IconService.cs             # POLISH: sprite-first / glyph-fallback facade
│   │   ├── DefaultSprite.cs           # Procedural rounded-rect, gradient, circle
│   │   ├── PolishSprites.cs           # POLISH: star / glow / ring / shadowed RR
│   │   ├── SafeAreaHandler.cs
│   │   ├── FadeOverlay.cs
│   │   ├── AnswerButton.cs            # POLISH: ✓/✗ stamp + scale-punch / shake
│   │   ├── StarRating.cs
│   │   ├── ProgressBar.cs
│   │   ├── Timer.cs                   # Threshold colours + pulse + tick SFX
│   │   ├── AnimatedFeedback.cs        # POLISH: face puck + streak counter + burst
│   │   ├── ReactionFace.cs            # POLISH: animated mood widget
│   │   ├── MascotHost.cs              # POLISH: full-body cartoon host
│   │   ├── EmojiBurst.cs              # POLISH: confetti / emoji puffs
│   │   ├── QuestionVisualRenderer.cs  # Clock, dots, fractions, etc.
│   │   ├── ToggleSwitch.cs
│   │   ├── AvatarTile.cs
│   │   ├── AccuracyBarChart.cs
│   │   └── PasswordDialog.cs
│   ├── Gameplay/
│   │   └── GameplayManagerBase.cs     # Shared MCQ loop + pause + quit confirm
│   ├── Modes/
│   │   ├── BootstrapManager.cs        # POLISH: animated 📚 splash
│   │   ├── MainMenuManager.cs         # POLISH: IconService gear / parent
│   │   ├── LevelSelectManager.cs      # POLISH: star icons + lock sprite
│   │   ├── ModeSelectManager.cs
│   │   ├── LearnModeManager.cs        # POLISH: MascotHost + EmojiBurst
│   │   ├── PracticeModeManager.cs
│   │   ├── QuizModeManager.cs         # POLISH: surprised reaction on time-up
│   │   ├── StoryModeManager.cs
│   │   ├── SpeedRoundManager.cs       # POLISH: surprised reaction on time-up
│   │   └── ResultsManager.cs          # POLISH: trophy stamp + confetti rain
│   ├── Editor/
│   │   ├── DatabaseBuilderMenu.cs     # Fast single-asset build + Advanced submenu
│   │   ├── PolishBuilderMenu.cs       # POLISH: builds UITheme + IconLibrary, wires avatars
│   │   └── SceneBuilderMenu.cs
│   ├── MathEdu.Runtime.asmdef
│   └── Editor/MathEdu.Editor.asmdef
├── ScriptableObjects/                 # Optional per-grade asset files
├── Scenes/                            # Generated by the editor menu (13 scenes)
├── Resources/                         # MathDatabase + AvatarLibrary + UITheme + IconLibrary
├── Sprites/                           # UI/Backgrounds (Layer Lab + custom)
└── Epic Toon FX/                      # vendor pack (optional)

Docs/
├── SESSION_LOG.md                     # Original make-playable session log
└── RESCUE_FROM_DB_HANG.md             # Recovery steps if the old build hung Unity
```

---

## Math curriculum

Every subject ships 20 levels with 10 questions each. **L11+ are word
problems** with named characters and scaffolded multi-step hints — the
hints walk the player through the operations rather than just stating the
formula.

| Subject | Grade 1 | Grade 2 | Grade 3 | Grade 4 | Grade 5 |
|---|---|---|---|---|---|
| Counting | ✅ 1–30 | ✅ skip 2/5/10/25 | – | – | – |
| Addition | ✅ within 100 | ✅ within 999 | ✅ within 9999 | ✅ within 99 999 + city-population word problems | ✅ within 999 999 |
| Subtraction | ✅ within 100 | ✅ within 999 | ✅ within 9999 | ✅ within 99 999 + factory built/sold problems | ✅ within 999 999 |
| Multiplication | – | ✅ ×2/5/10 → ×1‑10 | ✅ tables 1‑12 + word | ✅ 2‑digit × 1‑/2‑digit, school-bus problems | ✅ 3‑digit × 1‑digit, 2‑digit × 2‑digit |
| Division | – | – | ✅ within 144, word problems L11+ | ✅ **with remainders**, share-the-cards problems | ✅ **long division**, 2-digit divisors |
| Shapes | ✅ 2‑D | ✅ 2‑D → 3‑D (L11+) | ✅ perimeter / area | ✅ **triangle area**, **angle classification** (acute/right/obtuse/straight) | ✅ **volume** of cube + rectangular prism |
| Patterns | ✅ AB / ABB / ABBC (emoji) | ✅ longer patterns | ✅ number patterns (+N, ×N) | ✅ **find the rule** (constant +N or ×N) | ✅ **term-rule** (2n+1, n², 3n−2, n(n+1)/2) |
| Fractions | – | ✅ halves / thirds / fifths | ✅ equivalent fractions | ✅ **add/subtract same denom**, compare | ✅ **add/subtract unlike denominators** |
| Measurement | ✅ compare | ✅ pick the unit | ✅ unit conversions (cm→m, m→km, …) | ✅ **compound conversion** (km+m, kg+g, l+ml), room area in m² | ✅ larger compound + bigger areas |
| Time | ✅ to the hour | ✅ ¼ / 5‑min / odd | ✅ to the minute, elapsed time L11+ | ✅ **24-hour ↔ 12-hour**, add a duration | ✅ multi-leg journey total |
| Money | ✅ coin recognition | ✅ totals (more coins) | ✅ making change, multi-step purchases | ✅ **multi-item bills**, two-line receipts | ✅ **percentages**, **discounts** (% off) |

L11–L15 = single-step word problems, L16–L19 = two-step,
L20 = three-step "challenge" at the grade's max range
(Grade 1 = 100 · Grade 2 = 999 · Grade 3 = 9 999 ·
**Grade 4 = 99 999 · Grade 5 = 999 999**).

Every prompt, hint, lesson example, story intro/outro and option label is
fully bilingual — the same procedural generator emits English when the
language is set to `en` and Arabic when set to `ar`. Numbers stay in
Western-Arabic digits (0–9) so the math symbols read identically across
languages, while Arabic prose uses Arabic punctuation (، ؟ etc.).

---

## Learning modes

| Mode | Behaviour |
|---|---|
| **Learn** | Guided lesson. Intro card → 3 auto‑reveal examples (each: show 1.5 s → highlight green + hint → 2.5 s pause → fade) → "Now it's YOUR turn!" → 7 practice questions with hints always visible. MascotHost speaks throughout. No scoring. |
| **Practice** | Untimed run through all 10 questions of the chosen level. Hints available. Mistakes don't penalise. |
| **Quiz** | Timed challenge — `LevelData.quizSecondsPerQuestion` per question. Curve: **30 s → 10 s** across the 20 levels. Score = base + time bonus. No hints. Surprised reaction face on timer expiry. |
| **Story** | Same MCQ loop as Practice, wrapped in a subject-themed narrative banner (Farmer Jenny / 🍕 pizza / Architect Aria / etc.). |
| **Speed Round** | Rapid‑fire — `LevelData.speedSecondsPerQuestion` per question. Curve: **8 s → 2.5 s**. One wrong answer ends the run. Up to 50 questions per session. |

All five modes route to the same `Results` scene which:
- Reads `GameSession.lastResult` exclusively (populated by Finish() before
  the scene transition, so Results survives backgrounding / scene reloads).
- Animates 3 individual star widgets that pop 0 → 1.3 → 1.0 over 0.25 s
  with 0.15 s inter-star delay. Plays "starReveal" SFX per pop.
- Fires a full-screen confetti rain on any win and a delayed badge sprinkle
  when new badges are earned or 3 stars are scored.
- Lists any newly earned badges with their pretty names + emoji.
- Disables **Next Level** unless the player earned ≥ 1 star AND the next
  level was actually unlocked by this run.

---

## Badges

Earned automatically by `ProgressManager.MaybeAwardMetaBadges()`:

| Badge | Trigger |
|---|---|
| 🌱 **First Step** | Complete any level with ≥1 star for the first time |
| 🎓 **{Subject} Apprentice (G1‑5)** | Clear Level 5 of a subject (per grade) |
| 🏆 **{Subject} Master (G1‑5)** | Clear Level 20 with 3 stars |
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
   ├── AudioManager         (named PlaySFX with 13 stereo procedural clips + ambient loop)
   ├── ProgressManager      (records subject stats, awards badges)
   ├── UIManager            (scene transitions with fade + menu/gameplay music swap)
   └── VFXManager           (Epic Toon FX hooks)

[UIThemeService]            (Resources/UITheme.asset, drives panel/button/slider sprites)
[IconService]               (Resources/IconLibrary.asset, drives named icons)
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
- **Sprite-first / glyph-fallback icons.** `IconService.IconButton()` and
  related helpers resolve named keys (`star`, `gear`, `emojiSmile`, …) to
  sprites when an `IconLibrary.asset` is present; otherwise they fall back
  to the emoji glyph supplied by the caller so every screen is always
  recognisable.
- **Procedural fallback content + audio.** If no `MathDatabase.asset` is
  present, `DatabaseBootstrapper.BuildInMemory()` constructs ~9,400
  questions at runtime (5 grades). AudioManager generates rich stereo SFX
  procedurally for **all 13 named clips** (correct, wrong, tap, hint,
  levelComplete, starReveal, streak, timerTick, timerExpire, pageTransition,
  badgeUnlocked, lose, swoosh) with ADSR envelopes, chord stacks and gentle
  pitch jitter so repeated taps feel organic. A 12‑second seamless ambient
  loop is also generated when no `music_menu` / `music_play` clip is
  supplied — UIManager swaps between menu and gameplay tracks on every
  scene transition.
- **Full English ↔ Arabic localization.** Every UI string (menus, buttons,
  pause overlay, results, parental dashboard) and every question prompt,
  hint, lesson and story flows through `Localization.T()`. The custom
  `ArabicShaper` (`Assets/Scripts/Utility/ArabicShaper.cs`) walks each
  Arabic string and substitutes the correct **initial / medial / final /
  isolated** presentation-form glyph for each letter — including the
  lam-alef ligature — so TextMeshPro renders proper **connected cursive
  Arabic** instead of disconnected isolated letters. Switching language in
  Settings purges every cached level's content so the next question
  re-generates in the new language with no app restart.
- **Single save file** at `Application.persistentDataPath/player_profile.json`
  with a redundant copy in PlayerPrefs for platforms with finicky file I/O.
- **Single consolidated database asset.** The default build writes ONE
  `Assets/Resources/MathDatabase.asset` with every Grade / Subject /
  Level stored as a nested sub-asset. The Project window still shows the
  full tree (Unity expands sub-assets under their parent). One file =
  one Asset Database import = predictable, fast builds.

---

## Scene flow

```
Bootstrap
    │  (1.5 s animated splash with 📚 mark)
    │  if (!profile.setupComplete)
    ▼
PlayerSetup ─────────► Name + Avatar (with GUI Pro character art) + Grade (1-5)
    │
    ▼ (Start Playing)
MainMenu  ────────────────► Grade buttons (1, 2, 3, 4, 5)
                            Subject grid (per-grade) — progress bar +
                            "Level X / 20" / stars, or "Tap to start!"
                            ⚙ Settings  /  👪 Parental Dashboard
                            (icons swap to GUI Pro pictoicons when wired)
    │
    ▼ (tap subject)
LevelSelect ──────────────► 20 level tiles, star icons w/ glow on unlocked,
                            lock sprite on locked
    │
    ▼ (tap unlocked level)
ModeSelect ───────────────► Learn / Practice / Quiz / Story / Speed
    │
    ▼ (tap mode)
[Mode scene]  ────────────► MCQ loop with mode-specific rules
                            ReactionFace puck reacts to every answer
                            EmojiBurst on correct / streak / wrong
                            Pause button (top-right) freezes Time.timeScale
                            Back button asks "Quit this level?"
    │
    ▼ (last question or fail)
Results ──────────────────► Trophy/sad stamp + animated stars w/ glow +
                            confetti rain (on win) + badges + XP
                            Menu / Retry / Next (Next gated on unlock+stars)
```

Every scene change is wrapped in a fade-to-black + page-transition SFX.

---

## Adding artwork (sprites you'll provide)

The repository ships with a UI/Backgrounds sprite bundle (the
`Layer Lab UI Assets` toggles and `UI asset` GUI packs under
`Assets/Sprites/UI/`) and a `backgrounds*` folder of backgrounds. To wire
these into the entire UI in one step:

1. **Create → MathEdu → UI Theme** in the Project window (or run
   `MathEdu / Polish / Build Default UI Theme & Icon Library` to auto-wire
   the GUI Pro pack).
2. Drag your sprites into the matching slots on the new `UITheme.asset`.
3. Move (or copy) the configured `UITheme.asset` into `Assets/Resources/`.
4. Press Play — the entire UI now uses your artwork.

If you don't want to wire a `UITheme`, the procedural defaults
(`DefaultSprite.RoundedRect`, `Gradient`, `Circle` + `PolishSprites.Star`,
`Glow`, `ShadowedRoundedRect`) keep every screen functional with zero
artwork.

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

### Primary entries

| Menu | What it does | Typical duration |
|---|---|---|
| `MathEdu / Polish / ✨ Run Full Polish Setup` | Runs Quick Start **and** builds the UITheme.asset + IconLibrary.asset with GUI Pro sprites wired. **Recommended.** | < 60 s |
| `MathEdu / ⚡ Quick Start (No DB Build — Recommended)` | Avatars + scenes only. Database is built in memory at startup. | < 30 s |
| `MathEdu / Run Full Setup (writes DB asset — may be slow on low-RAM Macs)` | Also writes the DB asset on disk. | < 60 s typical, longer on constrained machines |
| `MathEdu / Build All Scenes` | Creates **13** `.unity` scenes and registers them in Build Settings. | ~5 s |
| `MathEdu / Build Default Database` | Generates the full curriculum (grades 1-5) as ONE consolidated `MathDatabase.asset` in `Assets/Resources/`. | **~10 s** |
| `MathEdu / Build Default Avatar Library` | 10 emoji avatars under `Assets/ScriptableObjects/Avatars/` + a Resources copy. | ~1 s |
| `MathEdu / Polish / Build Default UI Theme & Icon Library` | Re-builds just the polish assets (theme + icons + avatar sprite wiring) — useful after dropping in new art. | ~3 s |
| `MathEdu / Polish / Wipe Polish Assets` | Deletes `UITheme.asset` + `IconLibrary.asset`. | < 1 s |
| `MathEdu / Wipe Generated Database` | Removes the generated folder + Resources copies. | < 1 s |
| `MathEdu / Reset Player Progress` | Wipes the save file + PlayerPrefs. | < 1 s |

### Advanced submenu

| Menu | What it does |
|---|---|
| `MathEdu / Advanced / Per-Grade Assets / Build Grade 1 Files` | Builds the Grade 1 subtree as **individual per-level `.asset` files** under `Assets/ScriptableObjects/Grades/Grade1/`. Properly batched. Independent and resumable. |
| `MathEdu / Advanced / Per-Grade Assets / Build Grade 2 Files` | Same as above for Grade 2. |
| `MathEdu / Advanced / Per-Grade Assets / Build Grade 3 Files` | Same as above for Grade 3. |
| `MathEdu / Advanced / Per-Grade Assets / Build Grade 4 Files` | Same as above for Grade 4 (10 subjects × 20 levels). |
| `MathEdu / Advanced / Per-Grade Assets / Build Grade 5 Files` | Same as above for Grade 5 (10 subjects × 20 levels). |
| `MathEdu / Advanced / Per-Grade Assets / Rebuild Master Index` | After building one or more grades via the per-file builders, this re-creates `MathDatabase.asset` to point at the on-disk grade assets. |
| `MathEdu / Advanced / Per-Subject Assets / Grade N - {Subject}` | One-subject-at-a-time builder (20 levels). Useful for diagnostics or very constrained machines. Available for grades 1-5 across all relevant subjects. |
| `MathEdu / Advanced / Use Runtime Database Only (no build)` | Shows an in-Editor popup explaining that `GameManager.EnsureDatabase()` builds the database in memory at startup if no asset is present. |
| `MathEdu / Advanced / Open Save File Location` | Reveals `Application.persistentDataPath` in Finder / Explorer. |

> **Performance note.** The default `Build Default Database` is the
> consolidated single-asset path — fast, ~10 s for all 5 grades. See
> [`Docs/RESCUE_FROM_DB_HANG.md`](Docs/RESCUE_FROM_DB_HANG.md) for the
> full perf rationale and recovery if the editor previously hung on the
> old un-batched build.

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

- **(FIXED) `Build Default Database` could hang Unity on macOS.** The
  previous implementation created ~570 individual `.asset` files
  without batching the AssetDatabase. The current implementation builds
  ONE consolidated asset in ~10 seconds. See
  [`Docs/RESCUE_FROM_DB_HANG.md`](Docs/RESCUE_FROM_DB_HANG.md) for
  recovery steps if you ran the old version.
- **Haptics on iOS** use `Handheld.Vibrate()` because the project must ship
  without third‑party packages. That maps to a "peek/pop" notification on
  iOS rather than the modern UIImpactFeedbackGenerator. A native plugin
  bridge can be wired in by replacing `HapticManager.Light/Medium/Heavy()`.
- **Language toggle** is fully wired up — English ↔ Arabic. Question
  prompts, hints, lessons, story copy, UI menus and parental dashboard all
  re-render in the player's language. Switching mid‑play purges every
  cached level's content via `DatabaseBootstrapper.ClearCachedLevelContent`
  so the next question reads in the new language with no app restart.
- **Editor scene authoring is intentionally minimal**: each scene contains
  only one GameObject. Everything visible is built at runtime from
  `UIFactory`. If you want hand-authored prefabs, build them under
  `Assets/Prefabs/` and reference them from a manager script.

---

## Remaining manual steps

After cloning, run **one** menu item:

```
MathEdu → Polish → ✨ Run Full Polish Setup (scenes + theme + icons)
```

Then open `Assets/Scenes/Bootstrap.unity` and press **▶ Play**.

If you prefer running the steps individually, see
[Quick start](#quick-start) Path B. If you only want to play (no
materialized database asset needed), see Path C — the runtime fallback
is fully featured.

---

## Roadmap

- 🌍 More language packs — the English/Arabic pipeline already runs every
  string through `Localization.T()` + `ArabicShaper.Shape()`; adding a third
  language is just one more dictionary in `LocalizationManager.cs`.
- 👤 Multi‑profile support (file naming already partitioned)
- 🏅 Daily streak rewards + push notifications (foundation in
  `PlayerProfile.playDays` + `consecutiveDayStreak`)
- 🧪 Adaptive Practice Mode that re‑surfaces previously missed questions
- 🎨 More avatar art (drop sprites onto `AvatarData.sprite`)
- 📐 Drawing-based answers for shape questions

---

Built for kids who deserve great math games. PRs welcome.
