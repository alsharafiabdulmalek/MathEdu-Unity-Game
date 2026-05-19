// -----------------------------------------------------------------------------
// QuestionGenerator.cs
// -----------------------------------------------------------------------------
// Procedural generator that produces curriculum-correct math question sets
// for grades 1-3. Used by:
//   * The editor menu "MathEdu / Build Default Database" to populate the
//     bundled ScriptableObjects in one click.
//   * The runtime fallback inside DatabaseBootstrapper, so the game still
//     ships content even if no .asset files were authored.
//
// All player-facing strings (prompts, hints, explanations, items, names,
// shape names, etc.) flow through `QuestionStrings.*`, which switches on
// `Localization.IsRTL` and returns either English or Arabic text. So every
// question this generator produces is in the player's current language.
//
// Difficulty ladder (per subject, applies wherever it makes sense):
//   * L1-L5   : single-digit, concrete-object prompts (apples, balls).
//   * L6-L10  : double-digit, abstract numbers.
//   * L11-L15 : single-step word problems with named characters.
//   * L16-L19 : two-operation word problems.
//   * L20     : three-step "challenge" problems using the grade's max range
//               (Grade 1 max 100, Grade 2 max 999, Grade 3 max 9999).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using MathEdu.Data;
using UnityEngine;

namespace MathEdu.Utility
{
    public static class QuestionGenerator
    {
        // -------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------

        public const int LevelsPerSubject     = 20;
        public const int QuestionsPerLevel    = 10;

        /// <summary>
        /// Generate a full question list for a given grade / subject / level.
        /// Deterministic for a given seed so saved progress can replay the
        /// same set in identical order if desired.
        ///
        /// SAFETY NET: every generator branch is wrapped in try/catch. If any
        /// per-subject method throws (out-of-range RNG, division by zero,
        /// runaway distractor loops, ...) we log a clear warning and fall back
        /// to a small set of trivially valid math questions so the gameplay
        /// scene NEVER opens to a black screen.
        /// </summary>
        public static List<MathQuestion> Generate(
            int grade, MathSubject subject, int level, int seed = 0)
        {
            var rng = new System.Random(seed != 0
                ? seed
                : HashSeed(grade, subject, level));

            List<MathQuestion> result;
            try
            {
                switch (subject)
                {
                    case MathSubject.Counting:       result = Counting(grade, level, rng);       break;
                    case MathSubject.Addition:       result = Addition(grade, level, rng);       break;
                    case MathSubject.Subtraction:    result = Subtraction(grade, level, rng);    break;
                    case MathSubject.Multiplication: result = Multiplication(grade, level, rng); break;
                    case MathSubject.Division:       result = Division(grade, level, rng);       break;
                    case MathSubject.Shapes:         result = Shapes(grade, level, rng);         break;
                    case MathSubject.Patterns:       result = Patterns(grade, level, rng);       break;
                    case MathSubject.Fractions:      result = Fractions(grade, level, rng);      break;
                    case MathSubject.Measurement:    result = Measurement(grade, level, rng);    break;
                    case MathSubject.Time:           result = Time(grade, level, rng);           break;
                    case MathSubject.Money:          result = Money(grade, level, rng);          break;
                    default:                         result = new List<MathQuestion>();          break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[QuestionGenerator] G{grade} {subject} L{level} threw: {ex.Message}\n{ex.StackTrace}");
                result = null;
            }

            // Defensive: drop any malformed entries and replace empty/invalid
            // levels with the universal fallback set. This catches both
            // "generator threw" and "generator returned a question with
            // correctIndex == -1" simultaneously.
            if (result == null) result = new List<MathQuestion>();
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (result[i] == null || !result[i].IsValid())
                {
                    Debug.LogWarning($"[QuestionGenerator] G{grade} {subject} L{level} produced invalid question at index {i}; dropping.");
                    result.RemoveAt(i);
                }
            }
            if (result.Count == 0)
            {
                Debug.LogWarning($"[QuestionGenerator] G{grade} {subject} L{level} produced 0 valid questions; using safe fallback set.");
                result = FallbackQuestions(grade, level);
            }
            // Backfill the list with safe fallback questions if the generator
            // only returned partial output. This keeps LearnMode (which slices
            // questions[0..2] and questions[3..9]) and any other index-based
            // consumer from running off the end of the list.
            if (result.Count < QuestionsPerLevel)
            {
                Debug.LogWarning($"[QuestionGenerator] G{grade} {subject} L{level} produced only {result.Count} valid questions; padding to {QuestionsPerLevel}.");
                var pad = FallbackQuestions(grade, level);
                int i = 0;
                while (result.Count < QuestionsPerLevel)
                    result.Add(pad[i++ % pad.Count]);
            }
            return result;
        }

        /// <summary>
        /// Universal grade-appropriate fallback set. Used when a generator
        /// branch throws or returns no valid questions. Keeps the gameplay
        /// loop honest even on the most pathological RNG seeds.
        /// </summary>
        private static List<MathQuestion> FallbackQuestions(int grade, int level)
        {
            int range = grade == 1 ? 10 : grade == 2 ? 50 : 100;
            var list = new List<MathQuestion>(QuestionsPerLevel);
            var rng = new System.Random(HashSeed(grade, MathSubject.Addition, level) ^ 0x5AFE);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                int a = rng.Next(1, range);
                int b = rng.Next(1, range);
                int ans = a + b;
                var q = new MathQuestion
                {
                    prompt      = $"{a} + {b} = ?",
                    options     = SafeNonNegOptions(ans, Math.Max(2, ans / 4), rng),
                    hint        = QuestionStrings.StartAtAndCountUp(Math.Max(a, b), Math.Min(a, b)),
                    explanation = QuestionStrings.AddFormula(a, b, ans),
                    difficulty  = ScaleDifficulty(level),
                    visual      = QuestionVisual.TextOnly
                };
                q.correctIndex = IndexOf(q.options, ans.ToString());
                if (q.correctIndex < 0) q.correctIndex = 0; // last-resort guard
                list.Add(q);
            }
            return list;
        }

        public static MathSubject[] SubjectsFor(int grade)
        {
            switch (grade)
            {
                case 1: return new[]
                {
                    MathSubject.Counting, MathSubject.Addition, MathSubject.Subtraction,
                    MathSubject.Shapes, MathSubject.Patterns, MathSubject.Measurement,
                    MathSubject.Time, MathSubject.Money
                };
                case 2: return new[]
                {
                    MathSubject.Counting, MathSubject.Addition, MathSubject.Subtraction,
                    MathSubject.Multiplication, MathSubject.Shapes, MathSubject.Fractions,
                    MathSubject.Measurement, MathSubject.Time, MathSubject.Money
                };
                case 3: return new[]
                {
                    MathSubject.Addition, MathSubject.Subtraction, MathSubject.Multiplication,
                    MathSubject.Division, MathSubject.Shapes, MathSubject.Fractions,
                    MathSubject.Measurement, MathSubject.Time, MathSubject.Money
                };
                default: return Array.Empty<MathSubject>();
            }
        }

        public static string LessonIntro(int grade, MathSubject subject) =>
            QuestionStrings.LessonIntro(grade, subject);

        public static string LessonExample(int grade, MathSubject subject, int level) =>
            QuestionStrings.LessonExample(grade, subject);

        public static string LessonTip(MathSubject subject) => QuestionStrings.LessonTip(subject);

        /// <summary>Language-aware pretty name for a subject.</summary>
        public static string Pretty(MathSubject s) => QuestionStrings.SubjectPretty(s);

        // -------------------------------------------------------------------
        // Generators (with L11+ word problems + scaffolded hints)
        // -------------------------------------------------------------------

        private static List<MathQuestion> Counting(int grade, int level, System.Random rng)
        {
            int max = grade == 1
                ? Mathf.Min(30, 6 + level)
                : Mathf.Min(300, 20 + level * 14);
            int skip = grade == 1
                ? 1
                : (level <= 6 ? 2 : level <= 12 ? 5 : level <= 16 ? 10 : 25);

            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                int start  = rng.Next(0, Math.Max(1, max - skip * 3));
                int answer = start + skip * 3;
                var q = new MathQuestion
                {
                    prompt      = QuestionStrings.WhatComesNext(start, start + skip, start + skip * 2),
                    options     = SafeNonNegOptions(answer, Mathf.Max(1, skip), rng),
                    hint        = QuestionStrings.SkipCountBy(skip),
                    explanation = QuestionStrings.EachStepAdds(skip),
                    difficulty  = ScaleDifficulty(level),
                    visual      = QuestionVisual.NumberLine
                };
                q.correctIndex = IndexOf(q.options, answer.ToString());
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Addition(int grade, int level, System.Random rng)
        {
            int maxByGrade = grade switch { 1 => 100, 2 => 999, _ => 9999 };

            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (level <= 5)
                {
                    int a = rng.Next(1, 6), b = rng.Next(1, 6);
                    int ans = a + b;
                    string item = QuestionStrings.Item(rng.Next(QuestionStrings.ItemCount));
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.YouHaveAndGetMore(a, item, b),
                        options     = SafeNonNegOptions(ans, 2, rng),
                        hint        = QuestionStrings.StartAtAndCountUp(Math.Max(a, b), Math.Min(a, b)),
                        explanation = QuestionStrings.AddFormula(a, b, ans),
                        difficulty  = ScaleDifficulty(level),
                        visual      = grade == 1 ? QuestionVisual.Dots : QuestionVisual.TextOnly,
                        visualPayload = grade == 1 ? new[] { a, b } : Array.Empty<int>()
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 10)
                {
                    int cap = grade == 1 ? 25 : grade == 2 ? 100 : 999;
                    int a = rng.Next(1, cap), b = rng.Next(1, cap);
                    int ans = a + b;
                    q = new MathQuestion
                    {
                        prompt      = $"{a} + {b} = ?",
                        options     = SafeNonNegOptions(ans, Mathf.Max(2, cap / 10), rng),
                        hint        = QuestionStrings.StartAtAndCountUp(Math.Max(a, b), Math.Min(a, b)),
                        explanation = QuestionStrings.AddFormula(a, b, ans),
                        difficulty  = ScaleDifficulty(level),
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 15)
                {
                    string name = QuestionStrings.NameA(rng.Next(QuestionStrings.NameCount));
                    int cap = grade == 1 ? 30 : grade == 2 ? 99 : 500;
                    int a = rng.Next(5, cap), b = rng.Next(2, cap / 2 + 1);
                    int ans = a + b;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.NameStickers(name, a, b),
                        options     = WordOptions(ans, new[] { a - b, a, b, ans + 10 }, rng),
                        hint        = QuestionStrings.AddOf(a, b),
                        explanation = QuestionStrings.AddFormula(a, b, ans),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 19)
                {
                    string name = QuestionStrings.NameA(rng.Next(QuestionStrings.NameCount));
                    string friend = QuestionStrings.NameB(rng.Next(QuestionStrings.NameCount));
                    int cap = grade == 1 ? 30 : grade == 2 ? 150 : 800;
                    int a = rng.Next(10, cap);
                    int gave = rng.Next(1, a / 2);
                    int got  = rng.Next(2, cap / 3 + 1);
                    int ans = a - gave + got;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.NameMarbles(name, a, gave, friend, got),
                        options     = WordOptions(ans,
                            new[] { a + got, a - gave, a + gave - got, ans + gave }, rng),
                        hint        = QuestionStrings.TwoStepHint(),
                        explanation = $"{a} - {gave} + {got} = {ans}.",
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else
                {
                    string name = QuestionStrings.NameA(rng.Next(QuestionStrings.NameCount));
                    int cap = maxByGrade;
                    int a = rng.Next(cap / 3, cap / 2);
                    int b = rng.Next(cap / 10, cap / 4);
                    int c = rng.Next(cap / 10, cap / 4);
                    int ans = a + b - c;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.Allowance(name, a, b, c),
                        options     = WordOptions(ans,
                            new[] { a + b + c, a - b - c, a + b, ans + c }, rng),
                        hint        = QuestionStrings.AllowanceHint(a, b, c),
                        explanation = $"{a} + {b} - {c} = {ans}.",
                        difficulty  = QuestionDifficulty.VeryHard
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Subtraction(int grade, int level, System.Random rng)
        {
            int maxByGrade = grade switch { 1 => 100, 2 => 999, _ => 9999 };
            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (level <= 5)
                {
                    int a = rng.Next(3, 10), b = rng.Next(1, a);
                    int ans = a - b;
                    string item = QuestionStrings.Item(rng.Next(QuestionStrings.ItemCount));
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.YouHaveGiveAway(a, item, b),
                        options     = SafeNonNegOptions(ans, 2, rng),
                        hint        = QuestionStrings.CountBackFrom(b, a),
                        explanation = QuestionStrings.SubFormula(a, b, ans),
                        difficulty  = ScaleDifficulty(level),
                        visual      = grade == 1 ? QuestionVisual.Dots : QuestionVisual.TextOnly,
                        visualPayload = grade == 1 ? new[] { a, b } : Array.Empty<int>()
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 10)
                {
                    int cap = grade == 1 ? 25 : grade == 2 ? 100 : 999;
                    int a = rng.Next(10, cap), b = rng.Next(1, a);
                    int ans = a - b;
                    q = new MathQuestion
                    {
                        prompt      = $"{a} - {b} = ?",
                        options     = SafeNonNegOptions(ans, Mathf.Max(2, cap / 10), rng),
                        hint        = QuestionStrings.CountBackFrom(b, a),
                        explanation = QuestionStrings.SubFormula(a, b, ans),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 15)
                {
                    string name = QuestionStrings.NameA(rng.Next(QuestionStrings.NameCount));
                    int cap = grade == 1 ? 30 : grade == 2 ? 99 : 500;
                    int a = rng.Next(10, cap), b = rng.Next(2, a - 1);
                    int ans = a - b;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.NameCookies(name, a, b),
                        options     = WordOptions(ans, new[] { a + b, b, a, ans + b }, rng),
                        hint        = QuestionStrings.SubtractOf(a, b),
                        explanation = QuestionStrings.SubFormula(a, b, ans),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 19)
                {
                    string name = QuestionStrings.NameA(rng.Next(QuestionStrings.NameCount));
                    int cap = grade == 1 ? 50 : grade == 2 ? 200 : 800;
                    int a = rng.Next(cap / 2, cap);
                    int spend1 = rng.Next(5, a / 3);
                    int spend2 = rng.Next(5, cap / 4);
                    int ans = a - spend1 - spend2;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.NameSpends(name, a, spend1, spend2),
                        options     = WordOptions(ans,
                            new[] { a + spend1 + spend2, a - spend1, a - spend2, ans + spend2 }, rng),
                        hint        = QuestionStrings.SpendStepHint(),
                        explanation = $"{a} - {spend1} - {spend2} = {ans}.",
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else
                {
                    string name = QuestionStrings.NameA(rng.Next(QuestionStrings.NameCount));
                    int cap = maxByGrade;
                    int a = rng.Next(cap / 2, cap);
                    int b = rng.Next(cap / 10, cap / 4);
                    int c = rng.Next(cap / 10, cap / 4);
                    int ans = a - b - c;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.NamePoints(name, a, b, c),
                        options     = WordOptions(ans,
                            new[] { a - b, a - c, a + b - c, ans + b }, rng),
                        hint        = QuestionStrings.PointsHint(a, b, c),
                        explanation = $"{a} - {b} - {c} = {ans}.",
                        difficulty  = QuestionDifficulty.VeryHard
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Multiplication(int grade, int level, System.Random rng)
        {
            int maxFactor = grade == 2
                ? Math.Min(10, 2 + level / 2)
                : 12;
            int[] grade2Tables = { 2, 5, 10, 3, 4, 1, 6, 7, 8, 9 };

            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (level <= 5)
                {
                    int a = grade == 2 ? grade2Tables[(level - 1) % grade2Tables.Length] : rng.Next(2, 6);
                    int b = rng.Next(1, 6);
                    int ans = a * b;
                    string item = QuestionStrings.Item(rng.Next(QuestionStrings.ItemCount));
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.BagsItems(a, b, item),
                        options     = SafeNonNegOptions(ans, Math.Max(2, ans / 4), rng),
                        hint        = QuestionStrings.GroupsOf(a, b),
                        explanation = QuestionStrings.MulFormula(a, b, ans),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 10)
                {
                    int a = grade == 2 ? grade2Tables[(level - 1) % grade2Tables.Length] : rng.Next(2, maxFactor + 1);
                    int b = rng.Next(2, maxFactor + 1);
                    int ans = a * b;
                    q = new MathQuestion
                    {
                        prompt      = $"{a} x {b} = ?",
                        options     = SafeNonNegOptions(ans, Math.Max(2, ans / 4), rng),
                        hint        = QuestionStrings.ThinkGroupsOf(a, b),
                        explanation = QuestionStrings.MulFormula(a, b, ans),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 15)
                {
                    string name = QuestionStrings.NameA(rng.Next(QuestionStrings.NameCount));
                    int rows = rng.Next(2, 7), each = rng.Next(2, maxFactor + 1);
                    int ans = rows * each;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.FlowerRows(name, rows, each),
                        options     = WordOptions(ans, new[] { rows + each, rows * each + each, rows * (each - 1), ans + rows }, rng),
                        hint        = QuestionStrings.MultiplyOf(rows, each),
                        explanation = $"{rows} x {each} = {ans}.",
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 19)
                {
                    int a = rng.Next(10, 40), b = rng.Next(10, 40);
                    int boxesA = rng.Next(2, 5), boxesB = rng.Next(2, 5);
                    int ans = a * boxesA + b * boxesB;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.CrayonBoxes(boxesA, a, boxesB, b),
                        options     = WordOptions(ans,
                            new[] { a * boxesA, b * boxesB, ans - 10, ans + a }, rng),
                        hint        = QuestionStrings.CrayonHint(boxesA, a, boxesB, b),
                        explanation = QuestionStrings.CrayonExplain(boxesA, a, boxesB, b, ans),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else
                {
                    int a = rng.Next(8, 25), b = rng.Next(5, 20), c = rng.Next(3, 12);
                    int ans = a * b + c * b;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.BakeryBuns(a, c, b),
                        options     = WordOptions(ans,
                            new[] { (a + c) * b - b, a * b, c * b, ans + b }, rng),
                        hint        = QuestionStrings.BakeryHint(a, c, b),
                        explanation = QuestionStrings.BakeryExplain(a, c, b, ans),
                        difficulty  = QuestionDifficulty.VeryHard
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Division(int grade, int level, System.Random rng)
        {
            int maxFactor = Math.Min(12, 2 + level / 2);
            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (level <= 10)
                {
                    int b = rng.Next(2, maxFactor + 1);
                    int ans = rng.Next(1, maxFactor + 1);
                    int a = b * ans;
                    q = new MathQuestion
                    {
                        prompt      = $"{a} / {b} = ?",
                        options     = SafeNonNegOptions(ans, Math.Max(1, ans / 2 + 1), rng),
                        hint        = QuestionStrings.HowManyGroups(b, a),
                        explanation = QuestionStrings.DivExplain(a, b, ans),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (level <= 15)
                {
                    string name = QuestionStrings.NameA(rng.Next(QuestionStrings.NameCount));
                    int friends = rng.Next(2, 6);
                    int each    = rng.Next(2, 9);
                    int total   = friends * each;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.ShareCandies(name, total, friends),
                        options     = WordOptions(each, new[] { total - friends, friends + each, total / 2, each + 1 }, rng),
                        hint        = QuestionStrings.DivideBy(total, friends),
                        explanation = QuestionStrings.DivFormula(total, friends, each),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, each.ToString());
                }
                else if (level <= 19)
                {
                    int total = rng.Next(40, 120);
                    int groups = rng.Next(3, 8);
                    while (total % groups != 0) total++;
                    int each = total / groups;
                    int extra = rng.Next(2, 10);
                    int ans = each + extra;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.StudentsBuses(total, groups, extra),
                        options     = WordOptions(ans, new[] { each, total / groups - extra, extra, ans + groups }, rng),
                        hint        = QuestionStrings.DivStepHint(total, groups, extra),
                        explanation = $"{total} / {groups} + {extra} = {ans}.",
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else
                {
                    int total = rng.Next(120, 400);
                    int groups = rng.Next(4, 10);
                    while (total % groups != 0) total++;
                    int each = total / groups;
                    int taken = rng.Next(1, each / 2 + 1);
                    int ans = each - taken;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.FarmEggs("", total, groups, taken),
                        options     = WordOptions(ans, new[] { each, taken, total / groups + taken, ans + taken }, rng),
                        hint        = QuestionStrings.EggsStepHint(total, groups, taken),
                        explanation = $"{total} / {groups} - {taken} = {ans}.",
                        difficulty  = QuestionDifficulty.VeryHard
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Shapes(int grade, int level, System.Random rng)
        {
            // English keys are the canonical shape identifier (used to look up
            // clues / side counts); display strings come from QuestionStrings.
            string[] shapes2DKeys = { "Triangle", "Square", "Rectangle", "Circle", "Pentagon", "Hexagon", "Octagon" };
            int[]    sides        = { 3, 4, 4, 0, 5, 6, 8 };
            string[] shapes3DKeys = { "Cube", "Sphere", "Cylinder", "Cone", "Pyramid" };

            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;

                if (grade == 3)
                {
                    int spread = 2 + level;
                    int w = rng.Next(2, 2 + spread);
                    int h = rng.Next(2, 2 + spread);
                    bool perimeter = (i % 2 == 0);
                    int ans = perimeter ? 2 * (w + h) : w * h;
                    q = new MathQuestion
                    {
                        prompt      = perimeter
                            ? QuestionStrings.PerimeterPrompt(w, h)
                            : QuestionStrings.AreaPrompt(w, h),
                        options     = SafeNonNegOptions(ans, Math.Max(2, ans / 4), rng),
                        hint        = perimeter
                            ? QuestionStrings.PerimeterFormula()
                            : QuestionStrings.AreaFormula(),
                        explanation = perimeter
                            ? QuestionStrings.PerimeterExplain(w, h, ans)
                            : QuestionStrings.AreaExplain(w, h, ans),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.ShapePicker
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (grade == 2 && level > 10)
                {
                    int idx = rng.Next(shapes3DKeys.Length);
                    string answerKey = shapes3DKeys[idx];
                    string answer    = QuestionStrings.All3DShapes()[idx];

                    var allLocalized = QuestionStrings.All3DShapes();
                    var opts = new List<string>(allLocalized);
                    opts.RemoveAt(idx);
                    Shuffle(opts, rng);
                    var picks = new[] { answer, opts[0], opts[1], opts[2] };
                    Shuffle(picks, rng);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.ShapeClue3D(answerKey),
                        options     = picks,
                        hint        = QuestionStrings.ThinkRealObject(),
                        explanation = QuestionStrings.ItsAShape(answer),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.ShapePicker
                    };
                    q.correctIndex = IndexOf(q.options, answer);
                }
                else
                {
                    int idx = rng.Next(shapes2DKeys.Length);
                    string answerKey = shapes2DKeys[idx];
                    string answer    = QuestionStrings.All2DShapes()[idx];
                    bool sideQ = rng.Next(2) == 0;
                    if (sideQ && sides[idx] > 0)
                    {
                        int correctSides = sides[idx];
                        q = new MathQuestion
                        {
                            prompt      = QuestionStrings.HowManySides(answer),
                            options     = SafeNonNegOptions(correctSides, 2, rng),
                            hint        = QuestionStrings.CountSides(),
                            explanation = QuestionStrings.ShapeHasSides(answer, correctSides),
                            difficulty  = ScaleDifficulty(level),
                            visual      = QuestionVisual.ShapePicker
                        };
                        q.correctIndex = IndexOf(q.options, correctSides.ToString());
                    }
                    else
                    {
                        var allLocalized = QuestionStrings.All2DShapes();
                        var opts = new List<string>(allLocalized);
                        opts.Remove(answer);
                        Shuffle(opts, rng);
                        var picks = new[] { answer, opts[0], opts[1], opts[2] };
                        Shuffle(picks, rng);
                        q = new MathQuestion
                        {
                            prompt      = QuestionStrings.ShapeClue2D(answerKey),
                            options     = picks,
                            hint        = QuestionStrings.CountMySides(),
                            explanation = QuestionStrings.ItsAShape(answer),
                            difficulty  = ScaleDifficulty(level),
                            visual      = QuestionVisual.ShapePicker
                        };
                        q.correctIndex = IndexOf(q.options, answer);
                    }
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Patterns(int grade, int level, System.Random rng)
        {
            var list = new List<MathQuestion>(QuestionsPerLevel);
            string[] tokens = { "\ud83d\udd34", "\ud83d\udd35", "\ud83d\udfe2", "\ud83d\udfe1", "\ud83d\udfe3", "\ud83d\udfe0", "\u26aa" };
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (grade == 3 && level >= 11)
                {
                    int start = rng.Next(2, 10);
                    int op    = rng.Next(3);
                    int step  = rng.Next(2, 6);
                    var seq = new List<int> { start };
                    for (int k = 1; k < 4; k++)
                    {
                        int prev = seq[k - 1];
                        int next = op == 0 ? prev + step : op == 1 ? prev * step : prev + (k * step);
                        seq.Add(next);
                    }
                    int answer = op == 0 ? seq[3] + step
                              : op == 1 ? seq[3] * step
                              : seq[3] + (4 * step);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.FindNextNumber(seq[0], seq[1], seq[2], seq[3]),
                        options     = SafeNonNegOptions(answer, Math.Max(2, answer / 5), rng),
                        hint        = op == 0 ? QuestionStrings.EachStepAdds(step)
                                    : op == 1 ? QuestionStrings.EachStepMultiplies(step)
                                              : QuestionStrings.EachStepLarger(),
                        explanation = QuestionStrings.NextIs(answer),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Pattern
                    };
                    q.correctIndex = IndexOf(q.options, answer.ToString());
                }
                else
                {
                    int patternLen = Math.Min(4, 2 + (level / 6));
                    var pattern = new string[patternLen];
                    int paletteSize = Math.Min(tokens.Length, 3 + level / 4);
                    for (int j = 0; j < patternLen; j++)
                        pattern[j] = tokens[rng.Next(paletteSize)];
                    var seq = new List<string>();
                    for (int j = 0; j < 5; j++) seq.Add(pattern[j % patternLen]);
                    string answer = pattern[5 % patternLen];

                    var distractors = new List<string>(tokens);
                    distractors.Remove(answer);
                    Shuffle(distractors, rng);
                    var opts = new[] { answer, distractors[0], distractors[1], distractors[2] };
                    Shuffle(opts, rng);

                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.PatternWhatComesNext(string.Join(" ", seq)),
                        options     = opts,
                        hint        = QuestionStrings.PatternRepeats(patternLen),
                        explanation = QuestionStrings.PatternExplain(string.Join(" ", pattern), answer),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Pattern
                    };
                    q.correctIndex = IndexOf(q.options, answer);
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Fractions(int grade, int level, System.Random rng)
        {
            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (grade == 2)
                {
                    // Grade 2 = "Which fraction means ONE of N equal parts?".
                    // Pool the half / third / quarter / fifth / sixth labels and
                    // always include the simplified answer plus 3 distractors
                    // drawn from the same family.
                    int den = rng.Next(2, Math.Min(7, 3 + level / 4 + 1));
                    if (den < 2) den = 2;
                    int num = 1;
                    string label = $"{num}/{den}";
                    string[] poolArr = { "1/2", "1/3", "1/4", "1/5", "1/6" };
                    var pool = new List<string>(poolArr);
                    pool.Remove(label);
                    Shuffle(pool, rng);
                    // Defensive: pool is guaranteed to have 4 items after the
                    // remove, but if a future change adds duplicates we still
                    // need exactly 3 distractors.
                    while (pool.Count < 3) pool.Add($"1/{rng.Next(2, 10)}");
                    var picks = new[] { label, pool[0], pool[1], pool[2] };
                    Shuffle(picks, rng);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.WhichFractionOneOf(den),
                        options     = picks,
                        hint        = QuestionStrings.LookBottom(),
                        explanation = QuestionStrings.OneOfPartsExplain(den, label),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Fraction,
                        visualPayload = new[] { num, den }
                    };
                    q.correctIndex = IndexOf(picks, label);
                }
                else
                {
                    // Grade 3 = "Which fraction is equal to num/den?". Build
                    // an equivalent (eqNum/eqDen) as the correct answer and
                    // three NON-equivalent but plausible distractors.
                    //
                    // History:
                    //   v1: looped picking x/eqDen -> infinite loop when eqDen
                    //       was too small to produce 4 unique options.
                    //   v2: used num*alt/den*alt distractors -- but those are
                    //       MATHEMATICALLY EQUIVALENT to the answer (1/2 ==
                    //       2/4 == 3/6 == 4/8). Players who picked any of
                    //       them got marked wrong even though they were
                    //       actually right.
                    //   v3 (current): every distractor is checked with
                    //       IsEquivalent() before being added, so no
                    //       multiple-correct trap survives. Distractors are
                    //       chosen from common student mistakes (off-by-one
                    //       in num or den, unscaled num + scaled den, etc.)
                    //       so they still feel plausible.
                    int denCap = Math.Max(3, Math.Min(10, 3 + level / 3 + 1));
                    int den = rng.Next(2, denCap);
                    if (den < 2) den = 2;
                    int num = rng.Next(1, den);
                    int factor = rng.Next(2, 5);
                    int eqNum = num * factor;
                    int eqDen = den * factor;
                    string answer = $"{eqNum}/{eqDen}";

                    var opts = new List<string> { answer };

                    // Plausible-but-WRONG candidates. Each is rejected if it
                    // happens to equal the answer (num/den).
                    bool IsEq(int n2, int d2) =>
                        d2 != 0 && num * d2 == n2 * den;

                    var attempts = new List<(int n, int d)>
                    {
                        (eqNum + 1, eqDen),                          // num off by +1   (e.g. 3/4 for 2/4)
                        (Math.Max(1, eqNum - 1), eqDen),             // num off by -1   (e.g. 1/4 for 2/4)
                        (eqNum, eqDen + 1),                          // den off by +1   (e.g. 2/5 for 2/4)
                        (eqNum, Math.Max(2, eqDen - 1)),             // den off by -1   (e.g. 2/3 for 2/4)
                        (num + 1, den),                              // wrong simplified (e.g. 2/2 for 1/2)
                        (num, den + 1),                              // wrong simplified (e.g. 1/3 for 1/2)
                        (num * factor + 1, den * factor + 1),        // both off by +1
                        (num * (factor + 1), den * factor),          // numerator over-scaled
                        (num * factor, den * (factor + 1)),          // denominator over-scaled
                    };

                    foreach (var (cn, cd) in attempts)
                    {
                        if (opts.Count >= 4) break;
                        if (cn < 1 || cd < 2 || cd > 99) continue;
                        if (IsEq(cn, cd)) continue;
                        string cand = $"{cn}/{cd}";
                        if (cand == answer || opts.Contains(cand)) continue;
                        opts.Add(cand);
                    }

                    // Random non-equivalent fallback if the curated list ran out.
                    int safety = 0;
                    while (opts.Count < 4 && safety < 80)
                    {
                        int cd = rng.Next(2, 12);
                        int cn = rng.Next(1, cd + 2);
                        safety++;
                        if (IsEq(cn, cd)) continue;
                        string cand = $"{cn}/{cd}";
                        if (cand == answer || opts.Contains(cand)) continue;
                        opts.Add(cand);
                    }

                    // Absolute last resort -- synthetic distinct strings that
                    // are guaranteed non-equivalent (denominator above 99 can
                    // never simplify to a small num/den).
                    int pad = 100;
                    while (opts.Count < 4)
                    {
                        string c = $"7/{pad++}";
                        if (!opts.Contains(c) && c != answer) opts.Add(c);
                    }

                    var arr = opts.ToArray();
                    Shuffle(arr, rng);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.WhichFractionEqualTo(num, den),
                        options     = arr,
                        hint        = QuestionStrings.MultiplyTopBottom(factor),
                        explanation = QuestionStrings.FracEqExplain(num, den, answer),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Fraction,
                        visualPayload = new[] { num, den }
                    };
                    q.correctIndex = IndexOf(q.options, answer);
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Measurement(int grade, int level, System.Random rng)
        {
            var list = new List<MathQuestion>(QuestionsPerLevel);
            string[] units = { "cm", "m", "km", "g", "kg", "ml", "l" };
            string[] objects =
            {
                "pencil", "tree", "book", "spoon of sugar", "bag of rice",
                "glass of water", "bottle of juice", "ant", "elephant",
                "desk", "house", "car", "school bus", "phone"
            };

            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (grade == 3 && level >= 11)
                {
                    int kind = rng.Next(4);
                    switch (kind)
                    {
                        case 0:
                        {
                            int cm = rng.Next(1, 10) * 100;
                            int ans = cm / 100;
                            q = new MathQuestion
                            {
                                prompt      = QuestionStrings.HowManyMetres(cm),
                                options     = SafeNonNegOptions(ans, 2, rng),
                                hint        = QuestionStrings.TipCm(),
                                explanation = $"{cm} cm = {ans} m.",
                                difficulty  = ScaleDifficulty(level)
                            };
                            q.correctIndex = IndexOf(q.options, ans.ToString());
                            break;
                        }
                        case 1:
                        {
                            int m = rng.Next(1, 10) * 1000;
                            int ans = m / 1000;
                            q = new MathQuestion
                            {
                                prompt      = QuestionStrings.HowManyKilometres(m),
                                options     = SafeNonNegOptions(ans, 2, rng),
                                hint        = QuestionStrings.TipM(),
                                explanation = $"{m} m = {ans} km.",
                                difficulty  = ScaleDifficulty(level)
                            };
                            q.correctIndex = IndexOf(q.options, ans.ToString());
                            break;
                        }
                        case 2:
                        {
                            int g = rng.Next(1, 10) * 1000;
                            int ans = g / 1000;
                            q = new MathQuestion
                            {
                                prompt      = QuestionStrings.HowManyKg(g),
                                options     = SafeNonNegOptions(ans, 2, rng),
                                hint        = QuestionStrings.TipG(),
                                explanation = $"{g} g = {ans} kg.",
                                difficulty  = ScaleDifficulty(level)
                            };
                            q.correctIndex = IndexOf(q.options, ans.ToString());
                            break;
                        }
                        default:
                        {
                            int ml = rng.Next(1, 10) * 1000;
                            int ans = ml / 1000;
                            q = new MathQuestion
                            {
                                prompt      = QuestionStrings.HowManyLitres(ml),
                                options     = SafeNonNegOptions(ans, 2, rng),
                                hint        = QuestionStrings.TipMl(),
                                explanation = $"{ml} ml = {ans} l.",
                                difficulty  = ScaleDifficulty(level)
                            };
                            q.correctIndex = IndexOf(q.options, ans.ToString());
                            break;
                        }
                    }
                }
                else if (grade == 1)
                {
                    var pairs = new (string s, string l)[]
                    {
                        ("ant", "cat"), ("pencil", "ruler"), ("spoon", "broom"),
                        ("book", "desk"), ("cup", "bottle"), ("phone", "tv"),
                        ("crayon", "yard stick"), ("mouse", "horse")
                    };
                    var pair = pairs[rng.Next(pairs.Length)];
                    string sLocal = QuestionStrings.MeasureObject(pair.s);
                    string lLocal = QuestionStrings.MeasureObject(pair.l);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.WhichLonger(pair.s, pair.l),
                        options     = new[] { lLocal, sLocal,
                                              QuestionStrings.MeasureSame(),
                                              QuestionStrings.MeasureCannotTell() },
                        hint        = QuestionStrings.PictureBoth(),
                        explanation = QuestionStrings.LongerIs(pair.l),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.TextOnly
                    };
                    q.correctIndex = 0;
                }
                else
                {
                    string obj = objects[rng.Next(objects.Length)];
                    string unit = obj switch
                    {
                        "pencil" => "cm", "tree" => "m", "book" => "cm",
                        "spoon of sugar" => "g", "bag of rice" => "kg",
                        "glass of water" => "ml", "bottle of juice" => "l",
                        "ant" => "cm", "elephant" => "kg", "desk" => "m",
                        "house" => "m", "car" => "m", "school bus" => "m",
                        "phone" => "cm", _ => "cm"
                    };
                    var others = new List<string>(units);
                    others.Remove(unit);
                    Shuffle(others, rng);
                    var opts = new[] { unit, others[0], others[1], others[2] };
                    Shuffle(opts, rng);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.WhichUnitFor(obj),
                        options     = opts,
                        hint        = QuestionStrings.PickUnitMatchSize(),
                        explanation = QuestionStrings.MeasureWithUnit(obj, unit),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(opts, unit);
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Time(int grade, int level, System.Random rng)
        {
            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (grade == 3 && level >= 11)
                {
                    int h1 = rng.Next(1, 11);
                    int m1 = rng.Next(0, 12) * 5;
                    int extraMinutes = rng.Next(10, 90);
                    int totalMinutes = m1 + extraMinutes;
                    int h2 = h1 + totalMinutes / 60;
                    int m2 = totalMinutes % 60;
                    int ans = extraMinutes;
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.ElapsedTime(h1, m1, h2, m2),
                        options     = SafeNonNegOptions(ans, 10, rng),
                        hint        = QuestionStrings.ElapsedHint(),
                        explanation = QuestionStrings.ElapsedExplain(h1, m1, h2, m2, ans),
                        difficulty  = ScaleDifficulty(level)
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else
                {
                    int hour = rng.Next(1, 13);
                    int minute;
                    if (grade == 1) minute = 0;
                    else if (grade == 2)
                    {
                        minute = level <= 8 ? rng.Next(0, 4) * 15
                               : level <= 16 ? rng.Next(0, 12) * 5
                                            : rng.Next(0, 60);
                    }
                    else
                    {
                        minute = level <= 6 ? rng.Next(0, 12) * 5 : rng.Next(0, 60);
                    }
                    string answer = $"{hour:0}:{minute:00}";
                    var opts = new List<string> { answer };
                    // Bounded distractor search: grade 1 only has 12 unique
                    // "h:00" values so the search space is tiny -- without a
                    // safety counter a pathological RNG seed could spin here.
                    int safety = 0;
                    while (opts.Count < 4 && safety < 60)
                    {
                        int h = rng.Next(1, 13);
                        int m = grade == 1 ? 0 : rng.Next(0, 60);
                        string c = $"{h:0}:{m:00}";
                        if (!opts.Contains(c)) opts.Add(c);
                        safety++;
                    }
                    // Deterministic fallback walk through every hour in case
                    // the random search came up short (extremely unlikely but
                    // keeps the generator total).
                    for (int h = 1; h <= 12 && opts.Count < 4; h++)
                    {
                        string c = $"{h:0}:{(grade == 1 ? 0 : minute):00}";
                        if (!opts.Contains(c)) opts.Add(c);
                    }
                    var arr = opts.ToArray();
                    Shuffle(arr, rng);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.WhatTimeShown(),
                        options     = arr,
                        hint        = QuestionStrings.ClockHandsHint(),
                        explanation = QuestionStrings.ClockShows(answer),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.ClockFace,
                        visualPayload = new[] { hour, minute }
                    };
                    q.correctIndex = IndexOf(arr, answer);
                }
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Money(int grade, int level, System.Random rng)
        {
            var list = new List<MathQuestion>(QuestionsPerLevel);
            (string name, int cents)[] coins =
            {
                ("penny",   1),
                ("nickel",  5),
                ("dime",    10),
                ("quarter", 25)
            };
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;
                if (grade == 1)
                {
                    var c = coins[rng.Next(coins.Length)];
                    var others = new List<(string, int)>(coins);
                    others.Remove(c);
                    Shuffle(others, rng);
                    var opts = new[]
                    {
                        $"{c.cents}{QuestionStrings.CentSuffix}",
                        $"{others[0].Item2}{QuestionStrings.CentSuffix}",
                        $"{others[1].Item2}{QuestionStrings.CentSuffix}",
                        $"{others[2].Item2}{QuestionStrings.CentSuffix}"
                    };
                    Shuffle(opts, rng);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.HowManyCents(c.name),
                        options     = opts,
                        hint        = QuestionStrings.CoinValuesHint(),
                        explanation = QuestionStrings.CoinExplain(c.name, c.cents),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Money
                    };
                    q.correctIndex = IndexOf(opts, $"{c.cents}{QuestionStrings.CentSuffix}");
                }
                else if (grade == 2)
                {
                    int n = 2 + rng.Next(2 + level / 5);
                    int total = 0;
                    var picks = new List<string>();
                    for (int k = 0; k < n; k++)
                    {
                        var c = coins[rng.Next(coins.Length)];
                        picks.Add(QuestionStrings.CoinNameShort(c.name));
                        total += c.cents;
                    }
                    var opts = SafeNonNegOptions(total, Math.Max(5, total / 4), rng);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.AddCoins(string.Join(" + ", picks)),
                        options     = AppendCents(opts),
                        hint        = QuestionStrings.AddCoinValues(),
                        explanation = QuestionStrings.TotalCents(total),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Money
                    };
                    q.correctIndex = IndexOf(q.options, $"{total}{QuestionStrings.CentSuffix}");
                }
                else
                {
                    int price = rng.Next(15, 95);
                    int paid  = level <= 10 ? 100 : (rng.Next(2, 6) * 50);
                    if (paid <= price) paid = price + rng.Next(5, 50);
                    int change = paid - price;
                    var opts = SafeNonNegOptions(change, 10, rng);
                    q = new MathQuestion
                    {
                        prompt      = QuestionStrings.MakeChange(price, paid),
                        options     = AppendCents(opts),
                        hint        = QuestionStrings.ChangeFormula(paid),
                        explanation = QuestionStrings.ChangeExplain(paid, price, change),
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Money
                    };
                    q.correctIndex = IndexOf(q.options, $"{change}{QuestionStrings.CentSuffix}");
                }
                list.Add(q);
            }
            return list;
        }

        // -------------------------------------------------------------------
        // Internal helpers
        // -------------------------------------------------------------------

        private static QuestionDifficulty ScaleDifficulty(int level)
        {
            if (level <=  4) return QuestionDifficulty.VeryEasy;
            if (level <=  8) return QuestionDifficulty.Easy;
            if (level <= 12) return QuestionDifficulty.Medium;
            if (level <= 16) return QuestionDifficulty.Hard;
            return QuestionDifficulty.VeryHard;
        }

        private static int HashSeed(int g, MathSubject s, int l)
            => g * 1_000_000 + (int)s * 1_000 + l;

        /// <summary>Build 4 plausible options around `answer`, always non-negative.</summary>
        private static string[] SafeNonNegOptions(int answer, int spread, System.Random rng)
        {
            spread = Math.Max(1, spread);
            int safeAnswer = Math.Max(0, answer);
            var set = new HashSet<int> { safeAnswer };
            int safety = 0;
            while (set.Count < 4 && safety < 50)
            {
                int delta = rng.Next(-spread, spread + 1);
                if (delta == 0) delta = spread;
                int candidate = answer + delta;
                if (candidate < 0) candidate = Math.Abs(candidate);
                set.Add(candidate);
                safety++;
            }
            // Deterministic padding: walk *upwards* from the highest value
            // already in the set so we never collide with an existing entry
            // (the previous implementation could collide and spin forever).
            int next = (set.Count > 0 ? Max(set) : safeAnswer) + 1;
            int padGuard = 0;
            while (set.Count < 4 && padGuard < 100)
            {
                set.Add(next);
                next++;
                padGuard++;
            }
            var arr = new List<int>(set).ToArray();
            Shuffle(arr, rng);
            var strs = new string[4];
            // The set may still be < 4 in pathological cases (e.g. answer is
            // gigantic and padGuard overflowed): backfill with synthetic
            // distinct strings so callers always get a 4-slot array.
            int n = Math.Min(4, arr.Length);
            for (int i = 0; i < n; i++) strs[i] = arr[i].ToString();
            for (int i = n; i < 4; i++) strs[i] = (safeAnswer + 1000 + i).ToString();
            return strs;
        }

        private static int Max(HashSet<int> set)
        {
            int m = int.MinValue;
            foreach (var v in set) if (v > m) m = v;
            return m;
        }

        /// <summary>
        /// Build 4 options for word problems: the correct answer plus three
        /// plausible distractors derived from common arithmetic mistakes.
        ///
        /// Defensive: the `seen` set is keyed off the *unclamped* answer so
        /// the answer is never lost during dedup, and the padding loop is
        /// bounded so a hostile distractor set can't spin the main thread.
        /// </summary>
        private static string[] WordOptions(int answer, int[] distractors, System.Random rng)
        {
            var seen = new HashSet<int> { answer };
            var list = new List<int> { answer };
            foreach (var d in distractors)
            {
                if (list.Count >= 4) break;
                int v = d < 0 ? Math.Abs(d) : d;
                if (v == answer) v = answer + 1;
                if (seen.Add(v)) list.Add(v);
            }
            // Pad with monotonically increasing values starting *above* the
            // current max, so each iteration is guaranteed to add a new
            // entry. Guarded with a safety counter as belt-and-braces.
            int next = Math.Max(answer, 0);
            foreach (var v in seen) if (v > next) next = v;
            next++;
            int padGuard = 0;
            while (list.Count < 4 && padGuard < 100)
            {
                if (seen.Add(next)) list.Add(next);
                next++;
                padGuard++;
            }
            // Last-resort backfill with synthetic distinct values.
            int synth = Math.Max(answer, 0) + 1000;
            while (list.Count < 4)
            {
                while (seen.Contains(synth)) synth++;
                seen.Add(synth);
                list.Add(synth);
                synth++;
            }
            var arr = list.ToArray();
            Shuffle(arr, rng);
            var strs = new string[4];
            for (int i = 0; i < 4; i++) strs[i] = arr[i].ToString();
            return strs;
        }

        private static string[] AppendCents(string[] arr)
        {
            for (int i = 0; i < arr.Length; i++) arr[i] = arr[i] + QuestionStrings.CentSuffix;
            return arr;
        }

        /// <summary>
        /// Find the index of <paramref name="value"/> in <paramref name="arr"/>.
        /// Returns -1 if not found so the calling Generator can surface a
        /// genuinely broken question via MathQuestion.IsValid() (which fails
        /// on correctIndex &lt; 0) and let the wrapper in Generate() drop it.
        /// The previous behaviour silently returned 0, which marked the
        /// first option as correct even when the real answer was missing --
        /// causing "right answer flagged wrong" bugs in the field.
        /// </summary>
        private static int IndexOf<T>(T[] arr, T value)
        {
            if (arr == null) return -1;
            for (int i = 0; i < arr.Length; i++)
                if (Equals(arr[i], value)) return i;
            Debug.LogWarning($"[QuestionGenerator] Answer '{value}' not found in options [{string.Join(", ", arr)}]; question will be dropped.");
            return -1;
        }

        private static void Shuffle<T>(IList<T> arr, System.Random rng)
        {
            for (int i = arr.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}
