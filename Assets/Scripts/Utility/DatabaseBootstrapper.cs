// -----------------------------------------------------------------------------
// DatabaseBootstrapper.cs
// -----------------------------------------------------------------------------
// Runtime fallback: if no MathDatabase asset has been authored (or it is empty),
// this builds a complete in-memory MathDatabase so the game still works on a
// fresh clone with zero authoring.
//
// ====== LAZY BUILD (perf fix) =================================================
//
// BuildInMemory() creates a *skeleton* database in well under 100 ms even
// on a constrained MacBook Air: ~571 minimal ScriptableObject instances
// (1 Database + 3 Grades + ~27 Subjects + 540 Levels) with only their
// identity fields populated. Questions, lesson text, and story text are
// generated lazily on first access via GameManager.CurrentLevel.
//
// ====== LOCALIZATION-AWARE REGENERATION =======================================
//
// All player-facing strings (questions, hints, lesson intros, story intros)
// go through `QuestionStrings.*` which switches on `Localization.IsRTL` and
// returns either English or Arabic text. So when the player flips Settings ->
// Language, we need to invalidate every cached level so it gets regenerated
// in the new language on next access. That's what
// `ClearCachedLevelContent(db)` does. SettingsManager calls it.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using MathEdu.Data;
using UnityEngine;

namespace MathEdu.Utility
{
    public static class DatabaseBootstrapper
    {
        /// <summary>
        /// Build a skeleton MathDatabase in memory. Only identity / metadata
        /// fields are populated. Question content and lesson/story text are
        /// generated lazily by <see cref="EnsureLevelContent"/> on first
        /// access from GameManager.CurrentLevel.
        /// </summary>
        public static MathDatabase BuildInMemory()
        {
            var db = ScriptableObject.CreateInstance<MathDatabase>();
            db.name = "MathDatabase (Runtime, Lazy)";

            for (int gradeNum = 1; gradeNum <= 3; gradeNum++)
            {
                var grade = ScriptableObject.CreateInstance<GradeData>();
                grade.gradeNumber = gradeNum;
                grade.displayName = $"Grade {gradeNum}";
                grade.themeColor  = ThemeForGrade(gradeNum);
                grade.description = GradeDescription(gradeNum);

                foreach (var subj in QuestionGenerator.SubjectsFor(gradeNum))
                {
                    var s = ScriptableObject.CreateInstance<SubjectData>();
                    s.subject     = subj;
                    s.displayName = QuestionGenerator.Pretty(subj);
                    s.themeColor  = ThemeForSubject(subj);
                    s.iconEmoji   = EmojiForSubject(subj);
                    // s.description (lesson intro) is filled lazily on entry.

                    for (int lvl = 1; lvl <= QuestionGenerator.LevelsPerSubject; lvl++)
                    {
                        var ld = ScriptableObject.CreateInstance<LevelData>();
                        ld.levelId      = $"g{gradeNum}_{subj.ToString().ToLowerInvariant()}_l{lvl}";
                        ld.levelNumber  = lvl;
                        ld.displayTitle = $"Level {lvl}";
                        // questions, lessonIntro/Example/Tip, storyIntro/Outro
                        // are intentionally LEFT EMPTY here. They get filled
                        // by EnsureLevelContent() on first CurrentLevel access.
                        ld.quizSecondsPerQuestion  = TimerForQuiz(lvl);
                        ld.speedSecondsPerQuestion = TimerForSpeed(lvl);
                        ld.xpReward = 20 + lvl * 5;
                        ld.badgeId  = lvl == QuestionGenerator.LevelsPerSubject
                            ? $"master_{subj}_{gradeNum}".ToLowerInvariant()
                            : (lvl == 10 ? $"halfway_{subj}_{gradeNum}".ToLowerInvariant() : "");
                        s.levels.Add(ld);
                    }

                    grade.subjects.Add(s);
                }

                db.grades.Add(grade);
            }

            return db;
        }

        /// <summary>
        /// Lazily populate a LevelData with its questions, lesson text, and
        /// story text. Idempotent - safe to call every time a level is shown.
        /// Cost per level: ~1 ms (10 question generations + a handful of
        /// string lookups). Called by GameManager.CurrentLevel.
        /// </summary>
        public static void EnsureLevelContent(LevelData level, int grade, MathSubject subject)
        {
            if (level == null) return;

            if (level.questions == null || level.questions.Count == 0)
                level.questions = QuestionGenerator.Generate(grade, subject, level.levelNumber);

            if (string.IsNullOrEmpty(level.lessonIntro))
                level.lessonIntro = QuestionGenerator.LessonIntro(grade, subject);
            if (string.IsNullOrEmpty(level.lessonExample))
                level.lessonExample = QuestionGenerator.LessonExample(grade, subject, level.levelNumber);
            if (string.IsNullOrEmpty(level.lessonTip))
                level.lessonTip = QuestionGenerator.LessonTip(subject);

            if (string.IsNullOrEmpty(level.storyIntro))
                level.storyIntro = StoryIntro(subject, grade, level.levelNumber);
            if (string.IsNullOrEmpty(level.storyOutro))
                level.storyOutro = StoryOutro(subject);
        }

        /// <summary>
        /// Clear every level's cached question / lesson / story content
        /// across the whole database so the next access regenerates them in
        /// the currently-selected language. Called by SettingsManager when
        /// the player switches between English and Arabic.
        /// </summary>
        public static void ClearCachedLevelContent(MathDatabase db)
        {
            if (db == null || db.grades == null) return;
            foreach (var grade in db.grades)
            {
                if (grade == null || grade.subjects == null) continue;
                foreach (var subj in grade.subjects)
                {
                    if (subj == null) continue;
                    // Refresh SubjectData.displayName (used by some headers
                    // that don't go through MainMenuManager.SubjectName).
                    subj.displayName = QuestionGenerator.Pretty(subj.subject);
                    if (subj.levels == null) continue;
                    foreach (var lv in subj.levels)
                    {
                        if (lv == null) continue;
                        if (lv.questions != null) lv.questions.Clear();
                        lv.lessonIntro   = string.Empty;
                        lv.lessonExample = string.Empty;
                        lv.lessonTip     = string.Empty;
                        lv.storyIntro    = string.Empty;
                        lv.storyOutro    = string.Empty;
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // Timer curves (used by both the runtime fallback and the editor menu)
        // -------------------------------------------------------------------

        /// <summary>20-level curve: 30s -> 10s, smoother in the middle.</summary>
        public static float TimerForQuiz(int level) =>
            Mathf.Lerp(30f, 10f, Mathf.InverseLerp(1, 20, level));

        /// <summary>20-level curve: 8s -> 2.5s for Speed Round.</summary>
        public static float TimerForSpeed(int level) =>
            Mathf.Lerp(8f, 2.5f, Mathf.InverseLerp(1, 20, level));

        // -------------------------------------------------------------------
        // Theming / copy helpers
        // -------------------------------------------------------------------

        public static Color ThemeForGrade(int g) => g switch
        {
            1 => new Color(1.00f, 0.78f, 0.36f), // warm yellow
            2 => new Color(0.56f, 0.85f, 0.55f), // mint green
            3 => new Color(0.55f, 0.70f, 0.95f), // sky blue
            _ => Color.white
        };

        public static Color ThemeForSubject(MathSubject s) => s switch
        {
            MathSubject.Counting       => new Color(0.95f, 0.78f, 0.20f),
            MathSubject.Addition       => new Color(0.30f, 0.65f, 0.95f),
            MathSubject.Subtraction    => new Color(0.95f, 0.45f, 0.45f),
            MathSubject.Multiplication => new Color(0.55f, 0.40f, 0.90f),
            MathSubject.Division       => new Color(0.20f, 0.75f, 0.65f),
            MathSubject.Shapes         => new Color(0.95f, 0.55f, 0.20f),
            MathSubject.Patterns       => new Color(0.80f, 0.40f, 0.75f),
            MathSubject.Fractions      => new Color(0.40f, 0.75f, 0.30f),
            MathSubject.Measurement    => new Color(0.30f, 0.55f, 0.75f),
            MathSubject.Time           => new Color(0.95f, 0.65f, 0.25f),
            MathSubject.Money          => new Color(0.30f, 0.80f, 0.40f),
            _                          => Color.gray
        };

        public static string EmojiForSubject(MathSubject s) => s switch
        {
            MathSubject.Counting       => "\ud83d\udd22",
            MathSubject.Addition       => "\u2795",
            MathSubject.Subtraction    => "\u2796",
            MathSubject.Multiplication => "\u2716",
            MathSubject.Division       => "\u2797",
            MathSubject.Shapes         => "\u25b2",
            MathSubject.Patterns       => "\u25c6\u25c7",
            MathSubject.Fractions      => "\u00bd",
            MathSubject.Measurement    => "\ud83d\udccf",
            MathSubject.Time           => "\ud83d\udd52",
            MathSubject.Money          => "\ud83d\udcb0",
            _                          => "?"
        };

        public static string GradeDescription(int g)
        {
            if (Localization.IsRTL)
            {
                return g switch
                {
                    1 => "\u0627\u0644\u0639\u062f\u0651\u060c \u0627\u0644\u062c\u0645\u0639 \u0648\u0627\u0644\u0637\u0631\u062d \u0627\u0644\u0628\u0633\u064a\u0637\u060c \u0627\u0644\u0623\u0634\u0643\u0627\u0644\u060c \u0648\u0627\u0644\u0648\u0642\u062a.",
                    2 => "\u0627\u0644\u0639\u062f\u0651 \u0628\u0645\u0636\u0627\u0639\u0641\u0627\u062a\u060c \u0623\u0639\u062f\u0627\u062f \u0623\u0643\u0628\u0631\u060c \u0645\u0642\u062f\u0651\u0645\u0629 \u0627\u0644\u0636\u0631\u0628\u060c \u0627\u0644\u0643\u0633\u0648\u0631.",
                    3 => "\u062d\u0633\u0627\u0628 \u0645\u062a\u0639\u062f\u0651\u062f \u0627\u0644\u0623\u0631\u0642\u0627\u0645\u060c \u062c\u062f\u0627\u0648\u0644 \u0627\u0644\u0636\u0631\u0628\u060c \u0627\u0644\u0642\u0633\u0645\u0629\u060c \u0627\u0644\u0643\u0633\u0648\u0631\u060c \u0648\u0627\u0644\u0647\u0646\u062f\u0633\u0629.",
                    _ => ""
                };
            }
            return g switch
            {
                1 => "Counting, simple addition & subtraction, shapes, and time.",
                2 => "Skip counting, larger numbers, multiplication intro, fractions.",
                3 => "Multi-digit math, tables, division, fractions and geometry.",
                _ => ""
            };
        }

        // -------------------------------------------------------------------
        // Story templates (subject-themed)
        // -------------------------------------------------------------------
        public static string StoryIntro(MathSubject s, int grade, int level) =>
            QuestionStrings.StoryIntro(s, grade, level);

        public static string StoryOutro(MathSubject s) =>
            QuestionStrings.StoryOutro(s);
    }
}
