// -----------------------------------------------------------------------------
// QuizModeManager.cs
// -----------------------------------------------------------------------------
// Timed challenge. Each question must be answered within
// level.quizSecondsPerQuestion or it counts as wrong. Score gains a small
// time bonus for fast answers.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Gameplay;
using MathEdu.Managers;
using MathEdu.UI;
using UnityEngine;

namespace MathEdu.Modes
{
    public class QuizModeManager : GameplayManagerBase
    {
        protected override string HeaderTitle => "Quiz";
        protected override Color  HeaderColor => UIFactory.Accent;
        protected override bool   ShowHint    => false;

        private Timer _timer;

        protected override void BuildHeaderExtras(RectTransform header)
        {
            _timer = Timer.Spawn(header);
            var rt = (RectTransform)_timer.transform;
            rt.anchorMin = new Vector2(0.55f, 0);
            rt.anchorMax = new Vector2(0.85f, 1);
            rt.offsetMin = new Vector2(0, 8); rt.offsetMax = new Vector2(0, -8);
            _timer.OnExpired += OnTimerExpired;
        }

        protected override void ShowQuestion(int index)
        {
            base.ShowQuestion(index);
            if (_timer != null && _currentIndex < _questions.Count)
            {
                _timer.Begin(_level.quizSecondsPerQuestion);
            }
        }

        protected override void HandleAnswer(int chosenIndex, MathQuestion q)
        {
            base.HandleAnswer(chosenIndex, q);
            if (_timer != null) _timer.Pause();
        }

        protected override int ScoreForCorrect(MathQuestion q)
        {
            int baseScore = base.ScoreForCorrect(q);
            float timeFrac = _timer != null && _level.quizSecondsPerQuestion > 0
                ? _timer.Remaining / _level.quizSecondsPerQuestion
                : 0;
            int bonus = Mathf.RoundToInt(10 * timeFrac);
            return baseScore + bonus;
        }

        /// <summary>
        /// Timer ran out: forward to HandleAnswer with an invalid index so the
        /// run is counted as wrong without flashing a specific button.
        /// </summary>
        private void OnTimerExpired()
        {
            if (_locked || _finished) return;
            GameManager.Instance.Audio.PlaySFX("timerExpire");
            // Use base.HandleAnswer logic via a fake "no answer" path: we mark
            // the run as wrong inline so the "answer was X" feedback still
            // reveals the correct option.
            _locked = true;
            _wrong++;
            _currentStreak = 0;
            var q = _questions[_currentIndex];
            var buttons = _answersHolder.GetComponentsInChildren<AnswerButton>();
            if (q.correctIndex >= 0 && q.correctIndex < buttons.Length)
                buttons[q.correctIndex].FlashCorrect();
            _feedback.ShowWrong("Time's up!");
            GameManager.Instance.VFX?.PlayWrong();
            StartCoroutine(AdvanceAfterDelay());
        }
    }
}
