// -----------------------------------------------------------------------------
// GameplayManagerBase.cs
// -----------------------------------------------------------------------------
// Shared scaffolding for every play mode that walks a list of multiple-choice
// questions: building the question HUD, hooking up the answer buttons,
// counting correct/wrong, computing the final star rating, and routing to
// the Results screen.
//
// Subclasses provide:
//   - The header colour and title.
//   - Whether the questions are timed (override BuildHeaderExtras to add a Timer).
//   - Hook points for OnCorrect / OnWrong / OnFinished if they need custom logic
//     (e.g. Speed Round ending the run on first wrong answer).
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using MathEdu.Data;
using MathEdu.Managers;
using MathEdu.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Gameplay
{
    public abstract class GameplayManagerBase : MonoBehaviour
    {
        // ----- subclass-tunable hooks --------------------------------------
        protected abstract string  HeaderTitle { get; }
        protected abstract Color   HeaderColor { get; }
        protected virtual  bool    ShuffleQuestions => true;
        protected virtual  bool    StopOnFirstWrong => false;
        protected virtual  float   QuestionDelay     => 0.7f;
        protected virtual  bool    ShowHint          => true;

        // ----- runtime references ------------------------------------------
        protected RectTransform _safeArea;
        protected TextMeshProUGUI _questionLabel;
        protected TextMeshProUGUI _progressLabel;
        protected TextMeshProUGUI _scoreLabel;
        protected ProgressBar _progressBar;
        protected QuestionVisualRenderer _visual;
        protected AnimatedFeedback _feedback;
        protected RectTransform _answersHolder;
        protected Button _hintButton;
        protected TextMeshProUGUI _hintLabel;

        protected List<MathQuestion> _questions;
        protected LevelData _level;
        protected int _currentIndex;
        protected int _correct;
        protected int _wrong;
        protected int _score;
        protected bool _locked;

        protected virtual void Start()
        {
            _ = GameManager.Instance;
            _level = GameManager.Instance.CurrentLevel;
            if (_level == null)
            {
                Debug.LogWarning("[Gameplay] No level selected; returning to main menu.");
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
                return;
            }
            _questions = new List<MathQuestion>(_level.questions);
            if (ShuffleQuestions) Shuffle(_questions);

            BuildUI();
            ShowQuestion(0);
        }

        protected virtual void BuildUI()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[GameplayCanvas]");
            _safeArea = safe;
            UIFactory.CreateGradientBackground(safe, UIFactory.BgTop, UIFactory.BgBottom);

            // Header
            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.88f), new Vector2(1, 1f),
                HeaderColor, 0, "Header");

            UIFactory.CreateText(header,
                $"{HeaderTitle} - {GameManager.Instance.CurrentSubject?.displayName} L{_level.levelNumber}",
                40, Color.white, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            var back = UIFactory.CreateIconButton(header, "<",
                new Color(0, 0, 0, 0.3f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() => GameManager.Instance.UI.Go(UIManager.SceneModeSelect));

            BuildHeaderExtras(header);

            // Progress strip
            var strip = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.81f), new Vector2(1, 0.88f),
                new Color(0, 0, 0, 0.25f), 0, "Strip");
            var stripLayout = UIFactory.CreateHorizontalLayout(strip, 24,
                new RectOffset(32, 32, 12, 12), "StripLayout");
            var slr = (RectTransform)stripLayout.transform;
            slr.anchorMin = Vector2.zero; slr.anchorMax = Vector2.one;
            slr.offsetMin = Vector2.zero; slr.offsetMax = Vector2.zero;

            _progressLabel = UIFactory.CreateText((RectTransform)stripLayout.transform,
                "1 / 10", 36, Color.white, TextAlignmentOptions.Left, "ProgressLabel");
            _progressBar = ProgressBar.Spawn((RectTransform)stripLayout.transform, 24);
            var le = _progressBar.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            _scoreLabel = UIFactory.CreateText((RectTransform)stripLayout.transform,
                "Score: 0", 36, Color.white, TextAlignmentOptions.Right, "ScoreLabel");

            // Question card
            var card = UIFactory.CreatePanel(safe,
                new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.80f),
                UIFactory.Card, 28, "QuestionCard");

            var qcol = UIFactory.CreateVerticalLayout(card, 12,
                new RectOffset(24, 24, 24, 24), "QCol");
            var qrt = (RectTransform)qcol.transform;
            qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
            qrt.offsetMin = Vector2.zero; qrt.offsetMax = Vector2.zero;

            _questionLabel = UIFactory.CreateText((RectTransform)qcol.transform, "",
                60, UIFactory.TextDark, TextAlignmentOptions.Center, "QuestionLabel");
            _questionLabel.fontStyle = FontStyles.Bold;
            var qle = _questionLabel.gameObject.AddComponent<LayoutElement>();
            qle.preferredHeight = 220; qle.minHeight = 180;

            _visual = QuestionVisualRenderer.Spawn((RectTransform)qcol.transform, 280);

            // Hint
            if (ShowHint)
            {
                var hintHolder = UIFactory.CreatePanel(safe,
                    new Vector2(0.04f, 0.41f), new Vector2(0.96f, 0.45f),
                    new Color(0, 0, 0, 0.25f), 16, "HintHolder");
                var hl = hintHolder.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.padding = new RectOffset(16, 16, 8, 8);
                hl.spacing = 16;
                hl.childForceExpandWidth = false;
                hl.childAlignment = TextAnchor.MiddleLeft;

                _hintButton = UIFactory.CreateButton((RectTransform)hintHolder,
                    "💡 Hint", UIFactory.Accent, 32, "HintBtn");
                var hle = _hintButton.gameObject.AddComponent<LayoutElement>();
                hle.preferredWidth = 220; hle.minWidth = 220;
                _hintButton.onClick.AddListener(ShowHintNow);

                _hintLabel = UIFactory.CreateText((RectTransform)hintHolder,
                    "", 28, Color.white, TextAlignmentOptions.MidlineLeft, "HintLabel");
                var hlle = _hintLabel.gameObject.AddComponent<LayoutElement>();
                hlle.flexibleWidth = 1;
            }

            // Answer buttons grid
            var answersPanel = UIFactory.CreatePanel(safe,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.40f),
                new Color(0, 0, 0, 0.15f), 24, "AnswersPanel");

            var grid = answersPanel.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(460, 150);
            grid.spacing  = new Vector2(20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.padding = new RectOffset(24, 24, 24, 24);
            grid.childAlignment = TextAnchor.MiddleCenter;
            _answersHolder = answersPanel;

            _feedback = AnimatedFeedback.Spawn(safe);
        }

        protected virtual void BuildHeaderExtras(RectTransform header) { }

        protected virtual void ShowQuestion(int index)
        {
            _currentIndex = index;
            if (_currentIndex >= _questions.Count)
            {
                Finish();
                return;
            }

            var q = _questions[_currentIndex];
            _questionLabel.text = q.prompt;
            _visual.Show(q);
            UpdateHeader();

            for (int i = _answersHolder.childCount - 1; i >= 0; i--)
                Destroy(_answersHolder.GetChild(i).gameObject);

            var shuffled = ShuffleOptions(q);
            for (int i = 0; i < shuffled.options.Length; i++)
            {
                int captured = i;
                AnswerButton.Spawn(_answersHolder, captured, shuffled.options[i],
                    chosen => HandleAnswer(chosen, shuffled));
            }
            _locked = false;
            if (_hintLabel != null) _hintLabel.text = "";
        }

        protected void UpdateHeader()
        {
            _progressLabel.text = $"{_currentIndex + 1} / {_questions.Count}";
            _progressBar.SetValue((float)(_currentIndex + 1) / Mathf.Max(1, _questions.Count));
            _scoreLabel.text = $"Score: {_score}";
        }

        protected virtual void HandleAnswer(int chosenIndex, MathQuestion q)
        {
            if (_locked) return;
            _locked = true;

            bool correct = q.IsCorrect(chosenIndex);
            var buttons = _answersHolder.GetComponentsInChildren<AnswerButton>();
            if (chosenIndex < buttons.Length)
            {
                if (correct) buttons[chosenIndex].FlashCorrect();
                else         buttons[chosenIndex].FlashWrong();
            }
            if (!correct && q.correctIndex >= 0 && q.correctIndex < buttons.Length)
                buttons[q.correctIndex].FlashCorrect();

            if (correct)
            {
                _correct++;
                _score += ScoreForCorrect(q);
                GameManager.Instance.Audio.PlayCorrect();
                _feedback.ShowCorrect(EncouragementCorrect());
                OnCorrect(q);
            }
            else
            {
                _wrong++;
                GameManager.Instance.Audio.PlayWrong();
                _feedback.ShowWrong(EncouragementWrong(q));
                OnWrong(q);
                if (StopOnFirstWrong) { StartCoroutine(FinishDelayed()); return; }
            }

            StartCoroutine(AdvanceAfterDelay());
        }

        protected virtual int ScoreForCorrect(MathQuestion q)
        {
            return 10 + ((int)q.difficulty * 5);
        }

        protected virtual void OnCorrect(MathQuestion q) { }
        protected virtual void OnWrong(MathQuestion q)   { }

        protected virtual IEnumerator AdvanceAfterDelay()
        {
            yield return new WaitForSeconds(QuestionDelay);
            ShowQuestion(_currentIndex + 1);
        }

        protected IEnumerator FinishDelayed()
        {
            yield return new WaitForSeconds(QuestionDelay + 0.3f);
            Finish();
        }

        protected virtual void Finish()
        {
            int total = _correct + _wrong;
            int stars = GameManager.Instance.Progress.CompleteLevel(_level, _correct, total, _score);
            GameManager.Instance.Session.correctCount = _correct;
            GameManager.Instance.Session.wrongCount   = _wrong;
            GameManager.Instance.Session.score        = _score;
            GameManager.Instance.UI.Go(UIManager.SceneResults);
        }

        protected virtual void ShowHintNow()
        {
            if (_hintLabel == null || _currentIndex >= _questions.Count) return;
            _hintLabel.text = _questions[_currentIndex].hint;
            GameManager.Instance.Audio.PlayTap();
        }

        protected static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Produce a clone of q with its 4 options re-shuffled and a new correctIndex,
        /// so the correct answer doesn't always land on the same button slot.
        /// </summary>
        protected static MathQuestion ShuffleOptions(MathQuestion q)
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

        protected static string EncouragementCorrect()
        {
            string[] msgs = { "Correct!", "Great job!", "You got it!", "Awesome!", "Brilliant!", "Yes!" };
            return msgs[Random.Range(0, msgs.Length)];
        }

        protected static string EncouragementWrong(MathQuestion q)
        {
            return $"Answer was {q.CorrectAnswer}";
        }
    }
}
