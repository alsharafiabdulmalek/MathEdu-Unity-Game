// -----------------------------------------------------------------------------
// ProgressManager.cs
// -----------------------------------------------------------------------------
// Coordinates progress events: recording results, unlocking next levels,
// awarding XP / badges, and persisting changes via SaveSystem.
//
// This is intentionally thin — it sits between the gameplay screens and
// the PlayerProfile data, so future tweaks (e.g. an XP curve) live in one
// place.
// -----------------------------------------------------------------------------

using System;
using MathEdu.Data;
using MathEdu.Utility;
using UnityEngine;

namespace MathEdu.Managers
{
    public class ProgressManager : MonoBehaviour
    {
        public event Action<LevelData, int /*stars*/, int /*score*/> OnLevelCompleted;
        public event Action<string /*badgeId*/>                      OnBadgeAwarded;
        public event Action<int /*newXp*/>                           OnXPGained;

        /// <summary>
        /// Record a finished level. Returns the number of stars awarded.
        /// </summary>
        public int CompleteLevel(LevelData level, int correct, int total, int score)
        {
            if (level == null) return 0;
            var profile = GameManager.Instance.Profile;
            if (profile == null) return 0;

            int stars = level.ComputeStars(correct, total);
            profile.RecordResult(level.levelId, stars, score);

            // XP scaled by stars
            int xp = level.xpReward * Mathf.Max(1, stars);
            profile.xp += xp;
            OnXPGained?.Invoke(profile.xp);

            // Badge?
            if (stars == 3 && !string.IsNullOrEmpty(level.badgeId))
            {
                profile.AwardBadge(level.badgeId);
                OnBadgeAwarded?.Invoke(level.badgeId);
            }

            // Unlock next level in the same subject
            UnlockNext(level);

            SaveSystem.Save(profile);
            OnLevelCompleted?.Invoke(level, stars, score);
            return stars;
        }

        private void UnlockNext(LevelData current)
        {
            var subject = GameManager.Instance.CurrentSubject;
            if (subject == null) return;
            int idx = subject.levels.IndexOf(current);
            if (idx >= 0 && idx + 1 < subject.levels.Count)
            {
                var next = subject.levels[idx + 1];
                if (next != null) GameManager.Instance.Profile.Unlock(next.levelId);
            }
        }
    }
}
