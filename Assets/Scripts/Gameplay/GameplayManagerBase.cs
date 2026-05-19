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
//
// The base loop also:
//   - Records elapsed seconds on GameSession (used by per-subject stats).
//   - Tracks the longest correct streak (used by Speed Round badges + Results).
//   - Pads or falls back when a LevelData is missing or has fewer than the
//     desired number of questions, so we never crash mid-run.
//   - Adds a Back button with "Quit this level?" confirmation and a Pause
//     button (configurable per mode).
//   - Fires VFXManager prefabs on correct / wrong (Epic Toon FX support).
//   - Spawns a ReactionFace puck that reacts in real time to correct/wrong/streak.
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using MathEdu.Data;
using MathEdu.Managers;
using MathEdu.UI;
using MathEdu.Utility;
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
        protected virtual  bool    AllowPause        => true;
        protected virtual  int     TargetQuestionCount => 10;

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
        protected RectTransform _pauseOverlay;
        protected bool _paused;

        protected List<MathQuestion> _questions;
        protected LevelData _level;
        protected int _currentIndex;
        protected int _correct;
        protected int _wrong;
        protected int _score;
        protected int _currentStreak;
        protected int _maxStreak;
        protected bool _locked;
        protected bool _finished;
        protected ReactionFace _reactionFace;

        protected float _sessionStartTime;

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

            _questions = BuildQuestionList(_level);
            if (ShuffleQuestions) Shuffle(_questions);

            // Reset per-session counters and timing.
            GameManager.Instance.Session.ResetGameplay();
            _sessionStartTime = Time.unscaledTime;

            BuildUI();
            ShowQuestion(0);
        }

        /// <summary>
        /// Build a list of exactly TargetQuestionCount questions, padding by
        /// duplicating existing ones if the level has fewer. Skips any null
        /// or invalid entries. If the level has zero usable questions, a
        /// single fallback "1 + 1 = ?" is injected so the game never crashes.
        /// </summary>
        private List<MathQuestion> BuildQuestionList(LevelData level)
        {
            var src = new List<MathQuestion>();
            if (level.questions != null)
            {
                foreach (var q in level.questions)
                {
                    if (q != null && q.IsValid()) src.Add(q);
                }
            }
            if (src.Count == 0)
            {
                Debug.LogError($"[Gameplay] LevelData '{level.levelId}' has no usable questions; using fallback.");
                src.Add(new MathQuestion
                {
                    prompt = "1 + 1 = ?",
                    options = new[] { "1", "2", "3", "4" },
                    correctIndex = 1,
                    hint = "Count up by one.",
                    explanation = "1 + 1 = 2.",
                    difficulty = QuestionDifficulty.VeryEasy
                });
            }
            // Pad with duplicates if level has fewer than the target count.
            if (src.Count < TargetQuestionCount)
            {
                Debug.LogWarning($"[Gameplay] Level '{level.levelId}' has only {src.Count} questions; padding to {TargetQuestionCount}.");
                int i = 0;
                while (src.Count < TargetQuestionCount)
                {
                    src.Add(src[i % src.Count]);
                    i++;
                }
            }
            else if (src.Count > TargetQuestionCount)
            {
                src = src.GetRange(0, TargetQuestionCount);
            }
            return src;
        }

        protected virtual void Update()
        {
            // Track real elapsed time on the GameSession so per-subject time
            // stats are accurate even if frames stutter. Pauses freeze the clock.
            if (!_paused)
            {
                GameManager.Instance.Session.elapsedSeconds =
                    Time.unscaledTime - _sessionStartTime;
            }
        }

        protected virtual void BuildUI()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[GameplayCanvas]");
            _safeArea = safe;
            UIFactory.CreateThemedBackground(safe, "play");

            // Header
            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.88f), new Vector2(1, 1f),
                HeaderColor, 0, "Header");

            // Header title — fully localized via "gp.header_format"
            // so the mode name + subject + level read in the player's language.
            string subjectName = GameManager.Instance.CurrentSubject != null
                ? GameManager.Instance.CurrentSubject.displayName
                : "";
            UIFactory.CreateText(header,
                Localization.T("gp.header_format", HeaderTitle, subjectName, _level.levelNumber),
                40, Color.white, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            // Back button (with "Quit this level?" confirmation).
            var back = IconService.IconButton(header, "back", "<",
                new Color(0, 0, 0, 0.3f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(ConfirmQuit);

            // Pause button (top-right).
            if (AllowPause)
            {
                var pause = IconService.IconButton(header, "pause", "II",
                    new Color(0, 0, 0, 0.3f), "Pause");
                var prt = (RectTransform)pause.transform;
                prt.anchorMin = prt.anchorMax = new Vector2(1, 0.5f);
                prt.anchoredPosition = new Vector2(-80, 0);
                prt.sizeDelta = new Vector2(110, 110);
                pause.onClick.AddListener(ShowPauseOverlay);
            }

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
                Localization.T("gp.score", 0), 36, Color.white, TextAlignmentOptions.Right, "ScoreLabel");

            // Reaction face — a friendly mascot puck that reacts in real time.
            // Positioned above the question card on the right so it never
            // overlaps the question text.
            _reactionFace = ReactionFace.Spawn(safe,
                anchorMin: new Vector2(0.78f, 0.78f),
                anchorMax: new Vector2(0.78f, 0.78f),
                size: 180f);

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
                    Localization.T("gp.hint_btn"), UIFactory.Accent, 32, "HintBtn");
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
            // Question prompts come from QuestionStrings in raw logical-order
            // Arabic; shape them here so TMP renders connected glyphs.
            Localization.SetText(_questionLabel, q.prompt);
            // The visual renderer might throw on malformed payloads (e.g. a
            // ScriptableObject authored by hand with the wrong visualPayload
            // length). Catch and continue so the question itself still
            // renders — better a missing pie chart than a black screen.
            try { _visual.Show(q); }
            catch (System.Exception e)
            {
                Debug.LogError($"[Gameplay] Visual renderer threw on Q{index}: {e.Message}");
            }
            UpdateHeader();

            for (int i = _answersHolder.childCount - 1; i >= 0; i--)
                Destroy(_answersHolder.GetChild(i).gameObject);

            var shuffled = ShuffleOptions(q);
            for (int i = 0; i < shuffled.options.Length; i++)
            {
                int captured = i;
                // Shape the option text in case it contains Arabic words
                // (shape names, coin names, "Same"/"Cannot tell", etc.).
                AnswerButton.Spawn(_answersHolder, captured,
                    Localization.Shape(shuffled.options[i]),
                    chosen => HandleAnswer(chosen, shuffled));
            }
            _locked = false;
            if (_hintLabel != null) _hintLabel.text = "";
        }

        protected void UpdateHeader()
        {
            _progressLabel.text = $"{_currentIndex + 1} / {_questions.Count}";
            _progressBar.SetValue((float)(_currentIndex + 1) / Mathf.Max(1, _questions.Count));
            Localization.SetText(_scoreLabel, Localization.T("gp.score", _score));
        }

        protected virtual void HandleAnswer(int chosenIndex, MathQuestion q)
        {
            if (_locked) return;
            _locked = true;

            bool correct = q.IsCorrect(chosenIndex);
            var buttons = _answersHolder.GetComponentsInChildren<AnswerButton>();
            if (chosenIndex >= 0 && chosenIndex < buttons.Length)
            {
                if (correct) buttons[chosenIndex].FlashCorrect();
                else         buttons[chosenIndex].FlashWrong();
            }
            if (!correct && q.correctIndex >= 0 && q.correctIndex < buttons.Length)
                buttons[q.correctIndex].FlashCorrect();

            if (correct)
            {
                _correct++;
                _currentStreak++;
                if (_currentStreak > _maxStreak) _maxStreak = _currentStreak;
                _score += ScoreForCorrect(q);
                // Layered audio: streak milestones add a bright arpeggio on
                // top of the regular correct ding so the player feels rewarded
                // for getting several in a row.
                GameManager.Instance.Audio.PlaySFX("correct");
                if (_currentStreak == 3 || _currentStreak == 5 || _currentStreak == 10)
                    GameManager.Instance.Audio.PlaySFX("streak");
                GameManager.Instance.VFX?.PlayCorrect();
                HapticManager.Light();
                _feedback.ShowCorrect(EncouragementCorrect(_currentStreak), _currentStreak);
                if (_reactionFace != null)
                {
                    if (_currentStreak >= 3) _reactionFace.Cheer();
                    else                     _reactionFace.Happy();
                }
                OnCorrect(q);
            }
            else
            {
                _wrong++;
                _currentStreak = 0;
                GameManager.Instance.Audio.PlaySFX("wrong");
                GameManager.Instance.VFX?.PlayWrong();
                _feedback.ShowWrong(EncouragementWrong(q));
                if (_reactionFace != null) _reactionFace.Sad();
                OnWrong(q);
                if (StopOnFirstWrong)
                {
                    GameManager.Instance.Session.failedEarly = true;
                    StartCoroutine(FinishDelayed());
                    return;
                }
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
            if (_finished) return;
            _finished = true;

            // Cache live counters on the session so the Results scene can
            // still render them if it reloads.
            int total = _correct + _wrong;
            var session = GameManager.Instance.Session;
            session.correctCount = _correct;
            session.wrongCount   = _wrong;
            session.score        = _score;
            session.maxStreak    = _maxStreak;

            // Record the level and produce a full SessionResult snapshot.
            var result = GameManager.Instance.Progress.CompleteLevel(
                _level, _correct, total, _score,
                session.selectedMode, _maxStreak, session.failedEarly);

            session.lastResult = result;

            // Reset Time.timeScale in case we exit while paused.
            Time.timeScale = 1f;
            GameManager.Instance.Audio.PlaySFX("levelComplete");
            GameManager.Instance.UI.Go(UIManager.SceneResults);
        }

        protected virtual void ShowHintNow()
        {
            if (_hintLabel == null || _currentIndex >= _questions.Count) return;
            // Hints come from QuestionStrings in raw logical-order Arabic;
            // shape on display so cursive Arabic reads as words, not letters.
            Localization.SetText(_hintLabel, _questions[_currentIndex].hint);
            GameManager.Instance.Audio.PlaySFX("hint");
        }

        // -------------------------------------------------------------------
        // Pause + quit confirmation
        // -------------------------------------------------------------------
        protected virtual void ShowPauseOverlay()
        {
            if (_paused) return;
            _paused = true;
            Time.timeScale = 0f;
            GameManager.Instance.Audio.PlaySFX("tap");

            _pauseOverlay = UIFactory.CreatePanel(_safeArea,
                Vector2.zero, Vector2.one,
                new Color(0, 0, 0, 0.75f), 0, "PauseOverlay");

            var card = UIFactory.CreatePanel(_pauseOverlay,
                new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.70f),
                UIFactory.Card, 28, "PauseCard");

            var col = UIFactory.CreateVerticalLayout(card, 20,
                new RectOffset(40, 40, 40, 40), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, Localization.T("pause.paused"), 80,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            var resume = UIFactory.CreateButton((RectTransform)col.transform,
                Localization.T("pause.resume"), UIFactory.Success, 42, "Resume");
            resume.gameObject.AddComponent<LayoutElement>().preferredHeight = 130;
            resume.onClick.AddListener(HidePauseOverlay);

            var restart = UIFactory.CreateButton((RectTransform)col.transform,
                Localization.T("pause.restart"), UIFactory.Primary, 42, "Restart");
            restart.gameObject.AddComponent<LayoutElement>().preferredHeight = 130;
            restart.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                GameManager.Instance.UI.Go(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });

            var quit = UIFactory.CreateButton((RectTransform)col.transform,
                Localization.T("pause.quit_level"), UIFactory.Danger, 42, "Quit");
            quit.gameObject.AddComponent<LayoutElement>().preferredHeight = 130;
            quit.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                GameManager.Instance.UI.Go(UIManager.SceneModeSelect);
            });
        }

        protected void HidePauseOverlay()
        {
            if (!_paused) return;
            _paused = false;
            Time.timeScale = 1f;
            if (_pauseOverlay != null) Destroy(_pauseOverlay.gameObject);
            _pauseOverlay = null;
            GameManager.Instance.Audio.PlaySFX("tap");
        }

        /// <summary>Show a small "Quit this level?" modal before navigating away.</summary>
        protected void ConfirmQuit()
        {
            if (_paused) return;
            _paused = true;
            Time.timeScale = 0f;

            var overlay = UIFactory.CreatePanel(_safeArea,
                Vector2.zero, Vector2.one,
                new Color(0, 0, 0, 0.75f), 0, "QuitOverlay");
            _pauseOverlay = overlay;

            var card = UIFactory.CreatePanel(overlay,
                new Vector2(0.10f, 0.35f), new Vector2(0.90f, 0.65f),
                UIFactory.Card, 28, "QuitCard");
            var col = UIFactory.CreateVerticalLayout(card, 18,
                new RectOffset(32, 32, 32, 32), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, Localization.T("quit.title"),
                56, UIFactory.TextDark, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;
            UIFactory.CreateText((RectTransform)col.transform,
                Localization.T("quit.body"), 32,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Body");

            var row = new GameObject("BtnRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(col.transform, false);
            row.GetComponent<HorizontalLayoutGroup>().spacing = 24;
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            row.GetComponent<LayoutElement>().preferredHeight = 140;

            var stay = UIFactory.CreateButton((RectTransform)row.transform,
                Localization.T("quit.keep_playing"), UIFactory.Success, 36, "Stay");
            stay.onClick.AddListener(HidePauseOverlay);

            var quit = UIFactory.CreateButton((RectTransform)row.transform,
                Localization.T("quit.quit"), UIFactory.Danger, 36, "Quit");
            quit.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                GameManager.Instance.UI.Go(UIManager.SceneModeSelect);
            });
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

        protected static string EncouragementCorrect(int streak = 0)
        {
            // Special-case streak milestones with extra-celebratory copy.
            // All strings flow through Localization.T() so they read in the
            // player's selected language (and Arabic gets cursive shaping).
            if (streak >= 10) return Localization.T("gp.incredible");
            if (streak >= 5)  return Localization.T("gp.on_fire");
            if (streak >= 3)  return Localization.T("gp.streak");
            string[] keys = {
                "gp.correct", "gp.great_job", "gp.you_got_it",
                "gp.awesome", "gp.brilliant", "gp.yes_excl"
            };
            return Localization.T(keys[Random.Range(0, keys.Length)]);
        }

        protected static string EncouragementWrong(MathQuestion q)
        {
            return Localization.T("gp.wrong_answer_was", q.CorrectAnswer);
        }
    }
}
