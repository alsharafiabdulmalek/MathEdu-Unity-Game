# Rescue: "Build Default Database" Hung Unity

If you ran `MathEdu → Build Default Database` on an older build of this
project and Unity has been stuck on the rainbow / spinning-wait cursor for
more than ~30 seconds, this document gets you back to a playable state.

---

## Why it happened (short version)

The **previous** version of `DatabaseBuilderMenu.cs` created **~570
individual `.asset` files** (3 grades × ~9 subjects × 20 levels) on disk
*without* batching the AssetDatabase. On macOS, every `CreateAsset()`
triggered a full AssetDatabase cycle and the final `AssetDatabase.Refresh()`
re-imported the entire project. That meant 30+ minutes on a fast Mac and
up to "stuck for 10+ hours" on slower machines.

This is now fixed on `main` — see commit
`816ac31b9c7b661405202ea31649dccd0dbbb5ec`
(`perf(editor): fix Build Default Database hang — single nested-asset path`).
The new fast path produces ONE consolidated `MathDatabase.asset` and
typically takes 3–10 seconds.

---

## Recovery (in order)

### 1 — Force-quit Unity

The Editor is unresponsive because it is mid-import. It will not recover
on its own.

- **macOS:** `⌘` + `⌥` + `Esc` → select **Unity** → **Force Quit**
- If that's not enough: open Activity Monitor, search for `Unity`,
  select **Unity** (and `UnityShaderCompiler` if listed), click the
  octagonal **X** at the top, choose **Force Quit**.

### 2 — Clear the import cache

```bash
cd path/to/MathEdu-Unity-Game
rm -rf Library Logs Temp obj
```

This is safe — Unity rebuilds these on next open. Do **not** delete
`Assets/`, `ProjectSettings/`, or `Packages/`.

> If Unity created partial garbage under `Assets/ScriptableObjects/` (a
> half-finished tree of `Grade_X.asset` / `Subject_*.asset` /
> `Level_NN.asset`), leaving them there is **fine** — the new build
> wipes/overwrites them safely. If you want a clean slate, also delete
> `Assets/ScriptableObjects/Grades/` and `Assets/Resources/MathDatabase.asset`.

### 3 — Pull the fix

```bash
git pull origin main
```

Make sure you see the new menu file:
`Assets/Scripts/Editor/DatabaseBuilderMenu.cs` (with the long
"PERFORMANCE NOTE" header at the top).

### 4 — Re-open Unity

Unity will re-import the project. This first re-import after `Library/`
was deleted takes a minute or two — that is normal and unrelated to the
database build.

Wait for the Console to be quiet, then verify the new menu items appear:

- `MathEdu / Run Full Setup (DB + Avatars + Scenes)`
- `MathEdu / Build Default Database` *(now FAST — single asset)*
- `MathEdu / Build Default Avatar Library`
- `MathEdu / Build All Scenes`
- `MathEdu / Wipe Generated Database`
- `MathEdu / Reset Player Progress`
- `MathEdu / Advanced / Per-Grade Assets / Build Grade 1 Files`
- `MathEdu / Advanced / Per-Grade Assets / Build Grade 2 Files`
- `MathEdu / Advanced / Per-Grade Assets / Build Grade 3 Files`
- `MathEdu / Advanced / Per-Grade Assets / Rebuild Master Index`
- `MathEdu / Advanced / Use Runtime Database Only (no build)`
- `MathEdu / Advanced / Open Save File Location`

### 5 — Run **one** of the three options below

---

## Option A — Single click, do everything (recommended)

```
MathEdu → Run Full Setup (DB + Avatars + Scenes)
```

This sequentially runs:

1. Build Default Database (fast, ~3–10 s)
2. Build Default Avatar Library (~1 s)
3. Build All Scenes (~5 s)

Then open `Assets/Scenes/Bootstrap.unity` and press ▶ **Play**.

Each step shows its own progress bar; total time is well under a minute
on a modern Mac.

## Option B — Step by step (same result as A, but visible per step)

```
1. MathEdu → Build Default Database
2. MathEdu → Build Default Avatar Library
3. MathEdu → Build All Scenes
4. Open Assets/Scenes/Bootstrap.unity → Play
```

## Option C — No database build at all

The game is **playable without** ever clicking
`MathEdu → Build Default Database`. `GameManager.EnsureDatabase()` builds
a 4,800-question content tree in memory at startup via
`DatabaseBootstrapper.BuildInMemory()`. You only need a built asset if
you want to browse individual levels in the Inspector.

To go this route:

```
1. MathEdu → Build Default Avatar Library     (optional — also has a runtime fallback)
2. MathEdu → Build All Scenes                  (~5 s; required because Unity needs registered scenes)
3. Open Assets/Scenes/Bootstrap.unity → Play
```

The `MathEdu → Advanced → Use Runtime Database Only (no build)` menu
item surfaces this information as an in-Editor popup.

---

## What "fast" really means now

| Operation | Before (broken) | After (fixed) |
|---|---|---|
| Build Default Database | 30 min → 10+ hr | **3–10 seconds** |
| Build Default Avatar Library | ~5 s | ~1 s |
| Build All Scenes | ~10 s | ~5 s |
| Total "Run Full Setup" | — (impossible) | **< 60 seconds** |

The hang fix has three parts:

1. **Single consolidated asset.** The new build writes ONE file —
   `Assets/Resources/MathDatabase.asset` — with every Grade / Subject /
   Level stored as nested sub-assets (`AssetDatabase.AddObjectToAsset`).
   Unity imports ONE file instead of ~570.
2. **Asset-editing batching.** Every menu item now wraps work in
   `AssetDatabase.StartAssetEditing()` / `StopAssetEditing()`, so the
   per-asset import storm is suppressed during the build.
3. **No more `AssetDatabase.Refresh()` at the end.** The old code
   forced a full project reimport sweep when the build finished. The
   new code calls `ImportAsset` only on the one file it changed.

The Project window still shows the full Grade → Subject → Level tree —
Unity expands nested sub-assets under the parent, so you can still browse
and tune individual levels by selecting them and using the Inspector.

---

## Advanced: per-grade builds (only if you need per-file assets)

If you specifically want **each level as its own `.asset` file** on
disk (e.g. for fine-grained version control of individual levels), use
the `MathEdu / Advanced / Per-Grade Assets / Build Grade N Files`
items. Each grade builds independently (~10–30 s with the new batching)
and is **resumable** across Unity restarts. After building the grades
you want, run `Rebuild Master Index` to relink `MathDatabase.asset` to
the files on disk.

This path is **not recommended** unless you have a specific workflow
need — the consolidated single-asset path is faster, cleaner, and
identical from the runtime's perspective.

---

## Still stuck after recovery?

If the Editor still hangs the next time you click a `MathEdu` menu:

1. Check the Console for red errors — fix or report them first.
2. Confirm `Assets/Scripts/Editor/DatabaseBuilderMenu.cs` actually has
   the new code by opening it and searching for the comment
   `// FAST PATH (recommended)` near the top.
3. Re-pull `main` to make sure you have commit
   `816ac31b` or later.
4. Open a GitHub issue with a copy of `Logs/AssetImportWorker0.log`.
