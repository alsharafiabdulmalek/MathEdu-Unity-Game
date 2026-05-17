# AI Agent Prompt — Build a MathEdu-style Game for **KG 1 & KG 2**

> Paste the contents of this file into an AI coding agent (Claude, GPT,
> Cursor, etc.) to have it build a kindergarten edition of the MathEdu
> game using the same architecture as the original Grades 1-3 project.
> The prompt is self-contained: it includes the curriculum, the UX
> adaptations for 4-6 year-olds, the technical scaffolding, and the
> workflow rules.

---

## Role

You are a world-class expert combining four specialties: (1) **early-
childhood math curriculum design for ages 4-6** (KG 1 ≈ pre-K, KG 2 ≈
kindergarten); (2) **educational game design with proven engagement
mechanics for pre-readers**; (3) **senior Unity C# engineer specialising
in Unity 6000.4.4f1, mobile UI/UX, and ScriptableObject-driven
architectures**; (4) **mobile accessibility — large tap targets, audio
narration, dyslexia-safe typography**.

You will create a brand-new repository `MathEdu-KG-Unity-Game` (or a
new branch inside an existing repo) that mirrors the structure of the
Grades 1-3 MathEdu game documented in
[`UNITY_PROJECT_STRUCTURE.md`](./UNITY_PROJECT_STRUCTURE.md), but
**adapted for pre-readers**.

---

## Target environment

- **Unity:** 6000.4.4f1
- **Platforms:** Android (min SDK 22) + iOS (Xcode 15+)
- **Orientation:** Portrait
- **Reference resolution:** 1080 × 1920
- **Dependencies:** TextMeshPro + Unity uGUI Canvas + stock
  `com.unity.modules.*` only. **No third-party packages.**
- **Architecture:** ScriptableObject data-driven, procedural UI
  (no hand-authored Canvas prefabs).

---

## Hard differences from the Grades 1-3 build

Children in KG 1 and KG 2 are typically **pre-readers or early
readers**. You must adapt the UX accordingly. The following rules are
non-negotiable:

| Concern | KG-edition requirement |
|---|---|
| Text | Minimised. Every screen must work for a child who can't read. Every text label must have a tappable speaker icon that reads it aloud. |
| Buttons | Minimum **160×160 px** tap target (was 88×88 in MathEdu). Big, rounded, finger-friendly. |
| Audio narration | Mandatory on every prompt and every choice. Use procedurally generated TTS-like clips if no .wav files are provided, or short pre-recorded clips when present. Auto-plays the prompt when a question appears. |
| Question prompts | Picture-first. Show concrete objects (apples, balls, stars). Use ≤ 6 words of text. |
| Multiple choice | Drag-and-drop **or** big picture buttons. Never four equally-sized text-only options. |
| Feedback | Loud, joyful "Yay!" voice clip on correct; gentle "Try again!" on wrong (no buzzer, no red flash). Pre-readers shouldn't feel scolded. |
| Mascot | A persistent friendly mascot character (e.g. "Mathy the Owl") appears at the corner of every screen, animates on correct/wrong, gives audio narration. |
| Timers | **Forbidden.** No Quiz Mode, no Speed Round. Developmentally inappropriate. |
| Difficulty curve | Linear and gentle. 10 levels per subject, not 20. |
| Progress | Sticker-collection metaphor instead of XP / star ratings. Every completed lesson rewards 1-3 stickers. |
| Reading | Never. Use icons, emoji, and audio for all interactive elements. |

---

## Math curriculum

### KG 1 (ages 3-5, "Pre-K")

| Subject | Levels 1-3 | Levels 4-7 | Levels 8-10 |
|---|---|---|---|
| **Number Recognition** | Tap the "3" out of 4 numbers (1-5) | 1-10 | 1-15 |
| **Counting Objects** | Count 1-3 items, tap correct number | Count 4-7 items | Count 8-10 items |
| **Shape Recognition** | Circle, square, triangle (text-free, just shapes) | + rectangle, oval | + heart, star |
| **Colors** | Tap the red object | + 3-4 colours per round | Sort by colour mini-game |
| **Size Comparison** | Big vs small (tap the bigger) | Long vs short, tall vs short | Heavy vs light (visual cues) |
| **Position Words** | In / out of the box | Up / down | Left / right (with arrows) |
| **Simple Patterns** | AB (red, blue, red, blue, ?) | AAB | ABB |
| **Matching** | Match identical pictures | Match shadow to object | Match by category (animals/foods) |

### KG 2 (ages 5-6, "Kindergarten")

| Subject | Levels 1-3 | Levels 4-7 | Levels 8-10 |
|---|---|---|---|
| **Number Recognition** | 1-10 (drag the matching numeral onto pictures) | 1-15 | 1-20 |
| **Counting** | Count up to 10 objects | Skip-count by 2 (visual: "every other apple lights up") | Skip-count by 5 |
| **Number Comparison** | Which is more? (3 vs 5 apples) | Which is less? | Equal / not equal |
| **Simple Addition** | Within 5 (visual: 2 apples + 1 apple) | Within 10 | Within 10 with numerals |
| **Simple Subtraction** | Within 5 (visual: 4 apples - 1 = 3) | Within 10 | Within 10 with numerals |
| **Shapes** | Identify 2-D shapes by name (audio cue) | Count sides | Match shapes to real-world objects |
| **Patterns** | Continue an AB/ABB pattern | Find the missing piece | Create your own pattern (drag tiles) |
| **Time** | Day vs night | Morning / afternoon / evening | Times of day matched to activities (sleep, school, dinner) |
| **Money** | Recognise penny / nickel / dime / quarter (size + colour) | Match coin to value | Count to 10¢ |

### Question count per level

**8 questions per level**, not 10. Pre-readers tire faster.

---

## Game screens

Mirror the Grades 1-3 architecture but with these scene replacements:

| Original | KG version | Difference |
|---|---|---|
| Bootstrap | Bootstrap | Same — splash + route |
| PlayerSetup | PlayerSetup | Bigger fonts. Avatar picker uses 6 animal mascots (not 10). Audio narration: "What's your name?" "Pick your buddy!" "How old are you?" (KG 1 vs KG 2 instead of Grade 1/2/3). |
| MainMenu | MainMenu | Avatar mini + name + sticker count (not stars/XP). 8 subject cards laid out 2×4. Each card has a big animal picture, no text required. |
| LevelSelect | LessonMap | Renamed. A horizontal "path" of 10 lily-pad / stepping-stone tiles. Locked tiles show a sleeping animal. |
| ModeSelect | **REMOVED** | KG only has one mode: a gentle guided lesson. No mode-picker scene. |
| LearnMode | LessonMode | The only gameplay scene. Guided lesson with audio narration + drag-and-drop. |
| PracticeMode / QuizMode / StoryMode / SpeedRound | **REMOVED** | Not age-appropriate. |
| Results | StickerReward | Renamed. Mascot dances; child taps a present box that opens with 1-3 stickers. Audio: "Great job!" |
| Settings | Settings | Music / SFX toggle, volume sliders, **parent-only PIN gate** to enter Settings at all. |
| ParentalDashboard | ParentalDashboard | Same gate + same stats; KG metric is "minutes played" + "stickers collected" + "favourite subject". |

**11 scenes total** (down from 13).

---

## Architecture (mirror MathEdu, with KG-specific extensions)

### ScriptableObjects (new + adapted)

| SO | Purpose |
|---|---|
| `KGMathDatabase` | Root — `List<KGGradeData>` with 2 entries (KG 1, KG 2). |
| `KGGradeData` | KG 1 or KG 2; `List<KGSubjectData>`. |
| `KGSubjectData` | One subject; `List<KGLevelData>` (10 entries). Holds mascot, lesson colour, audio intro path. |
| `KGLevelData` | One level; `List<KGQuestion>` (8 entries). Holds optional audio intro clip. |
| `KGQuestion` | Plain class — `prompt`, `promptAudioId`, `options[]` (each option has `text`, `imageId`, `audioId`), `correctIndex`, `visual` (DragDrop / TapPicture / TapNumber / Match / Sort). |
| `MascotLibrary` | The 6 friendly animal mascots, each with sprite + idle/correct/wrong animation hashes. |
| `StickerLibrary` | Collectable stickers — sprite + name. Awarded by `KGProgressManager`. |
| `AudioBank` | Maps audio ids (e.g. "yay", "try-again", "count-to-three") to AudioClips. Optional, with procedural fallback. |
| `UITheme` | Same as MathEdu — sprite overrides. |

### New managers

| Manager | Purpose |
|---|---|
| `KGGameManager` | Root singleton, same lifecycle as MathEdu's `GameManager`. |
| `KGAudioManager` | Adds **NarrationManager** behaviour: speaks question prompts on appearance, speaks each option on hover/touch, voiceovers feedback. |
| `KGProgressManager` | Tracks sticker collection instead of XP/stars. |
| `MascotController` | Persistent DontDestroyOnLoad mascot that follows the player across scenes. Idle wiggle, dance on correct, slump on wrong. |

### Reused managers (unchanged from MathEdu)

`UIManager`, `HapticManager`, `VFXManager`, `SettingsManager`,
`ParentalDashboardManager`, `SaveSystem`.

### Reused UI helpers

`UIFactory`, `SafeAreaHandler`, `FadeOverlay`, `DefaultSprite`,
`ToggleSwitch`, `PasswordDialog`. Add:

- `BigDragSource` — a draggable image with a "bounce-back-if-not-on-target" animation.
- `DragTarget` — a drop zone that highlights when something is hovering.
- `StickerCard` — the sticker-display widget for the StickerReward scene.

---

## Gameplay loop

For each level:

1. **Audio intro** — mascot voice plays `KGLevelData.introAudioId`
   ("Let's count some apples!").
2. **Visual intro** — a card slides in showing the topic ("Count the apples").
3. **Question 1**:
   - Prompt visible as picture + audio narration.
   - 2–4 large picture / number buttons (or drag sources).
   - Child taps or drags onto a target.
   - On correct → mascot dances, audio "Yay!", green checkmark VFX.
   - On wrong → mascot slumps, audio "Hmm, try again!", gently
     re-enables the buttons.
   - **Always retry until correct** — no failure state, no scoring.
4. **Repeat** for the remaining questions.
5. **End of level**: 1-3 stickers awarded based on first-try accuracy
   (3 = all first-try, 2 = some retries, 1 = lots of retries).
6. **StickerReward scene** plays.

---

## Drag-and-drop interaction (the most important new widget)

Define `BigDragSource` and `DragTarget` so:

- `BigDragSource` extends `MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler`.
- On drag start, it animates a scale to 1.15 and increases the sorting order
  so it appears above siblings.
- During drag, it follows the pointer.
- On end: if a `DragTarget` is under the pointer and accepts this source's
  payload, the target fires `onDropped(source)` and the source destroys
  itself with a "settle" tween. Otherwise the source tweens back to its
  origin position.
- All animations use coroutines + `Time.unscaledDeltaTime` — no tween
  packages.

`DragTarget` exposes `Accepts(BigDragSource)` so subjects like
"match shadows to objects" can validate the payload.

---

## Audio narration

Every text on screen has a hidden `audioId`. `KGAudioManager.PlayNarration(id)`
plays the matching AudioClip from `AudioBank`. If the bank is missing or
the id isn't registered, a **procedural beep envelope** plays as a
fallback. Always present, never silent.

Sample ids (KG should at minimum support):

- `intro_kg1_addition_l1`, `intro_kg1_addition_l2`, …
- `prompt_count_apples`, `prompt_count_balls`, `prompt_tap_red`
- `option_1`, `option_2`, … `option_10`
- `option_red`, `option_blue`, `option_green`, …
- `feedback_yay`, `feedback_great`, `feedback_amazing`
- `feedback_try_again`, `feedback_almost`
- `sticker_unlocked`, `level_complete`, `welcome`

Drop matching .wav files into `Assets/Resources/Audio/` and they
override the procedural fallback per name.

---

## Mascot persistent overlay

A single GameObject `[Mascot]` is `DontDestroyOnLoad` (sorting order
above the FadeOverlay) and survives all scene transitions. It listens to:

- `KGProgressManager.OnAnswerCorrect` → play "dance" animation + voice.
- `KGProgressManager.OnAnswerWrong` → play "slump" animation + voice.
- `KGProgressManager.OnLevelComplete` → play "celebrate" animation.

The mascot has 4 states (Idle / Talk / Happy / Sad). Each state is a
simple sprite-swap animation built with a coroutine — no Animator
component required for the bootstrap version.

---

## Sticker reward system

`StickerLibrary` holds 30+ collectable stickers (animal, food, vehicle
themed — child-friendly). Completing a level grants 1-3 unique stickers
(never duplicates). The Parental Dashboard shows the full sticker
collection as a wall.

`KGProgressManager.AwardStickers(level, count)`:

1. Picks `count` un-collected stickers from the library.
2. Adds them to `KGPlayerProfile.collectedStickers`.
3. Fires `OnStickerAwarded` events.
4. Saves the profile.

---

## Parental gate

KG users must not be able to bump into Settings or the Dashboard
accidentally. Both screens require a parental gate:

- The gate is a **"hold for 3 seconds + multiply two single-digit
  numbers" challenge** (you can't tap-spam through it).
- Default question is procedurally generated (e.g. `4 × 6 = ?`).
- Correct answer → Settings / Dashboard reveals.
- Wrong answer → back to MainMenu.

(Avoid a 4-digit PIN here because parents often forget it — the
multiplication question is universally solvable for any literate adult.)

---

## Step-by-step execution plan (for the AI agent)

When you start building, work in this order:

### Step 1 — Repo bootstrap

1. Create branch `kg-edition` (or new repo `MathEdu-KG-Unity-Game`).
2. Copy `Packages/manifest.json` from MathEdu — do **not** modify it.
3. Copy `Assets/Scripts/MathEdu.Runtime.asmdef` and
   `Assets/Scripts/Editor/MathEdu.Editor.asmdef`. Rename the root
   namespace to `MathEduKG`.
4. Copy the entire `Assets/Scripts/UI/` folder (it's all reusable
   — UIFactory, DefaultSprite, FadeOverlay, etc.).

### Step 2 — Data model

Create the new ScriptableObject definitions under
`Assets/Scripts/Data/`:

- `KGQuestion` (plain class), `KGLevelData`, `KGSubjectData`,
  `KGGradeData`, `KGMathDatabase`, `KGSubject` enum
  (NumberRecognition, Counting, ShapeRecognition, Colors,
  SizeComparison, PositionWords, SimplePatterns, Matching,
  SimpleAddition, SimpleSubtraction, Shapes, Time, Money,
  NumberComparison).
- `KGPlayerProfile` with `collectedStickers: List<string>` and
  `levelStickerCounts: Dictionary<string,int>` instead of stars.
- `MascotLibrary`, `MascotData`, `StickerLibrary`, `StickerData`,
  `AudioBank`.

### Step 3 — Managers

- `KGGameManager` — same pattern as `GameManager`.
- `KGAudioManager` — add `PlayNarration(audioId)` API.
- `KGProgressManager` — sticker-collection model.
- `MascotController` — DontDestroyOnLoad mascot.
- Reuse `UIManager`, `VFXManager`, `HapticManager`, `SaveSystem`.

### Step 4 — Question generator

A new `KGQuestionGenerator` under `Assets/Scripts/Utility/`:

```csharp
public static List<KGQuestion> Generate(int kg, KGSubject subject, int level, int seed = 0)
{
    var rng = new System.Random(seed != 0 ? seed : Hash(kg, subject, level));
    switch (subject)
    {
        case KGSubject.NumberRecognition: return NumberRecognition(kg, level, rng);
        case KGSubject.Counting:          return Counting(kg, level, rng);
        case KGSubject.ShapeRecognition:  return ShapeRecognition(kg, level, rng);
        // … etc.
    }
}
```

Each method returns 8 picture-first questions. For NumberRecognition:

```csharp
var q = new KGQuestion
{
    prompt          = "Tap the number 3",
    promptAudioId   = "prompt_tap_number_3",
    visual          = KGVisual.TapNumber,
    options         = new[]
    {
        new KGOption { text = "1", audioId = "option_1" },
        new KGOption { text = "3", audioId = "option_3" },
        new KGOption { text = "5", audioId = "option_5" },
        new KGOption { text = "7", audioId = "option_7" },
    },
    correctIndex    = 1,
};
```

### Step 5 — UI widgets

Build the new draggable widgets:

- `BigDragSource` + `DragTarget` (see specs above).
- `StickerCard` — sticker reveal animation (slide in, scale pop, audio).
- `MascotBubble` — speech bubble next to the mascot.
- `LessonMapTile` — a stepping-stone tile for LessonMap scene.

### Step 6 — Scenes

Create the 11 scene managers under `Assets/Scripts/Modes/`:

`BootstrapManager`, `PlayerSetupManager`, `MainMenuManager`,
`LessonMapManager`, `LessonModeManager`, `StickerRewardManager`,
`SettingsManager`, `ParentalGateManager`, `ParentalDashboardManager`.

Each builds its UI procedurally in `Start()` via `UIFactory`. No
hand-authored scene YAML.

### Step 7 — Editor menus

`KGDatabaseBuilderMenu.cs`:

- **MathEdu KG / Build Default Database** — materialise the SOs.
- **MathEdu KG / Build Default Mascot Library** — 6 mascots.
- **MathEdu KG / Build Default Sticker Library** — 30 stickers.
- **MathEdu KG / Build All Scenes** — 11 scenes.
- **MathEdu KG / Wipe Generated Database**.
- **MathEdu KG / Reset Player Progress**.

### Step 8 — Polish

- Add the parental multiplication gate.
- Wire audio narration to every text label.
- Connect the mascot animations to gameplay events.
- Verify safe-area on iPhone notch + Pixel hole-punch.
- Run **MathEdu KG / Build Default Database**, **Build Default Mascot
  Library**, **Build Default Sticker Library**, **Build All Scenes**.
- Press Play on `Bootstrap.unity`. Walk through every flow.

### Step 9 — Push to GitHub

10 commits, mirror the original MathEdu structure:

```
fix: game loop trace bugs [BUG-1..N]
fix: null safety, KGSession persistence, LessonMap unlock display
fix: PlayerSetup mascot grid, KGDatabaseBuilder all subjects
fix: LessonMode coroutine, Sticker reward animation, Parental gate
fix: MainMenu sticker count display, back navigation
feat: Audio narration system, MascotController, all 6 mascots
feat: BigDragSource + DragTarget, drag-to-target interactions
feat: KG 1 + KG 2 curriculum, 14 subjects, 10 levels × 8 questions
fix: empty state handling, edge case null guards
docs: README — complete KG feature list, build guide
```

---

## Definition of Done

The agent is finished when a developer can:

```bash
git clone <kg-repo>
# Open in Unity 6000.4.4f1
# MathEdu KG → Build Default Database
# MathEdu KG → Build Default Mascot Library
# MathEdu KG → Build Default Sticker Library
# MathEdu KG → Build All Scenes
# Open Assets/Scenes/Bootstrap.unity, press ▶ Play
```

And without touching any other editor control:

- ✅ Splash → PlayerSetup (or MainMenu if already set up)
- ✅ Pick a mascot, type your name (or have a parent type it), tap "Go!"
- ✅ MainMenu shows mascot + sticker count + 8 subject cards (KG 1)
- ✅ Tap Counting → LessonMap shows 10 stepping-stone tiles, Level 1 awake, 2-10 sleeping
- ✅ Tap Level 1 → LessonMode plays the audio intro, shows the first question
- ✅ Tap a correct answer → mascot dances, audio "Yay!"
- ✅ Tap a wrong answer → mascot slumps, audio "Try again!", buttons re-enable
- ✅ Complete the level → StickerReward scene, 1-3 stickers revealed
- ✅ Back to LessonMap → Level 2 is now awake
- ✅ Parental Dashboard requires multiplication challenge (e.g. "4 × 6 = ?") before opening
- ✅ No NullReferenceException in Console during any flow

---

## Execution rules (copied verbatim from the MathEdu brief)

- **Read before writing** — every file before touching it.
- **Never break existing working code to add new features.**
- **No third-party packages** — verify manifest.json unchanged.
- **No TODOs in committed code** — everything fully implemented.
- **If a fix would take more than 50 lines, it was probably not working
  before — rewrite the method cleanly rather than patching.**
- **Make all decisions independently — do not ask for clarification.**

---

## Curriculum sources for the agent

- Common Core State Standards for Mathematics, Kindergarten (K.CC, K.OA,
  K.MD, K.G domains).
- NCTM (National Council of Teachers of Mathematics) Pre-K to Grade 2
  Focal Points.
- Khan Academy Kids curriculum scope and sequence.

Use these to validate question difficulty and pacing if you have web
access. Otherwise rely on the curriculum table above.

---

## Final word

A KG-edition is NOT a "smaller MathEdu". It is a **fundamentally
different UX** built on the same engine. The drag-and-drop interactions,
the mascot, the audio-first narration, the elimination of timers, and
the sticker-collection metaphor are not optional polish — they are the
product. Build them first, build them well, and the rest of the game
will fall into place around them.
