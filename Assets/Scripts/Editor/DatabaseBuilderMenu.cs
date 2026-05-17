// -----------------------------------------------------------------------------
// DatabaseBuilderMenu.cs
// -----------------------------------------------------------------------------
// Editor-only menu items that materialize the procedural math database into
// real .asset files. Run once after cloning to get the full content tree
// loaded into Resources.
//
// ====== PERFORMANCE NOTE (the reason this file was rewritten) =================
// The previous implementation created ~570 individual .asset files on disk —
// one per Grade / Subject / Level — *without* batching the asset operations.
// On macOS, every CreateAsset call triggered a full AssetDatabase cycle and
// the final AssetDatabase.Refresh() re-imported the entire project. On a
// typical Mac this took anywhere from 30 minutes to "stuck for 10+ hours".
//
// This rewrite fixes the hang with three independent improvements:
//
//   1. FAST PATH (the new default): "MathEdu / Build Default Database"
//      Creates ONE consolidated MathDatabase.asset directly in
//      Assets/Resources/ with every Grade / Subject / Level stored as a
//      *nested sub-asset* via AssetDatabase.AddObjectToAsset(). The whole
//      database is ONE file on disk → ONE asset import → typically 3–10
//      seconds even on a slow Mac. The Project window still shows the full
//      tree because Unity unfolds sub-assets under their parent.
//
//   2. ASSET-EDITING BATCHING
//      Every menu item now wraps its asset work in
//      AssetDatabase.StartAssetEditing() / StopAssetEditing() with a
//      try/finally. This suspends Unity's automatic per-asset import
//      processing during the build.
//
//   3. PROGRESS BAR + CANCEL
//      The build now shows EditorUtility.DisplayCancelableProgressBar so
//      the user can SEE that work is happening and abort if they need to.
//      The fast build is short enough that this is mainly informational.
//
// ====== ADVANCED (PER-GRADE FILE ASSETS) =====================================
// For users who want individual .asset files in the Project window (so they
// can tune levels in the Inspector), MathEdu / Advanced / Per-Grade Assets
// lets you build ONE grade at a time. Each grade build is independent and
// resumable — close Unity between grades if you want.
//
// ====== SKIP-THE-BUILD SHORTCUT ==============================================
// You don't actually have to run any of these menu items to play the game.
// GameManager.EnsureDatabase() builds a fully populated MathDatabase in
// memory via DatabaseBootstrapper.BuildInMemory() if no asset is found.
// "MathEdu / Advanced / Use Runtime Database Only" surfaces a popup
// explaining this.
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
        //                        FAST PATH (recommended)
        // ===================================================================

        /// <summary>
        /// Default "Build Database" entry point. Generates a single
        /// consolidated MathDatabase.asset in Assets/Resources/ with every
        /// Grade / Subject / Level stored as a nested sub-asset. Typically
        /// 3–10 seconds, even on slow machines, because Unity only has to
        /// import ONE file rather than ~570 separate .asset files.
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

                for (int gradeNum = 1; gradeNum <= 3 && !cancelled; gradeNum++)
                {
                    float gradeProgress = (gradeNum - 1) / 3f;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "MathEdu — Building Database (fast)",
                            $"Grade {gradeNum} of 3…",
                            gradeProgress))
                    {
                        cancelled = true;
                        break;
                    }

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
                        float subjectProgress = gradeProgress + (1f / 3f) * ((float)si / subjects.Length);
                        if (EditorUtility.DisplayCancelableProgressBar(
                                "MathEdu — Building Database (fast)",
                                $"Grade {gradeNum} • {QuestionGenerator.Pretty(subj)} ({si + 1}/{subjects.Length})",
                                subjectProgress))
                        {
                            cancelled = true;
                            break;
                        }

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
                            var ld = BuildLevelData(gradeNum, subj, lvl);
                            AssetDatabase.AddObjectToAsset(ld, db);
                            s.levels.Add(ld);
                            totalLevels++;
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

            // Save the parent asset (which also flushes every sub-asset).
            // We deliberately do NOT call AssetDatabase.Refresh() — there
            // is nothing to scan, only one .asset file changed.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ResDbPath);

            var built = AssetDatabase.LoadAssetAtPath<MathDatabase>(ResDbPath);
            int finalQuestions = built != null ? built.TotalQuestionCount : 0;
            int finalGrades    = built != null ? built.grades.Count       : 0;

            Debug.Log($"[MathEdu] Database built (fast). " +
                      $"Grades: {finalGrades}, Levels: {totalLevels}, Questions: {finalQuestions}" +
                      (cancelled ? " — partial (user cancelled)." : "."));

            EditorUtility.DisplayDialog(
                cancelled ? "MathEdu — Build Cancelled" : "MathEdu — Database Ready",
                cancelled
                    ? $"Build was cancelled. A partial database with {finalGrades} grade(s) " +
                      $"and {finalQuestions} questions was saved to:\n{ResDbPath}\n\n" +
                      $"Re-run \"MathEdu → Build Default Database\" to complete it."
                    : $"Math database built as a single consolidated asset.\n\n" +
                      $"📂  {ResDbPath}\n" +
                      $"📊  {finalGrades} grades  •  {totalLevels} levels  •  {finalQuestions} questions\n\n" +
                      $"This is the fast path — ONE .asset file instead of ~570.\n" +
                      $"GameManager loads it from Resources automatically.\n\n" +
                      $"Next steps:\n" +
                      $"   1. MathEdu → Build Default Avatar Library\n" +
                      $"   2. MathEdu → Build All Scenes\n" +
                      $"   3. Open Assets/Scenes/Bootstrap.unity and press ▶ Play",
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

                // Mirror to Resources so GameManager loads it automatically.
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

                for (int gradeNum = 1; gradeNum <= 3; gradeNum++)
                {
                    string gradePath = $"{Root}/Grades/Grade{gradeNum}/Grade_{gradeNum}.asset";
                    var grade = AssetDatabase.LoadAssetAtPath<GradeData>(gradePath);
                    if (grade != null) db.grades.Add(grade);
                }
                EditorUtility.SetDirty(db);

                // Mirror to Resources.
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
        /// Typically 10–30 seconds per grade on a modern Mac.
        ///
        /// After running one or more grade builds, call "Rebuild Master
        /// Index" to update Assets/Resources/MathDatabase.asset so the
        /// runtime sees the new content.
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
                for (int si = 0; si < subjects.Length && !cancelled; si++)
                {
                    var subj = subjects[si];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            $"MathEdu — Building Grade {gradeNum} (per-file)",
                            $"{QuestionGenerator.Pretty(subj)} ({si + 1}/{subjects.Length})",
                            (float)si / subjects.Length))
                    {
                        cancelled = true;
                        break;
                    }

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
                      $"Re-run the same menu item to finish."
                    : $"Grade {gradeNum} assets generated.\n\n" +
                      $"📂  {gradeDir}\n" +
                      $"📊  {totalLevels} levels  •  {totalQuestions} questions\n\n" +
                      $"➡  Run \"MathEdu → Advanced → Per-Grade Assets → Rebuild Master Index\" " +
                      $"once you've built all the grades you want.",
                "OK");
        }

        // ===================================================================
        //                    HELP / SAFETY ITEMS
        // ===================================================================

        [MenuItem("MathEdu/Advanced/Use Runtime Database Only (no build)", priority = 200)]
        public static void RuntimeDatabaseInfo()
        {
            EditorUtility.DisplayDialog("MathEdu — No Build Required",
                "You don't actually have to materialize the database to play!\n\n" +
                "GameManager.EnsureDatabase() detects when no MathDatabase asset " +
                "is present and builds the full 4,800-question content tree in " +
                "memory via DatabaseBootstrapper.BuildInMemory() at startup. The " +
                "game is fully playable that way — only Project-window browsing " +
                "of individual levels needs a built asset.\n\n" +
                "To play right now, without building anything:\n" +
                "   1. Skip the database build.\n" +
                "   2. (Optional) MathEdu → Build Default Avatar Library\n" +
                "   3. MathEdu → Build All Scenes\n" +
                "   4. Open Assets/Scenes/Bootstrap.unity → Play",
                "OK");
        }

        [MenuItem("MathEdu/Advanced/Open Save File Location", priority = 210)]
        public static void OpenSaveLocation()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        [MenuItem("MathEdu/Run Full Setup (DB + Avatars + Scenes)", priority = 1)]
        public static void RunFullSetup()
        {
            if (!EditorUtility.DisplayDialog("MathEdu — Full Setup",
                "This will run, in order:\n\n" +
                "   1. Build Default Database (fast, single asset)\n" +
                "   2. Build Default Avatar Library\n" +
                "   3. Build All Scenes\n\n" +
                "Each step shows its own progress bar. Total time is usually " +
                "well under a minute on a modern Mac.",
                "Run all", "Cancel"))
                return;

            BuildFast();
            BuildAvatars();
            // SceneBuilderMenu lives in this same assembly.
            SceneBuilderMenu.BuildAll();

            EditorUtility.DisplayDialog("MathEdu — Full Setup Complete",
                "Done! Open Assets/Scenes/Bootstrap.unity and press ▶ Play.",
                "OK");
        }

        // ===================================================================
        //                          INTERNAL HELPERS
        // ===================================================================

        /// <summary>
        /// Creates a fresh LevelData populated from the question generator.
        /// Used by the fast build path (where the LevelData becomes a
        /// sub-asset of the master MathDatabase).
        /// </summary>
        private static LevelData BuildLevelData(int gradeNum, MathSubject subj, int lvl)
        {
            var ld = ScriptableObject.CreateInstance<LevelData>();
            ld.name = $"Level_G{gradeNum}_{subj}_L{lvl:00}";
            PopulateLevelData(ld, gradeNum, subj, lvl);
            return ld;
        }

        /// <summary>
        /// Fills an existing LevelData with curriculum content for the
        /// given grade/subject/level. Used by both the fast (nested asset)
        /// and per-grade (file asset) build paths.
        /// </summary>
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

        /// <summary>
        /// Recursively ensure every folder in the path exists. Wraps
        /// AssetDatabase.CreateFolder which only accepts a single segment.
        /// </summary>
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
