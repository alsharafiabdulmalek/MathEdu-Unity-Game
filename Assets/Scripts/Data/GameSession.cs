// -----------------------------------------------------------------------------
// GameSession.cs
// -----------------------------------------------------------------------------
// Lightweight container that carries the player's current selection between
// scenes (Grade → Subject → Level → Mode). Lives as a property on
// GameManager so it can be reset cleanly.
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

        /// <summary>UTC time when the current play-session began.</summary>
        public DateTime sessionStartedUtc = DateTime.UtcNow;

        public void ResetGameplay()
        {
            currentQuestionIndex = 0;
            correctCount         = 0;
            wrongCount           = 0;
            score                = 0;
            elapsedSeconds       = 0f;
            sessionStartedUtc    = DateTime.UtcNow;
        }
    }
}
