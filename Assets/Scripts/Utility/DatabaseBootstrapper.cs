// -----------------------------------------------------------------------------
// DatabaseBootstrapper.cs
// -----------------------------------------------------------------------------
// Runtime fallback: if no MathDatabase asset has been authored (or it is empty),
// this builds a complete in-memory MathDatabase so the game still works on a
// fresh clone with zero authoring. The editor menu under "MathEdu / Build
// Default Database" creates the same structure as persisted .asset files.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using MathEdu.Data;
using UnityEngine;

namespace MathEdu.Utility
{
    public static class DatabaseBootstrapper
    {
        public static MathDatabase BuildInMemory()
        {
            var db = ScriptableObject.CreateInstance<MathDatabase>();
            db.name = "MathDatabase (Runtime)";

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
                    s.subject      = subj;
                    s.displayName  = QuestionGenerator.Pretty(subj);
                    s.themeColor   = ThemeForSubject(subj);
                    s.iconEmoji    = EmojiForSubject(subj);
                    s.description  = QuestionGenerator.LessonIntro(gradeNum, subj);

                    for (int lvl = 1; lvl <= QuestionGenerator.LevelsPerSubject; lvl++)
                    {
                        var ld = ScriptableObject.CreateInstance<LevelData>();
                        ld.levelId       = $"g{gradeNum}_{subj.ToString().ToLowerInvariant()}_l{lvl}";
                        ld.levelNumber   = lvl;
                        ld.displayTitle  = $"Level {lvl}";
                        ld.lessonIntro   = QuestionGenerator.LessonIntro(gradeNum, subj);
                        ld.lessonExample = QuestionGenerator.LessonExample(gradeNum, subj, lvl);
                        ld.lessonTip     = QuestionGenerator.LessonTip(subj);
                        ld.storyIntro    = StoryIntro(subj, gradeNum, lvl);
                        ld.storyOutro    = StoryOutro(subj);
                        ld.questions     = QuestionGenerator.Generate(gradeNum, subj, lvl);
                        ld.quizSecondsPerQuestion   = Mathf.Lerp(25f, 12f, lvl / 10f);
                        ld.speedSecondsPerQuestion  = Mathf.Lerp(7f,  3f,  lvl / 10f);
                        ld.xpReward = 25 + lvl * 5;
                        ld.badgeId  = lvl == QuestionGenerator.LevelsPerSubject
                            ? $"master_{subj}_{gradeNum}".ToLowerInvariant()
                            : "";
                        s.levels.Add(ld);
                    }

                    grade.subjects.Add(s);
                }

                db.grades.Add(grade);
            }

            return db;
        }

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
            MathSubject.Addition       => "+",
            MathSubject.Subtraction    => "-",
            MathSubject.Multiplication => "x",
            MathSubject.Division       => "/",
            MathSubject.Shapes         => "▲",
            MathSubject.Patterns       => "◆◇",
            MathSubject.Fractions      => "1/2",
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

        private static string StoryIntro(MathSubject s, int grade, int level) => s switch
        {
            MathSubject.Addition       => "Mia is packing apples in two baskets. Help her count them all!",
            MathSubject.Subtraction    => "A flock of birds flies away from a tree. How many remain?",
            MathSubject.Multiplication => "Captain Cat needs to share fish across boats. Multiply to find the total.",
            MathSubject.Division       => "Ruby the Robot must split her batteries into equal groups.",
            MathSubject.Counting       => "Hop along the number path with Frog! Count carefully.",
            MathSubject.Shapes         => "The Shape Wizard has lost his shapes. Identify each one!",
            MathSubject.Patterns       => "Decorate the parade with the right repeating pattern.",
            MathSubject.Fractions      => "Pizza party! Cut pizzas into the right number of pieces.",
            MathSubject.Time           => "The school bell rings. Read the clock to know which class is next!",
            MathSubject.Money          => "Help Sam at the corner store. Count the coins!",
            MathSubject.Measurement    => "The carpenter needs the right unit. Pick wisely!",
            _ => $"A new math adventure begins at Level {level}!"
        };

        private static string StoryOutro(MathSubject s) =>
            "Great job! The story continues in the next level…";
    }
}
