// -----------------------------------------------------------------------------
// LearnModeManager.cs
// -----------------------------------------------------------------------------
// A guided lesson, not a quiz: shows the lesson intro / example / tip, then
// walks the player through a handful of "Try it!" questions with full hints
// shown automatically. Designed as the gentlest on-ramp to a new topic.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Managers;
using MathEdu.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Modes
{
    public class LearnModeManager : MonoBehaviour
    {
        private LevelData _level;

        private RectTransform _content;
        private RectTransform _ctaHolder;
        private TextMeshProUGUI _stepLabel;
        private TextMeshProUGUI _bodyLabel;
        private TextMeshProUGUI _exampleLabel;

        private int _step;

        private void Start()
        {
            _ = GameManager.Instance;
            _level = GameManager.Instance.CurrentLevel;
            if (_level == null) { GameManager.Instance.UI.Go(UIManager.SceneMainMenu); return; }
            Build();
            ShowStep(0);
        }

        private void Build()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[LearnCanvas]");
            UIFactory.CreateGradientBackground(safe, UIFactory.BgTop, UIFactory.BgBottom);

            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.88f), new Vector2(1, 1f),
                UIFactory.Primary, 0, "Header");
            UIFactory.CreateText(header,
                $"Learn - {GameManager.Instance.CurrentSubject?.displayName} L{_level.levelNumber}",
                42, Color.white, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;
            var back = UIFactory.CreateIconButton(header, "<", new Color(0, 0, 0, 0.3f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() => GameManager.Instance.UI.Go(UIManager.SceneModeSelect));

            var stepHolder = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.82f), new Vector2(1, 0.88f),
                new Color(0, 0, 0, 0.25f), 0, "StepHolder");
            _stepLabel = UIFactory.CreateText(stepHolder,
                "", 32, Color.white, TextAlignmentOptions.Center, "Step");

            var card = UIFactory.CreatePanel(safe,
                new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.80f),
                UIFactory.Card, 28, "Content");

            var col = UIFactory.CreateVerticalLayout(card, 24,
                new RectOffset(32, 32, 32, 32), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            _bodyLabel = UIFactory.CreateText((RectTransform)col.transform, "",
                44, UIFactory.TextDark, TextAlignmentOptions.Center, "Body");
            var ble = _bodyLabel.gameObject.AddComponent<LayoutElement>();
            ble.preferredHeight = 260;

            _exampleLabel = UIFactory.CreateText((RectTransform)col.transform, "",
                40, UIFactory.Primary, TextAlignmentOptions.Center, "Example");
            _exampleLabel.fontStyle = FontStyles.Bold;
            _content = (RectTransform)col.transform;

            var ctaPanel = UIFactory.CreatePanel(safe,
                new Vector2(0, 0), new Vector2(1, 0.18f),
                new Color(0, 0, 0, 0.25f), 0, "CTA");
            _ctaHolder = ctaPanel;
            var hl = ctaPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(40, 40, 16, 16);
            hl.spacing = 32;
            hl.childForceExpandWidth = true;
            hl.childAlignment = TextAnchor.MiddleCenter;
        }

        private void ShowStep(int step)
        {
            _step = step;
            ClearCTA();

            int tryCount = Mathf.Min(3, _level.questions?.Count ?? 0);
            int totalSteps = 3 + tryCount;
            _stepLabel.text = $"Step {Mathf.Min(step + 1, totalSteps)} / {totalSteps}";

            if (step == 0)
            {
                _bodyLabel.text    = _level.lessonIntro;
                _exampleLabel.text = "";
                AddCTA("Show example", () => ShowStep(1));
            }
            else if (step == 1)
            {
                _bodyLabel.text    = "Look carefully:";
                _exampleLabel.text = _level.lessonExample;
                AddCTA("Got it!", () => ShowStep(2));
            }
            else if (step == 2)
            {
                _bodyLabel.text    = _level.lessonTip;
                _exampleLabel.text = "Ready to try?";
                AddCTA("Try it!", () => ShowStep(3));
            }
            else
            {
                int qIndex = step - 3;
                if (qIndex >= tryCount || qIndex >= _level.questions.Count)
                {
                    _bodyLabel.text = "You did it! You can now try Practice or Quiz mode.";
                    _exampleLabel.text = "";
                    AddCTA("Back to modes",
                        () => GameManager.Instance.UI.Go(UIManager.SceneModeSelect));
                    AddCTA("Practice now",
                        () =>
                        {
                            GameManager.Instance.SelectMode(LearningMode.Practice);
                            GameManager.Instance.UI.Go(UIManager.ScenePractice);
                        });
                    return;
                }
                ShowTryItQuestion(_level.questions[qIndex]);
            }
        }

        private void ShowTryItQuestion(MathQuestion q)
        {
            _bodyLabel.text    = q.prompt;
            _exampleLabel.text = $"💡 {q.hint}";

            ClearCTA();
            var hl = _ctaHolder.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 16;
            hl.padding = new RectOffset(24, 24, 24, 24);

            for (int i = 0; i < q.options.Length; i++)
            {
                int captured = i;
                var btn = UIFactory.CreateButton(_ctaHolder, q.options[i],
                    UIFactory.Card, 40, $"TryBtn_{i}");
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                label.color = UIFactory.TextDark;
                btn.onClick.AddListener(() => OnTryAnswer(captured, q, btn));
            }
        }

        private void OnTryAnswer(int chosen, MathQuestion q, Button btn)
        {
            if (q.IsCorrect(chosen))
            {
                GameManager.Instance.Audio.PlayCorrect();
                btn.GetComponent<Image>().color = UIFactory.Success;
                Invoke(nameof(NextStep), 0.6f);
            }
            else
            {
                GameManager.Instance.Audio.PlayWrong();
                btn.GetComponent<Image>().color = UIFactory.Danger;
                _exampleLabel.text = $"Not quite! Hint: {q.hint}";
            }
        }

        private void NextStep() => ShowStep(_step + 1);

        private void AddCTA(string label, UnityEngine.Events.UnityAction action)
        {
            var btn = UIFactory.CreateButton(_ctaHolder, label, UIFactory.Primary, 44, $"CTA_{label}");
            btn.onClick.AddListener(action);
        }

        private void ClearCTA()
        {
            if (_ctaHolder == null) return;
            for (int i = _ctaHolder.childCount - 1; i >= 0; i--)
                Destroy(_ctaHolder.GetChild(i).gameObject);
        }
    }
}
