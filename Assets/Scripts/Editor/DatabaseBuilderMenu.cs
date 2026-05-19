// -----------------------------------------------------------------------------
// DatabaseBuilderMenu.cs
// -----------------------------------------------------------------------------
// Editor-only menu items that prepare the project for Play.
//
// ╔══════════════════════════════════════════════════════════════════════════╗
// ║  ⚡ FAST PATH FOR CONSTRAINED MACHINES (e.g. 8 GB MacBook Air)            ║
// ║                                                                          ║
// ║      MathEdu → ⚡ Quick Start (No DB Build — Recommended)                ║
// ║                                                                          ║
// ║  Skips writing the database asset entirely. The game still plays with    ║
// ║  all 4,800 questions because GameManager.EnsureDatabase() builds the     ║
// ║  whole tree in memory at startup via DatabaseBootstrapper.BuildInMemory  ║
// ║  in well under a second. Quick Start only builds the avatar library     ║
// ║  (10 tiny assets) and the 13 scenes — total time < 30 seconds even on   ║
// ║  a constrained MacBook Air.                                              ║
// ╚══════════════════════════════════════════════════════════════════════════╝
//
// Why does Quick Start exist?
//   The previous slow database build (~570 individual .asset files) was
//   replaced with a single-asset path (~540 nested sub-assets). On a fast
//   Mac the single-asset path finishes in 3–10 s, but on a constrained
//   MacBook Air it can still take many minutes because writing/serializing
//   a 5+ MB asset with 540 sub-objects + Unity's import pipeline is RAM
//   hungry. Quick Start side-steps the whole issue by never writing an
//   asset — the game uses the runtime fallback that has always existed.
//
// The other paths (Build Default Database, Per-Grade Assets) are kept for
// users who specifically want the data visible as files in the Project
// window, but they are NOT required for play.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.IO;
using MathEdu.Data;
using MathEdu.Utility;
using UnityEditor;
using UnityEngine;

namespace MathEdu.EditorTools
{
    public static class DatabaseBuilderMenu
    {
        // -------------------------------------------------------------------
        // Paths
        // -------------------------------------------------------------------
        private const string Root          = "Assets/ScriptableObjects";
        private const string ResourcesDir  = "Assets/Resources";
        private const string ResDbPath     = "Assets/Resources/MathDatabase.asset";
        private const string ResAvatarPath = "Assets/Resources/AvatarLibrary.asset";
        private const string LegacyDbPath  = "Assets/ScriptableObjects/MathDatabase.asset";

        // ===================================================================
        //              ⚡ QUICK START (the bulletproof path)
        // ===================================================================

        /// <summary>
        /// THE recommended setup path on every machine, especially
        /// constrained ones (8 GB MacBook Air, older Macs, low-RAM Windows).
        /// Builds avatars + scenes. Does NOT touch the math database — that
        /// is built in memory at runtime in well under a second by
        /// DatabaseBootstrapper.BuildInMemory(). The game is fully playable
        /// with all 4,800 questions in all 11 subjects across all 5 modes.
        /// Typical wall time: well under 30 seconds.
        /// </summary>
        [MenuItem("MathEdu/⚡ Quick Start (No DB Build — Recommended)", priority = 0)]
        public static void QuickStart()
        {
            if (!EditorUtility.DisplayDialog("MathEdu — ⚡ Quick Start",
                "RECOMMENDED PATH — works even on a constrained MacBook Air.\n\n" +
                "This will run, in order:\n" +
                "   1. Build Default Avatar Library (10 avatars, ~1 s)\n" +
                "   2. Build All Scenes (13 scenes, ~5 s)\n\n" +
                "The math database is NOT written to disk. It is built in " +
                "memory at runtime by DatabaseBootstrapper.BuildInMemory() " +
                "when you press Play (well under a second). The game is " +
                "FULLY playable with all 4,800 questions, all 11 subjects, " +
                "all 5 modes.\n\n" +
                "If you later want the database visible in the Project " +
                "window, run \"MathEdu → Build Default Database\" — that's " +
                "optional, not required for play.",
                "Run Quick Start", "Cancel"))
                return;

            BuildAvatars();
            SceneBuilderMenu.BuildAll();

            EditorUtility.DisplayDialog("MathEdu — ⚡ Quick Start Complete",
                "All set!\n\n" +
                "▶  Open Assets/Scenes/Bootstrap.unity and press Play.\n\n" +
                "On first play the math database is built in memory in " +
                "well under a second — no progress bar, no waiting.",
                "OK");
        }

        // ===================================================================
        //              FULL SETUP (also writes the DB asset)
        // ===================================================================

        /// <summary>
        /// Same as Quick Start but ALSO writes the consolidated MathDatabase
        /// asset. On a constrained MacBook Air this can take several minutes
        /// or longer due to the volume of nested sub-assets — use Quick
        /// Start instead unless you specifically need the asset on disk.
        /// </summary>
        [MenuItem("MathEdu/Run Full Setup (writes DB asset — may be slow on low-RAM Macs)", priority = 5)]
        public static void RunFullSetup()
        {
            if (!EditorUtility.DisplayDialog("MathEdu — Full Setup",
                "This will run, in order:\n" +
                "   1. Build Default Database (consolidated single asset)\n" +
                "   2. Build Default Avatar Library\n" +
                "   3. Build All Scenes\n\n" +
                "⚠  On a constrained machine (8 GB MacBook Air, older Mac, " +
                "low-RAM Windows) step 1 can still take several minutes or " +
                "longer because it serializes ~540 nested sub-assets into " +
                "a single multi-megabyte .asset file.\n\n" +
                "If unsure, hit Cancel and use \"⚡ Quick Start (No DB " +
                "Build)\" — the game runs identically either way.",
                "Run Full Setup", "Cancel"))
                return;

            BuildFast();
            BuildAvatars();
            SceneBuilderMenu.BuildAll();

            EditorUtility.DisplayDialog("MathEdu — Full Setup Complete",
                "Done! Open Assets/Scenes/Bootstrap.unity and press ▶ Play.",
                "OK");
        }

        // ===================================================================
        //                        FAST DB BUILD (optional)
        // ===================================================================

        /// <summary>
        /// Generates a single consolidated MathDatabase.asset in
        /// Assets/Resources/ with every Grade / Subject / Level stored as a
        /// nested sub-asset. Optional — only needed for Project-window
        /// browsing. NOT required to play (see Quick Start).
        /// </summary>
        [MenuItem("MathEdu/Build Default Database", priority = 10)]
        public static void BuildFast()
        {
            EnsureFolder(ResourcesDir);

            int totalLevels    = 0;
            int totalQuestions = 0;
            bool cancelled     = false;

            EditorUtility.DisplayProgressBar("MathEdu", "Preparing database…", 0f);
            AssetDatabase.StartAssetEditing();
            try
            {
                // Start from a clean slate — avoid diffing hundreds of
                // sub-assets against any prior state.
                if (AssetDatabase.LoadAssetAtPath<MathDatabase>(ResDbPath) != null)
                    AssetDatabase.DeleteAsset(ResDbPath);

                var db = ScriptableObject.CreateInstance<MathDatabase>();
                db.name = "MathDatabase";
                AssetDatabase.CreateAsset(db, ResDbPath);

                // Rough upper bound for the progress bar. Real count comes
                // from QuestionGenerator.SubjectsFor() per grade.
                int approxTotalLevels =
                    (DatabaseBootstrapper.MaxGrade - DatabaseBootstrapper.MinGrade + 1)
                    * 10 * QuestionGenerator.LevelsPerSubject;
                int builtLevels = 0;

                for (int gradeNum = DatabaseBootstrapper.MinGrade;
                     gradeNum <= DatabaseBootstrapper.MaxGrade && !cancelled;
                     gradeNum++)
                {
                    var grade = ScriptableObject.CreateInstance<GradeData>();
                    grade.name        = $"Grade_{gradeNum}";
                    grade.gradeNumber = gradeNum;
                    grade.displayName = $"Grade {gradeNum}";
                    grade.themeColor  = DatabaseBootstrapper.ThemeForGrade(gradeNum);
                    grade.description = DatabaseBootstrapper.GradeDescription(gradeNum);
                    AssetDatabase.AddObjectToAsset(grade, db);

                    var subjects = QuestionGenerator.SubjectsFor(gradeNum);
                    for (int si = 0; si < subjects.Length && !cancelled; si++)
                    {
                        var subj = subjects[si];

                        var s = ScriptableObject.CreateInstance<SubjectData>();
                        s.name        = $"Subject_G{gradeNum}_{subj}";
                        s.subject     = subj;
                        s.displayName = QuestionGenerator.Pretty(subj);
                        s.themeColor  = DatabaseBootstrapper.ThemeForSubject(subj);
                        s.iconEmoji   = DatabaseBootstrapper.EmojiForSubject(subj);
                        s.description = QuestionGenerator.LessonIntro(gradeNum, subj);
                        AssetDatabase.AddObjectToAsset(s, db);

                        for (int lvl = 1; lvl <= QuestionGenerator.LevelsPerSubject; lvl++)
                        {
                            // Update progress per-level so the user can see
                            // forward progress even on a slow machine.
                            if (EditorUtility.DisplayCancelableProgressBar(
                                    "MathEdu — Building Database",
                                    $"G{gradeNum} • {QuestionGenerator.Pretty(subj)} • Level {lvl}/20",
                                    (float)builtLevels / approxTotalLevels))
                            {
                                cancelled = true;
                                break;
                            }

                            var ld = BuildLevelData(gradeNum, subj, lvl);
                            AssetDatabase.AddObjectToAsset(ld, db);
                            s.levels.Add(ld);
                            totalLevels++;
                            builtLevels++;
                            totalQuestions += ld.questions != null ? ld.questions.Count : 0;
                        }

                        grade.subjects.Add(s);
                    }
                    db.grades.Add(grade);
                }

                EditorUtility.SetDirty(db);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ResDbPath);

            var built = AssetDatabase.LoadAssetAtPath<MathDatabase>(ResDbPath);
            int finalQuestions = built != null ? built.TotalQuestionCount : 0;
            int finalGrades    = built != null ? built.grades.Count       : 0;

            Debug.Log($"[MathEdu] Database built. " +
                      $"Grades: {finalGrades}, Levels: {totalLevels}, Questions: {finalQuestions}" +
                      (cancelled ? " — partial (user cancelled)." : "."));

            EditorUtility.DisplayDialog(
                cancelled ? "MathEdu — Build Cancelled" : "MathEdu — Database Ready",
                cancelled
                    ? $"Build was cancelled. A partial database with {finalGrades} grade(s) " +
                      $"and {finalQuestions} questions was saved to:\n{ResDbPath}\n\n" +
                      $"The game is still playable — the runtime fallback fills in any " +
                      $"missing data. To complete the asset later, re-run this menu item " +
                      $"or use \"⚡ Quick Start\" to skip the asset entirely."
                    : $"Math database built as a single consolidated asset.\n\n" +
                      $"📂  {ResDbPath}\n" +
                      $"📊  {finalGrades} grades  •  {totalLevels} levels  •  {finalQuestions} questions",
                "OK");
        }

        // ===================================================================
        //                        AVATAR LIBRARY
        // ===================================================================

        [MenuItem("MathEdu/Build Default Avatar Library", priority = 11)]
        public static void BuildAvatars()
        {
            EnsureFolder(Root);
            EnsureFolder($"{Root}/Avatars");
            EnsureFolder(ResourcesDir);

            EditorUtility.DisplayProgressBar("MathEdu", "Building avatar library…", 0f);
            AssetDatabase.StartAssetEditing();
            try
            {
                string libPath = $"{Root}/AvatarLibrary.asset";
                var lib = AssetDatabase.LoadAssetAtPath<AvatarLibrary>(libPath);
                if (lib == null)
                {
                    lib = ScriptableObject.CreateInstance<AvatarLibrary>();
                    AssetDatabase.CreateAsset(lib, libPath);
                }
                lib.avatars.Clear();

                (string id, string name, string emoji, Color tint)[] seeds =
                {
                    ("fox",     "Fox",      "🦊", new Color(0.95f, 0.55f, 0.20f)),
                    ("panda",   "Panda",    "🐼", new Color(0.55f, 0.55f, 0.60f)),
                    ("rabbit",  "Rabbit",   "🐰", new Color(0.95f, 0.78f, 0.90f)),
                    ("owl",     "Owl",      "🦉", new Color(0.45f, 0.55f, 0.75f)),
                    ("monkey",  "Monkey",   "🐵", new Color(0.85f, 0.65f, 0.45f)),
                    ("cat",     "Cat",      "🐱", new Color(0.95f, 0.75f, 0.35f)),
                    ("dog",     "Dog",      "🐶", new Color(0.85f, 0.60f, 0.35f)),
                    ("unicorn", "Unicorn",  "🦄", new Color(0.85f, 0.55f, 0.90f)),
                    ("dragon",  "Dragon",   "🐲", new Color(0.40f, 0.75f, 0.45f)),
                    ("astro",   "Astro",    "🚀", new Color(0.35f, 0.50f, 0.85f)),
                };

                for (int i = 0; i < seeds.Length; i++)
                {
                    var s = seeds[i];
                    EditorUtility.DisplayProgressBar("MathEdu",
                        $"Avatar: {s.name}", (float)i / seeds.Length);

                    string path = $"{Root}/Avatars/Avatar_{s.id}.asset";
                    var a = AssetDatabase.LoadAssetAtPath<AvatarData>(path);
                    if (a == null)
                    {
                        a = ScriptableObject.CreateInstance<AvatarData>();
                        AssetDatabase.CreateAsset(a, path);
                    }
                    a.avatarId    = s.id;
                    a.displayName = s.name;
                    a.emoji       = s.emoji;
                    a.tint        = s.tint;
                    EditorUtility.SetDirty(a);
                    lib.avatars.Add(a);
                }

                EditorUtility.SetDirty(lib);

                if (AssetDatabase.LoadAssetAtPath<AvatarLibrary>(ResAvatarPath) != null)
                    AssetDatabase.DeleteAsset(ResAvatarPath);
                AssetDatabase.CopyAsset(libPath, ResAvatarPath);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ResAvatarPath);

            var built = AssetDatabase.LoadAssetAtPath<AvatarLibrary>(ResAvatarPath);
            EditorUtility.DisplayDialog("MathEdu — Avatars Ready",
                $"Built {(built != null ? built.avatars.Count : 0)} avatars.\n\n" +
                $"📂 {ResAvatarPath}",
                "OK");
        }

        // ===================================================================
        //                    DESTRUCTIVE / RESET MENU ITEMS
        // ===================================================================

        [MenuItem("MathEdu/Wipe Generated Database", priority = 20)]
        public static void Wipe()
        {
            if (!EditorUtility.DisplayDialog("MathEdu",
                "Delete the entire generated database (consolidated + per-grade files)?",
                "Yes, delete", "Cancel"))
                return;

            EditorUtility.DisplayProgressBar("MathEdu", "Deleting generated assets…", 0f);
            AssetDatabase.StartAssetEditing();
            try
            {
                if (AssetDatabase.IsValidFolder(Root))
                    AssetDatabase.DeleteAsset(Root);
                if (AssetDatabase.LoadAssetAtPath<MathDatabase>(ResDbPath) != null)
                    AssetDatabase.DeleteAsset(ResDbPath);
                if (AssetDatabase.LoadAssetAtPath<AvatarLibrary>(ResAvatarPath) != null)
                    AssetDatabase.DeleteAsset(ResAvatarPath);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[MathEdu] Generated database wiped.");
        }

        [MenuItem("MathEdu/Reset Player Progress", priority = 30)]
        public static void ResetPlayer()
        {
            if (!EditorUtility.DisplayDialog("MathEdu",
                "Reset the saved player profile (XP, stars, unlocks)?",
                "Yes, reset", "Cancel"))
                return;
            SaveSystem.DeleteAll();
            Debug.Log("[MathEdu] Player progress reset.");
        }

        // ===================================================================
        //                    ADVANCED / PER-GRADE BUILDS
        // ===================================================================

        [MenuItem("MathEdu/Advanced/Per-Grade Assets/Build Grade 1 Files", priority = 100)]
        public static void BuildGrade1Files() => BuildPerGradeAssets(1);

        [MenuItem("MathEdu/Advanced/Per-Grade Assets/Build Grade 2 Files", priority = 101)]
        public static void BuildGrade2Files() => BuildPerGradeAssets(2);

        [MenuItem("MathEdu/Advanced/Per-Grade Assets/Build Grade 3 Files", priority = 102)]
        public static void BuildGrade3Files() => BuildPerGradeAssets(3);

        [MenuItem("MathEdu/Advanced/Per-Grade Assets/Build Grade 4 Files", priority = 103)]
        public static void BuildGrade4Files() => BuildPerGradeAssets(4);

        [MenuItem("MathEdu/Advanced/Per-Grade Assets/Build Grade 5 Files", priority = 104)]
        public static void BuildGrade5Files() => BuildPerGradeAssets(5);

        [MenuItem("MathEdu/Advanced/Per-Grade Assets/Rebuild Master Index", priority = 110)]
        public static void RebuildMasterIndex()
        {
            EnsureFolder(Root);
            EnsureFolder(ResourcesDir);

            EditorUtility.DisplayProgressBar("MathEdu", "Indexing per-grade asset files…", 0f);
            AssetDatabase.StartAssetEditing();
            try
            {
                var db = AssetDatabase.LoadAssetAtPath<MathDatabase>(LegacyDbPath);
                if (db == null)
                {
                    db = ScriptableObject.CreateInstance<MathDatabase>();
                    AssetDatabase.CreateAsset(db, LegacyDbPath);
                }
                db.grades.Clear();

                for (int gradeNum = DatabaseBootstrapper.MinGrade;
                     gradeNum <= DatabaseBootstrapper.MaxGrade; gradeNum++)
                {
                    string gradePath = $"{Root}/Grades/Grade{gradeNum}/Grade_{gradeNum}.asset";
                    var grade = AssetDatabase.LoadAssetAtPath<GradeData>(gradePath);
                    if (grade != null) db.grades.Add(grade);
                }
                EditorUtility.SetDirty(db);

                if (AssetDatabase.LoadAssetAtPath<MathDatabase>(ResDbPath) != null)
                    AssetDatabase.DeleteAsset(ResDbPath);
                AssetDatabase.CopyAsset(LegacyDbPath, ResDbPath);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();

            var built = AssetDatabase.LoadAssetAtPath<MathDatabase>(ResDbPath);
            EditorUtility.DisplayDialog("MathEdu — Index Rebuilt",
                $"Master index updated.\n\n" +
                $"📂  {ResDbPath}\n" +
                $"📊  {(built != null ? built.grades.Count : 0)} grades  •  " +
                $"{(built != null ? built.TotalQuestionCount : 0)} questions",
                "OK");
        }

        /// <summary>
        /// Builds the per-file asset tree for a single grade only:
        ///   Assets/ScriptableObjects/Grades/Grade{N}/Grade_{N}.asset
        ///   Assets/ScriptableObjects/Grades/Grade{N}/{Subject}/Subject_{Subject}.asset
        ///   Assets/ScriptableObjects/Grades/Grade{N}/{Subject}/Level_{lvl:00}.asset
        ///
        /// Properly batched with StartAssetEditing(); shows a progress bar.
        /// Typically 10–30 seconds per grade on a modern Mac, but on a
        /// constrained machine this is still slower than Quick Start.
        /// </summary>
        private static void BuildPerGradeAssets(int gradeNum)
        {
            EnsureFolder(Root);
            EnsureFolder($"{Root}/Grades");
            string gradeDir = $"{Root}/Grades/Grade{gradeNum}";
            EnsureFolder(gradeDir);

            int totalLevels    = 0;
            int totalQuestions = 0;
            bool cancelled     = false;

            EditorUtility.DisplayProgressBar(
                $"MathEdu — Building Grade {gradeNum}", "Preparing…", 0f);

            AssetDatabase.StartAssetEditing();
            try
            {
                string gradePath = $"{gradeDir}/Grade_{gradeNum}.asset";
                var grade = AssetDatabase.LoadAssetAtPath<GradeData>(gradePath);
                if (grade == null)
                {
                    grade = ScriptableObject.CreateInstance<GradeData>();
                    AssetDatabase.CreateAsset(grade, gradePath);
                }
                grade.gradeNumber = gradeNum;
                grade.displayName = $"Grade {gradeNum}";
                grade.themeColor  = DatabaseBootstrapper.ThemeForGrade(gradeNum);
                grade.description = DatabaseBootstrapper.GradeDescription(gradeNum);
                grade.subjects.Clear();

                var subjects = QuestionGenerator.SubjectsFor(gradeNum);
                int totalUnits = subjects.Length * QuestionGenerator.LevelsPerSubject;
                int done = 0;
                for (int si = 0; si < subjects.Length && !cancelled; si++)
                {
                    var subj = subjects[si];
                    string subjectDir  = $"{gradeDir}/{subj}";
                    EnsureFolder(subjectDir);
                    string subjectPath = $"{subjectDir}/Subject_{subj}.asset";

                    var s = AssetDatabase.LoadAssetAtPath<SubjectData>(subjectPath);
                    if (s == null)
                    {
                        s = ScriptableObject.CreateInstance<SubjectData>();
                        AssetDatabase.CreateAsset(s, subjectPath);
                    }
                    s.subject     = subj;
                    s.displayName = QuestionGenerator.Pretty(subj);
                    s.themeColor  = DatabaseBootstrapper.ThemeForSubject(subj);
                    s.iconEmoji   = DatabaseBootstrapper.EmojiForSubject(subj);
                    s.description = QuestionGenerator.LessonIntro(gradeNum, subj);
                    s.levels.Clear();

                    for (int lvl = 1; lvl <= QuestionGenerator.LevelsPerSubject; lvl++)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(
                                $"MathEdu — Grade {gradeNum} (per-file)",
                                $"{QuestionGenerator.Pretty(subj)} • Level {lvl}/20",
                                (float)done / totalUnits))
                        {
                            cancelled = true;
                            break;
                        }

                        string lvlPath = $"{subjectDir}/Level_{lvl:00}.asset";
                        var ld = AssetDatabase.LoadAssetAtPath<LevelData>(lvlPath);
                        if (ld == null)
                        {
                            ld = ScriptableObject.CreateInstance<LevelData>();
                            AssetDatabase.CreateAsset(ld, lvlPath);
                        }
                        PopulateLevelData(ld, gradeNum, subj, lvl);
                        EditorUtility.SetDirty(ld);
                        s.levels.Add(ld);
                        totalLevels++;
                        done++;
                        totalQuestions += ld.questions != null ? ld.questions.Count : 0;
                    }

                    EditorUtility.SetDirty(s);
                    grade.subjects.Add(s);
                }
                EditorUtility.SetDirty(grade);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();

            Debug.Log($"[MathEdu] Grade {gradeNum} built (per-file). " +
                      $"Levels: {totalLevels}, Questions: {totalQuestions}" +
                      (cancelled ? " — partial (user cancelled)." : "."));

            EditorUtility.DisplayDialog(
                cancelled ? "MathEdu — Build Cancelled" : $"MathEdu — Grade {gradeNum} Ready",
                cancelled
                    ? $"Grade {gradeNum} build was cancelled. Partial assets saved under:\n{gradeDir}\n\n" +
                      $"The game still plays via the runtime fallback. " +
                      $"Re-run the same menu item later to finish."
                    : $"Grade {gradeNum} assets generated.\n\n" +
                      $"📂  {gradeDir}\n" +
                      $"📊  {totalLevels} levels  •  {totalQuestions} questions\n\n" +
                      $"➡  Run \"MathEdu → Advanced → Per-Grade Assets → Rebuild Master Index\" " +
                      $"once you've built all the grades you want.",
                "OK");
        }

        // ===================================================================
        //                    ADVANCED / PER-SUBJECT BUILDS
        // ===================================================================
        //
        // Tiny incremental builders — one subject of one grade at a time
        // (20 levels each). Useful on very constrained machines or for
        // diagnostics. Run "Rebuild Master Index" after building.

        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 1 - Counting",        priority = 130)]
        public static void BuildG1Counting()       => BuildOneSubject(1, MathSubject.Counting);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 1 - Addition",        priority = 131)]
        public static void BuildG1Addition()       => BuildOneSubject(1, MathSubject.Addition);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 1 - Subtraction",     priority = 132)]
        public static void BuildG1Subtraction()    => BuildOneSubject(1, MathSubject.Subtraction);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 1 - Shapes",          priority = 133)]
        public static void BuildG1Shapes()         => BuildOneSubject(1, MathSubject.Shapes);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 1 - Patterns",        priority = 134)]
        public static void BuildG1Patterns()       => BuildOneSubject(1, MathSubject.Patterns);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 1 - Measurement",     priority = 135)]
        public static void BuildG1Measurement()    => BuildOneSubject(1, MathSubject.Measurement);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 1 - Time",            priority = 136)]
        public static void BuildG1Time()           => BuildOneSubject(1, MathSubject.Time);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 1 - Money",           priority = 137)]
        public static void BuildG1Money()          => BuildOneSubject(1, MathSubject.Money);

        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Counting",        priority = 140)]
        public static void BuildG2Counting()       => BuildOneSubject(2, MathSubject.Counting);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Addition",        priority = 141)]
        public static void BuildG2Addition()       => BuildOneSubject(2, MathSubject.Addition);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Subtraction",     priority = 142)]
        public static void BuildG2Subtraction()    => BuildOneSubject(2, MathSubject.Subtraction);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Multiplication",  priority = 143)]
        public static void BuildG2Multiplication() => BuildOneSubject(2, MathSubject.Multiplication);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Shapes",          priority = 144)]
        public static void BuildG2Shapes()         => BuildOneSubject(2, MathSubject.Shapes);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Fractions",       priority = 145)]
        public static void BuildG2Fractions()      => BuildOneSubject(2, MathSubject.Fractions);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Measurement",     priority = 146)]
        public static void BuildG2Measurement()    => BuildOneSubject(2, MathSubject.Measurement);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Time",            priority = 147)]
        public static void BuildG2Time()           => BuildOneSubject(2, MathSubject.Time);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 2 - Money",           priority = 148)]
        public static void BuildG2Money()          => BuildOneSubject(2, MathSubject.Money);

        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Addition",        priority = 160)]
        public static void BuildG3Addition()       => BuildOneSubject(3, MathSubject.Addition);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Subtraction",     priority = 161)]
        public static void BuildG3Subtraction()    => BuildOneSubject(3, MathSubject.Subtraction);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Multiplication",  priority = 162)]
        public static void BuildG3Multiplication() => BuildOneSubject(3, MathSubject.Multiplication);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Division",        priority = 163)]
        public static void BuildG3Division()       => BuildOneSubject(3, MathSubject.Division);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Shapes",          priority = 164)]
        public static void BuildG3Shapes()         => BuildOneSubject(3, MathSubject.Shapes);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Fractions",       priority = 165)]
        public static void BuildG3Fractions()      => BuildOneSubject(3, MathSubject.Fractions);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Measurement",     priority = 166)]
        public static void BuildG3Measurement()    => BuildOneSubject(3, MathSubject.Measurement);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Time",            priority = 167)]
        public static void BuildG3Time()           => BuildOneSubject(3, MathSubject.Time);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 3 - Money",           priority = 168)]
        public static void BuildG3Money()          => BuildOneSubject(3, MathSubject.Money);

        // ----- Grade 4 (10 subjects) -----
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Addition",        priority = 180)]
        public static void BuildG4Addition()       => BuildOneSubject(4, MathSubject.Addition);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Subtraction",     priority = 181)]
        public static void BuildG4Subtraction()    => BuildOneSubject(4, MathSubject.Subtraction);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Multiplication",  priority = 182)]
        public static void BuildG4Multiplication() => BuildOneSubject(4, MathSubject.Multiplication);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Division",        priority = 183)]
        public static void BuildG4Division()       => BuildOneSubject(4, MathSubject.Division);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Shapes",          priority = 184)]
        public static void BuildG4Shapes()         => BuildOneSubject(4, MathSubject.Shapes);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Patterns",        priority = 185)]
        public static void BuildG4Patterns()       => BuildOneSubject(4, MathSubject.Patterns);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Fractions",       priority = 186)]
        public static void BuildG4Fractions()      => BuildOneSubject(4, MathSubject.Fractions);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Measurement",     priority = 187)]
        public static void BuildG4Measurement()    => BuildOneSubject(4, MathSubject.Measurement);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Time",            priority = 188)]
        public static void BuildG4Time()           => BuildOneSubject(4, MathSubject.Time);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 4 - Money",           priority = 189)]
        public static void BuildG4Money()          => BuildOneSubject(4, MathSubject.Money);

        // ----- Grade 5 (10 subjects) -----
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Addition",        priority = 200)]
        public static void BuildG5Addition()       => BuildOneSubject(5, MathSubject.Addition);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Subtraction",     priority = 201)]
        public static void BuildG5Subtraction()    => BuildOneSubject(5, MathSubject.Subtraction);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Multiplication",  priority = 202)]
        public static void BuildG5Multiplication() => BuildOneSubject(5, MathSubject.Multiplication);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Division",        priority = 203)]
        public static void BuildG5Division()       => BuildOneSubject(5, MathSubject.Division);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Shapes",          priority = 204)]
        public static void BuildG5Shapes()         => BuildOneSubject(5, MathSubject.Shapes);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Patterns",        priority = 205)]
        public static void BuildG5Patterns()       => BuildOneSubject(5, MathSubject.Patterns);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Fractions",       priority = 206)]
        public static void BuildG5Fractions()      => BuildOneSubject(5, MathSubject.Fractions);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Measurement",     priority = 207)]
        public static void BuildG5Measurement()    => BuildOneSubject(5, MathSubject.Measurement);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Time",            priority = 208)]
        public static void BuildG5Time()           => BuildOneSubject(5, MathSubject.Time);
        [MenuItem("MathEdu/Advanced/Per-Subject Assets/Grade 5 - Money",           priority = 209)]
        public static void BuildG5Money()          => BuildOneSubject(5, MathSubject.Money);

        /// <summary>
        /// Builds (or rebuilds) per-level .asset files for exactly one
        /// subject of one grade. 20 levels = 20 small files. Typically
        /// 1–3 seconds, even on a constrained MacBook Air.
        /// </summary>
        private static void BuildOneSubject(int gradeNum, MathSubject subj)
        {
            EnsureFolder(Root);
            EnsureFolder($"{Root}/Grades");
            string gradeDir   = $"{Root}/Grades/Grade{gradeNum}";
            EnsureFolder(gradeDir);
            string subjectDir = $"{gradeDir}/{subj}";
            EnsureFolder(subjectDir);

            int totalQuestions = 0;
            bool cancelled     = false;

            EditorUtility.DisplayProgressBar(
                $"MathEdu — G{gradeNum} {QuestionGenerator.Pretty(subj)}",
                "Preparing…", 0f);

            AssetDatabase.StartAssetEditing();
            try
            {
                string gradePath = $"{gradeDir}/Grade_{gradeNum}.asset";
                var grade = AssetDatabase.LoadAssetAtPath<GradeData>(gradePath);
                if (grade == null)
                {
                    grade = ScriptableObject.CreateInstance<GradeData>();
                    grade.gradeNumber = gradeNum;
                    grade.displayName = $"Grade {gradeNum}";
                    grade.themeColor  = DatabaseBootstrapper.ThemeForGrade(gradeNum);
                    grade.description = DatabaseBootstrapper.GradeDescription(gradeNum);
                    AssetDatabase.CreateAsset(grade, gradePath);
                }

                string subjectPath = $"{subjectDir}/Subject_{subj}.asset";
                var s = AssetDatabase.LoadAssetAtPath<SubjectData>(subjectPath);
                if (s == null)
                {
                    s = ScriptableObject.CreateInstance<SubjectData>();
                    AssetDatabase.CreateAsset(s, subjectPath);
                }
                s.subject     = subj;
                s.displayName = QuestionGenerator.Pretty(subj);
                s.themeColor  = DatabaseBootstrapper.ThemeForSubject(subj);
                s.iconEmoji   = DatabaseBootstrapper.EmojiForSubject(subj);
                s.description = QuestionGenerator.LessonIntro(gradeNum, subj);
                s.levels.Clear();

                for (int lvl = 1; lvl <= QuestionGenerator.LevelsPerSubject; lvl++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            $"MathEdu — G{gradeNum} {QuestionGenerator.Pretty(subj)}",
                            $"Level {lvl}/20",
                            (float)(lvl - 1) / QuestionGenerator.LevelsPerSubject))
                    {
                        cancelled = true;
                        break;
                    }

                    string lvlPath = $"{subjectDir}/Level_{lvl:00}.asset";
                    var ld = AssetDatabase.LoadAssetAtPath<LevelData>(lvlPath);
                    if (ld == null)
                    {
                        ld = ScriptableObject.CreateInstance<LevelData>();
                        AssetDatabase.CreateAsset(ld, lvlPath);
                    }
                    PopulateLevelData(ld, gradeNum, subj, lvl);
                    EditorUtility.SetDirty(ld);
                    s.levels.Add(ld);
                    totalQuestions += ld.questions != null ? ld.questions.Count : 0;
                }
                EditorUtility.SetDirty(s);

                // Update the grade's subject list (idempotent).
                if (!grade.subjects.Contains(s)) grade.subjects.Add(s);
                EditorUtility.SetDirty(grade);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }
            AssetDatabase.SaveAssets();

            Debug.Log($"[MathEdu] Built G{gradeNum} {subj}. " +
                      $"Levels: {QuestionGenerator.LevelsPerSubject}, " +
                      $"Questions: {totalQuestions}" +
                      (cancelled ? " — partial." : "."));

            EditorUtility.DisplayDialog(
                cancelled
                    ? $"MathEdu — G{gradeNum} {subj} (cancelled)"
                    : $"MathEdu — G{gradeNum} {subj} Ready",
                cancelled
                    ? $"Partial subject saved under:\n{subjectDir}"
                    : $"📂  {subjectDir}\n📊  20 levels  •  {totalQuestions} questions\n\n" +
                      $"➡  Run \"MathEdu → Advanced → Per-Grade Assets → Rebuild Master Index\" " +
                      $"once you've built all the subjects you want.",
                "OK");
        }

        // ===================================================================
        //                    HELP / SAFETY ITEMS
        // ===================================================================

        [MenuItem("MathEdu/Advanced/Use Runtime Database Only (no build)", priority = 200)]
        public static void RuntimeDatabaseInfo()
        {
            EditorUtility.DisplayDialog("MathEdu — No Build Required",
                "You don't have to materialize the database to play.\n\n" +
                "GameManager.EnsureDatabase() detects when no MathDatabase " +
                "asset is present and builds the full 4,800-question content " +
                "tree in memory via DatabaseBootstrapper.BuildInMemory() at " +
                "startup. The game is fully playable that way — only " +
                "Project-window browsing of individual levels needs an asset.\n\n" +
                "Easiest path: MathEdu → ⚡ Quick Start (No DB Build).",
                "OK");
        }

        [MenuItem("MathEdu/Advanced/Open Save File Location", priority = 210)]
        public static void OpenSaveLocation()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        // ===================================================================
        //                          INTERNAL HELPERS
        // ===================================================================

        private static LevelData BuildLevelData(int gradeNum, MathSubject subj, int lvl)
        {
            var ld = ScriptableObject.CreateInstance<LevelData>();
            ld.name = $"Level_G{gradeNum}_{subj}_L{lvl:00}";
            PopulateLevelData(ld, gradeNum, subj, lvl);
            return ld;
        }

        private static void PopulateLevelData(LevelData ld, int gradeNum, MathSubject subj, int lvl)
        {
            ld.levelId      = $"g{gradeNum}_{subj.ToString().ToLowerInvariant()}_l{lvl}";
            ld.levelNumber  = lvl;
            ld.displayTitle = $"Level {lvl}";
            ld.lessonIntro  = QuestionGenerator.LessonIntro(gradeNum, subj);
            ld.lessonExample = QuestionGenerator.LessonExample(gradeNum, subj, lvl);
            ld.lessonTip    = QuestionGenerator.LessonTip(subj);
            ld.storyIntro   = DatabaseBootstrapper.StoryIntro(subj, gradeNum, lvl);
            ld.storyOutro   = DatabaseBootstrapper.StoryOutro(subj);
            ld.questions    = QuestionGenerator.Generate(gradeNum, subj, lvl);
            ld.quizSecondsPerQuestion  = DatabaseBootstrapper.TimerForQuiz(lvl);
            ld.speedSecondsPerQuestion = DatabaseBootstrapper.TimerForSpeed(lvl);
            ld.xpReward = 20 + lvl * 5;
            ld.badgeId  = lvl == QuestionGenerator.LevelsPerSubject
                ? $"master_{subj}_{gradeNum}".ToLowerInvariant()
                : (lvl == 10 ? $"halfway_{subj}_{gradeNum}".ToLowerInvariant() : "");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            string leaf   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
