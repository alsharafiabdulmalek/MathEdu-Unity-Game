// -----------------------------------------------------------------------------
// MathQuestion.cs
// -----------------------------------------------------------------------------
// A single multiple-choice math question. Lightweight serializable class used
// inside LevelData. Not a ScriptableObject by itself (kept inline so designers
// don't have to manage thousands of question assets), but still fully
// inspector-friendly.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace MathEdu.Data
{
    /// <summary>
    /// Visual presentation hint for the question prompt. Allows the UI layer to
    /// render the question with the appropriate widget (text only, image,
    /// counting dots, clock face, etc.) without baking widgets into the data.
    /// </summary>
    public enum QuestionVisual
    {
        TextOnly,
        Dots,           // Render N dots / objects for counting questions
        ShapePicker,    // Render shape silhouettes as options
        ClockFace,      // Render an analog clock
        Money,          // Render coins / bills
        Fraction,       // Render a pie / bar fraction
        NumberLine,
        Pattern         // Render a sequence with a missing piece
    }

    /// <summary>
    /// Difficulty rating, 1 = easiest, 5 = hardest. Used to weight quiz
    /// scoring and to filter questions inside an adaptive Practice Mode.
    /// </summary>
    [Serializable]
    public enum QuestionDifficulty
    {
        VeryEasy = 1,
        Easy     = 2,
        Medium   = 3,
        Hard     = 4,
        VeryHard = 5
    }

    [Serializable]
    public class MathQuestion
    {
        [Tooltip("The prompt shown to the player. Supports TMP rich text.")]
        [TextArea(2, 4)]
        public string prompt;

        [Tooltip("Exactly four multiple-choice options.")]
        public string[] options = new string[4];

        [Tooltip("Index (0-3) of the correct option in 'options'.")]
        [Range(0, 3)]
        public int correctIndex;

        [Tooltip("Short hint shown when the player taps the hint button.")]
        [TextArea(1, 3)]
        public string hint;

        [Tooltip("Optional one-line explanation shown after answering.")]
        [TextArea(1, 3)]
        public string explanation;

        public QuestionDifficulty difficulty = QuestionDifficulty.Easy;

        [Tooltip("How this question should be rendered by the gameplay UI.")]
        public QuestionVisual visual = QuestionVisual.TextOnly;

        [Tooltip("Optional numeric payload used by visual renderers " +
                 "(e.g. number of dots, hour/minute for clock, fraction num/den).")]
        public int[] visualPayload = Array.Empty<int>();

        // -------------------------------------------------------------------
        // Convenience helpers used at runtime.
        // -------------------------------------------------------------------

        public string CorrectAnswer =>
            (options != null && correctIndex >= 0 && correctIndex < options.Length)
                ? options[correctIndex]
                : string.Empty;

        public bool IsCorrect(int chosenIndex) => chosenIndex == correctIndex;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(prompt)) return false;
            if (options == null || options.Length != 4) return false;
            if (correctIndex < 0 || correctIndex > 3) return false;
            for (int i = 0; i < 4; i++)
                if (string.IsNullOrEmpty(options[i])) return false;
            return true;
        }
    }
}
