# AI Agent Prompt — Build a MathEdu-style Game for **Grades 4 & 5**

> Paste the contents of this file into an AI coding agent (Claude, GPT,
> Cursor, etc.) to have it build a Grade 4-5 edition of the MathEdu game
> using the same architecture as the original Grades 1-3 project. The
> prompt is self-contained: it includes the curriculum, the UX shifts
> needed for older children, the technical scaffolding, and the
> workflow rules.

---

## Role

You are a world-class expert combining four specialties: (1) **upper-
elementary math curriculum design for ages 9-11** (Grade 4 ≈ 9-10,
Grade 5 ≈ 10-11); (2) **educational game design with proven engagement
mechanics for fluent readers and screen-natives**; (3) **senior Unity
C# engineer specialising in Unity 6000.4.4f1, mobile UI/UX, and
ScriptableObject-driven architectures**; (4) **assessment design — hint
scaffolding, mastery progression, error analysis**.

You will create a brand-new repository `MathEdu-G45-Unity-Game` (or a
new branch inside an existing repo) that mirrors the structure of the
Grades 1-3 MathEdu game documented in
[`UNITY_PROJECT_STRUCTURE.md`](./UNITY_PROJECT_STRUCTURE.md), but
**advanced for upper-elementary cognitive abilities**.

---

## Target environment

- **Unity:** 6000.4.4f1
- **Platforms:** Android (min SDK 22) + iOS (Xcode 15+)
- **Orientation:** Portrait (allow Landscape unlocks for tablet support)
- **Reference resolution:** 1080 × 1920
- **Dependencies:** TextMeshPro + Unity uGUI Canvas + stock
  `com.unity.modules.*` only. **No third-party packages.**
- **Architecture:** ScriptableObject data-driven, procedural UI.

---

## Hard differences from the Grades 1-3 build

Children in Grade 4 and 5 are **fluent readers, screen-natives, and
peer-aware**. They want challenge, autonomy, and meaningful feedback.
The UX must respect that:

| Concern | G4-5 requirement |
|---|---|
| Text density | Higher. Word problems can be 2-3 sentences. Multiple representations (numeric + visual + word). |
| Visual complexity | Coordinate grids, charts, fraction bars, area models, decimal grids. |
| Manipulatives | Interactive — drag fraction bars to compare, plot points on a grid, slide a decimal pointer. |
| Hint system | Scaffolded across 3 levels: *(1) "Think about…", (2) Step-by-step breakdown, (3) Worked example.* Each hint reveal costs XP. |
| Show-work | A scratchpad area where the child can sketch / type intermediate steps. Not graded, but available. |
| Timed challenges | Yes — Quiz mode 30 s → 15 s curve, Speed Round 6 s → 2 s. Optional Tournament Mode (against the clock for global leaderboard within the device). |
| Mastery tracking | Per-skill mastery score (0-100 %). A "Mastered" badge requires 90 %+ accuracy over 5 most recent attempts. Mastery degrades 1 pp per week of inactivity. |
| Achievement system | XP bands → "Apprentice → Adept → Expert → Master → Sage" titles. Pet-style "Math Familiar" that levels up with you. |
| Leaderboards | Local-only by default (per device). |
| Calculator | Toggleable, allowed for some problem categories (long division "show your steps" mode) but disabled for fluency-test categories. |
| Glossary | Tappable bolded vocab words pop a definition card. |
| Notebook | Per-subject notes auto-collated from explanations of missed questions. |

---

## Math curriculum

### Grade 4 (ages 9-10)

| Subject | Levels 1-5 | Levels 6-10 | Levels 11-15 | Levels 16-20 |
|---|---|---|---|---|
| **Multi-digit Operations** | Up to 4-digit + / − | 2-digit × 1-digit | 3-digit × 2-digit | 4-digit × 2-digit, long division |
| **Place Value** | Through millions | Compare / order | Round to nearest 10/100/1000 | Estimate sums / differences / products |
| **Factors / Multiples** | Find factors of N≤50 | Multiples to 100 | Prime vs composite (≤100) | Factor pairs in word context |
| **Fractions** | Identify, compare | Equivalent (multiply/divide top & bottom) | + / − with like denominators | Mixed numbers + / − |
| **Decimals** | Recognise tenths (0.1, 0.2) | Hundredths, compare | Order decimals | Decimal-fraction equivalence |
| **Measurement** | Convert within metric | Convert within customary | Area / perimeter of complex shapes | Word problems involving conversions |
| **Geometry** | Point / line / ray / angle classification | Parallel / perpendicular | Symmetry | Area of composite shapes |
| **Data / Graphs** | Bar charts | Line plots | Mean (intro) | Multi-step word problems with graphs |
| **Word Problems** | Single-step | Two-step | Multi-step with extra info | Multi-step with missing info (estimate or "not enough info") |

### Grade 5 (ages 10-11)

| Subject | Levels 1-5 | Levels 6-10 | Levels 11-15 | Levels 16-20 |
|---|---|---|---|---|
| **Decimal Operations** | + / − to thousandths | × by 10, 100, 1000 (place-value shift) | × decimals × whole | × decimals × decimals; ÷ decimals |
| **Fraction Operations** | + / − unlike denominators (LCM) | × fractions | × mixed numbers | ÷ fractions, ÷ by unit fractions |
| **Volume / 3-D** | Count unit cubes | V = l × w × h | Word problems | Composite figures (split into rectangular prisms) |
| **Coordinate Plane** | Plot points (Q1) | Read coordinates from plotted points | Draw line segments by coordinates | Distance / midpoint on the plane (intro) |
| **Order of Operations** | PEMDAS basics | Multi-operation expressions | Parenthesis nesting | Expressions with variables |
| **Powers of 10** | 10², 10³ | 10⁰ to 10⁶ | Convert between standard / scientific (intro) | Use exponents in unit conversions |
| **Measurement** | Convert customary ↔ customary | Convert metric ↔ metric | Cross-system word problems | Time-zone arithmetic |
| **Statistics** | Mean | Median | Mode | Range; choose-the-best-stat questions |
| **Pre-Algebra** | Identify the variable | Solve one-step equations (x + 3 = 8) | Two-step equations (2x − 1 = 9) | Plot a simple linear pattern |

### Question count per level

**12 questions per level**, not 10 (longer attention span, more variety
needed to prevent pattern-matching).

### Difficulty within a level

L1–L5: warm-up / drill (mostly numeric).
L6–L10: word problems with single-step reasoning.
L11–L15: multi-step word problems, multiple representations.
L16–L20: challenge tier — real-world contexts, "two truths and a lie"
style distractors, partial-credit questions where applicable.

---

## Game screens

Mirror the Grades 1-3 architecture with these additions / changes:

| Original | G4-5 version | Difference |
|---|---|---|
| Bootstrap | Bootstrap | Same |
| PlayerSetup | PlayerSetup | Adds a "Familiar" picker (math-themed pet: Numeo the otter, Algebra the dragon, etc.) after the standard avatar. Adds a difficulty preference toggle ("Auto-adjust to my level" or "Always start at L1"). |
| MainMenu | MainMenu | Header: avatar + name + familiar + level (e.g. "Apprentice L7") + total XP. Tile grid shows mastery % per subject (`87%` badge in the corner) — not just stars. |
| LevelSelect | LevelSelect | 20 tiles arranged in 4 rows of 5. Each tile shows a mastery dial (radial fill from 0-100%) plus stars (0-3). |
| ModeSelect | ModeSelect | Adds a sixth mode: **Tournament** (timed 20-question gauntlet across the whole subject, scores leaderboard, no hints). |
| LearnMode | LearnMode | Adds **example videos** placeholder (drop .mp4 files into Resources/Videos to enable) + a worked-example walkthrough with "Next step →" pacing. |
| PracticeMode | PracticeMode | Adds the 3-level hint system + scratchpad. |
| QuizMode | QuizMode | Same as MathEdu but timer curve is steeper (30 s → 15 s). |
| StoryMode | StoryMode | Multi-chapter — each level continues a single narrative arc. "Math Detective" theme: solve cases (e.g. "Detective, the bank robber dropped a sequence …"). |
| SpeedRound | SpeedRound | 6 s → 2 s curve. Endless pool. |
| Results | Results | Per-question breakdown ("You got 8/12 correct. Here's what tripped you up on Q5: …"). Plus mastery delta ("+3% mastery in Multiplication"). |
| Settings | Settings | Adds "Calculator allowed by default" toggle. |
| ParentalDashboard | ParentalDashboard | Mastery heat map per subject × grade. Time-spent line chart. Error analysis ("Most common mistake: forgot to carry"). |
| **(new)** TournamentLobby | TournamentLobby | Choose subject + difficulty band. Shows the device's recent leaderboard. |
| **(new)** GlossaryViewer | GlossaryViewer | Tap-to-define vocab. Cross-linked. Accessible from any LearnMode card. |
| **(new)** NotebookViewer | NotebookViewer | Auto-collated notes per subject from missed-question explanations. |

**16 scenes total** (up from 13).

---

## Architecture additions

### New ScriptableObjects

| SO | Purpose |
|---|---|
| `G45MathDatabase` | Root — `List<G45GradeData>` with 2 entries (Grade 4, Grade 5). |
| `G45GradeData` | Grade-level content. |
| `G45SubjectData` | One subject. |
| `G45LevelData` | One level; 12 questions, optional video clip path, mastery weighting per skill tag. |
| `G45Question` | Plain class with new fields: `skillTags[]` (e.g. "place-value", "carry"), `hint1`, `hint2`, `workedExample`, `calculatorAllowed`. |
| `FamiliarLibrary` | The pet familiars (5+ creatures). |
| `FamiliarData` | One familiar — sprite, idle/happy/leveling-up animations, growth thresholds (XP → level 1, 5, 20, 50). |
| `GlossaryDatabase` | Term ↔ definition mappings. |
| `MasteryProfile` | Per-(subject, skill) mastery score, attempt history. |

### New plain serialisable classes

```csharp
[Serializable] public class MasterySkill
{
    public string subjectKey;     // "fractions"
    public string skillTag;       // "unlike-denominators"
    public float  score;          // 0..100
    public List<bool> recentAttempts = new();   // most-recent-5
    public string lastAttemptIsoUtc;
}

[Serializable] public class TournamentScore
{
    public string subjectKey;
    public int    score;
    public int    correct;
    public float  elapsedSeconds;
    public string playerName;
    public string dateIsoUtc;
}
```

### New managers

| Manager | Purpose |
|---|---|
| `G45GameManager` | Root singleton with `Familiar` reference + `MasteryProfile` on the profile. |
| `MasteryManager` | Computes mastery per skill tag from recent attempts; decays inactive skills. |
| `HintManager` | Serves the 3-tiered hints; tracks how many times each tier was used per session. |
| `GlossaryManager` | Resolves vocab tags in TMP rich text to clickable links that open a `GlossaryCard`. |
| `NotebookManager` | Appends a note (level + question + worked solution) to the per-subject notebook on every miss. |
| `TournamentManager` | Runs the 20-question endless gauntlet, maintains per-device leaderboard. |

### Reused (unchanged from MathEdu)

`UIManager`, `AudioManager`, `HapticManager`, `VFXManager`,
`SaveSystem`, `UIFactory`, `Timer`, `AnswerButton`, `ProgressBar`,
`FadeOverlay`, `SafeAreaHandler`.

---

## New UI widgets

These exist in addition to the MathEdu set:

| Widget | Purpose |
|---|---|
| `MasteryRing` | Radial progress ring (0-100%) — used on LevelSelect tiles and MainMenu cards. |
| `Scratchpad` | A canvas the child can sketch on with finger / mouse (TouchScript-like) plus a free-text TMP field. Not graded — purely a workspace. |
| `HintCard` | Slides up from the bottom showing hint level 1 / 2 / 3 with an XP cost displayed. |
| `WorkedExampleCard` | A multi-step "Next →" walkthrough card. |
| `CoordinatePlaneWidget` | Renders the first-quadrant grid with snap-to-grid plotting. Used in Grade 5 coordinate problems. |
| `FractionBar` | Draggable fraction bar — useful for visual comparison and addition with unlike denominators. |
| `DecimalGrid` | 10×10 grid where each cell represents 0.01. Children tap to fill. |
| `ChartRenderer` | Bar chart / line plot / pie chart driven by `q.visualPayload[]`. |
| `LeaderboardRow` | Tournament leaderboard entry row. |
| `GlossaryCard` | A pop-up definition card with cross-linked terms. |
| `NotebookEntry` | One entry in the per-subject notebook viewer. |
| `FamiliarPanel` | Persistent corner panel showing the pet's current level + an XP bar. Tapping it opens the FamiliarViewer. |

---

## Hint system (the most important new feature)

`HintManager.RequestHint(questionId)`:

1. First call → returns `hint1` ("Think about what you need to find first.").
2. Second call → returns `hint2` ("Step 1: convert both fractions to twelfths. Step 2: …").
3. Third call → returns `workedExample` (the answer with each step shown).

Each tier costs increasing XP:

| Tier | Cost |
|---|---|
| Hint 1 | 1 XP |
| Hint 2 | 3 XP |
| Worked example | 5 XP (also caps level reward at 2★ max) |

The XP cost is deducted from the per-level XP reward, not from total
profile XP. So a child can use hints liberally for learning without
fearing they'll be "punished" beyond losing one star on that level.

---

## Mastery scoring

`MasteryManager.RecordAttempt(subjectKey, skillTag, correct)` updates
the rolling mastery:

```csharp
recentAttempts.Add(correct);
if (recentAttempts.Count > 5) recentAttempts.RemoveAt(0);
score = 100f * recentAttempts.Count(c => c) / recentAttempts.Count;
lastAttemptIsoUtc = DateTime.UtcNow.ToString("o");
```

Once `score >= 90 && recentAttempts.Count >= 5`, the
`{skill}_mastered_{grade}` badge is awarded.

Inactivity decay (called from `G45GameManager.Awake` once per day):

```csharp
int daysSinceLast = (DateTime.UtcNow.Date - last.Date).Days;
score = Math.Max(0, score - daysSinceLast);
```

So a skill at 95 % drops to 88 % after a week of inactivity, requiring
the child to revisit it to stay "Mastered".

---

## Tournament mode

`TournamentMode` is a new mode under `Modes/TournamentMode/`:

- 20 questions drawn from all 20 levels of one subject, weighted toward
  un-mastered skills.
- 12-second timer per question.
- No hints, no calculator (even if allowed in Settings).
- Final score = `correct × 10 + timeBonus`.
- Inserts the score into a per-device leaderboard
  (`PlayerProfile.tournamentScores: List<TournamentScore>`).
- The lobby scene shows the top 10 scores ever recorded on the device,
  the player's best, and a "Start Tournament" CTA.

This is the highest-engagement mode for upper-elementary kids who want
to flex their skill.

---

## Step-by-step execution plan (for the AI agent)

### Step 1 — Repo bootstrap

1. Create branch `g45-edition` (or new repo `MathEdu-G45-Unity-Game`).
2. Copy `Packages/manifest.json` from MathEdu — do **not** modify it.
3. Copy the asmdef files. Rename the root namespace to `MathEduG45`.
4. Copy the entire `Assets/Scripts/UI/` folder.
5. Copy `Assets/Scripts/Utility/SaveSystem.cs` (the JSON ⇄ file logic
   is reused as-is, just with a different filename:
   `g45_profile.json`).

### Step 2 — Data model

Create the new ScriptableObject definitions under
`Assets/Scripts/Data/`:

- `G45Question`, `G45LevelData`, `G45SubjectData`, `G45GradeData`,
  `G45MathDatabase`, `G45Subject` enum.
- `G45PlayerProfile` with the new fields:
  `xp`, `familiarId`, `familiarXp`, `masteryProfile: List<MasterySkill>`,
  `tournamentScores: List<TournamentScore>`, `notebookEntries:
  Dictionary<string, List<NotebookEntry>>`, `glossaryFamiliarTerms:
  HashSet<string>`.
- `FamiliarLibrary`, `FamiliarData`, `GlossaryDatabase`, `GlossaryTerm`.

### Step 3 — Managers

- `G45GameManager` — same pattern as MathEdu's `GameManager` + applies
  daily mastery decay in `Awake`.
- `MasteryManager`, `HintManager`, `GlossaryManager`, `NotebookManager`,
  `TournamentManager`.
- Reuse `UIManager`, `AudioManager`, `HapticManager`, `VFXManager`,
  `SaveSystem`.

### Step 4 — Question generator

A new `G45QuestionGenerator` with curriculum-driven generators per
subject. Each question must populate:

- `prompt`, 4 options, `correctIndex`
- `hint1` (one-line nudge), `hint2` (step-by-step), `workedExample`
  (full solution)
- `skillTags[]` (1-3 tags per question)
- `calculatorAllowed` (true for word problems, false for fluency tests)
- `visual` and `visualPayload` for charts / grids / etc.

Example for Grade 5 Fractions L11 (unlike denominators):

```csharp
int den1 = 3, den2 = 4;
int num1 = rng.Next(1, den1);
int num2 = rng.Next(1, den2);
int lcm = LCM(den1, den2);
int n1Scaled = num1 * (lcm / den1);
int n2Scaled = num2 * (lcm / den2);
int ansNum = n1Scaled + n2Scaled;
string ans = $"{ansNum}/{lcm}";

var q = new G45Question
{
    prompt       = $"What is {num1}/{den1} + {num2}/{den2}?",
    options      = WordOptions(ans, new[] {
        $"{num1 + num2}/{den1 + den2}",        // common mistake
        $"{num1 + num2}/{lcm}",                // numerators added, denom converted
        $"{n1Scaled + n2Scaled}/{den1 * den2}" // didn't simplify denom
    }, rng),
    correctIndex = …,
    hint1        = "These fractions don't have the same bottom number yet.",
    hint2        = $"Step 1: find a common denominator ({lcm}).\n" +
                   $"Step 2: convert {num1}/{den1} → {n1Scaled}/{lcm}.\n" +
                   $"Step 3: convert {num2}/{den2} → {n2Scaled}/{lcm}.\n" +
                   $"Step 4: add the numerators.",
    workedExample= $"{num1}/{den1} + {num2}/{den2} = " +
                   $"{n1Scaled}/{lcm} + {n2Scaled}/{lcm} = {ans}.",
    skillTags    = new[] { "fractions", "unlike-denominators", "lcm" },
    calculatorAllowed = false,
    difficulty   = QuestionDifficulty.Medium,
    visual       = QuestionVisual.Fraction,
    visualPayload= new[] { num1, den1, num2, den2 }
};
```

### Step 5 — UI widgets

Build the new widgets listed above. The most complex are:

- `Scratchpad` — capture pointer/touch events, draw on a
  `Texture2D` then push it to a `RawImage`. Clear button.
- `CoordinatePlaneWidget` — render a 10×10 grid with axis labels, snap
  pointer to nearest integer coordinate, fire `onPointPlotted(x, y)`.
- `MasteryRing` — TMP percentage in centre + Image with
  `Image.Type.Filled` `FillMethod.Radial360`.

### Step 6 — Scenes

Create the 16 scene managers. Most reuse MathEdu's logic with
extensions. New scenes:

- `TournamentLobbyManager` — leaderboard + Start Tournament CTA.
- `GlossaryViewerManager` — alphabetical list of terms.
- `NotebookViewerManager` — per-subject auto-collated notes.

### Step 7 — Editor menus

`G45DatabaseBuilderMenu.cs`:

- **MathEdu G4-5 / Build Default Database**
- **MathEdu G4-5 / Build Default Familiar Library**
- **MathEdu G4-5 / Build Default Glossary**
- **MathEdu G4-5 / Build All Scenes**
- **MathEdu G4-5 / Wipe Generated Database**
- **MathEdu G4-5 / Reset Player Progress**

### Step 8 — Polish

- Wire mastery rings on LevelSelect tiles and MainMenu cards.
- Connect hint CTAs to `HintManager`.
- Apply daily mastery decay on app launch.
- Tournament: implement the per-device leaderboard storage in
  `G45PlayerProfile`.
- Glossary: link bolded vocab in TMP rich text using
  `<link="LCM">least common multiple</link>` syntax + an
  `OnPointerDown` listener.
- Verify safe-area, calculator toggle, scratchpad input on touch.

### Step 9 — Push to GitHub

15-20 commits, grouped logically:

```
fix: game loop trace bugs [BUG-1..N]
fix: standalone-scene safe init, GameSession + SessionResult
fix: PlayerSetup avatar + familiar grid, full database wiring
fix: Timer visual, Results breakdown, Settings/Dashboard PIN
fix: MainMenu mastery rings, back navigation, pause menu
feat: Hint system with 3-tier scaffolding + XP cost
feat: Mastery profile + daily inactivity decay + Mastered badge
feat: Tournament mode + per-device leaderboard
feat: Glossary + Notebook auto-collation
feat: Scratchpad + CoordinatePlane + FractionBar + DecimalGrid widgets
feat: Familiar pet that levels up with the player
feat: Grade 4 + Grade 5 curriculum (18 subjects, 20 levels × 12 questions)
feat: Story Mode multi-chapter narrative ("Math Detective")
fix: empty state handling, edge case null guards
docs: README — complete G4-5 feature list, build guide
```

---

## Definition of Done

The agent is finished when a developer can:

```bash
git clone <g45-repo>
# Open in Unity 6000.4.4f1
# MathEdu G4-5 → Build Default Database
# MathEdu G4-5 → Build Default Familiar Library
# MathEdu G4-5 → Build Default Glossary
# MathEdu G4-5 → Build All Scenes
# Open Assets/Scenes/Bootstrap.unity, press ▶ Play
```

And without touching any other editor control:

- ✅ Splash → PlayerSetup
- ✅ Set name, pick avatar, pick familiar (e.g. Numeo the otter), pick Grade 4
- ✅ MainMenu shows your familiar in the corner with an XP bar
- ✅ Subject cards show mastery rings (0% on first play, no stars yet)
- ✅ Tap Fractions → LevelSelect with 20 tiles, mastery 0% on all
- ✅ Tap Level 1 → ModeSelect with 6 modes (Learn / Practice / Quiz / Story / Speed / Tournament)
- ✅ Tap Practice → 12 questions, Hint button visible, Scratchpad accessible
- ✅ Request Hint 1 → bottom card slides up with "Think about…" + "-1 XP"
- ✅ Tap an answer correctly → mastery score updates in real time
- ✅ Get 5+ correct in a row on the same skill tag → "{skill} Mastered" badge popup
- ✅ Finish a level → Results breakdown by question + "+3% mastery in Fractions"
- ✅ Back to LevelSelect → Level 2 unlocked, mastery ring shows your progress
- ✅ Tap Tournament Lobby → leaderboard empty, start a tournament
- ✅ 20-question gauntlet with 12s timer → score saved to device leaderboard
- ✅ Tap Glossary → alphabetical term list, tap "LCM" → definition card with cross-links
- ✅ Tap Notebook → per-subject notes auto-collated from missed questions
- ✅ Settings → toggle "Calculator allowed" → calculator appears next to gameplay buttons
- ✅ Parental Dashboard (PIN-gated, 4-digit pad) → mastery heat map + error analysis
- ✅ No NullReferenceException in Console during any of the above

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

- Common Core State Standards for Mathematics, Grades 4 and 5
  (4.NBT, 4.OA, 4.NF, 4.MD, 4.G; 5.NBT, 5.OA, 5.NF, 5.MD, 5.G).
- NCTM Curriculum Focal Points for Grade 4 / 5.
- Engage NY / EngageMath grade-level modules (publicly available).

Use these to validate question difficulty and pacing if you have web
access. Otherwise rely on the curriculum table above.

---

## Final word

The Grades 4-5 edition is **not just "harder MathEdu"**. It introduces
mastery-based progression, scaffolded hinting, multiple-representation
question types, a tournament mode, and meta-systems like the glossary,
notebook, and familiar pet. These are the features that keep
upper-elementary kids engaged for the dozens of hours they need to
build true fluency.

Build the mastery system and the hint system first — every other
feature (Tournament, mastery decay, the "Mastered" badge, the level-up
familiar) hangs off them. If those two are right, the rest of the game
falls into place around them.
