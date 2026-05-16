// -----------------------------------------------------------------------------
// PlayerProfile.cs
// -----------------------------------------------------------------------------
// Plain serializable player save data. Saved to JSON via ProgressManager.
// Keeping this as a [Serializable] class (not a ScriptableObject) means we
// can write multiple profiles to disk without polluting the asset folder.
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

    [Serializable]
    public class PlayerProfile
    {
        // -------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------
        public string playerName = "Player";
        public string avatarId   = "default";
        public int    selectedGrade = 1;

        // -------------------------------------------------------------------
        // Progression
        // -------------------------------------------------------------------
        public int xp        = 0;
        public int totalStars = 0;
        public List<string> badges = new List<string>();
        public List<LevelProgress> levelProgress = new List<LevelProgress>();

        // -------------------------------------------------------------------
        // Settings
        // -------------------------------------------------------------------
        public float musicVolume = 0.7f;
        public float sfxVolume   = 1.0f;
        public bool  hapticsOn   = true;

        // -------------------------------------------------------------------
        // Helpers
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
    }
}
