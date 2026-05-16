// -----------------------------------------------------------------------------
// ModeSelectManager.cs
// -----------------------------------------------------------------------------
// Lets the player pick one of the five learning modes for the currently
// selected level: Learn, Practice, Quiz, Story, Speed Round.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Managers;
using MathEdu.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Modes
{
    public class ModeSelectManager : MonoBehaviour
    {
        private void Start()
        {
            _ = GameManager.Instance;
            Build();
        }

        private void Build()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[ModeSelectCanvas]");
            UIFactory.CreateGradientBackground(safe, UIFactory.BgTop, UIFactory.BgBottom);

            var subject = GameManager.Instance.CurrentSubject;
            var level   = GameManager.Instance.CurrentLevel;
            var grade   = GameManager.Instance.CurrentGrade;

            var header = UIFactory.CreatePanel(safe, new Vector2(0, 0.88f), new Vector2(1, 1f),
                subject != null ? subject.themeColor : UIFactory.Primary, 0, "Header");

            UIFactory.CreateText(header,
                $"Grade {grade?.gradeNumber} • {subject?.displayName} • Level {level?.levelNumber}",
                40, Color.white, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            var back = UIFactory.CreateIconButton(header, "<", new Color(0,0,0,0.35f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() => GameManager.Instance.UI.Go(UIManager.SceneLevelSelect));

            var listHolder = UIFactory.CreatePanel(safe,
                new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.86f),
                new Color(0, 0, 0, 0.15f), 24, "Modes");

            var v = listHolder.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(28, 28, 28, 28);
            v.spacing = 24;
            v.childForceExpandWidth = true;
            v.childAlignment = TextAnchor.MiddleCenter;

            AddMode(listHolder, "Learn",      "Step-by-step lesson with an example.",   LearningMode.Learn,      UIFactory.Primary);
            AddMode(listHolder, "Practice",   "Untimed multiple choice. Take your time.",LearningMode.Practice,   UIFactory.Success);
            AddMode(listHolder, "Quiz",       "Timed challenge. Score = correct + speed.",LearningMode.Quiz,      UIFactory.Accent);
            AddMode(listHolder, "Story",      "Math adventure with characters.",         LearningMode.Story,      new Color(0.55f, 0.40f, 0.90f));
            AddMode(listHolder, "Speed Round","Fast-fire questions. How many in a row?", LearningMode.SpeedRound, UIFactory.Danger);
        }

        private void AddMode(RectTransform parent, string title, string subtitle,
                             LearningMode mode, Color color)
        {
            var card = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                color, 24, $"Mode_{mode}");
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 140; le.preferredHeight = 140;

            var col = UIFactory.CreateVerticalLayout(card, 4,
                new RectOffset(28, 28, 16, 16), "ModeCol");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, title, 56,
                Color.white, TextAlignmentOptions.Left, "Title").fontStyle = FontStyles.Bold;
            UIFactory.CreateText((RectTransform)col.transform, subtitle, 30,
                new Color(1, 1, 1, 0.9f), TextAlignmentOptions.Left, "Sub");

            var btn = card.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.9f);
            btn.colors = colors;
            btn.onClick.AddListener(() => Begin(mode));
        }

        private void Begin(LearningMode m)
        {
            GameManager.Instance.Audio.PlayTap();
            GameManager.Instance.SelectMode(m);
            switch (m)
            {
                case LearningMode.Learn:      GameManager.Instance.UI.Go(UIManager.SceneLearn);    break;
                case LearningMode.Practice:   GameManager.Instance.UI.Go(UIManager.ScenePractice); break;
                case LearningMode.Quiz:       GameManager.Instance.UI.Go(UIManager.SceneQuiz);     break;
                case LearningMode.Story:      GameManager.Instance.UI.Go(UIManager.SceneStory);    break;
                case LearningMode.SpeedRound: GameManager.Instance.UI.Go(UIManager.SceneSpeed);    break;
            }
        }
    }
}
