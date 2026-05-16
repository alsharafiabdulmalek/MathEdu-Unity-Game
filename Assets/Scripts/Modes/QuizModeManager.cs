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
            rt.anchorMin = new Vector2(0.7f, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 8); rt.offsetMax = new Vector2(-24, -8);
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

        private void OnTimerExpired()
        {
            if (_locked) return;
            _locked = true;
            _wrong++;
            GameManager.Instance.Audio.PlayWrong();
            _feedback.ShowWrong("Time's up!");
            StartCoroutine(AdvanceAfterDelay());
        }
    }
}
