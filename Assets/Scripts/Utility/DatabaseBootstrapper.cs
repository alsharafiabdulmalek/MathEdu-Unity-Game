// -----------------------------------------------------------------------------
// DatabaseBootstrapper.cs
// -----------------------------------------------------------------------------
// Runtime fallback: if no MathDatabase asset has been authored (or it is empty),
// this builds a complete in-memory MathDatabase so the game still works on a
// fresh clone with zero authoring.
//
// ====== LAZY BUILD (perf fix) =================================================
//
// BuildInMemory() now creates a *skeleton* database in well under 100 ms even
// on a constrained MacBook Air: ~571 minimal ScriptableObject instances
// (1 Database + 3 Grades + ~27 Subjects + 540 Levels) with only their
// identity fields populated. Questions, lesson text, and story text are
// generated lazily on first access via GameManager.CurrentLevel.
//
// Before this change, BuildInMemory() invoked QuestionGenerator.Generate()
// 540 times up-front, generating 5,400 MathQuestion objects with strings and
// arrays, which on a low-RAM machine could block the main thread for seconds.
//
// The lazy fill happens when the player opens a level (Learn/Practice/Quiz/
// Story/Speed), which costs ~1 ms per level — invisible to the user.
//
// All other consumers of the database (LevelSelect tiles, MainMenu subject
// progress, ProgressManager star roll-ups) read level metadata only
// (`levelNumber`, `levelId`, `displayTitle`) and don't touch `questions`,
// so they are unaffected by the lazy fill.
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
        /// story text. Idempotent — safe to call every time a level is shown.
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

        // -------------------------------------------------------------------
        // Timer curves (used by both the runtime fallback and the editor menu)
        // -------------------------------------------------------------------

        /// <summary>20-level curve: 30s → 10s, smoother in the middle.</summary>
        public static float TimerForQuiz(int level) =>
            Mathf.Lerp(30f, 10f, Mathf.InverseLerp(1, 20, level));

        /// <summary>20-level curve: 8s → 2.5s for Speed Round.</summary>
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
            MathSubject.Counting       => "🔢",
            MathSubject.Addition       => "➕",
            MathSubject.Subtraction    => "➖",
            MathSubject.Multiplication => "✖",
            MathSubject.Division       => "➗",
            MathSubject.Shapes         => "▲",
            MathSubject.Patterns       => "◆◇",
            MathSubject.Fractions      => "½",
            MathSubject.Measurement    => "📏",
            MathSubject.Time           => "🕒",
            MathSubject.Money          => "💰",
            _                          => "?"
        };

        public static string GradeDescription(int g) => g switch
        {
            1 => "Counting, simple addition & subtraction, shapes, and time.",
            2 => "Skip counting, larger numbers, multiplication intro, fractions.",
            3 => "Multi-digit math, tables, division, fractions and geometry.",
            _ => ""
        };

        // -------------------------------------------------------------------
        // Story templates (subject-themed)
        // -------------------------------------------------------------------
        public static string StoryIntro(MathSubject s, int grade, int level)
        {
            int a = 1 + (level * 2);
            int b = 1 + (level + grade);
            switch (s)
            {
                case MathSubject.Addition:
                    return $"🍎 Farmer Jenny has {a} apples. She picks {b} more. Help her count!";
                case MathSubject.Subtraction:
                    return $"🐦 {a + b} birds sat on a wire. {b} flew away. How many are left?";
                case MathSubject.Multiplication:
                    return $"🚗 There are {b} parking rows with {a} cars each. How many total?";
                case MathSubject.Division:
                    return $"🍕 You have {a * b} pizza slices to share equally with {b} friends.";
                case MathSubject.Counting:
                    return "🌟 The night sky is magical! Can you count all the stars?";
                case MathSubject.Shapes:
                    return "🏗️ Architect Aria is designing buildings. She needs your help!";
                case MathSubject.Patterns:
                    return "🎨 Artist Max is creating a pattern. Can you figure out what comes next?";
                case MathSubject.Fractions:
                    return "🎂 It's birthday time! Help slice the cake into equal pieces.";
                case MathSubject.Measurement:
                    return "📏 Builder Bob needs exact measurements. Can you help him?";
                case MathSubject.Time:
                    return "⏰ The train schedule needs your help! Read the clocks correctly.";
                case MathSubject.Money:
                    return "🏪 Welcome to Math Mart! Help the cashier make correct change.";
                default:
                    return $"A new math adventure begins at Level {level}!";
            }
        }

        public static string StoryOutro(MathSubject s)
        {
            switch (s)
            {
                case MathSubject.Addition:
                    return "Amazing! Jenny is so happy with your help counting her apples! 🎉";
                case MathSubject.Subtraction:
                    return "Wonderful! The birds are settled and you helped count them. 🐦";
                case MathSubject.Multiplication:
                    return "Brilliant! The parking lot is organized thanks to you. 🚗";
                case MathSubject.Division:
                    return "Yum! Everyone got an equal slice of pizza. 🍕";
                case MathSubject.Counting:
                    return "Look at all those stars you counted! ⭐";
                case MathSubject.Shapes:
                    return "Architect Aria thinks you're a natural builder. 🏗️";
                case MathSubject.Patterns:
                    return "Artist Max is impressed with your pattern eye. 🎨";
                case MathSubject.Fractions:
                    return "Everyone enjoyed their fair slice of cake. 🎂";
                case MathSubject.Measurement:
                    return "Builder Bob's project is perfect — great measuring! 📏";
                case MathSubject.Time:
                    return "Every train left on time, thanks to you. ⏰";
                case MathSubject.Money:
                    return "Every customer at Math Mart got the right change. 🏪";
                default:
                    return "Great job! The story continues…";
            }
        }
    }
}
