// -----------------------------------------------------------------------------
// LevelData.cs
// -----------------------------------------------------------------------------
// One playable level inside a subject. Each level is a ScriptableObject so
// teachers / designers can tune the question bank, time limits, and reward
// stars without touching code.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace MathEdu.Data
{
    [CreateAssetMenu(
        fileName = "Level_",
        menuName = "MathEdu/Level Data",
        order    = 30)]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id, e.g. 'g1_addition_l3'. Used as the save key.")]
        public string levelId;

        [Tooltip("Player-facing level number (1, 2, 3 …).")]
        public int levelNumber = 1;

        [Tooltip("Human-readable title shown in the level select tile.")]
        public string displayTitle = "Level 1";

        [Header("Lesson Text (used by Learn Mode)")]
        [TextArea(2, 4)] public string lessonIntro;
        [TextArea(2, 6)] public string lessonExample;
        [TextArea(2, 4)] public string lessonTip;

        [Header("Story Mode (optional narrative)")]
        [TextArea(2, 4)] public string storyIntro;
        [TextArea(2, 4)] public string storyOutro;

        [Header("Question Set")]
        [Tooltip("All multiple-choice questions for this level.")]
        public List<MathQuestion> questions = new List<MathQuestion>();

        [Header("Quiz / Speed Round Tuning")]
        [Tooltip("Seconds allowed per question in Quiz Mode (0 = untimed).")]
        public float quizSecondsPerQuestion = 20f;

        [Tooltip("Seconds allowed per question in Speed Round (0 = untimed).")]
        public float speedSecondsPerQuestion = 5f;

        [Header("Star Thresholds (% correct)")]
        [Range(0, 100)] public int oneStarPercent   = 50;
        [Range(0, 100)] public int twoStarPercent   = 75;
        [Range(0, 100)] public int threeStarPercent = 95;

        [Header("Rewards")]
        public int xpReward    = 25;
        public string badgeId  = "";

        /// <summary>
        /// Compute the star rating earned for a given percentage of correct answers.
        /// </summary>
        public int ComputeStars(int correct, int total)
        {
            if (total <= 0) return 0;
            int pct = Mathf.RoundToInt(100f * correct / total);
            if (pct >= threeStarPercent) return 3;
            if (pct >= twoStarPercent)   return 2;
            if (pct >= oneStarPercent)   return 1;
            return 0;
        }
    }
}
