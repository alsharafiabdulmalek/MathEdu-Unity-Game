// -----------------------------------------------------------------------------
// GradeData.cs
// -----------------------------------------------------------------------------
// A grade (1, 2, or 3) groups together the subjects available to children at
// that grade level. The Main Menu's grade selector picks one of these.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace MathEdu.Data
{
    [CreateAssetMenu(
        fileName = "Grade_",
        menuName = "MathEdu/Grade Data",
        order    = 10)]
    public class GradeData : ScriptableObject
    {
        [Header("Identity")]
        [Range(1, 3)] public int gradeNumber = 1;
        public string displayName = "Grade 1";

        [TextArea(2, 4)]
        public string description = "Counting, simple addition, and shapes.";

        [Header("Theme")]
        public Color themeColor = new Color(1.00f, 0.78f, 0.36f); // warm yellow

        [Header("Subjects available to this grade")]
        public List<SubjectData> subjects = new List<SubjectData>();

        /// <summary>Stable id used as a save key prefix.</summary>
        public string GradeKey => $"g{gradeNumber}";
    }
}
