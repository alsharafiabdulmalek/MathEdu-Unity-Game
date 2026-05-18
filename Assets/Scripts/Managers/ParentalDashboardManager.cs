// -----------------------------------------------------------------------------
// ParentalDashboardManager.cs
// -----------------------------------------------------------------------------
// PIN-gated dashboard that reads PlayerProfile (already loaded from JSON by
// SaveSystem at boot) and visualises:
//   • Total play time
//   • XP, total stars, badges earned
//   • Per-subject accuracy bar chart
//   • Per-subject table: questions answered, levels completed, last played
//   • Per-grade level completion percentage
//   • Earned badges list (emoji + pretty name)
//
// PIN gate (spec 2I):
//   • Default PIN is "0000" (PlayerProfile.parentalPin initialised on first
//     save). Settings → Change PIN reveals the standard 3-step flow.
//   • Entry uses a 4-digit pad built from 10 number buttons + backspace +
//     confirm. Pad is built procedurally via UIFactory.
//   • Wrong PIN shakes the dot display and clears it. No error text reveals
//     the PIN length.
//   • 3 wrong attempts in a row disables the keypad for 30 seconds with a
//     visible countdown.
//   • Correct PIN slides the gate panel up over 0.4 s and reveals the
//     dashboard beneath it.
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using MathEdu.Data;
using MathEdu.UI;
using MathEdu.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Managers
{
    public class ParentalDashboardManager : MonoBehaviour
    {
        private PlayerProfile _profile;
        private bool _unlocked;

        // PIN gate widgets
        private RectTransform _gatePanel;
        private Image[] _dotImages;
        private string _entered = "";
        private int _wrongAttempts = 0;
        private List<Button> _keypadButtons = new List<Button>();
        private TextMeshProUGUI _lockoutLabel;
        private bool _locked;

        private void Start()
        {
            _ = GameManager.Instance;
            _profile = GameManager.Instance.Profile;
            BuildDashboard();   // build first (sits beneath the gate)
            BuildLockScreen();
        }

        // -------------------------------------------------------------------
        // PIN gate (keypad)
        // -------------------------------------------------------------------
        private void BuildLockScreen()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[ParentalGateCanvas]");
            canvas.sortingOrder = 500;
            UIFactory.CreateThemedBackground(safe, "parental");

            _gatePanel = UIFactory.CreatePanel(safe,
                Vector2.zero, Vector2.one,
                new Color(0.15f, 0.20f, 0.30f, 0.97f), 0, "GatePanel");

            // Header
            var header = UIFactory.CreatePanel(_gatePanel,
                new Vector2(0, 0.88f), new Vector2(1, 1f),
                new Color(0.30f, 0.35f, 0.45f), 0, "Header");
            UIFactory.CreateText(header, "Parental Dashboard", 56,
                Color.white, TextAlignmentOptions.Center, "Title").fontStyle = FontStyles.Bold;

            var back = IconService.IconButton(header, "back", "<", new Color(0, 0, 0, 0.35f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            });

            // Card holding the dots + keypad
            var card = UIFactory.CreatePanel(_gatePanel,
                new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.84f),
                UIFactory.Card, 32, "GateCard");
            var col = UIFactory.CreateVerticalLayout(card, 24,
                new RectOffset(32, 32, 32, 32), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, "🔒 For Parents", 64,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Lbl")
                .fontStyle = FontStyles.Bold;
            UIFactory.CreateText((RectTransform)col.transform,
                "Enter your PIN. Default is 0000.", 28,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Sub");

            // PIN dots row
            BuildDotsRow((RectTransform)col.transform);

            // Lockout label (hidden by default)
            _lockoutLabel = UIFactory.CreateText((RectTransform)col.transform,
                "", 32, UIFactory.Danger, TextAlignmentOptions.Center, "LockoutLbl");
            _lockoutLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            // Keypad
            BuildKeypad((RectTransform)col.transform);
        }

        private void BuildDotsRow(RectTransform parent)
        {
            var row = new GameObject("Dots",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<HorizontalLayoutGroup>().spacing = 24;
            row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            row.GetComponent<LayoutElement>().preferredHeight = 90;

            _dotImages = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var dot = new GameObject($"Dot_{i}", typeof(Image), typeof(LayoutElement));
                dot.transform.SetParent(row.transform, false);
                var le = dot.GetComponent<LayoutElement>();
                le.preferredWidth = 60; le.preferredHeight = 60;
                var img = dot.GetComponent<Image>();
                img.sprite = DefaultSprite.Circle();
                img.color = new Color(0.6f, 0.6f, 0.6f, 0.4f);
                _dotImages[i] = img;
            }
        }

        private void BuildKeypad(RectTransform parent)
        {
            var pad = new GameObject("Keypad",
                typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            pad.transform.SetParent(parent, false);
            var grid = pad.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(180, 180);
            grid.spacing  = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.MiddleCenter;
            pad.GetComponent<LayoutElement>().preferredHeight = 780;

            _keypadButtons.Clear();
            // 1..9
            for (int n = 1; n <= 9; n++)
            {
                int captured = n;
                var btn = UIFactory.CreateButton((RectTransform)pad.transform,
                    captured.ToString(), UIFactory.Primary, 56, $"Key_{captured}");
                btn.onClick.AddListener(() => PressDigit(captured.ToString()));
                _keypadButtons.Add(btn);
            }
            // Backspace, 0, Confirm
            var backBtn = UIFactory.CreateButton((RectTransform)pad.transform,
                "⌫", new Color(0.65f, 0.45f, 0.45f), 56, "Key_Back");
            backBtn.onClick.AddListener(PressBackspace);
            _keypadButtons.Add(backBtn);

            var zero = UIFactory.CreateButton((RectTransform)pad.transform,
                "0", UIFactory.Primary, 56, "Key_0");
            zero.onClick.AddListener(() => PressDigit("0"));
            _keypadButtons.Add(zero);

            var ok = UIFactory.CreateButton((RectTransform)pad.transform,
                "OK", UIFactory.Success, 56, "Key_OK");
            ok.onClick.AddListener(PressConfirm);
            _keypadButtons.Add(ok);
        }

        private void PressDigit(string d)
        {
            if (_locked || _unlocked) return;
            GameManager.Instance.Audio.PlaySFX("tap");
            if (_entered.Length < 4) _entered += d;
            RenderDots();
            if (_entered.Length == 4) PressConfirm();
        }

        private void PressBackspace()
        {
            if (_locked || _unlocked) return;
            GameManager.Instance.Audio.PlaySFX("tap");
            if (_entered.Length > 0)
                _entered = _entered.Substring(0, _entered.Length - 1);
            RenderDots();
        }

        private void PressConfirm()
        {
            if (_locked || _unlocked) return;
            if (_entered.Length < 4) return;
            if (_entered == (_profile.parentalPin ?? "0000"))
            {
                _unlocked = true;
                GameManager.Instance.Audio.PlaySFX("correct");
                StartCoroutine(SlideUpThenDestroy());
            }
            else
            {
                GameManager.Instance.Audio.PlaySFX("wrong");
                _wrongAttempts++;
                _entered = "";
                RenderDots();
                StartCoroutine(ShakeDots());
                if (_wrongAttempts >= 3)
                {
                    StartCoroutine(LockoutFor(30));
                }
            }
        }

        private void RenderDots()
        {
            if (_dotImages == null) return;
            for (int i = 0; i < _dotImages.Length; i++)
            {
                bool filled = i < _entered.Length;
                _dotImages[i].color = filled
                    ? UIFactory.Primary
                    : new Color(0.6f, 0.6f, 0.6f, 0.4f);
            }
        }

        private IEnumerator ShakeDots()
        {
            // Shake the dots row left/right for 0.4 seconds.
            if (_dotImages == null || _dotImages.Length == 0) yield break;
            var t = _dotImages[0].transform.parent as RectTransform;
            if (t == null) yield break;
            Vector2 start = t.anchoredPosition;
            float dur = 0.4f, elapsed = 0;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                t.anchoredPosition = start + new Vector2(Mathf.Sin(elapsed * 50) * 18, 0);
                yield return null;
            }
            t.anchoredPosition = start;
        }

        private IEnumerator LockoutFor(int seconds)
        {
            _locked = true;
            foreach (var b in _keypadButtons) if (b != null) b.interactable = false;
            int remaining = seconds;
            while (remaining > 0 && _locked)
            {
                if (_lockoutLabel != null)
                    _lockoutLabel.text = $"Try again in {remaining} s";
                yield return new WaitForSecondsRealtime(1f);
                remaining--;
            }
            if (_lockoutLabel != null) _lockoutLabel.text = "";
            foreach (var b in _keypadButtons) if (b != null) b.interactable = true;
            _locked = false;
            _wrongAttempts = 0;
        }

        private IEnumerator SlideUpThenDestroy()
        {
            const float dur = 0.4f;
            const float dist = 1600f; // covers any portrait screen
            float elapsed = 0;
            Vector2 origMin = _gatePanel.anchorMin;
            Vector2 origMax = _gatePanel.anchorMax;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / dur);
                // Easing out cubic
                float e = 1f - Mathf.Pow(1f - k, 3f);
                _gatePanel.anchoredPosition = new Vector2(0, dist * e);
                yield return null;
            }
            var canvas = _gatePanel.GetComponentInParent<Canvas>();
            if (canvas != null) Destroy(canvas.gameObject);
        }

        // -------------------------------------------------------------------
        // Dashboard (built underneath the gate, visible after the slide-up)
        // -------------------------------------------------------------------
        private void BuildDashboard()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[ParentalDashboardCanvas]");
            UIFactory.CreateThemedBackground(safe, "parental");

            // Header
            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.92f), new Vector2(1, 1f),
                new Color(0.30f, 0.35f, 0.45f), 0, "Header");
            UIFactory.CreateText(header,
                $"{_profile.playerName}'s Progress", 48,
                Color.white, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            var back = IconService.IconButton(header, "back", "<", new Color(0, 0, 0, 0.35f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            });

            // Scrollable body
            var scroll = UIFactory.CreateScrollView(safe, "DashScroll");
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0, 0.06f); srt.anchorMax = new Vector2(1, 0.92f);
            srt.offsetMin = new Vector2(16, 0); srt.offsetMax = new Vector2(-16, 0);
            var content = scroll.content;

            BuildSummaryCard(content);
            BuildAccuracyCard(content);
            BuildSubjectTable(content);
            BuildBadgeWall(content);
            BuildGradeCompletion(content);

            // Footer (change PIN + reset progress)
            var footer = new GameObject("FooterRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            footer.transform.SetParent(content, false);
            var fhl = footer.GetComponent<HorizontalLayoutGroup>();
            fhl.spacing = 16; fhl.childForceExpandWidth = true;
            fhl.childAlignment = TextAnchor.MiddleCenter;
            var fle = footer.GetComponent<LayoutElement>();
            fle.preferredHeight = 140; fle.minHeight = 140;

            var pinBtn = UIFactory.CreateButton((RectTransform)footer.transform,
                "Change PIN", UIFactory.Primary, 32, "PinBtn");
            pinBtn.onClick.AddListener(ChangePinFromDashboard);

            var resetBtn = UIFactory.CreateButton((RectTransform)footer.transform,
                "Reset Progress", UIFactory.Danger, 32, "ResetBtn");
            resetBtn.onClick.AddListener(ResetProgress);
        }

        private void BuildSummaryCard(RectTransform parent)
        {
            var card = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                new Color(1f, 1f, 1f, 0.10f), 24, "Summary");
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 280; le.minHeight = 280;

            var grid = card.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300, 220);
            grid.spacing  = new Vector2(16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.childAlignment = TextAnchor.UpperCenter;

            int totalLevels = CountCompletedLevels();
            int totalBadges = _profile.badges?.Count ?? 0;
            string playStr  = FormatTime(_profile.totalPlaySeconds);

            AddTile(card, "⭐ Stars",   _profile.totalStars.ToString());
            AddTile(card, "🎯 XP",      _profile.xp.ToString());
            AddTile(card, "🏆 Badges",  totalBadges.ToString());
            AddTile(card, "📚 Levels",  totalLevels.ToString());
            AddTile(card, "⏱ Time",    playStr);
            AddTile(card, "🎓 Grade",   _profile.selectedGrade.ToString());
        }

        private static void AddTile(RectTransform parent, string label, string value)
        {
            var tile = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                UIFactory.Card, 20, $"Tile_{label}");
            var col = UIFactory.CreateVerticalLayout(tile, 8,
                new RectOffset(12, 12, 12, 12), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, label, 26,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Lbl")
                .fontStyle = FontStyles.Bold;
            UIFactory.CreateText((RectTransform)col.transform, value, 64,
                UIFactory.Primary, TextAlignmentOptions.Center, "Value")
                .fontStyle = FontStyles.Bold;
        }

        private void BuildAccuracyCard(RectTransform parent)
        {
            var rows = new List<(string label, float pct, Color color)>();
            foreach (var s in _profile.subjectStats)
            {
                if (s == null || s.questionsAnswered == 0) continue;
                rows.Add((PrettySubject(s.subjectKey), s.Accuracy, ColorForSubject(s.subjectKey)));
            }
            if (rows.Count == 0)
            {
                var card = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                    new Color(1, 1, 1, 0.10f), 24, "EmptyAccuracy");
                var le = card.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 160; le.minHeight = 160;
                UIFactory.CreateText(card,
                    "Accuracy stats appear after the first session.",
                    30, Color.white, TextAlignmentOptions.Center, "EmptyTxt");
                return;
            }

            var chartHolder = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                new Color(1, 1, 1, 0.10f), 24, "ChartHolder");
            var chle = chartHolder.gameObject.AddComponent<LayoutElement>();
            chle.preferredHeight = 120 + rows.Count * 80;
            chle.minHeight       = 120 + rows.Count * 80;

            AccuracyBarChart.Spawn(chartHolder, rows, "Accuracy by Subject");
        }

        private void BuildSubjectTable(RectTransform parent)
        {
            var holder = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                new Color(1, 1, 1, 0.10f), 24, "TableHolder");
            var le = holder.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 120 + Mathf.Max(1, _profile.subjectStats.Count) * 80;
            le.minHeight       = 120;

            var col = UIFactory.CreateVerticalLayout(holder, 8,
                new RectOffset(20, 20, 20, 20), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, "Subject Details", 40,
                Color.white, TextAlignmentOptions.Left, "Title").fontStyle = FontStyles.Bold;

            BuildTableHeader((RectTransform)col.transform);

            if (_profile.subjectStats.Count == 0)
            {
                UIFactory.CreateText((RectTransform)col.transform,
                    "No subjects played yet.", 28,
                    Color.white, TextAlignmentOptions.Center, "Empty");
                return;
            }

            foreach (var s in _profile.subjectStats)
                BuildTableRow((RectTransform)col.transform, s);
        }

        private static void BuildTableHeader(RectTransform parent)
        {
            var row = new GameObject("TableHeader",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 8; hl.childForceExpandWidth = true;
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 50;

            string[] cols = { "Subject", "Q", "Correct", "Stars", "Levels", "Time" };
            foreach (var c in cols)
            {
                var t = UIFactory.CreateText((RectTransform)row.transform, c, 26,
                    new Color(1, 1, 1, 0.85f), TextAlignmentOptions.Center, c);
                t.fontStyle = FontStyles.Bold;
            }
        }

        private void BuildTableRow(RectTransform parent, SubjectStats s)
        {
            var row = new GameObject("Row_" + s.subjectKey,
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 8; hl.childForceExpandWidth = true;
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 70;

            string[] cells =
            {
                PrettySubject(s.subjectKey),
                s.questionsAnswered.ToString(),
                $"{s.questionsCorrect} ({Mathf.RoundToInt(s.Accuracy)}%)",
                s.starsEarned.ToString(),
                s.levelsCompleted.ToString(),
                FormatTime(s.timeSpentSeconds)
            };
            foreach (var c in cells)
                UIFactory.CreateText((RectTransform)row.transform, c, 26,
                    Color.white, TextAlignmentOptions.Center, "Cell");
        }

        private void BuildBadgeWall(RectTransform parent)
        {
            var holder = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                new Color(1, 1, 1, 0.10f), 24, "BadgeHolder");
            var le = holder.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 360; le.minHeight = 240;

            var col = UIFactory.CreateVerticalLayout(holder, 10,
                new RectOffset(20, 20, 20, 20), "BadgeCol");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, "Badges", 40,
                Color.white, TextAlignmentOptions.Left, "Title").fontStyle = FontStyles.Bold;

            if (_profile.badges == null || _profile.badges.Count == 0)
            {
                UIFactory.CreateText((RectTransform)col.transform,
                    "No badges yet — earn one by clearing a level!",
                    26, Color.white, TextAlignmentOptions.Center, "Empty");
                return;
            }
            var grid = new GameObject("BadgeGrid",
                typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            grid.transform.SetParent(col.transform, false);
            var g = grid.GetComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(360, 70);
            g.spacing  = new Vector2(8, 8);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;
            grid.GetComponent<LayoutElement>().preferredHeight = 260;
            foreach (var id in _profile.badges)
            {
                UIFactory.CreateText((RectTransform)grid.transform,
                    ProgressManager.PrettyBadgeName(id), 28,
                    Color.white, TextAlignmentOptions.MidlineLeft, "B_" + id);
            }
        }

        private void BuildGradeCompletion(RectTransform parent)
        {
            var holder = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                new Color(1, 1, 1, 0.10f), 24, "GradeHolder");
            var le = holder.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 360; le.minHeight = 360;

            var col = UIFactory.CreateVerticalLayout(holder, 8,
                new RectOffset(20, 20, 20, 20), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, "Grade Completion", 40,
                Color.white, TextAlignmentOptions.Left, "Title").fontStyle = FontStyles.Bold;

            var db = GameManager.Instance.database;
            if (db == null) return;

            var rows = new List<(string, float, Color)>();
            foreach (var g in db.grades)
            {
                if (g == null) continue;
                int total = 0, done = 0;
                foreach (var subj in g.subjects)
                {
                    if (subj == null) continue;
                    foreach (var lv in subj.levels)
                    {
                        if (lv == null) continue;
                        total++;
                        if (_profile.GetStars(lv.levelId) > 0) done++;
                    }
                }
                float pct = total > 0 ? 100f * done / total : 0f;
                rows.Add(($"Grade {g.gradeNumber}", pct, g.themeColor));
            }
            AccuracyBarChart.Spawn(holder, rows, "");
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private int CountCompletedLevels()
        {
            int n = 0;
            foreach (var lp in _profile.levelProgress)
                if (lp != null && lp.stars > 0) n++;
            return n;
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 60f) return $"{Mathf.RoundToInt(seconds)}s";
            int mins = Mathf.FloorToInt(seconds / 60f);
            if (mins < 60) return $"{mins}m";
            int hrs = mins / 60;
            int rem = mins % 60;
            return $"{hrs}h{rem:00}m";
        }

        private static string PrettySubject(string key)
        {
            if (string.IsNullOrEmpty(key)) return "?";
            return char.ToUpper(key[0]) + key.Substring(1);
        }

        private static Color ColorForSubject(string key)
        {
            return key switch
            {
                "addition"       => new Color(0.30f, 0.65f, 0.95f),
                "subtraction"    => new Color(0.95f, 0.45f, 0.45f),
                "multiplication" => new Color(0.55f, 0.40f, 0.90f),
                "division"       => new Color(0.20f, 0.75f, 0.65f),
                "counting"       => new Color(0.95f, 0.78f, 0.20f),
                "shapes"         => new Color(0.95f, 0.55f, 0.20f),
                "patterns"       => new Color(0.80f, 0.40f, 0.75f),
                "fractions"      => new Color(0.40f, 0.75f, 0.30f),
                "measurement"    => new Color(0.30f, 0.55f, 0.75f),
                "time"           => new Color(0.95f, 0.65f, 0.25f),
                "money"          => new Color(0.30f, 0.80f, 0.40f),
                _                => Color.gray
            };
        }

        private void ChangePinFromDashboard()
        {
            PasswordDialog.Show(
                "New Parental PIN",
                "Pick a 4-8 digit PIN.",
                onSubmit: pin =>
                {
                    if (string.IsNullOrEmpty(pin) || pin.Length < 4)
                    {
                        GameManager.Instance.Audio.PlaySFX("wrong");
                        return;
                    }
                    _profile.parentalPin = pin;
                    GameManager.Instance.SaveProfile();
                    GameManager.Instance.Audio.PlaySFX("correct");
                });
        }

        private void ResetProgress()
        {
            PasswordDialog.Show(
                "Confirm Reset",
                "Type the parental PIN to wipe all progress.",
                onSubmit: pin =>
                {
                    if (pin == _profile.parentalPin)
                    {
                        SaveSystem.DeleteAll();
                        GameManager.Instance.UI.Go(UIManager.SceneBootstrap);
                    }
                });
        }
    }
}
