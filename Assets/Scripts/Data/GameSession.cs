// -----------------------------------------------------------------------------
// GameSession.cs
// -----------------------------------------------------------------------------
// Lightweight container that carries the player's current selection between
// scenes (Grade → Subject → Level → Mode). Lives as a property on
// GameManager so it can be reset cleanly.
//
// We also keep a serializable `lastResult` snapshot of the most recently
// completed level so the Results scene can render it safely even if the
// player navigates back, refreshes, or returns from background.
// -----------------------------------------------------------------------------

using System;

namespace MathEdu.Data
{
    public enum LearningMode
    {
        Learn,
        Practice,
        Quiz,
        Story,
        SpeedRound
    }

    [Serializable]
    public class SessionResult
    {
        public string       levelId;
        public int          gradeNumber;
        public MathSubject  subject;
        public int          levelNumber;
        public LearningMode mode;

        public int correct;
        public int wrong;
        public int total;
        public int score;
        public int stars;
        public int xpEarned;
        public bool nextLevelUnlocked;
        /// <summary>Longest correct streak (used by Speed Round summary).</summary>
        public int streak;
        /// <summary>True if the run was ended early by a wrong answer (Speed Round).</summary>
        public bool failedEarly;
        /// <summary>Badge ids unlocked by this session — pretty-printed on Results.</summary>
        public string[] newBadges = Array.Empty<string>();
    }

    [Serializable]
    public class GameSession
    {
        public int          selectedGrade  = 1;
        public MathSubject  selectedSubject = MathSubject.Addition;
        public int          selectedLevel  = 1;
        public LearningMode selectedMode   = LearningMode.Practice;

        // Live, transient gameplay state -----------------------------------
        public int   currentQuestionIndex;
        public int   correctCount;
        public int   wrongCount;
        public int   score;
        public float elapsedSeconds;
        public int   maxStreak;     // longest correct-in-a-row this session
        public bool  failedEarly;   // set by Speed Round when a wrong answer ends the run

        /// <summary>UTC time when the current play-session began.</summary>
        public DateTime sessionStartedUtc = DateTime.UtcNow;

        /// <summary>
        /// Snapshot populated by GameplayManagerBase.Finish() just before
        /// transitioning to the Results scene. Results reads this exclusively
        /// so it never depends on live counters that might have been mutated
        /// by a subsequent scene reload.
        /// </summary>
        public SessionResult lastResult;

        public void ResetGameplay()
        {
            currentQuestionIndex = 0;
            correctCount         = 0;
            wrongCount           = 0;
            score                = 0;
            elapsedSeconds       = 0f;
            maxStreak            = 0;
            failedEarly          = false;
            sessionStartedUtc    = DateTime.UtcNow;
        }
    }
}
