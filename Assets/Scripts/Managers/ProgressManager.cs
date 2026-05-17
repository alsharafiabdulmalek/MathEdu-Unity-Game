// -----------------------------------------------------------------------------
// ProgressManager.cs
// -----------------------------------------------------------------------------
// Coordinates progress events: recording results, unlocking next levels,
// awarding XP / badges, and persisting changes via SaveSystem.
//
// In addition to per-level state, ProgressManager now updates per-subject
// statistics on PlayerProfile.subjectStats so the Parental Dashboard can read
// the numbers directly without scanning the entire content tree.
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

            int wrong = Math.Max(0, total - correct);
            int stars = level.ComputeStars(correct, total);
            bool firstCompletion = profile.GetOrCreate(level.levelId).timesPlayed == 0;
            profile.RecordResult(level.levelId, stars, score);

            // XP scaled by stars (minimum 1× for a played-but-failed level).
            int xp = level.xpReward * Mathf.Max(1, stars);
            profile.xp += xp;
            OnXPGained?.Invoke(profile.xp);

            // Badge?
            if (stars == 3 && !string.IsNullOrEmpty(level.badgeId))
            {
                profile.AwardBadge(level.badgeId);
                OnBadgeAwarded?.Invoke(level.badgeId);
            }

            // Per-subject rollup for the Parental Dashboard.
            var subj = GameManager.Instance.CurrentSubject;
            if (subj != null)
            {
                profile.RecordSession(
                    subjectKey: subj.SubjectKey,
                    correct:    correct,
                    wrong:      wrong,
                    starsThisSession: stars,
                    levelCompleted:   firstCompletion && stars > 0,
                    seconds:    GameManager.Instance.Session.elapsedSeconds);
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
