// -----------------------------------------------------------------------------
// SubjectData.cs
// -----------------------------------------------------------------------------
// A subject is one row on the Subject Grid (e.g. Addition, Subtraction,
// Shapes, Time). It owns an ordered list of LevelData assets that the
// player unlocks sequentially.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace MathEdu.Data
{
    /// <summary>
    /// Canonical list of math subjects supported by the game. Adding a new
    /// subject is as simple as adding an enum value and a matching
    /// SubjectData asset.
    /// </summary>
    public enum MathSubject
    {
        Counting,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Shapes,
        Patterns,
        Fractions,
        Measurement,
        Time,
        Money
    }

    [CreateAssetMenu(
        fileName = "Subject_",
        menuName = "MathEdu/Subject Data",
        order    = 20)]
    public class SubjectData : ScriptableObject
    {
        [Header("Identity")]
        public MathSubject subject = MathSubject.Addition;
        public string displayName  = "Addition";

        [TextArea(2, 4)]
        public string description = "Practice your addition skills!";

        [Header("Visuals (placeholder until assets arrive)")]
        public Color themeColor = new Color(0.30f, 0.65f, 0.95f);
        public string iconEmoji = "➕";   // Used until sprite icons land
        public Sprite icon;               // Optional override

        [Header("Levels (in play order)")]
        public List<LevelData> levels = new List<LevelData>();

        /// <summary>Stable id used as a save key.</summary>
        public string SubjectKey => subject.ToString().ToLowerInvariant();
    }
}
