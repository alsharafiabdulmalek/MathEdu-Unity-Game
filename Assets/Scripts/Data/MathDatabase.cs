// -----------------------------------------------------------------------------
// MathDatabase.cs
// -----------------------------------------------------------------------------
// Root container that holds references to every grade in the game. The
// GameManager keeps a reference to a single MathDatabase asset and queries
// it for grades / subjects / levels at runtime.
//
// We deliberately keep this lookup-only: write operations (unlocks, stars)
// live on PlayerProfile.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace MathEdu.Data
{
    [CreateAssetMenu(
        fileName = "MathDatabase",
        menuName = "MathEdu/Math Database",
        order    = 0)]
    public class MathDatabase : ScriptableObject
    {
        [Header("All Grades (1-3)")]
        public List<GradeData> grades = new List<GradeData>();

        // -------------------------------------------------------------------
        // Lookups
        // -------------------------------------------------------------------

        public GradeData GetGrade(int gradeNumber)
        {
            foreach (var g in grades)
                if (g != null && g.gradeNumber == gradeNumber)
                    return g;
            return null;
        }

        public SubjectData GetSubject(int gradeNumber, MathSubject subject)
        {
            var g = GetGrade(gradeNumber);
            if (g == null) return null;
            foreach (var s in g.subjects)
                if (s != null && s.subject == subject)
                    return s;
            return null;
        }

        public LevelData GetLevel(int gradeNumber, MathSubject subject, int levelNumber)
        {
            var s = GetSubject(gradeNumber, subject);
            if (s == null) return null;
            foreach (var l in s.levels)
                if (l != null && l.levelNumber == levelNumber)
                    return l;
            return null;
        }

        public int TotalQuestionCount
        {
            get
            {
                int n = 0;
                foreach (var g in grades)
                    if (g != null)
                        foreach (var s in g.subjects)
                            if (s != null)
                                foreach (var l in s.levels)
                                    if (l != null)
                                        n += l.questions.Count;
                return n;
            }
        }
    }
}
