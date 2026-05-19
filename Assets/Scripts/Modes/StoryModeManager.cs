// -----------------------------------------------------------------------------
// StoryModeManager.cs
// -----------------------------------------------------------------------------
// Wraps each question inside a short narrative beat. Reuses the gameplay base
// to handle progress + scoring; the only difference is the top "story panel"
// that paints character dialogue before each question and a celebratory
// outro on Finish().
//
// Story templates live on LevelData.storyIntro / storyOutro and are populated
// by DatabaseBootstrapper / DatabaseBuilderMenu using subject-specific
// templates so even procedurally generated levels feel themed.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Gameplay;
using MathEdu.UI;
using MathEdu.Utility;
using TMPro;
using UnityEngine;

namespace MathEdu.Modes
{
    public class StoryModeManager : GameplayManagerBase
    {
        protected override string HeaderTitle => Localization.T("modesel.story");
        protected override Color  HeaderColor => new Color(0.55f, 0.40f, 0.90f);
        protected override bool   ShowHint    => true;
        protected override bool   ShuffleQuestions => false;

        private TextMeshProUGUI _storyLabel;
        private string _outroText;

        protected override void BuildUI()
        {
            base.BuildUI();

            var banner = UIFactory.CreatePanel(_safeArea,
                new Vector2(0.04f, 0.80f), new Vector2(0.96f, 0.86f),
                new Color(0.55f, 0.40f, 0.90f, 0.85f), 20, "StoryBanner");
            _storyLabel = UIFactory.CreateText(banner,
                string.IsNullOrEmpty(_level.storyIntro)
                    ? Localization.T("story.intro_default")
                    : _level.storyIntro,
                30, Color.white, TextAlignmentOptions.Center, "StoryLabel");
            _storyLabel.fontStyle = FontStyles.Italic;

            _outroText = string.IsNullOrEmpty(_level.storyOutro)
                ? Localization.T("story.outro_default")
                : _level.storyOutro;
        }

        protected override void OnCorrect(MathQuestion q)
        {
            if (_storyLabel != null)
                Localization.SetText(_storyLabel, Localization.T("story.moves_forward"));
        }

        protected override void Finish()
        {
            if (_storyLabel != null) Localization.SetText(_storyLabel, _outroText);
            base.Finish();
        }
    }
}
