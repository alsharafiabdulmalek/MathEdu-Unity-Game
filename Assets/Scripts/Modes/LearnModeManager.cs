// -----------------------------------------------------------------------------
// LearnModeManager.cs
// -----------------------------------------------------------------------------
// Guided lesson, structured per spec 3B:
//   1. Intro card (lessonIntro text + lessonExample).
//   2. Three auto-reveal example questions:
//        • show the question + 4 options
//        • 1.5 s later highlight the correct option in green
//        • show the hint as scaffolding
//        • after 2.5 s more fade and move to the next example
//   3. "Now it's YOUR turn!" transition for 1.5 s.
//   4. 7 practice questions (untimed) with the hint visible at all times.
//
// No scoring; finishing routes back to Mode Select. Designed as the gentlest
// on-ramp to a new topic.
//
// Polish: a MascotHost lives in the bottom-left corner and talks the player
// through the lesson, reacting happy/sad as they get answers right or wrong.
// EmojiBurst fires on correct answers and at the wrap-up.
// -----------------------------------------------------------------------------

using System.Collections;
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
        private RectTransform _safeArea;
        private RectTransform _stage;
        private TextMeshProUGUI _stageLabel;
        private TextMeshProUGUI _bodyLabel;
        private TextMeshProUGUI _hintLabel;
        private RectTransform _answersHolder;
        private CanvasGroup _cardGroup;
        private MascotHost _host;

        private const int ExampleCount = 3;
        private const int PracticeCount = 7;

        private int _practiceIndex;
        private bool _locked;

        private void Start()
        {
            _ = GameManager.Instance;
            _level = GameManager.Instance.CurrentLevel;
            if (_level == null) { GameManager.Instance.UI.Go(UIManager.SceneMainMenu); return; }
            BuildUI();
            StartCoroutine(IntroFlow());
        }

        private void BuildUI()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[LearnCanvas]");
            _safeArea = safe;
            UIFactory.CreateGradientBackground(safe, UIFactory.BgTop, UIFactory.BgBottom);

            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.88f), new Vector2(1, 1f),
                UIFactory.Primary, 0, "Header");
            UIFactory.CreateText(header,
                $"Learn - {GameManager.Instance.CurrentSubject?.displayName} L{_level.levelNumber}",
                42, Color.white, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;
            var back = IconService.IconButton(header, "back", "<", new Color(0, 0, 0, 0.3f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(UIManager.SceneModeSelect);
            });

            var stageHolder = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.82f), new Vector2(1, 0.88f),
                new Color(0, 0, 0, 0.25f), 0, "StageHolder");
            _stageLabel = UIFactory.CreateText(stageHolder,
                "", 32, Color.white, TextAlignmentOptions.Center, "Stage");

            var card = UIFactory.CreatePanel(safe,
                new Vector2(0.05f, 0.42f), new Vector2(0.95f, 0.80f),
                UIFactory.Card, 28, "Content");
            _cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            _stage = card;

            var col = UIFactory.CreateVerticalLayout(card, 16,
                new RectOffset(28, 28, 28, 28), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            _bodyLabel = UIFactory.CreateText((RectTransform)col.transform, "",
                42, UIFactory.TextDark, TextAlignmentOptions.Center, "Body");
            var ble = _bodyLabel.gameObject.AddComponent<LayoutElement>();
            ble.preferredHeight = 220;

            _hintLabel = UIFactory.CreateText((RectTransform)col.transform, "",
                30, UIFactory.Primary, TextAlignmentOptions.Center, "Hint");
            _hintLabel.fontStyle = FontStyles.Italic;
            var hle = _hintLabel.gameObject.AddComponent<LayoutElement>();
            hle.preferredHeight = 80;

            // Answer button grid lives in its own panel beneath the card.
            var answersPanel = UIFactory.CreatePanel(safe,
                new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.40f),
                new Color(0, 0, 0, 0.15f), 24, "AnswersPanel");
            var grid = answersPanel.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(440, 140);
            grid.spacing  = new Vector2(20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.padding = new RectOffset(24, 24, 24, 24);
            grid.childAlignment = TextAnchor.MiddleCenter;
            _answersHolder = answersPanel;

            // Friendly host mascot lives in the bottom-left corner of the
            // screen and "talks" the player through the lesson.
            _host = MascotHost.Spawn(safe,
                anchorMin: new Vector2(0.00f, 0.40f),
                anchorMax: new Vector2(0.32f, 0.78f),
                bodyTint: UIFactory.Accent);
        }

        // -------------------------------------------------------------------
        // Flow
        // -------------------------------------------------------------------
        private IEnumerator IntroFlow()
        {
            _stageLabel.text = "Lesson";
            _bodyLabel.text  = _level.lessonIntro;
            _hintLabel.text  = _level.lessonExample;
            ClearAnswers();

            _host?.Speak("Welcome! Let's learn together.", 2.5f);
            _host?.React(MascotHost.Mood.Happy);

            yield return new WaitForSeconds(2.5f);

            // 3 auto-reveal examples
            int taken = 0;
            for (int i = 0; i < _level.questions.Count && taken < ExampleCount; i++)
            {
                var q = _level.questions[i];
                if (q == null || !q.IsValid()) continue;
                yield return PlayExample(taken + 1, q);
                taken++;
            }

            // Transition
            _stageLabel.text = "Practice";
            _bodyLabel.text  = "Now it's YOUR turn! 💪";
            _hintLabel.text  = _level.lessonTip;
            ClearAnswers();
            _host?.React(MascotHost.Mood.Cheer);
            _host?.Speak("You've got this!", 1.6f);
            yield return new WaitForSeconds(1.5f);

            // 7 practice questions
            int practiced = 0;
            for (int i = ExampleCount; i < _level.questions.Count && practiced < PracticeCount; i++)
            {
                var q = _level.questions[i];
                if (q == null || !q.IsValid()) continue;
                yield return PlayPractice(q, practiced + 1);
                practiced++;
            }

            // Wrap-up
            _stageLabel.text = "Done!";
            _bodyLabel.text  = "Great job — you finished the lesson!";
            _hintLabel.text  = "Try Practice or Quiz mode next.";
            ClearAnswers();
            _host?.React(MascotHost.Mood.Cheer);
            _host?.Speak("Brilliant work!", 3.0f);
            EmojiBurst.Cheer(_safeArea, new Vector2(_safeArea.rect.width * 0.5f,
                                                    _safeArea.rect.height * 0.5f));
            AddCTA("Back to modes",
                () => GameManager.Instance.UI.Go(UIManager.SceneModeSelect));
            AddCTA("Practice now", () =>
            {
                GameManager.Instance.SelectMode(LearningMode.Practice);
                GameManager.Instance.UI.Go(UIManager.ScenePractice);
            });
        }

        private IEnumerator PlayExample(int idx, MathQuestion q)
        {
            _stageLabel.text = $"Example {idx} / {ExampleCount}";
            _bodyLabel.text  = q.prompt;
            _hintLabel.text  = "";
            ClearAnswers();

            // Spawn read-only answer buttons
            var btns = new Button[q.options.Length];
            for (int i = 0; i < q.options.Length; i++)
            {
                btns[i] = UIFactory.CreateButton(_answersHolder, q.options[i],
                    UIFactory.Card, 40, $"Ex_{idx}_{i}");
                var lbl = btns[i].GetComponentInChildren<TextMeshProUGUI>();
                lbl.color = UIFactory.TextDark;
                btns[i].interactable = false;
            }

            yield return FadeCard(0f, 1f, 0.25f);

            // 1.5s pause — let the eye scan options.
            yield return new WaitForSeconds(1.5f);

            // Highlight correct answer.
            if (q.correctIndex >= 0 && q.correctIndex < btns.Length)
            {
                var img = btns[q.correctIndex].GetComponent<Image>();
                img.color = UIFactory.Success;
                var lbl = btns[q.correctIndex].GetComponentInChildren<TextMeshProUGUI>();
                lbl.color = Color.white;
                GameManager.Instance.Audio.PlaySFX("correct");
            }
            _hintLabel.text = $"💡 {q.hint}";

            // 2.5s more, then fade out.
            yield return new WaitForSeconds(2.5f);
            yield return FadeCard(1f, 0f, 0.25f);
        }

        private IEnumerator PlayPractice(MathQuestion q, int idx)
        {
            _stageLabel.text = $"Practice {idx} / {PracticeCount}";
            _bodyLabel.text  = q.prompt;
            _hintLabel.text  = $"💡 {q.hint}";
            ClearAnswers();
            _locked = false;

            var shuffled = GameplayBase_ShuffleOptions(q);
            bool answered = false;
            int chosenIndex = -1;

            for (int i = 0; i < shuffled.options.Length; i++)
            {
                int captured = i;
                var btn = UIFactory.CreateButton(_answersHolder, shuffled.options[i],
                    UIFactory.Card, 40, $"Pra_{idx}_{i}");
                var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
                lbl.color = UIFactory.TextDark;
                btn.onClick.AddListener(() =>
                {
                    if (_locked) return;
                    _locked = true;
                    chosenIndex = captured;
                    answered = true;
                    bool ok = shuffled.IsCorrect(captured);
                    var img = btn.GetComponent<Image>();
                    if (ok)
                    {
                        img.color = UIFactory.Success;
                        GameManager.Instance.Audio.PlaySFX("correct");
                        HapticManager.Light();
                        _host?.React(MascotHost.Mood.Happy);
                        EmojiBurst.Correct(_safeArea,
                            new Vector2(_safeArea.rect.width * 0.5f,
                                        _safeArea.rect.height * 0.55f));
                    }
                    else
                    {
                        img.color = UIFactory.Danger;
                        GameManager.Instance.Audio.PlaySFX("wrong");
                        _host?.React(MascotHost.Mood.Sad);
                        _host?.Speak("Try again — you can do it!", 1.6f);
                    }
                });
            }
            yield return FadeCard(0f, 1f, 0.20f);

            // Wait for an answer (no timeout).
            while (!answered) yield return null;

            // Short reveal pause to let the player see whether they were right.
            yield return new WaitForSeconds(0.8f);
            yield return FadeCard(1f, 0f, 0.25f);
        }

        private IEnumerator FadeCard(float from, float to, float dur)
        {
            if (_cardGroup == null) yield break;
            float t = 0;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                _cardGroup.alpha = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            _cardGroup.alpha = to;
        }

        private void ClearAnswers()
        {
            if (_answersHolder == null) return;
            for (int i = _answersHolder.childCount - 1; i >= 0; i--)
                Destroy(_answersHolder.GetChild(i).gameObject);
        }

        private void AddCTA(string label, UnityEngine.Events.UnityAction action)
        {
            var btn = UIFactory.CreateButton(_answersHolder, label,
                UIFactory.Primary, 44, $"CTA_{label}");
            btn.onClick.AddListener(action);
        }

        // Slim copy of GameplayManagerBase.ShuffleOptions so LearnMode can
        // reshuffle without inheriting the gameplay loop.
        private static MathQuestion GameplayBase_ShuffleOptions(MathQuestion q)
        {
            var indices = new int[] { 0, 1, 2, 3 };
            for (int i = 3; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            var newOpts = new string[4];
            int newCorrect = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                int src = indices[slot];
                newOpts[slot] = q.options[src];
                if (src == q.correctIndex) newCorrect = slot;
            }
            return new MathQuestion
            {
                prompt        = q.prompt,
                options       = newOpts,
                correctIndex  = newCorrect,
                hint          = q.hint,
                explanation   = q.explanation,
                difficulty    = q.difficulty,
                visual        = q.visual,
                visualPayload = q.visualPayload
            };
        }
    }
}
