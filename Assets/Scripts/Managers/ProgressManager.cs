// -----------------------------------------------------------------------------
// ProgressManager.cs (with localized PrettyBadgeName)
// -----------------------------------------------------------------------------
// Same business logic as before. The only externally-visible change is that
// PrettyBadgeName() now routes through Localization.T() so Arabic users see
// 'first_step' as 'al-khatwah al-uwla' instead of '🌱 First Step'.
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
            if (profile == null) return fallback;

            int wrong = Math.Max(0, total - correct);
            int stars = level.ComputeStars(correct, total);
            bool firstCompletion = profile.GetOrCreate(level.levelId).timesPlayed == 0;
            profile.RecordResult(level.levelId, stars, score);

            int xp = level.xpReward * Mathf.Max(1, stars);
            profile.xp += xp;
            OnXPGained?.Invoke(profile.xp);

            var newBadges = new List<string>();

            if (stars == 3 && !string.IsNullOrEmpty(level.badgeId)
                && !profile.HasBadge(level.badgeId))
            {
                profile.AwardBadge(level.badgeId);
                newBadges.Add(level.badgeId);
                OnBadgeAwarded?.Invoke(level.badgeId);
            }

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

            MaybeAwardMetaBadges(profile, level, correct, total, score, stars, mode, maxStreak, newBadges, subjectKey);

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
            if (stars > 0 && !profile.HasBadge("first_step"))
            {
                profile.AwardBadge("first_step");
                newBadges.Add("first_step");
                OnBadgeAwarded?.Invoke("first_step");
            }
            if (stars > 0 && level.levelNumber == 10 && !profile.HasBadge("half_way_there"))
            {
                profile.AwardBadge("half_way_there");
                newBadges.Add("half_way_there");
                OnBadgeAwarded?.Invoke("half_way_there");
            }
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
            if (mode == LearningMode.Quiz && total > 0 && correct == total
                && !profile.HasBadge("perfect_score"))
            {
                profile.AwardBadge("perfect_score");
                newBadges.Add("perfect_score");
                OnBadgeAwarded?.Invoke("perfect_score");
            }
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
            if (stars > 0 && DateTime.Now.Hour < 8
                && !profile.HasBadge("early_bird"))
            {
                profile.AwardBadge("early_bird");
                newBadges.Add("early_bird");
                OnBadgeAwarded?.Invoke("early_bird");
            }
        }

        // -------------------------------------------------------------------
        // Pretty names for badges - now flows through Localization
        // -------------------------------------------------------------------
        public static string PrettyBadgeName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            switch (id)
            {
                case "first_step":     return Localization.T("badge.first_step");
                case "half_way_there": return Localization.T("badge.half_way");
                case "perfect_score":  return Localization.T("badge.perfect_score");
                case "speed_demon":    return Localization.T("badge.speed_demon");
                case "early_bird":     return Localization.T("badge.early_bird");
                case "dedicated":      return Localization.T("badge.dedicated");
            }
            // Pattern-derived names (subject-grade specific). We pass the
            // *localized* subject name to the apprentice/master format string
            // so Arabic users see 'mutadrib aljame' instead of 'addition Apprentice'.
            if (id.EndsWith("_apprentice_g1") || id.EndsWith("_apprentice_g2") || id.EndsWith("_apprentice_g3"))
            {
                string subj = id.Substring(0, id.IndexOf("_apprentice"));
                string g = id.Substring(id.Length - 1);
                return Localization.T("badge.apprentice_fmt", LocalizedSubjectFromKey(subj), g);
            }
            if (id.EndsWith("_master_g1") || id.EndsWith("_master_g2") || id.EndsWith("_master_g3"))
            {
                string subj = id.Substring(0, id.IndexOf("_master"));
                string g = id.Substring(id.Length - 1);
                return Localization.T("badge.master_fmt", LocalizedSubjectFromKey(subj), g);
            }
            // Legacy fallbacks for older save data.
            if (id.StartsWith("master_"))   return Localization.T("badge.master_fmt", LocalizedSubjectFromKey(id.Substring(7)), "?");
            if (id.StartsWith("halfway_"))  return Localization.T("badge.half_way");
            return id;
        }

        /// <summary>
        /// Map a lowercase subject key back to the localized display name.
        /// </summary>
        private static string LocalizedSubjectFromKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "?";
            return key switch
            {
                "counting"       => Localization.T("subj.counting"),
                "addition"       => Localization.T("subj.addition"),
                "subtraction"    => Localization.T("subj.subtraction"),
                "multiplication" => Localization.T("subj.multiplication"),
                "division"       => Localization.T("subj.division"),
                "shapes"         => Localization.T("subj.shapes"),
                "patterns"       => Localization.T("subj.patterns"),
                "fractions"      => Localization.T("subj.fractions"),
                "measurement"    => Localization.T("subj.measurement"),
                "time"           => Localization.T("subj.time"),
                "money"          => Localization.T("subj.money"),
                _                => char.ToUpper(key[0]) + key.Substring(1)
            };
        }
    }
}
