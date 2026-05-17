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
            foreach (var p in levelProgress)
                if (p.levelId == levelId)
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
            if (!badges.Contains(badgeId)) badges.Add(badgeId);
        }

        // -------------------------------------------------------------------
        // Subject stats helpers
        // -------------------------------------------------------------------

        public SubjectStats GetSubjectStats(string subjectKey)
        {
            foreach (var s in subjectStats)
                if (s.subjectKey == subjectKey)
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
    }
}
