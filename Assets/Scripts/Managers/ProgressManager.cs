// -----------------------------------------------------------------------------
// ProgressManager.cs
// -----------------------------------------------------------------------------
// Coordinates progress events: recording results, unlocking next levels,
// awarding XP / badges, persisting changes via SaveSystem, and producing
// the SessionResult payload consumed by the Results scene.
//
// Badge taxonomy implemented here (see MaybeAwardMetaBadges for full rules):
//   • first_step              — first level ever completed with ≥1 star
//   • {subject}_apprentice_g{N}  — complete Level 5 of subject in grade N
//   • {subject}_master_g{N}      — complete Level 20 of subject in grade N
//   • half_way_there           — any subject Level 10 completed
//   • speed_demon             — survive 25 correct in a row in Speed Round
//   • perfect_score           — get 10/10 in Quiz Mode
//   • early_bird              — complete a level before 8 AM local time
//   • dedicated               — play on 3 consecutive days
//
// Unlock rules:
//   • Level 1 of every subject is unlocked at boot via GameManager.
//   • Level N+1 unlocks when level N has been completed with at least 1 star.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
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
        /// Record a finished level. Returns a fully-populated SessionResult
        /// for the Results scene to render. Always safe to call even when
        /// inputs are partially null (returns a fallback SessionResult).
        /// </summary>
        public SessionResult CompleteLevel(
            LevelData level, int correct, int total, int score,
            LearningMode mode, int maxStreak, bool failedEarly)
        {
            var fallback = new SessionResult
            {
                levelId     = level != null ? level.levelId : "unknown",
                levelNumber = level != null ? level.levelNumber : 1,
                gradeNumber = GameManager.Instance != null ? GameManager.Instance.Session.selectedGrade : 1,
                subject     = GameManager.Instance != null ? GameManager.Instance.Session.selectedSubject : MathSubject.Addition,
                mode        = mode,
                correct     = correct,
                wrong       = Math.Max(0, total - correct),
                total       = total,
                score       = score,
                streak      = maxStreak,
                failedEarly = failedEarly,
                stars       = 0,
                xpEarned    = 0,
                nextLevelUnlocked = false,
                newBadges   = Array.Empty<string>()
            };

            if (level == null)
            {
                Debug.LogWarning("[ProgressManager] CompleteLevel called with null level.");
                return fallback;
            }
            var profile = GameManager.Instance != null ? GameManager.Instance.Profile : null;
            if (profile == null)
            {
                Debug.LogWarning("[ProgressManager] No PlayerProfile available; returning fallback result.");
                return fallback;
            }

            int wrong = Math.Max(0, total - correct);
            int stars = level.ComputeStars(correct, total);
            bool firstCompletion = profile.GetOrCreate(level.levelId).timesPlayed == 0;
            profile.RecordResult(level.levelId, stars, score);

            // XP scaled by stars (minimum 1× for a played-but-failed level).
            int xp = level.xpReward * Mathf.Max(1, stars);
            profile.xp += xp;
            OnXPGained?.Invoke(profile.xp);

            var newBadges = new List<string>();

            // Optional badge baked onto the LevelData itself (e.g. mastery
            // badges on level 20). Only granted at 3-star clears.
            if (stars == 3 && !string.IsNullOrEmpty(level.badgeId)
                && !profile.HasBadge(level.badgeId))
            {
                profile.AwardBadge(level.badgeId);
                newBadges.Add(level.badgeId);
                OnBadgeAwarded?.Invoke(level.badgeId);
            }

            // Per-subject rollup for the Parental Dashboard.
            var subj = GameManager.Instance.CurrentSubject;
            string subjectKey = subj != null ? subj.SubjectKey : level.levelId;
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

            // Unlock next level — only if the player earned at least 1 star.
            bool nextUnlocked = stars > 0 && UnlockNext(level);
            if (nextUnlocked && subj != null)
            {
                int nextLevelNumber = level.levelNumber + 1;
                profile.RecordSubjectHighestUnlocked(subj.SubjectKey, nextLevelNumber);
            }
            else if (subj != null)
            {
                profile.RecordSubjectHighestUnlocked(subj.SubjectKey, level.levelNumber);
            }

            // Meta badges (first step, perfect score, speed demon, etc.).
            MaybeAwardMetaBadges(profile, level, correct, total, score, stars, mode, maxStreak, newBadges, subjectKey);

            // Daily play streak.
            bool streakExtended = profile.TouchPlayDay();
            if (streakExtended && profile.consecutiveDayStreak >= 3
                && !profile.HasBadge("dedicated"))
            {
                profile.AwardBadge("dedicated");
                newBadges.Add("dedicated");
                OnBadgeAwarded?.Invoke("dedicated");
            }

            SaveSystem.Save(profile);
            OnLevelCompleted?.Invoke(level, stars, score);

            return new SessionResult
            {
                levelId           = level.levelId,
                levelNumber       = level.levelNumber,
                gradeNumber       = GameManager.Instance.Session.selectedGrade,
                subject           = GameManager.Instance.Session.selectedSubject,
                mode              = mode,
                correct           = correct,
                wrong             = wrong,
                total             = total,
                score             = score,
                stars             = stars,
                xpEarned          = xp,
                streak            = maxStreak,
                failedEarly       = failedEarly,
                nextLevelUnlocked = nextUnlocked,
                newBadges         = newBadges.ToArray()
            };
        }

        /// <summary>
        /// Convenience: read the unlock state for any (subject, levelNumber)
        /// pair. Level 1 is always considered unlocked even before the
        /// PlayerProfile entry exists, so a fresh user can play immediately.
        /// </summary>
        public bool IsLevelUnlocked(SubjectData subject, int levelNumber)
        {
            if (subject == null || levelNumber < 1) return false;
            if (levelNumber == 1) return true;
            var level = FindLevel(subject, levelNumber);
            if (level == null) return false;
            var profile = GameManager.Instance?.Profile;
            if (profile == null) return levelNumber == 1;
            return profile.IsUnlocked(level.levelId);
        }

        /// <summary>Total stars earned across every level of a given subject.</summary>
        public int StarsForSubject(SubjectData subject)
        {
            if (subject == null) return 0;
            int total = 0;
            var profile = GameManager.Instance?.Profile;
            if (profile == null) return 0;
            foreach (var lv in subject.levels)
                if (lv != null) total += profile.GetStars(lv.levelId);
            return total;
        }

        /// <summary>Highest level number reached in a subject (1..N).</summary>
        public int HighestLevelReached(SubjectData subject)
        {
            if (subject == null) return 1;
            var profile = GameManager.Instance?.Profile;
            if (profile == null) return 1;
            int highest = 1;
            foreach (var lv in subject.levels)
            {
                if (lv == null) continue;
                if (profile.IsUnlocked(lv.levelId) && lv.levelNumber > highest)
                    highest = lv.levelNumber;
            }
            return highest;
        }

        // -------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------
        private bool UnlockNext(LevelData current)
        {
            var subject = GameManager.Instance.CurrentSubject;
            if (subject == null) return false;
            int idx = subject.levels.IndexOf(current);
            if (idx < 0 || idx + 1 >= subject.levels.Count) return false;
            var next = subject.levels[idx + 1];
            if (next == null) return false;
            var profile = GameManager.Instance.Profile;
            bool wasLocked = !profile.IsUnlocked(next.levelId);
            profile.Unlock(next.levelId);
            return wasLocked;
        }

        private LevelData FindLevel(SubjectData subject, int levelNumber)
        {
            foreach (var lv in subject.levels)
                if (lv != null && lv.levelNumber == levelNumber) return lv;
            return null;
        }

        private void MaybeAwardMetaBadges(PlayerProfile profile, LevelData level,
            int correct, int total, int score, int stars,
            LearningMode mode, int maxStreak, List<string> newBadges, string subjectKey)
        {
            // "First Step" — first level ever cleared.
            if (stars > 0 && !profile.HasBadge("first_step"))
            {
                profile.AwardBadge("first_step");
                newBadges.Add("first_step");
                OnBadgeAwarded?.Invoke("first_step");
            }

            // "Half Way There" — clear any subject's Level 10.
            if (stars > 0 && level.levelNumber == 10 && !profile.HasBadge("half_way_there"))
            {
                profile.AwardBadge("half_way_there");
                newBadges.Add("half_way_there");
                OnBadgeAwarded?.Invoke("half_way_there");
            }

            // "{Subject} Apprentice" at L5, "{Subject} Master" at L20.
            int grade = GameManager.Instance.Session.selectedGrade;
            string subKey = subjectKey;
            if (stars > 0 && level.levelNumber == 5)
            {
                string id = $"{subKey}_apprentice_g{grade}";
                if (!profile.HasBadge(id))
                {
                    profile.AwardBadge(id);
                    newBadges.Add(id);
                    OnBadgeAwarded?.Invoke(id);
                }
            }
            if (stars > 0 && level.levelNumber == 20)
            {
                string id = $"{subKey}_master_g{grade}";
                if (!profile.HasBadge(id))
                {
                    profile.AwardBadge(id);
                    newBadges.Add(id);
                    OnBadgeAwarded?.Invoke(id);
                }
            }

            // "Perfect Score" — 10/10 in Quiz Mode.
            if (mode == LearningMode.Quiz && total > 0 && correct == total
                && !profile.HasBadge("perfect_score"))
            {
                profile.AwardBadge("perfect_score");
                newBadges.Add("perfect_score");
                OnBadgeAwarded?.Invoke("perfect_score");
            }

            // "Speed Demon" — survive 25 correct in a row in Speed Round.
            if (mode == LearningMode.SpeedRound)
            {
                if (maxStreak > profile.speedRoundBestStreak)
                    profile.speedRoundBestStreak = maxStreak;
                if (profile.speedRoundBestStreak >= 25
                    && !profile.HasBadge("speed_demon"))
                {
                    profile.AwardBadge("speed_demon");
                    newBadges.Add("speed_demon");
                    OnBadgeAwarded?.Invoke("speed_demon");
                }
            }

            // "Early Bird" — completed a level before 8 AM local time.
            if (stars > 0 && DateTime.Now.Hour < 8
                && !profile.HasBadge("early_bird"))
            {
                profile.AwardBadge("early_bird");
                newBadges.Add("early_bird");
                OnBadgeAwarded?.Invoke("early_bird");
            }
        }

        // -------------------------------------------------------------------
        // Pretty names for badges (used by Parental Dashboard + Results)
        // -------------------------------------------------------------------
        public static string PrettyBadgeName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            switch (id)
            {
                case "first_step":     return "🌱 First Step";
                case "half_way_there": return "🛤 Half Way There";
                case "perfect_score":  return "💯 Perfect Score";
                case "speed_demon":    return "⚡ Speed Demon";
                case "early_bird":     return "🌅 Early Bird";
                case "dedicated":      return "📅 Dedicated";
            }
            // Pattern-derived names.
            if (id.EndsWith("_apprentice_g1") || id.EndsWith("_apprentice_g2") || id.EndsWith("_apprentice_g3"))
            {
                string subj = id.Substring(0, id.IndexOf("_apprentice"));
                string g = id.Substring(id.Length - 1);
                return $"🎓 {Capitalize(subj)} Apprentice (G{g})";
            }
            if (id.EndsWith("_master_g1") || id.EndsWith("_master_g2") || id.EndsWith("_master_g3"))
            {
                string subj = id.Substring(0, id.IndexOf("_master"));
                string g = id.Substring(id.Length - 1);
                return $"🏆 {Capitalize(subj)} Master (G{g})";
            }
            // Legacy: master_addition_1 style from earlier builds.
            if (id.StartsWith("master_"))   return $"🏆 {Capitalize(id.Substring(7))} Master";
            if (id.StartsWith("halfway_"))  return $"🛤 {Capitalize(id.Substring(8))} Half-Way";
            return id;
        }

        private static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
