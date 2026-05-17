// -----------------------------------------------------------------------------
// PlayerProfile.cs
// -----------------------------------------------------------------------------
// Plain serializable player save data. Saved to JSON via ProgressManager.
// Keeping this as a [Serializable] class (not a ScriptableObject) means we
// can write multiple profiles to disk without polluting the asset folder.
//
// This file is the source of truth for:
//   • Player identity (name, avatar, selected grade)
//   • Per-level progress (stars, best score, unlocked, times played)
//   • Per-subject roll-up stats (accuracy, time spent, levels completed) — the
//     Parental Dashboard reads these directly without re-computing.
//   • Settings (music / SFX volume, haptics, parental PIN)
//   • Badge / streak tracking (Speed Round best streak, day-streak for the
//     "Dedicated" achievement)
//   • First-launch flag (drives the Bootstrap → PlayerSetup detour).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathEdu.Data
{
    [Serializable]
    public class LevelProgress
    {
        public string levelId;
        public int    stars;       // 0 - 3
        public int    bestScore;
        public int    timesPlayed;
        public bool   unlocked;
    }

    /// <summary>
    /// Per-subject aggregate stats used by the Parental Dashboard. We update
    /// these on every CompleteLevel() so the dashboard never has to walk the
    /// entire level tree to render.
    /// </summary>
    [Serializable]
    public class SubjectStats
    {
        public string subjectKey;        // SubjectData.SubjectKey, e.g. "addition"
        public int    questionsAnswered;
        public int    questionsCorrect;
        public int    levelsCompleted;   // levels played at least once
        public int    starsEarned;       // total stars across this subject
        public float  timeSpentSeconds;  // total play time on this subject
        public string lastPlayedIsoUtc;  // ISO-8601 UTC timestamp
        public int    highestLevelUnlocked = 1; // 1..20 for the subject progress bar

        public float Accuracy =>
            questionsAnswered <= 0 ? 0f : 100f * questionsCorrect / questionsAnswered;
    }

    [Serializable]
    public class PlayerProfile
    {
        // -------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------
        public string playerName    = "Player";
        public string avatarId      = "default";
        public int    selectedGrade = 1;

        /// <summary>
        /// Cleared on a fresh profile so the Bootstrap scene knows to route to
        /// the Player Setup screen the first time the app launches.
        /// </summary>
        public bool   setupComplete = false;

        // -------------------------------------------------------------------
        // Progression
        // -------------------------------------------------------------------
        public int xp        = 0;
        public int totalStars = 0;
        public List<string> badges = new List<string>();
        public List<LevelProgress> levelProgress = new List<LevelProgress>();

        // -------------------------------------------------------------------
        // Per-subject roll-up (Parental Dashboard reads this directly)
        // -------------------------------------------------------------------
        public List<SubjectStats> subjectStats = new List<SubjectStats>();
        public float totalPlaySeconds = 0f;

        // -------------------------------------------------------------------
        // Streak / badge tracking (used by ProgressManager.MaybeAwardMetaBadges)
        // -------------------------------------------------------------------
        /// <summary>Longest consecutive correct streak achieved in Speed Round.</summary>
        public int speedRoundBestStreak = 0;
        /// <summary>YYYY-MM-DD dates the player completed at least one level.</summary>
        public List<string> playDays = new List<string>();
        /// <summary>Last date the daily-streak count was updated.</summary>
        public string lastPlayDate = "";
        /// <summary>Consecutive day streak length.</summary>
        public int    consecutiveDayStreak = 0;

        // -------------------------------------------------------------------
        // Settings
        // -------------------------------------------------------------------
        public bool  musicOn      = true;
        public bool  sfxOn        = true;
        public float musicVolume  = 0.7f;
        public float sfxVolume    = 1.0f;
        public bool  hapticsOn    = true;
        public string language    = "en";

        /// <summary>
        /// Parental gate. Stored as a short string (e.g. "1234"). The default
        /// is "0000" so first-time parents can open the dashboard without
        /// being locked out, then change the PIN inside the dashboard.
        /// </summary>
        public string parentalPin = "0000";

        // -------------------------------------------------------------------
        // Level helpers
        // -------------------------------------------------------------------

        public LevelProgress GetOrCreate(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) levelId = "unknown";
            foreach (var p in levelProgress)
                if (p != null && p.levelId == levelId)
                    return p;

            var fresh = new LevelProgress { levelId = levelId };
            levelProgress.Add(fresh);
            return fresh;
        }

        public bool IsUnlocked(string levelId) => GetOrCreate(levelId).unlocked;

        public int  GetStars(string levelId)   => GetOrCreate(levelId).stars;

        /// <summary>
        /// Record a level result. Returns true if this is a new best.
        /// </summary>
        public bool RecordResult(string levelId, int stars, int score)
        {
            var p = GetOrCreate(levelId);
            p.timesPlayed++;
            int delta = Math.Max(0, stars - p.stars);
            totalStars += delta;
            p.stars     = Math.Max(p.stars, stars);
            bool best   = score > p.bestScore;
            p.bestScore = Math.Max(p.bestScore, score);
            return best;
        }

        public void Unlock(string levelId)
        {
            GetOrCreate(levelId).unlocked = true;
        }

        public void AwardBadge(string badgeId)
        {
            if (string.IsNullOrEmpty(badgeId)) return;
            if (badges == null) badges = new List<string>();
            if (!badges.Contains(badgeId)) badges.Add(badgeId);
        }

        public bool HasBadge(string badgeId) =>
            !string.IsNullOrEmpty(badgeId) && badges != null && badges.Contains(badgeId);

        // -------------------------------------------------------------------
        // Subject stats helpers
        // -------------------------------------------------------------------

        public SubjectStats GetSubjectStats(string subjectKey)
        {
            if (string.IsNullOrEmpty(subjectKey)) subjectKey = "unknown";
            if (subjectStats == null) subjectStats = new List<SubjectStats>();
            foreach (var s in subjectStats)
                if (s != null && s.subjectKey == subjectKey)
                    return s;
            var fresh = new SubjectStats { subjectKey = subjectKey };
            subjectStats.Add(fresh);
            return fresh;
        }

        /// <summary>
        /// Called at the end of a play session (or any time we want partial
        /// metrics) to log answers + time into a subject's rolling totals.
        /// </summary>
        public void RecordSession(string subjectKey, int correct, int wrong,
                                  int starsThisSession, bool levelCompleted,
                                  float seconds)
        {
            var s = GetSubjectStats(subjectKey);
            s.questionsAnswered += correct + wrong;
            s.questionsCorrect  += correct;
            s.starsEarned       += Math.Max(0, starsThisSession);
            if (levelCompleted) s.levelsCompleted++;
            s.timeSpentSeconds  += Math.Max(0, seconds);
            s.lastPlayedIsoUtc   = DateTime.UtcNow.ToString("o");

            totalPlaySeconds += Math.Max(0, seconds);
        }

        /// <summary>
        /// Update the "highest level unlocked" tracker for the given subject.
        /// Used by the Main Menu subject cards to render a progress bar.
        /// </summary>
        public void RecordSubjectHighestUnlocked(string subjectKey, int level)
        {
            if (level < 1) level = 1;
            var s = GetSubjectStats(subjectKey);
            if (level > s.highestLevelUnlocked) s.highestLevelUnlocked = level;
        }

        /// <summary>
        /// Marks today as a play day. Returns true if the streak was extended
        /// (i.e. the previous play day was exactly yesterday).
        /// </summary>
        public bool TouchPlayDay()
        {
            if (playDays == null) playDays = new List<string>();
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            bool extended = false;
            if (lastPlayDate == today)
            {
                // already counted today
                return false;
            }
            if (!string.IsNullOrEmpty(lastPlayDate)
                && DateTime.TryParse(lastPlayDate, out var prev))
            {
                var diff = (DateTime.UtcNow.Date - prev.Date).TotalDays;
                if (diff <= 1.5 && diff >= 0.5)
                {
                    consecutiveDayStreak = Math.Max(1, consecutiveDayStreak) + 1;
                    extended = true;
                }
                else
                {
                    consecutiveDayStreak = 1;
                }
            }
            else
            {
                consecutiveDayStreak = 1;
            }
            lastPlayDate = today;
            if (!playDays.Contains(today)) playDays.Add(today);
            return extended;
        }
    }
}
