// -----------------------------------------------------------------------------
// QuestionStrings.cs
// -----------------------------------------------------------------------------
// All question-generator-facing strings (prompts, hints, explanations, items,
// names, shape names, story intros/outros, etc.) live here so the curriculum
// can be regenerated in either English or Arabic without touching the math
// logic inside QuestionGenerator / DatabaseBootstrapper.
//
// Every method here switches on `Localization.IsRTL` and returns either the
// English template (the original prompt copy) or the Arabic equivalent.
//
// Note on Arabic numerals: the design returns Western-Arabic digits (0-9) so
// the math symbols read the same in both languages. Arabic prose uses the
// Arabic comma (\u060c) and question mark (\u061f) where appropriate.
//
// When the player flips Settings -> Language, `SettingsManager.SwitchLanguage`
// calls `DatabaseBootstrapper.ClearCachedLevelContent` which nulls out every
// LevelData's questions / lessonIntro / lessonExample / lessonTip + story
// fields and they get refilled on the next access via
// `DatabaseBootstrapper.EnsureLevelContent`. So every prompt the player sees
// always reads in the currently-selected language - no restart required.
//
// === Grade 4 & 5 extensions (new section at the bottom of the file) =========
// Adds prompts/hints/explanations for the advanced curriculum:
//   * 5-6 digit addition & subtraction word problems.
//   * 2x2-digit and 3x1-digit multiplication word problems.
//   * Long division with remainders (G4) and 2-digit divisors (G5).
//   * Triangle area, angle classification, parallelogram (G4 Shapes).
//   * Cube/rectangular prism volume, composite shapes (G5 Shapes).
//   * Fraction add/sub same denom (G4), unlike denom (G5), simplify, compare.
//   * Number rules (find the rule 2n+1), position-to-term (G5 Patterns).
//   * Compound unit conversions (G4/G5 Measurement) - mm, cm, m, km, ml, l.
//   * 24-hour time, multi-step time word problems (G4/G5 Time).
//   * Multi-item totals (G4 Money), percentage discounts (G5 Money).
// -----------------------------------------------------------------------------

using System;
using MathEdu.Data;

namespace MathEdu.Utility
{
    public static class QuestionStrings
    {
        private static bool Ar => Localization.IsRTL;

        // ----- Items / characters / shapes (lookups + translations) ------------

        // Object pool used by L1-5 word problems ("You have 3 apples...").
        // Returns a localized noun like "apples" / "\u062a\u0641\u0627\u062d\u0627\u062a".
        public static string Item(int idx)
        {
            string[] en = { "apples", "balls", "stickers", "blocks", "crayons", "marbles", "candies", "cards" };
            string[] ar = { "\u062a\u0641\u0627\u062d\u0627\u062a", "\u0643\u0631\u0627\u062a", "\u0645\u0644\u0635\u0642\u0627\u062a", "\u0645\u0643\u0639\u0628\u0627\u062a", "\u0623\u0642\u0644\u0627\u0645 \u062a\u0644\u0648\u064a\u0646", "\u0643\u0631\u0627\u062a \u0632\u062c\u0627\u062c\u064a\u0629", "\u062d\u0644\u0648\u064a\u0627\u062a", "\u0628\u0637\u0627\u0642\u0627\u062a" };
            idx = Math.Abs(idx) % en.Length;
            return Ar ? ar[idx] : en[idx];
        }
        public const int ItemCount = 8;

        // Named children for word problems ("Sam has 5 stickers...").
        private static readonly string[] NamesAEn = { "Sam", "Ava", "Leo", "Maya", "Owen", "Zoe", "Kai", "Mia" };
        private static readonly string[] NamesAAr = { "\u0633\u0627\u0645\u0631", "\u0622\u0644\u0627\u0621", "\u0644\u064a\u062b", "\u0645\u0647\u0627", "\u0639\u0645\u0631", "\u0632\u064a\u0646\u0629", "\u0643\u0631\u064a\u0645", "\u0645\u064a\u0633\u0627\u0621" };
        private static readonly string[] NamesBEn = { "Alex", "Lily", "Noah", "Aria", "Mateo", "Ivy", "Ren", "Eli" };
        private static readonly string[] NamesBAr = { "\u0623\u062d\u0645\u062f", "\u0644\u064a\u0644\u0649", "\u0646\u0648\u062d", "\u0631\u064a\u0645", "\u0645\u062d\u0645\u062f", "\u0625\u064a\u0645\u0627\u0646", "\u0631\u0646\u0627", "\u0639\u0644\u064a" };

        public static string NameA(int idx)
        {
            idx = Math.Abs(idx) % NamesAEn.Length;
            return Ar ? NamesAAr[idx] : NamesAEn[idx];
        }
        public static string NameB(int idx)
        {
            idx = Math.Abs(idx) % NamesBEn.Length;
            return Ar ? NamesBAr[idx] : NamesBEn[idx];
        }
        public const int NameCount = 8;

        // ----- Subject "pretty" names ----------------------------------------
        public static string SubjectPretty(MathSubject s)
        {
            if (Ar) return s switch
            {
                MathSubject.Counting       => "\u0627\u0644\u0639\u062f\u0651",
                MathSubject.Addition       => "\u0627\u0644\u062c\u0645\u0639",
                MathSubject.Subtraction    => "\u0627\u0644\u0637\u0631\u062d",
                MathSubject.Multiplication => "\u0627\u0644\u0636\u0631\u0628",
                MathSubject.Division       => "\u0627\u0644\u0642\u0633\u0645\u0629",
                MathSubject.Shapes         => "\u0627\u0644\u0623\u0634\u0643\u0627\u0644",
                MathSubject.Patterns       => "\u0627\u0644\u0623\u0646\u0645\u0627\u0637",
                MathSubject.Fractions      => "\u0627\u0644\u0643\u0633\u0648\u0631",
                MathSubject.Measurement    => "\u0627\u0644\u0642\u064a\u0627\u0633",
                MathSubject.Time           => "\u0627\u0644\u0648\u0642\u062a",
                MathSubject.Money          => "\u0627\u0644\u0646\u0642\u0648\u062f",
                _                          => s.ToString()
            };
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

        // ----- 2D / 3D shape vocabulary --------------------------------------
        public static string[] All2DShapes() =>
            Ar ? new[] { "\u0645\u062b\u0644\u062b", "\u0645\u0631\u0628\u0639", "\u0645\u0633\u062a\u0637\u064a\u0644", "\u062f\u0627\u0626\u0631\u0629", "\u062e\u0645\u0627\u0633\u064a", "\u0633\u062f\u0627\u0633\u064a", "\u062b\u0645\u0627\u0646\u064a" }
               : new[] { "Triangle", "Square", "Rectangle", "Circle", "Pentagon", "Hexagon", "Octagon" };

        public static string[] All3DShapes() =>
            Ar ? new[] { "\u0645\u0643\u0639\u0628", "\u0643\u0631\u0629", "\u0623\u0633\u0637\u0648\u0627\u0646\u0629", "\u0645\u062e\u0631\u0648\u0637", "\u0647\u0631\u0645" }
               : new[] { "Cube", "Sphere", "Cylinder", "Cone", "Pyramid" };

        public static string ShapeClue2D(string shapeNameEn)
        {
            if (Ar) return shapeNameEn switch
            {
                "Triangle"  => "\u0644\u064a 3 \u0623\u0636\u0644\u0627\u0639 \u06483 \u0632\u0648\u0627\u064a\u0627.",
                "Square"    => "\u0644\u064a 4 \u0623\u0636\u0644\u0627\u0639 \u0645\u062a\u0633\u0627\u0648\u064a\u0629.",
                "Rectangle" => "\u0644\u064a 4 \u0623\u0636\u0644\u0627\u0639\u061b \u0636\u0644\u0639\u0627\u0646 \u0637\u0648\u064a\u0644\u0627\u0646 \u0648\u0636\u0644\u0639\u0627\u0646 \u0642\u0635\u064a\u0631\u0627\u0646.",
                "Circle"    => "\u0644\u064a\u0633 \u0644\u064a \u0632\u0648\u0627\u064a\u0627 \u0648\u0623\u0646\u0627 \u0645\u0633\u062a\u062f\u064a\u0631.",
                "Pentagon"  => "\u0644\u064a 5 \u0623\u0636\u0644\u0627\u0639.",
                "Hexagon"   => "\u0644\u064a 6 \u0623\u0636\u0644\u0627\u0639\u060c \u0645\u062b\u0644 \u0642\u0631\u0635 \u0627\u0644\u0639\u0633\u0644.",
                "Octagon"   => "\u0644\u064a 8 \u0623\u0636\u0644\u0627\u0639\u060c \u0645\u062b\u0644 \u0625\u0634\u0627\u0631\u0629 \u0627\u0644\u062a\u0648\u0642\u0641.",
                _ => "\u062e\u0645\u0651\u0646 \u0627\u0644\u0634\u0643\u0644!"
            };
            return shapeNameEn switch
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
        }

        public static string ShapeClue3D(string shapeNameEn)
        {
            if (Ar) return shapeNameEn switch
            {
                "Cube"     => "\u0644\u064a 6 \u0648\u062c\u0648\u0647 \u0645\u0631\u0628\u0651\u0639\u0629 \u06488 \u0631\u0624\u0648\u0633 \u064812 \u062d\u0631\u0641\u064b\u0627.",
                "Sphere"   => "\u0623\u0628\u062f\u0648 \u0643\u0643\u0631\u0629 \u0648\u0644\u064a\u0633 \u0644\u064a \u062d\u0648\u0627\u0641.",
                "Cylinder" => "\u0644\u064a \u062f\u0627\u0626\u0631\u062a\u0627\u0646 \u0645\u0633\u0637\u0651\u062d\u062a\u0627\u0646 \u0648\u062c\u0627\u0646\u0628 \u0645\u0646\u062d\u0646\u064d.",
                "Cone"     => "\u0644\u064a \u062f\u0627\u0626\u0631\u0629 \u0645\u0633\u0637\u0651\u062d\u0629 \u0648\u0627\u062d\u062f\u0629 \u0648\u0642\u0645\u0651\u0629 \u0648\u0627\u062d\u062f\u0629.",
                "Pyramid"  => "\u0644\u064a \u0642\u0627\u0639\u062f\u0629 \u0645\u0631\u0628\u0651\u0639\u0629 \u06484 \u0648\u062c\u0648\u0647 \u0645\u062b\u0644\u0651\u062b\u0629.",
                _ => "\u062e\u0645\u0651\u0646 \u0627\u0644\u0634\u0643\u0644 \u062b\u0644\u0627\u062b\u064a \u0627\u0644\u0623\u0628\u0639\u0627\u062f!"
            };
            return shapeNameEn switch
            {
                "Cube"     => "I have 6 square faces, 8 vertices, 12 edges.",
                "Sphere"   => "I look like a ball and have no edges.",
                "Cylinder" => "I have two flat circles and a curved side.",
                "Cone"     => "I have one flat circle and a single point.",
                "Pyramid"  => "I have a square base and 4 triangular faces.",
                _ => "Guess the 3-D shape!"
            };
        }

        public static string HowManySides(string shapeName) =>
            Ar ? $"\u0643\u0645 \u0636\u0644\u0639\u064b\u0627 \u0644\u0644\u0634\u0643\u0644 {shapeName}\u061f"
               : $"How many sides does a {shapeName.ToLower()} have?";

        public static string CountSides() => Ar ? "\u0627\u062d\u0633\u0628 \u0627\u0644\u062d\u0648\u0627\u0641 \u0627\u0644\u0645\u0633\u062a\u0642\u064a\u0645\u0629." : "Count the straight edges.";
        public static string CountMySides() => Ar ? "\u0627\u062d\u0633\u0628 \u0623\u0636\u0644\u0627\u0639\u064a." : "Count my sides.";
        public static string ShapeHasSides(string shapeName, int sides) =>
            Ar ? $"\u064a\u062d\u062a\u0648\u064a \u0627\u0644\u0634\u0643\u0644 {shapeName} \u0639\u0644\u0649 {sides} \u0623\u0636\u0644\u0627\u0639."
               : $"A {shapeName.ToLower()} has {sides} sides.";
        public static string ItsAShape(string shapeName) =>
            Ar ? $"\u0625\u0646\u0647 {shapeName}." : $"It's a {shapeName.ToLower()}.";
        public static string ThinkRealObject() =>
            Ar ? "\u0641\u0643\u0651\u0631 \u0641\u064a \u062c\u0633\u0645 \u062d\u0642\u064a\u0642\u064a \u0628\u0647\u0630\u0627 \u0627\u0644\u0634\u0643\u0644." : "Think of a real object that has this shape.";

        // Measurement objects + units
        public static string MeasureObject(string en)
        {
            if (!Ar) return en;
            return en switch
            {
                "pencil"            => "\u0642\u0644\u0645 \u0631\u0635\u0627\u0635",
                "tree"              => "\u0634\u062c\u0631\u0629",
                "book"              => "\u0643\u062a\u0627\u0628",
                "spoon of sugar"    => "\u0645\u0644\u0639\u0642\u0629 \u0633\u0643\u0631",
                "bag of rice"       => "\u0643\u064a\u0633 \u0623\u0631\u0632",
                "glass of water"    => "\u0643\u0648\u0628 \u0645\u0627\u0621",
                "bottle of juice"   => "\u0632\u062c\u0627\u062c\u0629 \u0639\u0635\u064a\u0631",
                "ant"               => "\u0646\u0645\u0644\u0629",
                "elephant"          => "\u0641\u064a\u0644",
                "desk"              => "\u0637\u0627\u0648\u0644\u0629",
                "house"             => "\u0645\u0646\u0632\u0644",
                "car"               => "\u0633\u064a\u0651\u0627\u0631\u0629",
                "school bus"        => "\u062d\u0627\u0641\u0644\u0629 \u0645\u062f\u0631\u0633\u0629",
                "phone"             => "\u0647\u0627\u062a\u0641",
                "cat"               => "\u0642\u0637\u0651\u0629",
                "ruler"             => "\u0645\u0633\u0637\u0631\u0629",
                "spoon"             => "\u0645\u0644\u0639\u0642\u0629",
                "broom"             => "\u0645\u0643\u0646\u0633\u0629",
                "cup"               => "\u0643\u0648\u0628",
                "bottle"            => "\u0632\u062c\u0627\u062c\u0629",
                "tv"                => "\u062a\u0644\u0641\u0627\u0632",
                "crayon"            => "\u0642\u0644\u0645 \u062a\u0644\u0648\u064a\u0646",
                "yard stick"        => "\u0630\u0631\u0627\u0639 \u0642\u064a\u0627\u0633",
                "mouse"             => "\u0641\u0623\u0631",
                "horse"             => "\u062d\u0635\u0627\u0646",
                _                   => en
            };
        }

        public static string WhichLonger(string sObj, string lObj) =>
            Ar ? $"\u0623\u064a\u0647\u0645\u0627 \u0623\u0637\u0648\u0644\u060c {MeasureObject(sObj)} \u0623\u0645 {MeasureObject(lObj)}\u061f"
               : $"Which is LONGER, a {sObj} or a {lObj}?";

        public static string WhichUnitFor(string obj) =>
            Ar ? $"\u0623\u064a \u0648\u062d\u062f\u0629 \u0642\u064a\u0627\u0633 \u062a\u0646\u0627\u0633\u0628 {MeasureObject(obj)}\u061f"
               : $"Which unit best measures a {obj}?";

        public static string MeasureWithUnit(string obj, string unit) =>
            Ar ? $"\u0646\u0642\u064a\u0633 {MeasureObject(obj)} \u0628\u0640{unit}."
               : $"We measure a {obj} in {unit}.";

        public static string MeasureSame() => Ar ? "\u0645\u062a\u0633\u0627\u0648\u064a\u0627\u0646" : "Same";
        public static string MeasureCannotTell() => Ar ? "\u0644\u0627 \u064a\u0645\u0643\u0646 \u0627\u0644\u062a\u062d\u062f\u064a\u062f" : "Cannot tell";

        public static string PictureBoth() => Ar ? "\u062a\u062e\u064a\u0651\u0644 \u0643\u0644\u0627 \u0627\u0644\u062c\u0633\u0645\u064a\u0646." : "Picture both objects.";
        public static string PickUnitMatchSize() => Ar ? "\u0627\u062e\u062a\u0631 \u0648\u062d\u062f\u0629 \u062a\u0646\u0627\u0633\u0628 \u062d\u062c\u0645 \u0627\u0644\u062c\u0633\u0645." : "Pick a unit that matches the size.";
        public static string LongerIs(string lObj) => Ar ? $"{MeasureObject(lObj)} \u0623\u0637\u0648\u0644." : $"The {lObj} is longer.";

        public static string HowManyMetres(int cm) =>
            Ar ? $"\u0643\u0645 \u0645\u062a\u0631\u064b\u0627 \u0641\u064a {cm} \u0633\u0646\u062a\u064a\u0645\u062a\u0631\u061f"
               : $"How many metres are in {cm} centimetres?";
        public static string TipCm() => Ar ? "100 \u0633\u0645 = 1 \u0645. \u0627\u0642\u0633\u0645 \u0639\u0644\u0649 100." : "100 cm = 1 m. Divide by 100.";

        public static string HowManyKilometres(int m) =>
            Ar ? $"\u0643\u0645 \u0643\u064a\u0644\u0648\u0645\u062a\u0631\u064b\u0627 \u0641\u064a {m} \u0645\u062a\u0631\u061f"
               : $"How many kilometres are in {m} metres?";
        public static string TipM() => Ar ? "1000 \u0645 = 1 \u0643\u0645. \u0627\u0642\u0633\u0645 \u0639\u0644\u0649 1000." : "1000 m = 1 km. Divide by 1000.";

        public static string HowManyKg(int g) =>
            Ar ? $"\u0643\u0645 \u0643\u064a\u0644\u0648\u063a\u0631\u0627\u0645\u064b\u0627 \u0641\u064a {g} \u063a\u0631\u0627\u0645\u061f"
               : $"How many kilograms are in {g} grams?";
        public static string TipG() => Ar ? "1000 \u063a = 1 \u0643\u063a." : "1000 g = 1 kg.";

        public static string HowManyLitres(int ml) =>
            Ar ? $"\u0643\u0645 \u0644\u062a\u0631\u064b\u0627 \u0641\u064a {ml} \u0645\u0644\u0644\u064a\u0644\u062a\u0631\u061f"
               : $"How many litres are in {ml} millilitres?";
        public static string TipMl() => Ar ? "1000 \u0645\u0644 = 1 \u0644." : "1000 ml = 1 l.";

        // Counting prompts
        public static string WhatComesNext(int s0, int s1, int s2) =>
            Ar ? $"\u0645\u0627 \u0627\u0644\u0639\u062f\u062f \u0627\u0644\u062a\u0627\u0644\u064a\u061f\n{s0}\u060c {s1}\u060c {s2}\u060c \u061f"
               : $"What comes next?\n{s0}, {s1}, {s2}, ?";
        public static string SkipCountBy(int n) => Ar ? $"\u0639\u062f\u0651 \u0628\u0645\u0636\u0627\u0639\u0641\u0627\u062a {n}." : $"Skip-count by {n}.";
        public static string EachStepAdds(int n) => Ar ? $"\u0643\u0644 \u062e\u0637\u0648\u0629 \u062a\u0636\u064a\u0641 {n}." : $"Each step adds {n}.";

        // Addition / subtraction prompts
        public static string YouHaveAndGetMore(int a, string item, int b) =>
            Ar ? $"\u0644\u062f\u064a\u0643 {a} {item}\u060c \u062b\u0645 \u062d\u0635\u0644\u062a \u0639\u0644\u0649 {b} \u0625\u0636\u0627\u0641\u064a\u0629. \u0643\u0645 \u0644\u062f\u064a\u0643 \u0627\u0644\u0622\u0646\u061f"
               : $"You have {a} {item} and get {b} more. How many now?";
        public static string StartAtAndCountUp(int a, int b) =>
            Ar ? $"\u0627\u0628\u062f\u0623 \u0645\u0646 {a} \u0648\u0639\u062f\u0651 \u0645\u0636\u064a\u0641\u064b\u0627 {b}." : $"Start at {a} and count up by {b}.";
        public static string AddFormula(int a, int b, int ans) =>
            $"{a} + {b} = {ans}.";

        public static string NameStickers(string name, int a, int b) =>
            Ar ? $"{name} \u0644\u062f\u064a\u0647 {a} \u0645\u0644\u0635\u0642\u064b\u0627 \u0648\u062d\u0635\u0644 \u0639\u0644\u0649 {b} \u0625\u0636\u0627\u0641\u064a\u0629. \u0643\u0645 \u0644\u062f\u064a\u0647 \u0627\u0644\u0622\u0646\u061f"
               : $"{name} has {a} stickers and earns {b} more. How many does {name} have now?";
        public static string AddOf(int a, int b) =>
            Ar ? $"\u0627\u062c\u0645\u0639: {a} + {b}." : $"Add: {a} + {b}.";

        public static string NameMarbles(string name, int a, int gave, string friend, int got) =>
            Ar ? $"{name} \u0644\u062f\u064a\u0647 {a} \u0643\u0631\u0629 \u0632\u062c\u0627\u062c\u064a\u0629. \u0623\u0639\u0637\u0649 {gave} \u0645\u0646\u0647\u0627 \u0625\u0644\u0649 {friend}\u060c \u062b\u0645 \u0648\u062c\u062f {got} \u0623\u062e\u0631\u0649. \u0643\u0645 \u0644\u062f\u064a\u0647 \u0627\u0644\u0622\u0646\u061f"
               : $"{name} has {a} marbles. {name} gives {gave} to {friend} and then finds {got} more. How many does {name} have now?";
        public static string TwoStepHint() =>
            Ar ? "\u0627\u0644\u062e\u0637\u0648\u0629 1: \u0627\u0637\u0631\u062d \u0645\u0627 \u0623\u0639\u0637\u0627\u0647.\n\u0627\u0644\u062e\u0637\u0648\u0629 2: \u0623\u0636\u0641 \u0645\u0627 \u0648\u062c\u062f\u0647." : "Step 1: subtract what was given away.\nStep 2: add the new ones.";

        public static string Allowance(string name, int a, int b, int c) =>
            Ar ? $"\u0645\u0635\u0631\u0648\u0641 {name} {a} \u0641\u0644\u0633\u064b\u0627. \u0643\u0633\u0628 {b} \u0641\u0644\u0633\u064b\u0627 \u0625\u0636\u0627\u0641\u064a\u064b\u0627 \u0648\u0623\u0646\u0641\u0642 {c} \u0641\u0644\u0633\u064b\u0627. \u0643\u0645 \u0641\u0644\u0633\u064b\u0627 \u0645\u0639\u0647\u061f"
               : $"{name}'s allowance is {a}c. {name} earns {b}c extra and spends {c}c. How many cents does {name} have?";
        public static string AllowanceHint(int a, int b, int c) =>
            Ar ? $"\u0627\u0644\u062e\u0637\u0648\u0629 1: \u0627\u062c\u0645\u0639 \u0627\u0644\u0645\u0643\u0627\u0641\u0623\u0629 ({a} + {b}).\n\u0627\u0644\u062e\u0637\u0648\u0629 2: \u0627\u0637\u0631\u062d \u0627\u0644\u0625\u0646\u0641\u0627\u0642 ({c}).\n\u0627\u0644\u062e\u0637\u0648\u0629 3: \u062a\u0644\u0643 \u0647\u064a \u0627\u0644\u0625\u062c\u0627\u0628\u0629."
               : $"Step 1: add the bonus ({a} + {b}).\nStep 2: subtract the spending ({c}).\nStep 3: that's the answer.";

        public static string YouHaveGiveAway(int a, string item, int b) =>
            Ar ? $"\u0644\u062f\u064a\u0643 {a} {item}\u060c \u062b\u0645 \u0623\u0639\u0637\u064a\u062a {b}. \u0643\u0645 \u062a\u0628\u0642\u0651\u0649\u061f"
               : $"You have {a} {item} and give away {b}. How many left?";
        public static string CountBackFrom(int b, int a) =>
            Ar ? $"\u0639\u062f\u0651 \u062a\u0646\u0627\u0632\u0644\u064a\u064b\u0627 {b} \u0627\u0628\u062a\u062f\u0627\u0621\u064b \u0645\u0646 {a}." : $"Count back {b} from {a}.";
        public static string SubFormula(int a, int b, int ans) =>
            $"{a} - {b} = {ans}.";

        public static string NameCookies(string name, int a, int b) =>
            Ar ? $"{name} \u0643\u0627\u0646 \u0644\u062f\u064a\u0647 {a} \u0642\u0637\u0639\u0629 \u062d\u0644\u0648\u0649 \u0648\u0623\u0643\u0644 {b}. \u0643\u0645 \u062a\u0628\u0642\u0651\u0649\u061f"
               : $"{name} had {a} cookies and ate {b}. How many are left?";
        public static string SubtractOf(int a, int b) =>
            Ar ? $"\u0627\u0637\u0631\u062d: {a} - {b}." : $"Subtract: {a} - {b}.";

        public static string NameSpends(string name, int a, int s1, int s2) =>
            Ar ? $"{name} \u0644\u062f\u064a\u0647 {a} \u0641\u0644\u0633\u064b\u0627. \u0623\u0646\u0641\u0642 {s1} \u0641\u0644\u0633\u064b\u0627 \u0639\u0644\u0649 \u0645\u0644\u0635\u0642\u0627\u062a \u0648{s2} \u0641\u0644\u0633\u064b\u0627 \u0639\u0644\u0649 \u0648\u062c\u0628\u0629. \u0643\u0645 \u0628\u0642\u064a \u0645\u0639\u0647\u061f"
               : $"{name} has {a} cents. {name} spends {s1}c on stickers and {s2}c on a snack. How many cents are left?";
        public static string SpendStepHint() =>
            Ar ? "\u0627\u0644\u062e\u0637\u0648\u0629 1: \u0627\u0637\u0631\u062d \u062b\u0645\u0646 \u0627\u0644\u0645\u0644\u0635\u0642\u0627\u062a.\n\u0627\u0644\u062e\u0637\u0648\u0629 2: \u0627\u0637\u0631\u062d \u062b\u0645\u0646 \u0627\u0644\u0648\u062c\u0628\u0629." : "Step 1: subtract the stickers cost.\nStep 2: subtract the snack cost.";

        public static string NamePoints(string name, int a, int b, int c) =>
            Ar ? $"\u064a\u0628\u062f\u0623 {name} \u0628\u0640{a} \u0646\u0642\u0637\u0629. \u064a\u0641\u0642\u062f {b} \u0641\u064a \u0627\u0644\u062c\u0648\u0644\u0629 \u0627\u0644\u0623\u0648\u0644\u0649 \u0648{c} \u0641\u064a \u0627\u0644\u062c\u0648\u0644\u0629 \u0627\u0644\u062b\u0627\u0646\u064a\u0629. \u0645\u0627 \u0627\u0644\u0646\u062a\u064a\u062c\u0629 \u0627\u0644\u0646\u0647\u0627\u0626\u064a\u0629\u061f"
               : $"{name} starts with {a} points. {name} loses {b} on round one and {c} on round two. What's the final score?";
        public static string PointsHint(int a, int b, int c) =>
            Ar ? $"\u0627\u0644\u062e\u0637\u0648\u0629 1: \u0627\u0637\u0631\u062d \u0627\u0644\u062e\u0633\u0627\u0631\u0629 \u0627\u0644\u0623\u0648\u0644\u0649 ({a} - {b}).\n\u0627\u0644\u062e\u0637\u0648\u0629 2: \u0627\u0637\u0631\u062d \u0627\u0644\u062e\u0633\u0627\u0631\u0629 \u0627\u0644\u062b\u0627\u0646\u064a\u0629 ({c})."
               : $"Step 1: subtract the first loss ({a} - {b}).\nStep 2: subtract the second loss ({c}).";

        // Multiplication / division
        public static string BagsItems(int a, int b, string item) =>
            Ar ? $"\u0644\u062f\u064a\u0643 {a} \u0623\u0643\u064a\u0627\u0633\u060c \u0641\u064a \u0643\u0644 \u0643\u064a\u0633 {b} {item}. \u0643\u0645 \u0627\u0644\u0645\u062c\u0645\u0648\u0639\u061f"
               : $"You have {a} bags with {b} {item} in each. How many in total?";
        public static string GroupsOf(int a, int b) =>
            Ar ? $"{a} \u0645\u062c\u0645\u0648\u0639\u0627\u062a \u0645\u0646 {b}." : $"{a} groups of {b}.";
        public static string MulFormula(int a, int b, int ans) =>
            Ar ? $"{a} \u00d7 {b} = {ans}." : $"{a} x {b} = {ans}.";

        public static string ThinkGroupsOf(int a, int b) =>
            Ar ? $"\u0641\u0643\u0651\u0631 \u0641\u064a {a} \u0645\u062c\u0645\u0648\u0639\u0627\u062a \u0645\u0646 {b}." : $"Think of {a} groups of {b}.";

        public static string FlowerRows(string name, int rows, int each) =>
            Ar ? $"\u0632\u0631\u0639 {name} {rows} \u0635\u0641\u0648\u0641 \u0645\u0646 {each} \u0632\u0647\u0631\u0629. \u0643\u0645 \u0632\u0647\u0631\u0629 \u0641\u064a \u0627\u0644\u0645\u062c\u0645\u0648\u0639\u061f"
               : $"{name} planted {rows} rows of {each} flowers. How many flowers in all?";
        public static string MultiplyOf(int a, int b) =>
            Ar ? $"\u0627\u0636\u0631\u0628: {a} \u00d7 {b}." : $"Multiply: {a} \u00d7 {b}.";

        public static string CrayonBoxes(int bA, int a, int bB, int b) =>
            Ar ? $"\u064a\u0628\u064a\u0639 \u0645\u062a\u062c\u0631 {bA} \u0639\u0644\u0628 \u0641\u064a\u0647\u0627 {a} \u0642\u0644\u0645 \u062a\u0644\u0648\u064a\u0646 \u0648{bB} \u0639\u0644\u0628 \u0641\u064a\u0647\u0627 {b} \u0642\u0644\u0645 \u062a\u0644\u0648\u064a\u0646. \u0643\u0645 \u0642\u0644\u0645 \u062a\u0644\u0648\u064a\u0646 \u0641\u064a \u0627\u0644\u0645\u062c\u0645\u0648\u0639\u061f"
               : $"A shop sells {bA} boxes of {a} crayons and {bB} boxes of {b} crayons. How many crayons in total?";
        public static string CrayonHint(int bA, int a, int bB, int b) =>
            Ar ? $"\u0627\u0644\u062e\u0637\u0648\u0629 1: {bA} \u00d7 {a}.\n\u0627\u0644\u062e\u0637\u0648\u0629 2: {bB} \u00d7 {b}.\n\u0627\u0644\u062e\u0637\u0648\u0629 3: \u0627\u062c\u0645\u0639 \u0627\u0644\u0645\u062c\u0645\u0648\u0639\u064a\u0646."
               : $"Step 1: {bA} \u00d7 {a}.\nStep 2: {bB} \u00d7 {b}.\nStep 3: add the two totals.";
        public static string CrayonExplain(int bA, int a, int bB, int b, int ans) =>
            $"{bA}\u00d7{a} + {bB}\u00d7{b} = {ans}.";

        public static string BakeryBuns(int a, int c, int b) =>
            Ar ? $"\u064a\u062e\u0628\u0632 \u0645\u062d\u0644\u0651 {a} \u0643\u0639\u0643\u0629\u060c \u062b\u0645 {c} \u0643\u0639\u0643\u0627\u062a \u0623\u062e\u0631\u0649. \u062a\u062a\u0633\u0639 \u0643\u0644 \u0635\u064a\u0646\u064a\u0629 \u0644\u0640{b} \u0643\u0639\u0643\u0627\u062a. \u0643\u0645 \u0643\u0639\u0643\u0629 \u062a\u062a\u0633\u0639 \u0641\u064a \u0627\u0644\u0635\u0648\u0627\u0646\u064a\u061f"
               : $"A bakery makes {a} buns and {c} more buns. Each tray holds {b} buns. How many buns total fit on the trays?";
        public static string BakeryHint(int a, int c, int b) =>
            Ar ? $"\u0627\u0644\u062e\u0637\u0648\u0629 1: \u0627\u062c\u0645\u0639 \u0627\u0644\u0643\u0639\u0643\u0627\u062a ({a} + {c}).\n\u0627\u0644\u062e\u0637\u0648\u0629 2: \u0627\u0636\u0631\u0628 \u0641\u064a {b}."
               : $"Step 1: add the buns ({a} + {c}).\nStep 2: multiply by {b}.";
        public static string BakeryExplain(int a, int c, int b, int ans) =>
            $"({a} + {c}) \u00d7 {b} = {ans}.";

        public static string DivFormula(int a, int b, int ans) =>
            $"{a} / {b} = {ans}.";
        public static string DivExplain(int a, int b, int ans) =>
            Ar ? $"{a} / {b} = {ans} (\u0644\u0623\u0646 {b} \u00d7 {ans} = {a})."
               : $"{a} / {b} = {ans} (because {b} x {ans} = {a}).";
        public static string HowManyGroups(int b, int a) =>
            Ar ? $"\u0643\u0645 \u0645\u062c\u0645\u0648\u0639\u0629 \u0645\u0646 {b} \u062a\u0643\u0648\u0651\u0646 {a}\u061f" : $"How many groups of {b} make {a}?";

        public static string ShareCandies(string name, int total, int friends) =>
            Ar ? $"\u064a\u0642\u0633\u0645 {name} {total} \u062d\u0644\u0648\u0649 \u0628\u0627\u0644\u062a\u0633\u0627\u0648\u064a \u0628\u064a\u0646 {friends} \u0623\u0635\u062f\u0642\u0627\u0621. \u0643\u0645 \u062d\u0644\u0648\u0649 \u064a\u062d\u0635\u0644 \u0639\u0644\u064a\u0647\u0627 \u0643\u0644 \u0635\u062f\u064a\u0642\u061f"
               : $"{name} shares {total} candies equally among {friends} friends. How many candies does each friend get?";
        public static string DivideBy(int total, int friends) =>
            Ar ? $"\u0627\u0642\u0633\u0645 {total} \u0639\u0644\u0649 {friends}." : $"Divide {total} by {friends}.";

        public static string StudentsBuses(int total, int groups, int extra) =>
            Ar ? $"\u064a\u0635\u0639\u062f {total} \u0637\u0627\u0644\u0628\u064b\u0627 \u0625\u0644\u0649 {groups} \u062d\u0627\u0641\u0644\u0627\u062a \u0628\u0627\u0644\u062a\u0633\u0627\u0648\u064a\u060c \u062b\u0645 \u064a\u0646\u0636\u0645\u0651 {extra} \u0637\u0644\u0627\u0628 \u0622\u062e\u0631\u0648\u0646 \u0625\u0644\u0649 \u0643\u0644 \u062d\u0627\u0641\u0644\u0629. \u0643\u0645 \u0637\u0627\u0644\u0628\u064b\u0627 \u0641\u064a \u0643\u0644 \u062d\u0627\u0641\u0644\u0629 \u0627\u0644\u0622\u0646\u061f"
               : $"{total} students board {groups} buses equally, then {extra} more students join each bus. How many students per bus now?";
        public static string DivStepHint(int total, int groups, int extra) =>
            Ar ? $"\u0627\u0644\u062e\u0637\u0648\u0629 1: \u0627\u0642\u0633\u0645 ({total} / {groups}).\n\u0627\u0644\u062e\u0637\u0648\u0629 2: \u0623\u0636\u0641 {extra}."
               : $"Step 1: divide ({total} / {groups}).\nStep 2: add {extra}.";

        public static string FarmEggs(string ignored, int total, int groups, int taken) =>
            Ar ? $"\u062a\u0639\u0628\u0651\u0626 \u0645\u0632\u0631\u0639\u0629 {total} \u0628\u064a\u0636\u0629 \u0641\u064a {groups} \u0635\u0646\u0627\u062f\u064a\u0642 \u0628\u0627\u0644\u062a\u0633\u0627\u0648\u064a. \u062b\u0645 \u062a\u064f\u0624\u062e\u0630 {taken} \u0628\u064a\u0636\u0627\u062a \u0645\u0646 \u0643\u0644 \u0635\u0646\u062f\u0648\u0642. \u0643\u0645 \u0628\u064a\u0636\u0629 \u062a\u062a\u0628\u0642\u0651\u0649 \u0641\u064a \u0643\u0644 \u0635\u0646\u062f\u0648\u0642\u061f"
               : $"A farm packs {total} eggs into {groups} crates evenly. Then {taken} eggs are removed from each crate. How many remain per crate?";
        public static string EggsStepHint(int total, int groups, int taken) =>
            Ar ? $"\u0627\u0644\u062e\u0637\u0648\u0629 1: \u0627\u0642\u0633\u0645 {total} \u0639\u0644\u0649 {groups}.\n\u0627\u0644\u062e\u0637\u0648\u0629 2: \u0627\u0637\u0631\u062d {taken}."
               : $"Step 1: divide {total} by {groups}.\nStep 2: subtract {taken}.";

        // Shapes - perimeter / area
        public static string PerimeterPrompt(int w, int h) =>
            Ar ? $"\u0645\u0633\u062a\u0637\u064a\u0644 \u0623\u0628\u0639\u0627\u062f\u0647 {w} \u0633\u0645 \u0641\u064a {h} \u0633\u0645. \u0645\u0627 \u0645\u062d\u064a\u0637\u0647\u061f"
               : $"A rectangle is {w} cm by {h} cm. What is its perimeter?";
        public static string AreaPrompt(int w, int h) =>
            Ar ? $"\u0645\u0633\u062a\u0637\u064a\u0644 \u0623\u0628\u0639\u0627\u062f\u0647 {w} \u0633\u0645 \u0641\u064a {h} \u0633\u0645. \u0645\u0627 \u0645\u0633\u0627\u062d\u062a\u0647\u061f"
               : $"A rectangle is {w} cm by {h} cm. What is its area?";
        public static string PerimeterFormula() =>
            Ar ? "\u0627\u0644\u0645\u062d\u064a\u0637 = 2 \u00d7 (\u0627\u0644\u0639\u0631\u0636 + \u0627\u0644\u0627\u0631\u062a\u0641\u0627\u0639)." : "Perimeter = 2 \u00d7 (width + height).";
        public static string AreaFormula() =>
            Ar ? "\u0627\u0644\u0645\u0633\u0627\u062d\u0629 = \u0627\u0644\u0639\u0631\u0636 \u00d7 \u0627\u0644\u0627\u0631\u062a\u0641\u0627\u0639." : "Area = width \u00d7 height.";
        public static string PerimeterExplain(int w, int h, int ans) =>
            $"2 \u00d7 ({w} + {h}) = {ans}.";
        public static string AreaExplain(int w, int h, int ans) =>
            $"{w} \u00d7 {h} = {ans}.";

        // Patterns
        public static string FindNextNumber(int s0, int s1, int s2, int s3) =>
            Ar ? $"\u0623\u0648\u062c\u062f \u0627\u0644\u0639\u062f\u062f \u0627\u0644\u062a\u0627\u0644\u064a:\n{s0}\u060c {s1}\u060c {s2}\u060c {s3}\u060c \u061f"
               : $"Find the next number:\n{s0}, {s1}, {s2}, {s3}, ?";
        public static string EachStepMultiplies(int step) =>
            Ar ? $"\u0643\u0644 \u062e\u0637\u0648\u0629 \u062a\u0636\u0631\u0628 \u0641\u064a {step}." : $"Each step multiplies by {step}.";
        public static string EachStepLarger() =>
            Ar ? "\u0643\u0644 \u062e\u0637\u0648\u0629 \u062a\u0636\u064a\u0641 \u0645\u0642\u062f\u0627\u0631\u064b\u0627 \u0623\u0643\u0628\u0631." : "Each step adds a larger amount.";
        public static string NextIs(int n) =>
            Ar ? $"\u0627\u0644\u062a\u0627\u0644\u064a \u0647\u0648 {n}." : $"Next is {n}.";

        public static string PatternWhatComesNext(string seq) =>
            Ar ? $"\u0645\u0627 \u0627\u0644\u0630\u064a \u064a\u0644\u064a\u061f\n{seq} \u061f"
               : $"What comes next?\n{seq} ?";
        public static string PatternRepeats(int len) =>
            Ar ? $"\u064a\u062a\u0643\u0631\u0651\u0631 \u0627\u0644\u0646\u0645\u0637 \u0643\u0644 {len} \u0639\u0646\u0627\u0635\u0631." : $"The pattern repeats every {len} items.";
        public static string PatternExplain(string pattern, string answer) =>
            Ar ? $"\u0627\u0644\u0646\u0645\u0637: {pattern}. \u0627\u0644\u062a\u0627\u0644\u064a \u0647\u0648 {answer}." : $"Pattern: {pattern}. Next is {answer}.";

        // Fractions
        public static string WhichFractionOneOf(int den) =>
            Ar ? $"\u0623\u064a \u0643\u0633\u0631 \u064a\u0639\u0646\u064a \u0648\u0627\u062d\u062f\u064b\u0627 \u0645\u0646 {den} \u0623\u062c\u0632\u0627\u0621 \u0645\u062a\u0633\u0627\u0648\u064a\u0629\u061f"
               : $"Which fraction means ONE of {den} equal parts?";
        public static string LookBottom() =>
            Ar ? "\u0627\u0646\u0638\u0631 \u0625\u0644\u0649 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0641\u0644\u064a." : "Look at the bottom number.";
        public static string OneOfPartsExplain(int den, string label) =>
            Ar ? $"\u0648\u0627\u062d\u062f \u0645\u0646 {den} \u0623\u062c\u0632\u0627\u0621 \u0645\u062a\u0633\u0627\u0648\u064a\u0629 \u064a\u064f\u0643\u062a\u0628 {label}." : $"One of {den} equal parts is written {label}.";

        public static string WhichFractionEqualTo(int num, int den) =>
            Ar ? $"\u0623\u064a \u0643\u0633\u0631 \u064a\u0633\u0627\u0648\u064a {num}/{den}\u061f"
               : $"Which fraction is equal to {num}/{den}?";
        public static string MultiplyTopBottom(int factor) =>
            Ar ? $"\u0627\u0636\u0631\u0628 \u0627\u0644\u0628\u0633\u0637 \u0648\u0627\u0644\u0645\u0642\u0627\u0645 \u0641\u064a {factor}." : $"Multiply top and bottom by {factor}.";
        public static string FracEqExplain(int num, int den, string answer) =>
            $"{num}/{den} = {answer}.";

        // Time
        public static string ElapsedTime(int h1, int m1, int h2, int m2) =>
            Ar ? $"\u0645\u0646 {h1}:{m1:00} \u0625\u0644\u0649 {h2}:{m2:00}\u060c \u0643\u0645 \u062f\u0642\u064a\u0642\u0629 \u0645\u0631\u0651\u062a\u061f"
               : $"From {h1}:{m1:00} to {h2}:{m2:00}, how many minutes pass?";
        public static string ElapsedHint() =>
            Ar ? "\u0627\u0644\u062e\u0637\u0648\u0629 1: \u0627\u062d\u0633\u0628 \u0627\u0644\u0633\u0627\u0639\u0627\u062a \u0628\u064a\u0646 \u0627\u0644\u0648\u0642\u062a\u064a\u0646.\n\u0627\u0644\u062e\u0637\u0648\u0629 2: \u0623\u0636\u0641 \u0627\u0644\u062f\u0642\u0627\u0626\u0642 \u0627\u0644\u0625\u0636\u0627\u0641\u064a\u0629."
               : "Step 1: count hours between the two times.\nStep 2: add the extra minutes.";
        public static string ElapsedExplain(int h1, int m1, int h2, int m2, int ans) =>
            Ar ? $"{h1}:{m1:00} \u2192 {h2}:{m2:00} = {ans} \u062f\u0642\u064a\u0642\u0629." : $"{h1}:{m1:00} \u2192 {h2}:{m2:00} = {ans} minutes.";

        public static string WhatTimeShown() =>
            Ar ? "\u0645\u0627 \u0627\u0644\u0648\u0642\u062a \u0627\u0644\u0630\u064a \u062a\u0634\u064a\u0631 \u0625\u0644\u064a\u0647 \u0627\u0644\u0633\u0627\u0639\u0629\u061f" : "What time is shown on the clock?";
        public static string ClockHandsHint() =>
            Ar ? "\u0627\u0644\u0639\u0642\u0631\u0628 \u0627\u0644\u0642\u0635\u064a\u0631 \u064a\u0634\u064a\u0631 \u0625\u0644\u0649 \u0627\u0644\u0633\u0627\u0639\u0629. \u0627\u0644\u0639\u0642\u0631\u0628 \u0627\u0644\u0637\u0648\u064a\u0644 \u064a\u0634\u064a\u0631 \u0625\u0644\u0649 \u0627\u0644\u062f\u0642\u0627\u0626\u0642."
               : "The short hand is the hour. The long hand is the minute.";
        public static string ClockShows(string time) =>
            Ar ? $"\u0627\u0644\u0633\u0627\u0639\u0629 \u062a\u0634\u064a\u0631 \u0625\u0644\u0649 {time}." : $"The clock shows {time}.";

        // Money
        public static string CoinNameShort(string en)
        {
            if (!Ar) return en;
            return en switch
            {
                "penny"   => "\u0627\u0644\u0628\u0646\u0651\u064a",
                "nickel"  => "\u0627\u0644\u0646\u064a\u0643\u0644",
                "dime"    => "\u0627\u0644\u062f\u0627\u064a\u0645",
                "quarter" => "\u0627\u0644\u0643\u0648\u0627\u0631\u062a\u0631",
                _         => en
            };
        }

        public static string HowManyCents(string coin) =>
            Ar ? $"\u0643\u0645 \u0641\u0644\u0633\u064b\u0627 \u064a\u0633\u0627\u0648\u064a {CoinNameShort(coin)}\u061f"
               : $"How many cents is a {coin}?";
        public static string CoinValuesHint() =>
            Ar ? "\u0628\u0646\u0651\u064a=1\u060c \u0646\u064a\u0643\u0644=5\u060c \u062f\u0627\u064a\u0645=10\u060c \u0643\u0648\u0627\u0631\u062a\u0631=25." : "Penny=1, Nickel=5, Dime=10, Quarter=25.";
        public static string CoinExplain(string coin, int cents) =>
            Ar ? $"{CoinNameShort(coin)} \u064a\u0633\u0627\u0648\u064a {cents} \u0641\u0644\u0633\u064b\u0627."
               : $"A {coin} is worth {cents} cents.";

        public static string AddCoins(string picksJoined) =>
            Ar ? $"\u0627\u062c\u0645\u0639 \u0627\u0644\u0641\u0644\u0648\u0633: {picksJoined}. \u0645\u0627 \u0627\u0644\u0645\u062c\u0645\u0648\u0639 \u0628\u0627\u0644\u0641\u0644\u0648\u0633\u061f"
               : $"Add the coins: {picksJoined}. Total cents?";
        public static string AddCoinValues() =>
            Ar ? "\u0627\u062c\u0645\u0639 \u0642\u064a\u0645\u0629 \u0643\u0644 \u0639\u0645\u0644\u0629." : "Add the value of each coin.";
        public static string TotalCents(int total) =>
            Ar ? $"\u0627\u0644\u0645\u062c\u0645\u0648\u0639 = {total} \u0641\u0644\u0633\u064b\u0627." : $"Total = {total} cents.";

        public static string MakeChange(int price, int paid) =>
            Ar ? $"\u0627\u0634\u062a\u0631\u064a\u062a \u0648\u062c\u0628\u0629 \u0628\u0640{price} \u0641\u0644\u0633\u064b\u0627 \u0648\u062f\u0641\u0639\u062a {paid} \u0641\u0644\u0633\u064b\u0627. \u0645\u0627 \u0627\u0644\u0628\u0627\u0642\u064a\u061f"
               : $"You buy a snack for {price}c and pay {paid}c. What is your change?";
        public static string ChangeFormula(int paid) =>
            Ar ? $"\u0627\u0644\u0628\u0627\u0642\u064a = {paid} - \u0627\u0644\u0633\u0639\u0631." : $"Change = {paid} - price.";
        public static string ChangeExplain(int paid, int price, int change) =>
            Ar ? $"{paid} - {price} = {change} \u0641\u0644\u0633\u064b\u0627." : $"{paid} - {price} = {change} cents.";

        // Cent suffix used in option lists ("25c") - keep "c" generic so the
        // math reads the same in both languages.
        public static string CentSuffix => "c";

        // ----- Lesson copy ---------------------------------------------------
        public static string LessonIntro(int grade, MathSubject subject) =>
            Ar
                ? $"\u0645\u0631\u062d\u0628\u064b\u0627 \u0628\u0643 \u0641\u064a {SubjectPretty(subject)} \u0644\u0644\u0635\u0641 {grade}! \u0627\u0642\u0631\u0623 \u0643\u0644 \u0633\u0624\u0627\u0644\u060c \u0627\u0646\u0638\u0631 \u0625\u0644\u0649 \u0627\u0644\u0645\u062b\u0627\u0644\u060c \u062b\u0645 \u0627\u062e\u062a\u0631 \u0623\u0641\u0636\u0644 \u0625\u062c\u0627\u0628\u0629."
                : $"Welcome to {SubjectPretty(subject)} for Grade {grade}! Read each question, look at the example, then choose the best answer.";

        public static string LessonExample(int grade, MathSubject subject)
        {
            string ex = Ar ? "\u0645\u062b\u0627\u0644: " : "Example: ";
            switch (subject)
            {
                case MathSubject.Counting:
                    return Ar ? "\u0645\u062b\u0627\u0644: 1\u060c 2\u060c 3\u060c ___. \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u062a\u0627\u0644\u064a \u0647\u0648 4." : "Example: 1, 2, 3, ___. The next number is 4.";
                case MathSubject.Addition:
                    return grade == 1 ? ex + "2 + 3 = 5."
                         : grade == 2 ? ex + "24 + 13 = 37."
                         : grade == 3 ? ex + "245 + 138 = 383."
                         : grade == 4 ? ex + "3 472 + 1 845 = 5 317."
                                      : ex + "12 480 + 9 365 = 21 845.";
                case MathSubject.Subtraction:
                    return grade == 1 ? ex + "5 - 2 = 3."
                         : grade == 2 ? ex + "47 - 12 = 35."
                         : grade == 3 ? ex + "642 - 215 = 427."
                         : grade == 4 ? ex + "8 350 - 2 624 = 5 726."
                                      : ex + "45 120 - 18 763 = 26 357.";
                case MathSubject.Multiplication:
                    return grade == 2
                        ? (Ar ? "\u0645\u062b\u0627\u0644: 2 \u00d7 4 = 8 (\u0645\u062c\u0645\u0648\u0639\u062a\u0627\u0646 \u0645\u0646 \u0623\u0631\u0628\u0639\u0629)." : "Example: 2 x 4 = 8 (two groups of four).")
                        : grade == 3 ? ex + "6 \u00d7 7 = 42."
                        : grade == 4 ? ex + "23 \u00d7 14 = 322."
                                      : ex + "146 \u00d7 27 = 3 942.";
                case MathSubject.Division:
                    return grade == 3
                        ? (Ar ? "\u0645\u062b\u0627\u0644: 12 \u00f7 3 = 4 (\u0627\u062b\u0646\u0627 \u0639\u0634\u0631 \u062a\u064f\u0642\u0633\u064e\u0651\u0645 \u0625\u0644\u0649 3 \u0645\u062c\u0645\u0648\u0639\u0627\u062a)." : "Example: 12 / 3 = 4 (twelve shared into 3 groups).")
                        : grade == 4 ? (Ar ? "\u0645\u062b\u0627\u0644: 67 \u00f7 5 = 13 \u0648\u0627\u0644\u0628\u0627\u0642\u064a 2." : "Example: 67 / 5 = 13 remainder 2.")
                                      : (Ar ? "\u0645\u062b\u0627\u0644: 384 \u00f7 16 = 24." : "Example: 384 / 16 = 24.");
                case MathSubject.Shapes:
                    return grade == 4 ? (Ar ? "\u0645\u062b\u0627\u0644: \u0645\u0633\u0627\u062d\u0629 \u0645\u062b\u0644\u0651\u062b = 1/2 \u00d7 \u0627\u0644\u0642\u0627\u0639\u062f\u0629 \u00d7 \u0627\u0644\u0627\u0631\u062a\u0641\u0627\u0639. \u0644\u0642\u0627\u0639\u062f\u0629 8 \u0648\u0627\u0631\u062a\u0641\u0627\u0639 6: \u0627\u0644\u0645\u0633\u0627\u062d\u0629 = 24."
                                              : "Example: Triangle area = 1/2 \u00d7 base \u00d7 height. Base 8, height 6 -> area 24.")
                        : grade == 5 ? (Ar ? "\u0645\u062b\u0627\u0644: \u062d\u062c\u0645 \u0635\u0646\u062f\u0648\u0642 4\u00d73\u00d72 = 24 \u0633\u0645\u00b3." : "Example: Volume of a 4\u00d73\u00d72 box = 24 cm\u00b3.")
                        : (Ar ? "\u0645\u062b\u0627\u0644: \u0627\u0644\u0634\u0643\u0644 \u0630\u0648 \u0627\u0644\u062b\u0644\u0627\u062b\u0629 \u0623\u0636\u0644\u0627\u0639 \u0647\u0648 \u0627\u0644\u0645\u062b\u0644\u062b." : "Example: A shape with 3 sides is a triangle.");
                case MathSubject.Patterns:
                    return grade == 4 ? (Ar ? "\u0645\u062b\u0627\u0644: 2\u060c 5\u060c 8\u060c 11\u060c ___. \u0627\u0644\u0642\u0627\u0639\u062f\u0629: \u0627\u0636\u0641 3 \u0641\u064a \u0643\u0644 \u062e\u0637\u0648\u0629." : "Example: 2, 5, 8, 11, ___. Rule: add 3 each step.")
                        : grade == 5 ? (Ar ? "\u0645\u062b\u0627\u0644: \u0627\u0644\u062d\u062f \u0627\u0644\u0646\u0648\u0646\u064a 2n+1 \u064a\u0639\u0637\u064a 3\u060c 5\u060c 7\u060c 9\u060c ..." : "Example: Term-rule 2n+1 gives 3, 5, 7, 9, ...")
                        : (Ar ? "\u0645\u062b\u0627\u0644: \u0623\u060c \u0628\u060c \u0623\u060c \u0628\u060c \u0623\u060c ___. \u0627\u0644\u062a\u0627\u0644\u064a \u0647\u0648 \u0628." : "Example: A, B, A, B, A, ___. Next is B.");
                case MathSubject.Fractions:
                    return grade == 2
                        ? (Ar ? "\u0645\u062b\u0627\u0644: 1/2 \u062a\u0639\u0646\u064a \u0648\u0627\u062d\u062f\u064b\u0627 \u0645\u0646 \u062c\u0632\u0623\u064a\u0646 \u0645\u062a\u0633\u0627\u0648\u064a\u064a\u0646." : "Example: 1/2 means one of two equal parts.")
                        : grade == 3 ? (Ar ? "\u0645\u062b\u0627\u0644: 2/4 \u064a\u0633\u0627\u0648\u064a 1/2." : "Example: 2/4 is the same as 1/2.")
                        : grade == 4 ? (Ar ? "\u0645\u062b\u0627\u0644: 1/5 + 2/5 = 3/5." : "Example: 1/5 + 2/5 = 3/5.")
                                      : (Ar ? "\u0645\u062b\u0627\u0644: 1/2 + 1/3 = 5/6 (\u0648\u062d\u0651\u062f \u0627\u0644\u0645\u0642\u0627\u0645\u0627\u062a)." : "Example: 1/2 + 1/3 = 5/6 (find a common denominator).");
                case MathSubject.Measurement:
                    return grade == 4 ? (Ar ? "\u0645\u062b\u0627\u0644: 2 \u0643\u063a + 350 \u063a = 2 350 \u063a." : "Example: 2 kg + 350 g = 2 350 g.")
                        : grade == 5 ? (Ar ? "\u0645\u062b\u0627\u0644: \u0645\u0633\u0627\u062d\u0629 \u063a\u0631\u0641\u0629 4 \u0645 \u00d7 5 \u0645 = 20 \u0645\u00b2." : "Example: A room 4 m \u00d7 5 m has area 20 m\u00b2.")
                        : (Ar ? "\u0645\u062b\u0627\u0644: \u0642\u0644\u0645 \u0627\u0644\u0631\u0635\u0627\u0635 \u0623\u0642\u0635\u0631 \u0645\u0646 \u0627\u0644\u0637\u0627\u0648\u0644\u0629." : "Example: A pencil is shorter than a desk.");
                case MathSubject.Time:
                    return grade == 1
                        ? (Ar ? "\u0645\u062b\u0627\u0644: \u0639\u0646\u062f\u0645\u0627 \u064a\u0643\u0648\u0646 \u0627\u0644\u0639\u0642\u0631\u0628 \u0627\u0644\u0637\u0648\u064a\u0644 \u0639\u0644\u0649 12 \u0648\u0627\u0644\u0639\u0642\u0631\u0628 \u0627\u0644\u0642\u0635\u064a\u0631 \u0639\u0644\u0649 3\u060c \u062a\u0643\u0648\u0646 \u0627\u0644\u0633\u0627\u0639\u0629 3 \u062a\u0645\u0627\u0645\u064b\u0627."
                                  : "Example: When the long hand is on 12 and the short hand is on 3, it is 3 o'clock.")
                        : grade == 4 ? (Ar ? "\u0645\u062b\u0627\u0644: 14:30 \u0628\u0646\u0638\u0627\u0645 24 \u0633\u0627\u0639\u0629 = 2:30 \u0645\u0633\u0627\u0621\u064b." : "Example: 14:30 (24-hour) = 2:30 pm.")
                        : grade == 5 ? (Ar ? "\u0645\u062b\u0627\u0644: \u0645\u062f\u0651\u0629 \u0631\u062d\u0644\u0629 \u0645\u0646 9:45 \u0625\u0644\u0649 13:20 = 3 \u0633\u0627\u0639\u0627\u062a \u0648 35 \u062f\u0642\u064a\u0642\u0629." : "Example: 9:45 to 13:20 lasts 3 h 35 min.")
                        : (Ar ? "\u0645\u062b\u0627\u0644: 3:15 \u062a\u0639\u0646\u064a \u0627\u0644\u0633\u0627\u0639\u0629 3 \u0648\u062e\u0645\u0633 \u0639\u0634\u0631\u0629 \u062f\u0642\u064a\u0642\u0629." : "Example: 3:15 means 15 minutes past 3.");
                case MathSubject.Money:
                    return grade == 4 ? (Ar ? "\u0645\u062b\u0627\u0644: \u0633\u0639\u0631 3 \u062f\u0641\u0627\u062a\u0631 \u0628\u0640 240 \u0641\u0644\u0633\u064b\u0627 \u0644\u0644\u0648\u0627\u062d\u062f = 720 \u0641\u0644\u0633\u064b\u0627." : "Example: 3 notebooks at 240c each cost 720c.")
                        : grade == 5 ? (Ar ? "\u0645\u062b\u0627\u0644: \u062e\u0635\u0645 20% \u0639\u0644\u0649 500 \u0641\u0644\u0633 = 100 \u0641\u0644\u0633 \u062e\u0635\u0645\u064b\u0627 \u0648400 \u0641\u0644\u0633 \u0627\u0644\u0633\u0639\u0631 \u0627\u0644\u0646\u0647\u0627\u0626\u064a." : "Example: 20% off 500c = 100c off, final price 400c.")
                        : (Ar ? "\u0645\u062b\u0627\u0644: \u0627\u0644\u0646\u064a\u0643\u0644 = 5 \u0641\u0644\u0648\u0633\u060c \u0627\u0644\u062f\u0627\u064a\u0645 = 10 \u0641\u0644\u0648\u0633." : "Example: A nickel = 5 cents, a dime = 10 cents.");
                default: return string.Empty;
            }
        }

        public static string LessonTip(MathSubject subject)
        {
            if (Ar) switch (subject)
            {
                case MathSubject.Addition:       return "\u0646\u0635\u064a\u062d\u0629: \u0639\u062f\u0651 \u062a\u0635\u0627\u0639\u062f\u064a\u064b\u0627 \u0645\u0646 \u0627\u0644\u0639\u062f\u062f \u0627\u0644\u0623\u0643\u0628\u0631.";
                case MathSubject.Subtraction:    return "\u0646\u0635\u064a\u062d\u0629: \u0639\u062f\u0651 \u062a\u0646\u0627\u0632\u0644\u064a\u064b\u0627\u060c \u0623\u0648 \u0641\u0643\u0651\u0631 \u0641\u064a \u0627\u0644\u0645\u0636\u0627\u0641 \u0627\u0644\u0645\u0641\u0642\u0648\u062f.";
                case MathSubject.Multiplication: return "\u0646\u0635\u064a\u062d\u0629: \u0627\u0644\u0636\u0631\u0628 \u0647\u0648 \u062c\u0645\u0639 \u0645\u062a\u0643\u0631\u0651\u0631.";
                case MathSubject.Division:       return "\u0646\u0635\u064a\u062d\u0629: \u0641\u0643\u0651\u0631 '\u0643\u0645 \u0645\u062c\u0645\u0648\u0639\u0629\u061f'.";
                case MathSubject.Fractions:      return "\u0646\u0635\u064a\u062d\u0629: \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0641\u0644\u064a \u0647\u0648 \u0639\u062f\u062f \u0627\u0644\u0623\u062c\u0632\u0627\u0621 \u0627\u0644\u0645\u062a\u0633\u0627\u0648\u064a\u0629.";
                case MathSubject.Shapes:         return "\u0646\u0635\u064a\u062d\u0629: \u0639\u062f\u0651 \u0627\u0644\u0623\u0636\u0644\u0627\u0639 \u0648\u0627\u0644\u0632\u0648\u0627\u064a\u0627.";
                case MathSubject.Patterns:       return "\u0646\u0635\u064a\u062d\u0629: \u0627\u0628\u062d\u062b \u0639\u0646 \u0627\u0644\u062c\u0632\u0621 \u0627\u0644\u0645\u062a\u0643\u0631\u0651\u0631.";
                case MathSubject.Time:           return "\u0646\u0635\u064a\u062d\u0629: \u0627\u0644\u0639\u0642\u0631\u0628 \u0627\u0644\u0642\u0635\u064a\u0631 \u064a\u062f\u0644\u0651 \u0639\u0644\u0649 \u0627\u0644\u0633\u0627\u0639\u0629.";
                case MathSubject.Money:          return "\u0646\u0635\u064a\u062d\u0629: 100 \u0641\u0644\u0633 \u062a\u0633\u0627\u0648\u064a 1 \u062f\u0648\u0644\u0627\u0631.";
                case MathSubject.Measurement:    return "\u0646\u0635\u064a\u062d\u0629: \u0627\u062e\u062a\u0631 \u0627\u0644\u0648\u062d\u062f\u0629 \u0627\u0644\u0645\u0644\u0627\u0626\u0645\u0629 \u0644\u062d\u062c\u0645 \u0627\u0644\u062c\u0633\u0645.";
                case MathSubject.Counting:       return "\u0646\u0635\u064a\u062d\u0629: \u0639\u062f\u0651 \u0628\u0635\u0648\u062a \u0645\u0646\u062e\u0641\u0636.";
                default:                         return "\u0646\u0635\u064a\u062d\u0629: \u062e\u0630 \u0648\u0642\u062a\u0643 \u0648\u0627\u0642\u0631\u0623 \u0628\u0639\u0646\u0627\u064a\u0629.";
            }
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

        // ----- Story templates (subject-themed intros / outros) --------------
        public static string StoryIntro(MathSubject s, int grade, int level)
        {
            int a = 1 + (level * 2);
            int b = 1 + (level + grade);
            if (Ar) return s switch
            {
                MathSubject.Addition       => $"\ud83c\udf4e \u0627\u0644\u0645\u0632\u0627\u0631\u0639\u0629 \u062c\u0646\u0649 \u0644\u062f\u064a\u0647\u0627 {a} \u062a\u0641\u0627\u062d\u0629. \u062a\u0642\u0637\u0641 {b} \u0623\u062e\u0631\u0649. \u0633\u0627\u0639\u062f\u0647\u0627 \u0639\u0644\u0649 \u0627\u0644\u0639\u062f\u0651!",
                MathSubject.Subtraction    => $"\ud83d\udc26 {a + b} \u0637\u064a\u0648\u0631 \u062c\u0644\u0633\u062a \u0639\u0644\u0649 \u0633\u0644\u0643. \u0637\u0627\u0631 {b} \u0645\u0646\u0647\u0627. \u0643\u0645 \u062a\u0628\u0642\u0651\u0649\u061f",
                MathSubject.Multiplication => $"\ud83d\ude97 \u062a\u0648\u062c\u062f {b} \u0635\u0641\u0648\u0641 \u0645\u0648\u0627\u0642\u0641 \u0648\u0641\u064a \u0643\u0644 \u0635\u0641\u0651 {a} \u0633\u064a\u0651\u0627\u0631\u0627\u062a. \u0643\u0645 \u0633\u064a\u0651\u0627\u0631\u0629 \u0641\u064a \u0627\u0644\u0645\u062c\u0645\u0648\u0639\u061f",
                MathSubject.Division       => $"\ud83c\udf55 \u0644\u062f\u064a\u0643 {a * b} \u0642\u0637\u0639\u0629 \u0628\u064a\u062a\u0632\u0627 \u0644\u062a\u0642\u0627\u0633\u0645\u0647\u0627 \u0628\u0627\u0644\u062a\u0633\u0627\u0648\u064a \u0645\u0639 {b} \u0623\u0635\u062f\u0642\u0627\u0621.",
                MathSubject.Counting       => "\ud83c\udf1f \u0627\u0644\u0633\u0645\u0627\u0621 \u0644\u064a\u0644\u064b\u0627 \u0645\u0630\u0647\u0644\u0629! \u0647\u0644 \u062a\u0633\u062a\u0637\u064a\u0639 \u0639\u062f\u0651 \u0643\u0644 \u0627\u0644\u0646\u062c\u0648\u0645\u061f",
                MathSubject.Shapes         => "\ud83c\udfd7\ufe0f \u0627\u0644\u0645\u0647\u0646\u062f\u0633\u0629 \u0622\u0631\u064a\u0627 \u062a\u0635\u0645\u0651\u0645 \u0645\u0628\u0627\u0646\u064d. \u062a\u062d\u062a\u0627\u062c \u0645\u0633\u0627\u0639\u062f\u062a\u0643!",
                MathSubject.Patterns       => "\ud83c\udfa8 \u0627\u0644\u0641\u0646\u0651\u0627\u0646 \u0645\u0627\u0643\u0633 \u064a\u0628\u062a\u0643\u0631 \u0646\u0645\u0637\u064b\u0627. \u0647\u0644 \u062a\u0639\u0631\u0641 \u0645\u0627 \u064a\u0623\u062a\u064a \u0628\u0639\u062f\u061f",
                MathSubject.Fractions      => "\ud83c\udf82 \u0625\u0646\u0647 \u0639\u064a\u062f \u0645\u064a\u0644\u0627\u062f! \u0633\u0627\u0639\u062f \u0641\u064a \u062a\u0642\u0637\u064a\u0639 \u0627\u0644\u0643\u0639\u0643\u0629 \u0625\u0644\u0649 \u0642\u0637\u0639 \u0645\u062a\u0633\u0627\u0648\u064a\u0629.",
                MathSubject.Measurement    => "\ud83d\udccf \u0627\u0644\u0628\u0646\u0651\u0627\u0621 \u0628\u0648\u0628 \u064a\u062d\u062a\u0627\u062c \u0642\u064a\u0627\u0633\u0627\u062a \u062f\u0642\u064a\u0642\u0629. \u0647\u0644 \u062a\u0633\u062a\u0637\u064a\u0639 \u0645\u0633\u0627\u0639\u062f\u062a\u0647\u061f",
                MathSubject.Time           => "\u23f0 \u062c\u062f\u0648\u0644 \u0627\u0644\u0642\u0637\u0627\u0631 \u064a\u062d\u062a\u0627\u062c \u0645\u0633\u0627\u0639\u062f\u062a\u0643! \u0627\u0642\u0631\u0623 \u0627\u0644\u0633\u0627\u0639\u0627\u062a \u0628\u0635\u062d\u0651\u0629.",
                MathSubject.Money          => "\ud83c\udfea \u0623\u0647\u0644\u064b\u0627 \u0628\u0643 \u0641\u064a \u0645\u062a\u062c\u0631 \u0627\u0644\u062d\u0633\u0627\u0628! \u0633\u0627\u0639\u062f \u0623\u0645\u064a\u0646 \u0627\u0644\u0635\u0646\u062f\u0648\u0642 \u0639\u0644\u0649 \u0625\u0639\u0637\u0627\u0621 \u0627\u0644\u0628\u0627\u0642\u064a \u0627\u0644\u0635\u062d\u064a\u062d.",
                _                          => $"\u062a\u0628\u062f\u0623 \u0645\u063a\u0627\u0645\u0631\u0629 \u0631\u064a\u0627\u0636\u064a\u0627\u062a \u062c\u062f\u064a\u062f\u0629 \u0641\u064a \u0627\u0644\u0645\u0633\u062a\u0648\u0649 {level}!"
            };
            return s switch
            {
                MathSubject.Addition       => $"\ud83c\udf4e Farmer Jenny has {a} apples. She picks {b} more. Help her count!",
                MathSubject.Subtraction    => $"\ud83d\udc26 {a + b} birds sat on a wire. {b} flew away. How many are left?",
                MathSubject.Multiplication => $"\ud83d\ude97 There are {b} parking rows with {a} cars each. How many total?",
                MathSubject.Division       => $"\ud83c\udf55 You have {a * b} pizza slices to share equally with {b} friends.",
                MathSubject.Counting       => "\ud83c\udf1f The night sky is magical! Can you count all the stars?",
                MathSubject.Shapes         => "\ud83c\udfd7\ufe0f Architect Aria is designing buildings. She needs your help!",
                MathSubject.Patterns       => "\ud83c\udfa8 Artist Max is creating a pattern. Can you figure out what comes next?",
                MathSubject.Fractions      => "\ud83c\udf82 It's birthday time! Help slice the cake into equal pieces.",
                MathSubject.Measurement    => "\ud83d\udccf Builder Bob needs exact measurements. Can you help him?",
                MathSubject.Time           => "\u23f0 The train schedule needs your help! Read the clocks correctly.",
                MathSubject.Money          => "\ud83c\udfea Welcome to Math Mart! Help the cashier make correct change.",
                _                          => $"A new math adventure begins at Level {level}!"
            };
        }

        public static string StoryOutro(MathSubject s)
        {
            if (Ar) return s switch
            {
                MathSubject.Addition       => "\u0631\u0627\u0626\u0639! \u062c\u0646\u0649 \u0633\u0639\u064a\u062f\u0629 \u062c\u062f\u064b\u0627 \u0628\u0645\u0633\u0627\u0639\u062f\u062a\u0643 \u0641\u064a \u0639\u062f\u0651 \u0627\u0644\u062a\u0641\u0627\u062d! \ud83c\udf89",
                MathSubject.Subtraction    => "\u0645\u0645\u062a\u0627\u0632! \u0627\u0633\u062a\u0642\u0631\u0651\u062a \u0627\u0644\u0637\u064a\u0648\u0631 \u0648\u0633\u0627\u0639\u062f\u062a \u0641\u064a \u0639\u062f\u0651\u0647\u0627. \ud83d\udc26",
                MathSubject.Multiplication => "\u0639\u0628\u0642\u0631\u064a! \u0645\u0648\u0627\u0642\u0641 \u0627\u0644\u0633\u064a\u0627\u0631\u0627\u062a \u0645\u0646\u0638\u0651\u0645\u0629 \u0628\u0641\u0636\u0644\u0643. \ud83d\ude97",
                MathSubject.Division       => "\u0644\u0630\u064a\u0630! \u062d\u0635\u0644 \u0643\u0644 \u0648\u0627\u062d\u062f \u0639\u0644\u0649 \u0642\u0637\u0639\u0629 \u0645\u062a\u0633\u0627\u0648\u064a\u0629. \ud83c\udf55",
                MathSubject.Counting       => "\u0627\u0646\u0638\u0631 \u0625\u0644\u0649 \u0643\u0644 \u062a\u0644\u0643 \u0627\u0644\u0646\u062c\u0648\u0645 \u0627\u0644\u062a\u064a \u0639\u062f\u062f\u062a\u0647\u0627! \u2b50",
                MathSubject.Shapes         => "\u062a\u0638\u0646\u0651 \u0627\u0644\u0645\u0647\u0646\u062f\u0633\u0629 \u0622\u0631\u064a\u0627 \u0623\u0646\u0643 \u0628\u0646\u0651\u0627\u0621 \u0628\u0627\u0644\u0641\u0637\u0631\u0629. \ud83c\udfd7\ufe0f",
                MathSubject.Patterns       => "\u0627\u0644\u0641\u0646\u0651\u0627\u0646 \u0645\u0627\u0643\u0633 \u0645\u0628\u0647\u0648\u0631 \u0628\u062d\u0627\u0633\u0651\u062a\u0643 \u0644\u0644\u0623\u0646\u0645\u0627\u0637. \ud83c\udfa8",
                MathSubject.Fractions      => "\u0627\u0633\u062a\u0645\u062a\u0639 \u0627\u0644\u062c\u0645\u064a\u0639 \u0628\u062d\u0635\u0651\u062a\u0647\u0645 \u0627\u0644\u0645\u062a\u0633\u0627\u0648\u064a\u0629 \u0645\u0646 \u0627\u0644\u0643\u0639\u0643\u0629. \ud83c\udf82",
                MathSubject.Measurement    => "\u0645\u0634\u0631\u0648\u0639 \u0627\u0644\u0628\u0646\u0651\u0627\u0621 \u0628\u0648\u0628 \u0645\u062b\u0627\u0644\u064a \u2014 \u0642\u064a\u0627\u0633 \u0631\u0627\u0626\u0639! \ud83d\udccf",
                MathSubject.Time           => "\u063a\u0627\u062f\u0631 \u0643\u0644 \u0642\u0637\u0627\u0631 \u0641\u064a \u0645\u0648\u0639\u062f\u0647\u060c \u0628\u0641\u0636\u0644\u0643. \u23f0",
                MathSubject.Money          => "\u062d\u0635\u0644 \u0643\u0644 \u0632\u0628\u0648\u0646 \u0641\u064a \u0627\u0644\u0645\u062a\u062c\u0631 \u0639\u0644\u0649 \u0627\u0644\u0628\u0627\u0642\u064a \u0627\u0644\u0635\u062d\u064a\u062d. \ud83c\udfea",
                _                          => "\u0623\u062d\u0633\u0646\u062a! \u062a\u0633\u062a\u0645\u0631\u0651 \u0627\u0644\u0642\u0635\u0651\u0629\u2026"
            };
            return s switch
            {
                MathSubject.Addition       => "Amazing! Jenny is so happy with your help counting her apples! \ud83c\udf89",
                MathSubject.Subtraction    => "Wonderful! The birds are settled and you helped count them. \ud83d\udc26",
                MathSubject.Multiplication => "Brilliant! The parking lot is organized thanks to you. \ud83d\ude97",
                MathSubject.Division       => "Yum! Everyone got an equal slice of pizza. \ud83c\udf55",
                MathSubject.Counting       => "Look at all those stars you counted! \u2b50",
                MathSubject.Shapes         => "Architect Aria thinks you're a natural builder. \ud83c\udfd7\ufe0f",
                MathSubject.Patterns       => "Artist Max is impressed with your pattern eye. \ud83c\udfa8",
                MathSubject.Fractions      => "Everyone enjoyed their fair slice of cake. \ud83c\udf82",
                MathSubject.Measurement    => "Builder Bob's project is perfect \u2014 great measuring! \ud83d\udccf",
                MathSubject.Time           => "Every train left on time, thanks to you. \u23f0",
                MathSubject.Money          => "Every customer at Math Mart got the right change. \ud83c\udfea",
                _                          => "Great job! The story continues\u2026"
            };
        }

        // =====================================================================
        // GRADE 4 & 5 EXTENSIONS
        // =====================================================================
        // Advanced curriculum prompts. All bilingual. Numbers always rendered
        // with Western-Arabic digits; Arabic prose uses Arabic punctuation
        // (\u060c comma, \u061f question mark, etc.).
        // ---------------------------------------------------------------------

        // ----- Addition / Subtraction: large-number word problems -----
        public static string CityPopulation(string name, int a, int b) =>
            Ar ? $"\u064a\u0639\u064a\u0634 \u0641\u064a \u0645\u062f\u064a\u0646\u0629 {name} {a:N0} \u0634\u062e\u0635\u064b\u0627. \u062b\u0645 \u0627\u0646\u062a\u0642\u0644 \u0625\u0644\u064a\u0647\u0627 {b:N0} \u0623\u062e\u0631\u0648\u0646. \u0643\u0645 \u0639\u062f\u062f \u0633\u0643\u0651\u0627\u0646\u0647\u0627 \u0627\u0644\u0622\u0646\u061f"
               : $"City {name} has {a:N0} people. {b:N0} more move in. What is the new population?";
        public static string CityPopulationHint() =>
            Ar ? "\u0627\u062c\u0645\u0639 \u0627\u0644\u0631\u0642\u0645\u064a\u0646 \u0639\u0645\u0648\u062f\u064a\u0651\u064b\u0627\u060c \u0628\u062f\u0621\u064b\u0627 \u0645\u0646 \u0623\u0642\u0635\u0649 \u0627\u0644\u064a\u0645\u064a\u0646."
               : "Add the numbers column by column starting from the right.";

        public static string FactoryBuiltSold(string product, int built, int sold) =>
            Ar ? $"\u0635\u0646\u0651\u0639\u062a \u0648\u0631\u0634\u0629 {built:N0} {product}\u060c \u0648\u0628\u0627\u0639\u062a {sold:N0}. \u0643\u0645 \u062a\u0628\u0642\u0651\u0649 \u0641\u064a \u0627\u0644\u0645\u062e\u0632\u0646\u061f"
               : $"A factory built {built:N0} {product} and sold {sold:N0}. How many remain in stock?";
        public static string FactoryHint(int built, int sold) =>
            Ar ? $"\u0627\u0637\u0631\u062d \u0627\u0644\u0645\u0628\u064a\u0639 \u0645\u0646 \u0627\u0644\u0645\u0635\u0646\u0648\u0639: {built:N0} - {sold:N0}."
               : $"Subtract sold from built: {built:N0} - {sold:N0}.";

        public static string ProductWidgetName(int idx)
        {
            string[] en = { "toys", "books", "phones", "shirts", "shoes", "watches" };
            string[] ar = { "\u0623\u0644\u0639\u0627\u0628", "\u0643\u062a\u0628", "\u0647\u0648\u0627\u062a\u0641", "\u0642\u0645\u0635\u0627\u0646", "\u0623\u062d\u0630\u064a\u0629", "\u0633\u0627\u0639\u0627\u062a" };
            idx = Math.Abs(idx) % en.Length;
            return Ar ? ar[idx] : en[idx];
        }

        // ----- Multiplication: 2x2 + 3x1 word problems -----
        public static string SchoolStudentsBuses(int buses, int per) =>
            Ar ? $"\u062a\u0646\u0642\u0644 \u0645\u062f\u0631\u0633\u0629 \u0637\u0644\u0627\u0628\u0647\u0627 \u0641\u064a {buses} \u062d\u0627\u0641\u0644\u0629\u060c \u0641\u064a \u0643\u0644 \u062d\u0627\u0641\u0644\u0629 {per} \u0637\u0627\u0644\u0628\u064b\u0627. \u0643\u0645 \u0637\u0627\u0644\u0628\u064b\u0627 \u0641\u064a \u0627\u0644\u0645\u062c\u0645\u0648\u0639\u061f"
               : $"A school transports its students in {buses} buses with {per} students each. How many students in total?";
        public static string BoxOfPacks(int packs, int per, string item) =>
            Ar ? $"\u062a\u062d\u062a\u0648\u064a \u0639\u0644\u0628\u0629 \u0639\u0644\u0649 {packs} \u062d\u0632\u0645\u0629\u060c \u0641\u064a \u0643\u0644 \u062d\u0632\u0645\u0629 {per} {item}. \u0643\u0645 \u0627\u0644\u0639\u062f\u062f \u0627\u0644\u0625\u062c\u0645\u0627\u0644\u064a\u061f"
               : $"A carton has {packs} packs with {per} {item} each. How many {item} in total?";

        // ----- Division: with remainders + 2-digit divisors -----
        public static string DivideWithRemainderPrompt(int a, int b) =>
            Ar ? $"{a} \u00f7 {b} = \u061f (\u0627\u0643\u062a\u0628 \u0627\u0644\u0646\u0627\u062a\u062c \u062b\u0645 \u0627\u0644\u0628\u0627\u0642\u064a)"
               : $"{a} \u00f7 {b} = ? (give quotient then remainder)";
        public static string RemainderHint(int b) =>
            Ar ? $"\u0627\u0648\u062c\u062f \u0623\u0643\u0628\u0631 \u0639\u062f\u062f \u0645\u0646 \u0645\u0636\u0627\u0639\u0641\u0627\u062a {b} \u0623\u0635\u063a\u0631 \u0623\u0648 \u064a\u0633\u0627\u0648\u064a \u0627\u0644\u0645\u0642\u0633\u0648\u0645."
               : $"Find the largest multiple of {b} less than or equal to the dividend.";
        public static string QuotientRemainderExplain(int a, int b, int q, int r) =>
            $"{a} = {b} \u00d7 {q} + {r}.";

        public static string SharePeopleRemainder(string name, int total, int people) =>
            Ar ? $"\u064a\u0648\u0632\u0651\u0639 {name} {total} \u0628\u0637\u0627\u0642\u0629 \u0639\u0644\u0649 {people} \u0623\u0635\u062f\u0642\u0627\u0621 \u0628\u0627\u0644\u062a\u0633\u0627\u0648\u064a. \u0643\u0645 \u0628\u0637\u0627\u0642\u0629 \u0633\u062a\u062a\u0628\u0642\u0651\u0649 \u0645\u0639\u0647\u061f"
               : $"{name} shares {total} cards equally between {people} friends. How many cards are left over?";

        public static string LongDivision2Digit(int total, int divisor) =>
            Ar ? $"\u0627\u0648\u062c\u062f \u0646\u0627\u062a\u062c {total} \u00f7 {divisor}."
               : $"Solve {total} \u00f7 {divisor}.";
        public static string LongDivisionHint() =>
            Ar ? "\u0627\u0639\u0645\u0644 \u0628\u0627\u0644\u0642\u0633\u0645\u0629 \u0627\u0644\u0645\u0637\u0648\u0651\u0644\u0629: \u062e\u0630 \u062e\u0627\u0646\u0629 \u062e\u0627\u0646\u0629 \u0645\u0646 \u0627\u0644\u064a\u0633\u0627\u0631."
               : "Use long division: take one digit at a time from the left.";

        // ----- Shapes (G4): triangle area, angles, parallelogram -----
        public static string TriangleAreaPrompt(int b, int h) =>
            Ar ? $"\u0645\u062b\u0644\u0651\u062b \u0642\u0627\u0639\u062f\u062a\u0647 {b} \u0633\u0645 \u0648\u0627\u0631\u062a\u0641\u0627\u0639\u0647 {h} \u0633\u0645. \u0645\u0627 \u0645\u0633\u0627\u062d\u062a\u0647\u061f"
               : $"A triangle has base {b} cm and height {h} cm. What is its area?";
        public static string TriangleAreaFormula() =>
            Ar ? "\u0645\u0633\u0627\u062d\u0629 \u0627\u0644\u0645\u062b\u0644\u062b = \u00bd \u00d7 \u0627\u0644\u0642\u0627\u0639\u062f\u0629 \u00d7 \u0627\u0644\u0627\u0631\u062a\u0641\u0627\u0639."
               : "Triangle area = \u00bd \u00d7 base \u00d7 height.";
        public static string TriangleAreaExplain(int b, int h, int ans) =>
            Ar ? $"\u00bd \u00d7 {b} \u00d7 {h} = {ans} \u0633\u0645\u00b2."
               : $"\u00bd \u00d7 {b} \u00d7 {h} = {ans} cm\u00b2.";

        public static string ClassifyAnglePrompt(int deg) =>
            Ar ? $"\u0635\u0646\u0651\u0641 \u0627\u0644\u0632\u0627\u0648\u064a\u0629 \u0627\u0644\u062a\u064a \u0642\u064a\u0627\u0633\u0647\u0627 {deg}\u00b0."
               : $"Classify the angle that measures {deg}\u00b0.";
        public static string[] AngleOptions() =>
            Ar ? new[] { "\u062d\u0627\u062f\u0629", "\u0642\u0627\u0626\u0645\u0629", "\u0645\u0646\u0641\u0631\u062c\u0629", "\u0645\u0633\u062a\u0642\u064a\u0645\u0629" }
               : new[] { "Acute", "Right", "Obtuse", "Straight" };
        public static string AngleClassify(int deg)
        {
            if (deg < 90) return Ar ? "\u062d\u0627\u062f\u0629" : "Acute";
            if (deg == 90) return Ar ? "\u0642\u0627\u0626\u0645\u0629" : "Right";
            if (deg < 180) return Ar ? "\u0645\u0646\u0641\u0631\u062c\u0629" : "Obtuse";
            return Ar ? "\u0645\u0633\u062a\u0642\u064a\u0645\u0629" : "Straight";
        }
        public static string AngleHint() =>
            Ar ? "\u0627\u0644\u0632\u0627\u0648\u064a\u0629 \u0627\u0644\u0642\u0627\u0626\u0645\u0629 = 90\u00b0\u060c \u0648\u0627\u0644\u0645\u0633\u062a\u0642\u064a\u0645\u0629 = 180\u00b0."
               : "Right angle = 90\u00b0, straight angle = 180\u00b0.";

        // ----- Shapes (G5): volume + composite -----
        public static string CubeVolumePrompt(int s) =>
            Ar ? $"\u0645\u0643\u0639\u0651\u0628 \u0637\u0648\u0644 \u062d\u0631\u0641\u0647 {s} \u0633\u0645. \u0645\u0627 \u062d\u062c\u0645\u0647\u061f"
               : $"A cube has edge {s} cm. What is its volume?";
        public static string CubeVolumeFormula() =>
            Ar ? "\u062d\u062c\u0645 \u0627\u0644\u0645\u0643\u0639\u0651\u0628 = \u0627\u0644\u0636\u0644\u0639 \u00d7 \u0627\u0644\u0636\u0644\u0639 \u00d7 \u0627\u0644\u0636\u0644\u0639." : "Cube volume = edge \u00d7 edge \u00d7 edge.";
        public static string CubeVolumeExplain(int s, int v) =>
            Ar ? $"{s}\u00d7{s}\u00d7{s} = {v} \u0633\u0645\u00b3." : $"{s}\u00d7{s}\u00d7{s} = {v} cm\u00b3.";

        public static string PrismVolumePrompt(int l, int w, int h) =>
            Ar ? $"\u0645\u062a\u0648\u0627\u0632\u064a \u0645\u0633\u062a\u0637\u064a\u0644\u0627\u062a \u0623\u0628\u0639\u0627\u062f\u0647 {l}\u00d7{w}\u00d7{h} \u0633\u0645. \u0645\u0627 \u062d\u062c\u0645\u0647\u061f"
               : $"A rectangular prism is {l}\u00d7{w}\u00d7{h} cm. What is its volume?";
        public static string PrismVolumeFormula() =>
            Ar ? "\u0627\u0644\u062d\u062c\u0645 = \u0627\u0644\u0637\u0648\u0644 \u00d7 \u0627\u0644\u0639\u0631\u0636 \u00d7 \u0627\u0644\u0627\u0631\u062a\u0641\u0627\u0639." : "Volume = length \u00d7 width \u00d7 height.";
        public static string PrismVolumeExplain(int l, int w, int h, int v) =>
            Ar ? $"{l}\u00d7{w}\u00d7{h} = {v} \u0633\u0645\u00b3." : $"{l}\u00d7{w}\u00d7{h} = {v} cm\u00b3.";

        // ----- Patterns (G4/G5): find-the-rule + position-to-term -----
        public static string FindRulePrompt(int s0, int s1, int s2, int s3) =>
            Ar ? $"\u0627\u0648\u062c\u062f \u0627\u0644\u0642\u0627\u0639\u062f\u0629\u060c \u062b\u0645 \u062f\u064f\u0644\u0651 \u0639\u0644\u0649 \u0627\u0644\u062d\u062f \u0627\u0644\u062a\u0627\u0644\u064a:\n{s0}\u060c {s1}\u060c {s2}\u060c {s3}\u060c \u061f"
               : $"Find the rule, then give the next term:\n{s0}, {s1}, {s2}, {s3}, ?";
        public static string FindRuleAddHint(int step) =>
            Ar ? $"\u0627\u0644\u0641\u0631\u0642 \u062b\u0627\u0628\u062a: +{step}." : $"Constant difference: +{step}.";
        public static string FindRuleMulHint(int ratio) =>
            Ar ? $"\u0627\u0644\u0646\u0633\u0628\u0629 \u062b\u0627\u0628\u062a\u0629: \u00d7{ratio}." : $"Constant ratio: \u00d7{ratio}.";

        public static string TermRulePrompt(string ruleEn, string ruleAr, int n) =>
            Ar ? $"\u0627\u062d\u0633\u0628 \u0627\u0644\u062d\u062f\u0651 \u0631\u0642\u0645 {n} \u0644\u0644\u0642\u0627\u0639\u062f\u0629: {ruleAr}."
               : $"Compute term number {n} for the rule: {ruleEn}.";
        public static string TermRuleHint() =>
            Ar ? "\u0639\u0648\u0651\u0636 \u0639\u0646 n \u0628\u0631\u0642\u0645 \u0627\u0644\u062d\u062f\u0651." : "Substitute n with the term position.";
        public static string TermRuleExplain(string ruleEn, string ruleAr, int n, int ans) =>
            Ar ? $"{ruleAr} \u0639\u0646\u062f n={n}: \u0627\u0644\u0646\u0627\u062a\u062c {ans}." : $"{ruleEn} at n={n} gives {ans}.";

        // ----- Fractions (G4): add/sub same denom + compare + simplify -----
        public static string FracAddPrompt(int n1, int n2, int den) =>
            Ar ? $"\u0623\u0648\u062c\u062f: {n1}/{den} + {n2}/{den}." : $"Compute: {n1}/{den} + {n2}/{den}.";
        public static string FracSubPrompt(int n1, int n2, int den) =>
            Ar ? $"\u0623\u0648\u062c\u062f: {n1}/{den} - {n2}/{den}." : $"Compute: {n1}/{den} - {n2}/{den}.";
        public static string FracSameDenHint() =>
            Ar ? "\u0639\u0646\u062f \u062a\u0633\u0627\u0648\u064a \u0627\u0644\u0645\u0642\u0627\u0645\u060c \u0627\u062c\u0645\u0639 \u0623\u0648 \u0627\u0637\u0631\u062d \u0627\u0644\u0628\u0633\u0648\u0637 \u0641\u0642\u0637."
               : "With equal denominators, add or subtract the numerators only.";
        public static string FracSimplifyPrompt(int n, int d) =>
            Ar ? $"\u0628\u0633\u0651\u0637: {n}/{d}." : $"Simplify: {n}/{d}.";
        public static string FracSimplifyHint() =>
            Ar ? "\u0627\u0642\u0633\u0645 \u0627\u0644\u0628\u0633\u0637 \u0648\u0627\u0644\u0645\u0642\u0627\u0645 \u0639\u0644\u0649 \u0627\u0644\u0639\u0627\u0645\u0644 \u0627\u0644\u0645\u0634\u062a\u0631\u0643 \u0627\u0644\u0623\u0643\u0628\u0631."
               : "Divide top and bottom by the greatest common factor.";
        public static string FracCompare(int n1, int d1, int n2, int d2) =>
            Ar ? $"\u0623\u064a\u0651\u0647\u0645\u0627 \u0623\u0643\u0628\u0631: {n1}/{d1} \u0623\u0645 {n2}/{d2}\u061f"
               : $"Which is greater: {n1}/{d1} or {n2}/{d2}?";
        public static string FracCompareHint() =>
            Ar ? "\u0648\u062d\u0651\u062f \u0627\u0644\u0645\u0642\u0627\u0645\u0627\u062a \u0623\u0648\u0644\u0627\u064b." : "Make the denominators equal first.";

        // ----- Fractions (G5): unlike denominators -----
        public static string FracAddUnlike(int n1, int d1, int n2, int d2) =>
            Ar ? $"\u0623\u0648\u062c\u062f: {n1}/{d1} + {n2}/{d2}." : $"Compute: {n1}/{d1} + {n2}/{d2}.";
        public static string FracSubUnlike(int n1, int d1, int n2, int d2) =>
            Ar ? $"\u0623\u0648\u062c\u062f: {n1}/{d1} - {n2}/{d2}." : $"Compute: {n1}/{d1} - {n2}/{d2}.";
        public static string FracUnlikeHint(int lcm) =>
            Ar ? $"\u0648\u062d\u0651\u062f \u0627\u0644\u0645\u0642\u0627\u0645\u0627\u062a \u0625\u0644\u0649 {lcm} \u0623\u0648\u0644\u0627\u064b."
               : $"Convert both fractions to have denominator {lcm} first.";

        // ----- Measurement (G4/G5): compound + area/volume -----
        public static string CompoundLength(int km, int m) =>
            Ar ? $"\u0643\u0645 \u0645\u062a\u0631\u064b\u0627 \u0641\u064a {km} \u0643\u0645 + {m} \u0645\u061f"
               : $"How many metres in {km} km + {m} m?";
        public static string CompoundLengthHint() =>
            Ar ? "1 \u0643\u0645 = 1000 \u0645. \u0627\u0636\u0631\u0628 \u062b\u0645 \u0627\u062c\u0645\u0639."
               : "1 km = 1000 m. Multiply then add.";

        public static string CompoundMass(int kg, int g) =>
            Ar ? $"\u0643\u0645 \u063a\u0631\u0627\u0645\u064b\u0627 \u0641\u064a {kg} \u0643\u063a + {g} \u063a\u061f"
               : $"How many grams in {kg} kg + {g} g?";
        public static string CompoundMassHint() =>
            Ar ? "1 \u0643\u063a = 1000 \u063a." : "1 kg = 1000 g.";

        public static string CompoundVolume(int l, int ml) =>
            Ar ? $"\u0643\u0645 \u0645\u0644\u0644\u064a\u0644\u062a\u0631\u064b\u0627 \u0641\u064a {l} \u0644 + {ml} \u0645\u0644\u061f"
               : $"How many millilitres in {l} l + {ml} ml?";
        public static string CompoundVolumeHint() =>
            Ar ? "1 \u0644 = 1000 \u0645\u0644." : "1 l = 1000 ml.";

        public static string AreaInMetres(int w, int h) =>
            Ar ? $"\u063a\u0631\u0641\u0629 \u0623\u0628\u0639\u0627\u062f\u0647\u0627 {w} \u0645 \u0641\u064a {h} \u0645. \u0645\u0627 \u0645\u0633\u0627\u062d\u062a\u0647\u0627 \u0628\u0627\u0644\u0645\u062a\u0631 \u0627\u0644\u0645\u0631\u0628\u0651\u0639\u061f"
               : $"A room is {w} m by {h} m. What is its area in m\u00b2?";
        public static string AreaInMetresExplain(int w, int h, int ans) =>
            Ar ? $"{w} \u00d7 {h} = {ans} \u0645\u00b2." : $"{w} \u00d7 {h} = {ans} m\u00b2.";

        // ----- Time (G4): 24-hour conversion + add durations -----
        public static string Convert24To12(int h24, int m) =>
            Ar ? $"\u0645\u0627 \u0645\u0627 \u064a\u0642\u0627\u0628\u0644 {h24:00}:{m:00} \u0628\u0646\u0638\u0627\u0645 12 \u0633\u0627\u0639\u0629\u061f"
               : $"What is {h24:00}:{m:00} in 12-hour time?";
        public static string TwentyFourHourHint() =>
            Ar ? "\u0625\u0630\u0627 \u0643\u0627\u0646\u062a \u0627\u0644\u0633\u0627\u0639\u0629 > 12\u060c \u0627\u0637\u0631\u062d 12 \u0648\u0623\u0636\u0641 'm'."
               : "If the hour > 12, subtract 12 and add ' pm'.";
        public static string Format12HourClock(int h24, int m)
        {
            string suffix = h24 >= 12 ? (Ar ? "\u0645\u0633\u0627\u0621\u064b" : "pm") : (Ar ? "\u0635\u0628\u0627\u062d\u064b\u0627" : "am");
            int h12 = h24 % 12; if (h12 == 0) h12 = 12;
            return Ar ? $"{h12}:{m:00} {suffix}" : $"{h12}:{m:00} {suffix}";
        }

        public static string AddDuration(int h1, int m1, int addMin) =>
            Ar ? $"\u0628\u062f\u0623\u062a \u0627\u0644\u0645\u0628\u0627\u0631\u0627\u0629 \u0641\u064a {h1}:{m1:00} \u0648\u0627\u0633\u062a\u0645\u0631\u0651\u062a {addMin} \u062f\u0642\u064a\u0642\u0629. \u0645\u062a\u0649 \u0627\u0646\u062a\u0647\u062a\u061f"
               : $"A match starts at {h1}:{m1:00} and lasts {addMin} minutes. What time does it end?";
        public static string AddDurationHint() =>
            Ar ? "\u0623\u0636\u0641 \u0627\u0644\u062f\u0642\u0627\u0626\u0642\u060c \u062b\u0645 \u062d\u0648\u0651\u0644 \u0643\u0644 60 \u062f\u0642\u064a\u0642\u0629 \u0625\u0644\u0649 \u0633\u0627\u0639\u0629 \u062c\u062f\u064a\u062f\u0629."
               : "Add minutes, then convert every 60 minutes into an extra hour.";

        // ----- Time (G5): across days, multi-leg journey -----
        public static string MultiLegTrip(int leg1, int leg2) =>
            Ar ? $"\u0627\u0633\u062a\u063a\u0631\u0642\u062a \u0631\u062d\u0644\u0629 \u0623\u0648\u0644\u0649 {leg1} \u062f\u0642\u064a\u0642\u0629 \u0648\u062b\u0627\u0646\u064a\u0629 {leg2} \u062f\u0642\u064a\u0642\u0629. \u0645\u0627 \u0625\u062c\u0645\u0627\u0644\u064a \u0627\u0644\u0648\u0642\u062a \u0628\u0627\u0644\u062f\u0642\u0627\u0626\u0642\u061f"
               : $"Trip A takes {leg1} min, trip B takes {leg2} min. Total minutes?";
        public static string MultiLegHint(int leg1, int leg2) =>
            Ar ? $"\u0627\u062c\u0645\u0639: {leg1} + {leg2}." : $"Add: {leg1} + {leg2}.";

        // ----- Money (G4): multi-item totals -----
        public static string BillManyItems(int qty, int unitPrice, string item) =>
            Ar ? $"\u0633\u0639\u0631 \u0627\u0644\u0648\u0627\u062d\u062f {unitPrice}\u0641 \u0648\u062a\u0631\u064a\u062f \u0634\u0631\u0627\u0621 {qty} \u0645\u0646 {item}. \u0645\u0627 \u0627\u0644\u062a\u0643\u0644\u0641\u0629 \u0627\u0644\u0625\u062c\u0645\u0627\u0644\u064a\u0629\u061f"
               : $"{item} cost {unitPrice}c each. What is the total cost for {qty}?";
        public static string BillManyItemsHint(int qty, int up) =>
            Ar ? $"\u0627\u0636\u0631\u0628: {qty} \u00d7 {up}." : $"Multiply: {qty} \u00d7 {up}.";

        public static string TwoLineBill(int q1, int p1, string i1, int q2, int p2, string i2) =>
            Ar ? $"\u0627\u0634\u062a\u0631\u064a\u062a {q1} {i1} \u0628\u0640{p1}\u0641 \u0644\u0644\u0648\u0627\u062d\u062f \u0648{q2} {i2} \u0628\u0640{p2}\u0641 \u0644\u0644\u0648\u0627\u062d\u062f. \u0645\u0627 \u0627\u0644\u0645\u062c\u0645\u0648\u0639\u061f"
               : $"You buy {q1} {i1} at {p1}c each and {q2} {i2} at {p2}c each. Total?";
        public static string TwoLineBillHint(int q1, int p1, int q2, int p2) =>
            Ar ? $"\u0627\u0644\u062e\u0637\u0648\u0629 1: {q1}\u00d7{p1}.\n\u0627\u0644\u062e\u0637\u0648\u0629 2: {q2}\u00d7{p2}.\n\u0627\u0644\u062e\u0637\u0648\u0629 3: \u0627\u0644\u0645\u062c\u0645\u0648\u0639."
               : $"Step 1: {q1}\u00d7{p1}.\nStep 2: {q2}\u00d7{p2}.\nStep 3: add the two.";

        // ----- Money (G5): percentages + discounts -----
        public static string PercentOf(int pct, int amount) =>
            Ar ? $"\u0643\u0645 \u064a\u0628\u0644\u063a {pct}% \u0645\u0646 {amount}\u0641\u061f"
               : $"What is {pct}% of {amount}c?";
        public static string PercentHint() =>
            Ar ? "\u0627\u0636\u0631\u0628 \u0627\u0644\u0645\u0628\u0644\u063a \u0641\u064a \u0627\u0644\u0646\u0633\u0628\u0629 \u0648\u0627\u0642\u0633\u0645 \u0639\u0644\u0649 100."
               : "Multiply the amount by the percent, then divide by 100.";
        public static string PercentExplain(int pct, int amount, int ans) =>
            Ar ? $"{amount} \u00d7 {pct} \u00f7 100 = {ans}\u0641." : $"{amount} \u00d7 {pct} \u00f7 100 = {ans}c.";

        public static string DiscountPrompt(int price, int pctOff) =>
            Ar ? $"\u0633\u0639\u0631 \u0623\u0635\u0644\u064a {price}\u0641 \u0648\u062e\u0635\u0645 {pctOff}%. \u0645\u0627 \u0627\u0644\u0633\u0639\u0631 \u0628\u0639\u062f \u0627\u0644\u062e\u0635\u0645\u061f"
               : $"Original price {price}c, discount {pctOff}%. Final price?";
        public static string DiscountHint(int pctOff) =>
            Ar ? $"\u0627\u062d\u0633\u0628 {pctOff}% \u0645\u0646 \u0627\u0644\u0633\u0639\u0631 \u062b\u0645 \u0627\u0637\u0631\u062d\u0647 \u0645\u0646 \u0627\u0644\u0633\u0639\u0631 \u0627\u0644\u0623\u0635\u0644\u064a."
               : $"Find {pctOff}% of the price then subtract it from the original price.";
        public static string DiscountExplain(int price, int pctOff, int off, int final) =>
            Ar ? $"{price} - {off} = {final}\u0641 ({pctOff}% \u062e\u0635\u0645)."
               : $"{price} - {off} = {final}c ({pctOff}% off).";
    }
}
