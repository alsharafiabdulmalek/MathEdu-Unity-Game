// -----------------------------------------------------------------------------
// DatabaseBuilderMenu.cs
// -----------------------------------------------------------------------------
// Editor-only menu items that materialize the procedural math database into
// real .asset files under Assets/ScriptableObjects/. Run this once after
// cloning to get the full content tree visible in the Project window.
//
// Menu:
//   MathEdu / Build Default Database          → generate or update all assets
//   MathEdu / Wipe Generated Database         → delete the generated tree
//   MathEdu / Reset Player Progress           → wipe save file + PlayerPrefs
//   MathEdu / Build Default Avatar Library    → 10 emoji avatars under SO/Avatars
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
        private const string Root = "Assets/ScriptableObjects";

        [MenuItem("MathEdu/Build Default Database", priority = 10)]
        public static void Build()
        {
            EnsureFolder(Root);
            EnsureFolder($"{Root}/Grades");
            EnsureFolder($"{Root}/Subjects");
            EnsureFolder($"{Root}/Levels");
            EnsureFolder("Assets/Resources");

            string dbPath = $"{Root}/MathDatabase.asset";
            var db = AssetDatabase.LoadAssetAtPath<MathDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<MathDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }
            db.grades.Clear();

            for (int gradeNum = 1; gradeNum <= 3; gradeNum++)
            {
                string gradeDir = $"{Root}/Grades/Grade{gradeNum}";
                EnsureFolder(gradeDir);

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

                foreach (var subj in QuestionGenerator.SubjectsFor(gradeNum))
                {
                    string subjectDir = $"{gradeDir}/{subj}";
                    EnsureFolder(subjectDir);

                    string subjectPath = $"{subjectDir}/Subject_{subj}.asset";
                    var s = AssetDatabase.LoadAssetAtPath<SubjectData>(subjectPath);
                    if (s == null)
                    {
                        s = ScriptableObject.CreateInstance<SubjectData>();
                        AssetDatabase.CreateAsset(s, subjectPath);
                    }
                    s.subject      = subj;
                    s.displayName  = QuestionGenerator.Pretty(subj);
                    s.themeColor   = DatabaseBootstrapper.ThemeForSubject(subj);
                    s.iconEmoji    = DatabaseBootstrapper.EmojiForSubject(subj);
                    s.description  = QuestionGenerator.LessonIntro(gradeNum, subj);
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
                        ld.levelId       = $"g{gradeNum}_{subj.ToString().ToLowerInvariant()}_l{lvl}";
                        ld.levelNumber   = lvl;
                        ld.displayTitle  = $"Level {lvl}";
                        ld.lessonIntro   = QuestionGenerator.LessonIntro(gradeNum, subj);
                        ld.lessonExample = QuestionGenerator.LessonExample(gradeNum, subj, lvl);
                        ld.lessonTip     = QuestionGenerator.LessonTip(subj);
                        // Use subject-specific templates from DatabaseBootstrapper
                        // so each level reads as a themed adventure.
                        ld.storyIntro    = DatabaseBootstrapper.StoryIntro(subj, gradeNum, lvl);
                        ld.storyOutro    = DatabaseBootstrapper.StoryOutro(subj);
                        ld.questions     = QuestionGenerator.Generate(gradeNum, subj, lvl);
                        ld.quizSecondsPerQuestion  = DatabaseBootstrapper.TimerForQuiz(lvl);
                        ld.speedSecondsPerQuestion = DatabaseBootstrapper.TimerForSpeed(lvl);
                        ld.xpReward = 20 + lvl * 5;
                        ld.badgeId  = lvl == QuestionGenerator.LevelsPerSubject
                            ? $"master_{subj}_{gradeNum}".ToLowerInvariant()
                            : (lvl == 10 ? $"halfway_{subj}_{gradeNum}".ToLowerInvariant() : "");
                        EditorUtility.SetDirty(ld);
                        s.levels.Add(ld);
                    }

                    EditorUtility.SetDirty(s);
                    grade.subjects.Add(s);
                }
                EditorUtility.SetDirty(grade);
                db.grades.Add(grade);
            }

            EditorUtility.SetDirty(db);

            // Also drop a duplicate at Assets/Resources so GameManager finds it
            // without any inspector wiring on a fresh clone.
            string resPath = "Assets/Resources/MathDatabase.asset";
            if (!AssetDatabase.LoadAssetAtPath<MathDatabase>(resPath))
            {
                AssetDatabase.CopyAsset(dbPath, resPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MathEdu] Database built. Total questions: {db.TotalQuestionCount}");
            EditorUtility.DisplayDialog("MathEdu",
                $"Math database built.\n\nGrades: {db.grades.Count}\nLevels/subject: {QuestionGenerator.LevelsPerSubject}\nQuestions: {db.TotalQuestionCount}",
                "OK");
        }

        [MenuItem("MathEdu/Build Default Avatar Library", priority = 12)]
        public static void BuildAvatars()
        {
            EnsureFolder(Root);
            EnsureFolder($"{Root}/Avatars");
            EnsureFolder("Assets/Resources");

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

            foreach (var s in seeds)
            {
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

            string resPath = "Assets/Resources/AvatarLibrary.asset";
            if (!AssetDatabase.LoadAssetAtPath<AvatarLibrary>(resPath))
                AssetDatabase.CopyAsset(libPath, resPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("MathEdu",
                $"Built {lib.avatars.Count} avatars.\nResources copy at:\n{resPath}",
                "OK");
        }

        [MenuItem("MathEdu/Wipe Generated Database", priority = 20)]
        public static void Wipe()
        {
            if (!EditorUtility.DisplayDialog("MathEdu",
                "Delete the entire generated database folder?", "Yes, delete", "Cancel"))
                return;
            if (AssetDatabase.IsValidFolder(Root))
                AssetDatabase.DeleteAsset(Root);
            if (AssetDatabase.LoadAssetAtPath<MathDatabase>("Assets/Resources/MathDatabase.asset") != null)
                AssetDatabase.DeleteAsset("Assets/Resources/MathDatabase.asset");
            if (AssetDatabase.LoadAssetAtPath<AvatarLibrary>("Assets/Resources/AvatarLibrary.asset") != null)
                AssetDatabase.DeleteAsset("Assets/Resources/AvatarLibrary.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
