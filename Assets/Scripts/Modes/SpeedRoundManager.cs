// -----------------------------------------------------------------------------
// SpeedRoundManager.cs
// -----------------------------------------------------------------------------
// Rapid-fire mode. Each question gets a very short window
// (level.speedSecondsPerQuestion). One wrong answer ends the run, so the
// goal is "how many in a row?" rather than total correct.
//
// Speed Round explicitly disables the pause button — that's the challenge.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Gameplay;
using MathEdu.Managers;
using MathEdu.UI;
using UnityEngine;

namespace MathEdu.Modes
{
    public class SpeedRoundManager : GameplayManagerBase
    {
        protected override string HeaderTitle      => "Speed Round";
        protected override Color  HeaderColor      => UIFactory.Danger;
        protected override bool   ShowHint         => false;
        protected override bool   StopOnFirstWrong => true;
        protected override float  QuestionDelay    => 0.4f;
        protected override bool   AllowPause       => false;

        // Speed Round needs more questions than a level provides because
        // we keep going until the player gets one wrong. We pre-build a
        // large pool by recycling the level's questions.
        protected override int TargetQuestionCount => 50;

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
                _timer.Begin(_level.speedSecondsPerQuestion);
            }
        }

        protected override void HandleAnswer(int chosenIndex, MathQuestion q)
        {
            base.HandleAnswer(chosenIndex, q);
            if (_timer != null) _timer.Pause();
        }

        protected override int ScoreForCorrect(MathQuestion q)
        {
            return 15 + ((int)q.difficulty * 5);
        }

        private void OnTimerExpired()
        {
            if (_locked || _finished) return;
            GameManager.Instance.Audio.PlaySFX("timerExpire");
            _locked = true;
            _wrong++;
            _currentStreak = 0;
            GameManager.Instance.Session.failedEarly = true;
            _feedback.ShowWrong("Too slow!");
            GameManager.Instance.VFX?.PlayWrong();
            StartCoroutine(FinishDelayed());
        }
    }
}
