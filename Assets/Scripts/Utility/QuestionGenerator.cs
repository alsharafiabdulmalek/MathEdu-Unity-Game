// -----------------------------------------------------------------------------
// QuestionGenerator.cs
// -----------------------------------------------------------------------------
// Procedural generator that produces curriculum-correct math question sets
// for grades 1-3. Used by:
//   • The editor menu "MathEdu / Build Default Database" to populate the
//     bundled ScriptableObjects in one click.
//   • The runtime fallback inside DatabaseBootstrapper, so the game still
//     ships content even if no .asset files were authored.
//
// Curriculum coverage (Common-Core flavoured, grade-appropriate):
//   Grade 1: Counting 1-20, Addition <=20, Subtraction <=20, Shapes (2-D),
//            Patterns (AB/ABB), Measurement (compare), Time (hour),
//            Money (coin recognition).
//   Grade 2: Counting (skip 2/5/10), Addition <=100, Subtraction <=100,
//            Multiplication intro (x2, x5, x10), Shapes (2-D/3-D),
//            Fractions (1/2, 1/3, 1/4), Time (5-min), Money (coins+bills),
//            Measurement (units).
//   Grade 3: Addition (3-digit), Subtraction (3-digit), Multiplication tables
//            (1-10), Division (within 100), Fractions (equivalent),
//            Shapes (perimeter / area / 3-D), Time (minute), Measurement,
//            Money (making change).
//
// Each (grade, subject, level) yields a fixed-size question list whose
// difficulty escalates with the level number. We now ship 20 levels per
// subject (per Learning Mode), with the difficulty curve rebalanced so the
// final levels remain challenging but still solvable.
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
        /// </summary>
        public static List<MathQuestion> Generate(
            int grade, MathSubject subject, int level, int seed = 0)
        {
            var rng = new System.Random(seed != 0
                ? seed
                : HashSeed(grade, subject, level));

            switch (subject)
            {
                case MathSubject.Counting:       return Counting(grade, level, rng);
                case MathSubject.Addition:       return Addition(grade, level, rng);
                case MathSubject.Subtraction:    return Subtraction(grade, level, rng);
                case MathSubject.Multiplication: return Multiplication(grade, level, rng);
                case MathSubject.Division:       return Division(grade, level, rng);
                case MathSubject.Shapes:         return Shapes(grade, level, rng);
                case MathSubject.Patterns:       return Patterns(grade, level, rng);
                case MathSubject.Fractions:      return Fractions(grade, level, rng);
                case MathSubject.Measurement:    return Measurement(grade, level, rng);
                case MathSubject.Time:           return Time(grade, level, rng);
                case MathSubject.Money:          return Money(grade, level, rng);
                default:                         return new List<MathQuestion>();
            }
        }

        /// <summary>Subjects that should appear for a given grade.</summary>
        public static MathSubject[] SubjectsFor(int grade)
        {
            switch (grade)
            {
                case 1: return new[]
                {
                    MathSubject.Counting,
                    MathSubject.Addition,
                    MathSubject.Subtraction,
                    MathSubject.Shapes,
                    MathSubject.Patterns,
                    MathSubject.Measurement,
                    MathSubject.Time,
                    MathSubject.Money
                };
                case 2: return new[]
                {
                    MathSubject.Counting,
                    MathSubject.Addition,
                    MathSubject.Subtraction,
                    MathSubject.Multiplication,
                    MathSubject.Shapes,
                    MathSubject.Fractions,
                    MathSubject.Measurement,
                    MathSubject.Time,
                    MathSubject.Money
                };
                case 3: return new[]
                {
                    MathSubject.Addition,
                    MathSubject.Subtraction,
                    MathSubject.Multiplication,
                    MathSubject.Division,
                    MathSubject.Shapes,
                    MathSubject.Fractions,
                    MathSubject.Measurement,
                    MathSubject.Time,
                    MathSubject.Money
                };
                default: return Array.Empty<MathSubject>();
            }
        }

        public static string LessonIntro(int grade, MathSubject subject) =>
            $"Welcome to {Pretty(subject)} for Grade {grade}! " +
            "Read each question, look at the example, then choose the best answer.";

        public static string LessonExample(int grade, MathSubject subject, int level)
        {
            switch (subject)
            {
                case MathSubject.Counting:
                    return "Example: 1, 2, 3, ___. The next number is 4.";
                case MathSubject.Addition:
                    return grade == 1 ? "Example: 2 + 3 = 5."
                         : grade == 2 ? "Example: 24 + 13 = 37."
                                      : "Example: 245 + 138 = 383.";
                case MathSubject.Subtraction:
                    return grade == 1 ? "Example: 5 - 2 = 3."
                         : grade == 2 ? "Example: 47 - 12 = 35."
                                      : "Example: 642 - 215 = 427.";
                case MathSubject.Multiplication:
                    return grade == 2 ? "Example: 2 x 4 = 8 (two groups of four)."
                                      : "Example: 6 x 7 = 42.";
                case MathSubject.Division:
                    return "Example: 12 / 3 = 4 (twelve shared into 3 groups).";
                case MathSubject.Shapes:
                    return "Example: A shape with 3 sides is a triangle.";
                case MathSubject.Patterns:
                    return "Example: A, B, A, B, A, ___. Next is B.";
                case MathSubject.Fractions:
                    return grade == 2 ? "Example: 1/2 means one of two equal parts."
                                      : "Example: 2/4 is the same as 1/2.";
                case MathSubject.Measurement:
                    return "Example: A pencil is shorter than a desk.";
                case MathSubject.Time:
                    return grade == 1 ? "Example: When the long hand is on 12 and the short hand is on 3, it is 3 o'clock."
                                      : "Example: 3:15 means 15 minutes past 3.";
                case MathSubject.Money:
                    return "Example: A nickel = 5 cents, a dime = 10 cents.";
                default: return string.Empty;
            }
        }

        public static string LessonTip(MathSubject subject)
        {
            switch (subject)
            {
                case MathSubject.Addition:       return "Tip: Count up from the bigger number.";
                case MathSubject.Subtraction:    return "Tip: Count back, or think of the missing addend.";
                case MathSubject.Multiplication: return "Tip: Multiplication is repeated addition.";
                case MathSubject.Division:       return "Tip: Think 'how many groups?'.";
                case MathSubject.Fractions:      return "Tip: The bottom number is how many equal parts.";
                case MathSubject.Shapes:         return "Tip: Count the sides and corners.";
                case MathSubject.Patterns:       return "Tip: Look for the part that repeats.";
                case MathSubject.Time:           return "Tip: The short hand shows the hour.";
                case MathSubject.Money:          return "Tip: 100 cents make 1 dollar.";
                case MathSubject.Measurement:    return "Tip: Match the unit to the size of the object.";
                case MathSubject.Counting:       return "Tip: Whisper-count out loud.";
                default:                         return "Tip: Take your time and read carefully.";
            }
        }

        public static string Pretty(MathSubject s)
        {
            return s switch
            {
                MathSubject.Counting       => "Counting",
                MathSubject.Addition       => "Addition",
                MathSubject.Subtraction    => "Subtraction",
                MathSubject.Multiplication => "Multiplication",
                MathSubject.Division       => "Division",
                MathSubject.Shapes         => "Shapes",
                MathSubject.Patterns       => "Patterns",
                MathSubject.Fractions      => "Fractions",
                MathSubject.Measurement    => "Measurement",
                MathSubject.Time           => "Time",
                MathSubject.Money          => "Money",
                _                          => s.ToString()
            };
        }

        // -------------------------------------------------------------------
        // Generators -- one per subject (all rescaled to support up to 20 levels)
        // -------------------------------------------------------------------

        private static List<MathQuestion> Counting(int grade, int level, System.Random rng)
        {
            // Grade 1: count 1..N where N grows from 8 → 30 across 20 levels.
            // Grade 2/3: skip-counting by 2/5/10/25 with growing max.
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
                    prompt      = $"What comes next?\n{start}, {start + skip}, {start + skip * 2}, ?",
                    options     = ShuffleAround(answer, Mathf.Max(1, skip), rng),
                    hint        = $"Skip-count by {skip}.",
                    explanation = $"Each step adds {skip}.",
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
            // Operand ceiling escalates per level — 20 levels supported.
            int max = grade switch
            {
                1 => 4 + level,            // L1: 5..  L20: 24
                2 => 10 + level * 5,       // L1: 15.. L20: 110
                _ => 100 + level * 50      // L1: 150.. L20: 1100
            };
            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                int a = rng.Next(1, max);
                int b = rng.Next(1, max);
                int ans = a + b;
                var q = new MathQuestion
                {
                    prompt      = $"{a} + {b} = ?",
                    options     = ShuffleAround(ans, Mathf.Max(1, max / 10), rng),
                    hint        = $"Start at {Math.Max(a, b)} and count up by {Math.Min(a, b)}.",
                    explanation = $"{a} + {b} = {ans}.",
                    difficulty  = ScaleDifficulty(level),
                    visual      = grade == 1 && level <= 5 ? QuestionVisual.Dots : QuestionVisual.TextOnly,
                    visualPayload = grade == 1 ? new[] { a, b } : Array.Empty<int>()
                };
                q.correctIndex = IndexOf(q.options, ans.ToString());
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Subtraction(int grade, int level, System.Random rng)
        {
            int max = grade switch
            {
                1 => 4 + level,
                2 => 10 + level * 5,
                _ => 100 + level * 50
            };
            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                int a = rng.Next(2, max);
                int b = rng.Next(1, a);
                int ans = a - b;
                var q = new MathQuestion
                {
                    prompt      = $"{a} - {b} = ?",
                    options     = ShuffleAround(ans, Mathf.Max(1, max / 10), rng),
                    hint        = $"Count back {b} from {a}.",
                    explanation = $"{a} - {b} = {ans}.",
                    difficulty  = ScaleDifficulty(level),
                    visual      = grade == 1 && level <= 5 ? QuestionVisual.Dots : QuestionVisual.TextOnly,
                    visualPayload = grade == 1 ? new[] { a, b } : Array.Empty<int>()
                };
                q.correctIndex = IndexOf(q.options, ans.ToString());
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Multiplication(int grade, int level, System.Random rng)
        {
            int maxFactor = grade == 2
                ? Math.Min(10, 2 + level / 2) // Grade 2 grows more slowly
                : 12;
            int floor = grade == 2 ? 2 : 1;
            // Grade 2 cycles through friendly tables before mixing them up.
            int[] grade2Tables = { 2, 5, 10, 3, 4, 1, 6, 7, 8, 9 };

            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                int a = grade == 2
                    ? grade2Tables[(level - 1) % grade2Tables.Length]
                    : rng.Next(floor, maxFactor + 1);
                int b = rng.Next(floor, maxFactor + 1);
                int ans = a * b;
                var q = new MathQuestion
                {
                    prompt      = $"{a} x {b} = ?",
                    options     = ShuffleAround(ans, Math.Max(2, ans / 4), rng),
                    hint        = $"Think of {a} groups of {b}.",
                    explanation = $"{a} x {b} = {ans}.",
                    difficulty  = ScaleDifficulty(level)
                };
                q.correctIndex = IndexOf(q.options, ans.ToString());
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
                int b = rng.Next(2, maxFactor + 1);
                int ans = rng.Next(1, maxFactor + 1);
                int a = b * ans;
                var q = new MathQuestion
                {
                    prompt      = $"{a} / {b} = ?",
                    options     = ShuffleAround(ans, Math.Max(1, ans / 2 + 1), rng),
                    hint        = $"How many groups of {b} make {a}?",
                    explanation = $"{a} / {b} = {ans} (because {b} x {ans} = {a}).",
                    difficulty  = ScaleDifficulty(level)
                };
                q.correctIndex = IndexOf(q.options, ans.ToString());
                list.Add(q);
            }
            return list;
        }

        private static List<MathQuestion> Shapes(int grade, int level, System.Random rng)
        {
            // Grade 1: name 2-D shapes; Grade 2 (after L8): include 3-D;
            // Grade 3: perimeter / area / properties.
            string[] shapes2D = { "Triangle", "Square", "Rectangle", "Circle", "Pentagon", "Hexagon", "Octagon" };
            int[]    sides    = { 3,           4,        4,           0,        5,           6,         8        };
            string[] shapes3D = { "Cube", "Sphere", "Cylinder", "Cone", "Pyramid" };

            var list = new List<MathQuestion>(QuestionsPerLevel);
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                MathQuestion q;

                if (grade == 3)
                {
                    // Higher levels = larger rectangles.
                    int spread = 2 + level;
                    int w = rng.Next(2, 2 + spread);
                    int h = rng.Next(2, 2 + spread);
                    bool perimeter = (i % 2 == 0);
                    int ans = perimeter ? 2 * (w + h) : w * h;
                    q = new MathQuestion
                    {
                        prompt      = perimeter
                            ? $"A rectangle is {w} cm by {h} cm. What is its perimeter?"
                            : $"A rectangle is {w} cm by {h} cm. What is its area?",
                        options     = ShuffleAround(ans, Math.Max(2, ans / 4), rng),
                        hint        = perimeter ? "Perimeter = 2 x (width + height)."
                                                : "Area = width x height.",
                        explanation = perimeter
                            ? $"2 x ({w} + {h}) = {ans}."
                            : $"{w} x {h} = {ans}.",
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.ShapePicker
                    };
                    q.correctIndex = IndexOf(q.options, ans.ToString());
                }
                else if (grade == 2 && level > 10)
                {
                    int idx = rng.Next(shapes3D.Length);
                    string answer = shapes3D[idx];
                    var opts = new List<string>(shapes3D);
                    opts.RemoveAt(idx);
                    Shuffle(opts, rng);
                    var picks = new[] { answer, opts[0], opts[1], opts[2] };
                    Shuffle(picks, rng);
                    string clue = answer switch
                    {
                        "Cube"     => "I have 6 square faces.",
                        "Sphere"   => "I look like a ball.",
                        "Cylinder" => "I look like a soup can.",
                        "Cone"     => "I look like an ice-cream cone.",
                        "Pyramid"  => "I have a square base and a point on top.",
                        _ => "Guess the 3-D shape!"
                    };
                    q = new MathQuestion
                    {
                        prompt      = clue,
                        options     = picks,
                        hint        = "Think about a real object that has this shape.",
                        explanation = $"It's a {answer}.",
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.ShapePicker
                    };
                    q.correctIndex = IndexOf(q.options, answer);
                }
                else
                {
                    int idx = rng.Next(shapes2D.Length);
                    string answer = shapes2D[idx];
                    bool sideQ = rng.Next(2) == 0;
                    if (sideQ && sides[idx] > 0)
                    {
                        int correctSides = sides[idx];
                        q = new MathQuestion
                        {
                            prompt      = $"How many sides does a {answer.ToLower()} have?",
                            options     = ShuffleAround(correctSides, 2, rng),
                            hint        = "Count the straight edges.",
                            explanation = $"A {answer.ToLower()} has {correctSides} sides.",
                            difficulty  = ScaleDifficulty(level),
                            visual      = QuestionVisual.ShapePicker
                        };
                        q.correctIndex = IndexOf(q.options, correctSides.ToString());
                    }
                    else
                    {
                        var opts = new List<string>(shapes2D);
                        opts.Remove(answer);
                        Shuffle(opts, rng);
                        var picks = new[] { answer, opts[0], opts[1], opts[2] };
                        Shuffle(picks, rng);
                        string clue = answer switch
                        {
                            "Triangle"  => "I have 3 sides and 3 corners.",
                            "Square"    => "I have 4 equal sides.",
                            "Rectangle" => "I have 4 sides; two long and two short.",
                            "Circle"    => "I have no corners and I am round.",
                            "Pentagon"  => "I have 5 sides.",
                            "Hexagon"   => "I have 6 sides, like a honeycomb.",
                            "Octagon"   => "I have 8 sides, like a stop sign.",
                            _ => "Guess the shape!"
                        };
                        q = new MathQuestion
                        {
                            prompt      = clue,
                            options     = picks,
                            hint        = "Count my sides.",
                            explanation = $"It's a {answer.ToLower()}.",
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
            string[] tokens = { "A", "B", "C", "Star", "Circle", "Square", "Heart" };
            for (int i = 0; i < QuestionsPerLevel; i++)
            {
                int patternLen = Math.Min(4, 2 + (level / 6)); // 2 → 3 → 4 across 20 levels
                var pattern = new string[patternLen];
                int paletteSize = Math.Min(tokens.Length, 3 + level / 4);
                for (int j = 0; j < patternLen; j++)
                    pattern[j] = tokens[rng.Next(paletteSize)];
                // Build a sequence of 5 items
                var seq = new List<string>();
                for (int j = 0; j < 5; j++) seq.Add(pattern[j % patternLen]);
                string answer = pattern[5 % patternLen];

                var distractors = new List<string>(tokens);
                distractors.Remove(answer);
                Shuffle(distractors, rng);
                var opts = new[] { answer, distractors[0], distractors[1], distractors[2] };
                Shuffle(opts, rng);

                var q = new MathQuestion
                {
                    prompt      = $"What comes next?\n{string.Join(", ", seq)}, ?",
                    options     = opts,
                    hint        = $"The pattern repeats every {patternLen} items.",
                    explanation = $"Pattern: {string.Join(",", pattern)}. Next is {answer}.",
                    difficulty  = ScaleDifficulty(level),
                    visual      = QuestionVisual.Pattern
                };
                q.correctIndex = IndexOf(q.options, answer);
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
                    // Grade 2: identify halves/thirds/quarters/fifths/sixths.
                    int den = rng.Next(2, Math.Min(7, 3 + level / 4));
                    int num = 1;
                    string label = $"{num}/{den}";
                    string[] opts = { "1/2", "1/3", "1/4", "1/5", "1/6" };
                    // Pick four including label.
                    var pool = new List<string>(opts);
                    pool.Remove(label);
                    Shuffle(pool, rng);
                    var picks = new[] { label, pool[0], pool[1], pool[2] };
                    Shuffle(picks, rng);
                    q = new MathQuestion
                    {
                        prompt      = $"Which fraction means ONE of {den} equal parts?",
                        options     = picks,
                        hint        = "Look at the bottom number.",
                        explanation = $"One of {den} equal parts is written {label}.",
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Fraction,
                        visualPayload = new[] { num, den }
                    };
                    q.correctIndex = IndexOf(picks, label);
                }
                else
                {
                    // Grade 3: equivalent fractions.
                    int den = rng.Next(2, Math.Min(10, 3 + level / 3));
                    int num = rng.Next(1, den);
                    int factor = rng.Next(2, 5);
                    int eqNum = num * factor;
                    int eqDen = den * factor;
                    string answer = $"{eqNum}/{eqDen}";
                    var opts = new List<string> { answer };
                    while (opts.Count < 4)
                    {
                        string c = $"{rng.Next(1, eqDen)}/{eqDen}";
                        if (!opts.Contains(c) && c != answer) opts.Add(c);
                    }
                    var arr = opts.ToArray();
                    Shuffle(arr, rng);
                    q = new MathQuestion
                    {
                        prompt      = $"Which fraction is equal to {num}/{den}?",
                        options     = arr,
                        hint        = $"Multiply top and bottom by {factor}.",
                        explanation = $"{num}/{den} = {answer}.",
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
                if (grade == 1)
                {
                    var pairs = new (string s, string l)[]
                    {
                        ("ant", "cat"), ("pencil", "ruler"), ("spoon", "broom"),
                        ("book", "desk"), ("cup", "bottle"), ("phone", "tv"),
                        ("crayon", "yard stick"), ("mouse", "horse")
                    };
                    var pair = pairs[rng.Next(pairs.Length)];
                    q = new MathQuestion
                    {
                        prompt      = $"Which is LONGER, a {pair.s} or a {pair.l}?",
                        options     = new[] { pair.l, pair.s, "Same", "Cannot tell" },
                        hint        = "Picture both objects.",
                        explanation = $"The {pair.l} is longer.",
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
                        "pencil"           => "cm",
                        "tree"             => "m",
                        "book"             => "cm",
                        "spoon of sugar"   => "g",
                        "bag of rice"      => "kg",
                        "glass of water"   => "ml",
                        "bottle of juice"  => "l",
                        "ant"              => "cm",
                        "elephant"         => "kg",
                        "desk"             => "m",
                        "house"            => "m",
                        "car"              => "m",
                        "school bus"       => "m",
                        "phone"            => "cm",
                        _ => "cm"
                    };
                    var others = new List<string>(units);
                    others.Remove(unit);
                    Shuffle(others, rng);
                    var opts = new[] { unit, others[0], others[1], others[2] };
                    Shuffle(opts, rng);
                    q = new MathQuestion
                    {
                        prompt      = $"Which unit best measures a {obj}?",
                        options     = opts,
                        hint        = "Pick a unit that matches the size.",
                        explanation = $"We measure a {obj} in {unit}.",
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
                int hour = rng.Next(1, 13);
                int minute;
                if (grade == 1) minute = 0;
                else if (grade == 2)
                {
                    // Grade 2 progresses from quarter-hours → 5-min → odd intervals.
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
                while (opts.Count < 4)
                {
                    int h = rng.Next(1, 13);
                    int m = grade == 1 ? 0 : rng.Next(0, 60);
                    string c = $"{h:0}:{m:00}";
                    if (!opts.Contains(c)) opts.Add(c);
                }
                var arr = opts.ToArray();
                Shuffle(arr, rng);

                var q = new MathQuestion
                {
                    prompt      = "What time is shown on the clock?",
                    options     = arr,
                    hint        = "The short hand is the hour. The long hand is the minute.",
                    explanation = $"The clock shows {answer}.",
                    difficulty  = ScaleDifficulty(level),
                    visual      = QuestionVisual.ClockFace,
                    visualPayload = new[] { hour, minute }
                };
                q.correctIndex = IndexOf(arr, answer);
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
                        $"{c.cents}c",
                        $"{others[0].Item2}c",
                        $"{others[1].Item2}c",
                        $"{others[2].Item2}c"
                    };
                    Shuffle(opts, rng);
                    q = new MathQuestion
                    {
                        prompt      = $"How many cents is a {c.name}?",
                        options     = opts,
                        hint        = "Penny=1, Nickel=5, Dime=10, Quarter=25.",
                        explanation = $"A {c.name} is worth {c.cents} cents.",
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Money
                    };
                    q.correctIndex = IndexOf(opts, $"{c.cents}c");
                }
                else if (grade == 2)
                {
                    int n = 2 + rng.Next(2 + level / 5); // more coins at higher levels
                    int total = 0;
                    var picks = new List<string>();
                    for (int k = 0; k < n; k++)
                    {
                        var c = coins[rng.Next(coins.Length)];
                        picks.Add(c.name);
                        total += c.cents;
                    }
                    var opts = ShuffleAround(total, Math.Max(5, total / 4), rng);
                    q = new MathQuestion
                    {
                        prompt      = $"Add the coins: {string.Join(" + ", picks)}. Total cents?",
                        options     = AppendCents(opts),
                        hint        = "Add the value of each coin.",
                        explanation = $"Total = {total} cents.",
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Money
                    };
                    q.correctIndex = IndexOf(q.options, $"{total}c");
                }
                else
                {
                    int price = rng.Next(15, 95);
                    int paid  = level <= 10 ? 100 : (rng.Next(2, 6) * 50); // 1$..2.50$
                    if (paid <= price) paid = price + rng.Next(5, 50);
                    int change = paid - price;
                    var opts = ShuffleAround(change, 10, rng);
                    q = new MathQuestion
                    {
                        prompt      = $"You buy a snack for {price}c and pay {paid}c. What is your change?",
                        options     = AppendCents(opts),
                        hint        = $"Change = {paid} - price.",
                        explanation = $"{paid} - {price} = {change} cents.",
                        difficulty  = ScaleDifficulty(level),
                        visual      = QuestionVisual.Money
                    };
                    q.correctIndex = IndexOf(q.options, $"{change}c");
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
            // 20-level curve.
            if (level <=  4) return QuestionDifficulty.VeryEasy;
            if (level <=  8) return QuestionDifficulty.Easy;
            if (level <= 12) return QuestionDifficulty.Medium;
            if (level <= 16) return QuestionDifficulty.Hard;
            return QuestionDifficulty.VeryHard;
        }

        private static int HashSeed(int g, MathSubject s, int l)
            => g * 1_000_000 + (int)s * 1_000 + l;

        /// <summary>
        /// Build a 4-option string array that includes the correct answer and
        /// three nearby distractors (numeric variants of the correct value).
        /// </summary>
        private static string[] ShuffleAround(int answer, int spread, System.Random rng)
        {
            spread = Math.Max(1, spread);
            var set = new HashSet<int> { answer };
            int safety = 0;
            while (set.Count < 4 && safety < 30)
            {
                int delta = rng.Next(-spread, spread + 1);
                if (delta == 0) delta = spread;
                int candidate = answer + delta;
                if (candidate < 0) candidate = Math.Abs(candidate) + 1;
                set.Add(candidate);
                safety++;
            }
            while (set.Count < 4) set.Add(answer + set.Count); // pathological fallback
            var arr = new List<int>(set).ToArray();
            Shuffle(arr, rng);
            var strs = new string[4];
            for (int i = 0; i < 4; i++) strs[i] = arr[i].ToString();
            return strs;
        }

        private static string[] AppendCents(string[] arr)
        {
            for (int i = 0; i < arr.Length; i++) arr[i] = arr[i] + "c";
            return arr;
        }

        private static int IndexOf<T>(T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
                if (Equals(arr[i], value)) return i;
            return 0;
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
