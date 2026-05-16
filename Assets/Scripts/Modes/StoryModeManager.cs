// -----------------------------------------------------------------------------
// StoryModeManager.cs
// -----------------------------------------------------------------------------
// Wraps each question inside a short narrative beat. Reuses the gameplay base
// to handle progress + scoring; the only difference is the top "story panel"
// that paints character dialogue before each question.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Gameplay;
using MathEdu.UI;
using TMPro;
using UnityEngine;

namespace MathEdu.Modes
{
    public class StoryModeManager : GameplayManagerBase
    {
        protected override string HeaderTitle => "Story";
        protected override Color  HeaderColor => new Color(0.55f, 0.40f, 0.90f);
        protected override bool   ShowHint    => true;
        protected override bool   ShuffleQuestions => false;

        private TextMeshProUGUI _storyLabel;

        protected override void BuildUI()
        {
            base.BuildUI();

            var banner = UIFactory.CreatePanel(_safeArea,
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.86f),
                new Color(0.55f, 0.40f, 0.90f, 0.85f), 20, "StoryBanner");
            _storyLabel = UIFactory.CreateText(banner,
                _level.storyIntro,
                32, Color.white, TextAlignmentOptions.Center, "StoryLabel");
            _storyLabel.fontStyle = FontStyles.Italic;
        }

        protected override void OnCorrect(MathQuestion q)
        {
            if (_storyLabel != null)
                _storyLabel.text = "✨ The story moves forward…";
        }

        protected override void Finish()
        {
            if (_storyLabel != null) _storyLabel.text = _level.storyOutro;
            base.Finish();
        }
    }
}
