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
//
// The dashboard refuses to render until the user enters the parental PIN. The
// default PIN is "0000" — the Settings screen lets parents change it.
//
// All numbers come straight off PlayerProfile.subjectStats so no extra walks
// over the database are required.
// -----------------------------------------------------------------------------

using System;
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

        private void Start()
        {
            _ = GameManager.Instance;
            _profile = GameManager.Instance.Profile;
            BuildLockScreen();
        }

        // -------------------------------------------------------------------
        // PIN gate
        // -------------------------------------------------------------------
        private void BuildLockScreen()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[ParentalGateCanvas]");
            UIFactory.CreateThemedBackground(safe, "parental");

            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.88f), new Vector2(1, 1f),
                new Color(0.30f, 0.35f, 0.45f), 0, "Header");
            UIFactory.CreateText(header, "Parental Dashboard", 56,
                Color.white, TextAlignmentOptions.Center, "Title").fontStyle = FontStyles.Bold;

            var back = UIFactory.CreateIconButton(header, "<", new Color(0, 0, 0, 0.35f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() => GameManager.Instance.UI.Go(UIManager.SceneMainMenu));

            var card = UIFactory.CreatePanel(safe,
                new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.78f),
                UIFactory.Card, 32, "LockCard");
            var col = UIFactory.CreateVerticalLayout(card, 20,
                new RectOffset(28, 28, 28, 28), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, "🔒  For Parents",
                64, UIFactory.TextDark, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            UIFactory.CreateText((RectTransform)col.transform,
                "Enter the parental PIN to see your child's progress.\nDefault PIN: 0000",
                30, UIFactory.TextDark, TextAlignmentOptions.Center, "Sub");

            var input = UIFactory.CreateInputField((RectTransform)col.transform,
                "Enter PIN", 48, "PinInput");
            input.contentType = TMP_InputField.ContentType.Pin;
            input.characterLimit = 8;
            var ile = input.gameObject.AddComponent<LayoutElement>();
            ile.preferredHeight = 130;

            var unlockBtn = UIFactory.CreateButton((RectTransform)col.transform,
                "Unlock", UIFactory.Primary, 44, "Unlock");
            var ule = unlockBtn.gameObject.AddComponent<LayoutElement>();
            ule.preferredHeight = 130;
            unlockBtn.onClick.AddListener(() =>
            {
                if (input.text == _profile.parentalPin)
                {
                    _unlocked = true;
                    DestroyAllCanvases();
                    BuildDashboard();
                }
                else
                {
                    input.text = "";
                    GameManager.Instance.Audio.PlayWrong();
                }
            });
        }

        private void DestroyAllCanvases()
        {
            var gateCanvas = GameObject.Find("[ParentalGateCanvas]");
            if (gateCanvas != null) DestroyImmediate(gateCanvas);
        }

        // -------------------------------------------------------------------
        // Dashboard
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

            var back = UIFactory.CreateIconButton(header, "<", new Color(0, 0, 0, 0.35f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() => GameManager.Instance.UI.Go(UIManager.SceneMainMenu));

            // Scrollable body
            var scroll = UIFactory.CreateScrollView(safe, "DashScroll");
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0, 0.06f); srt.anchorMax = new Vector2(1, 0.92f);
            srt.offsetMin = new Vector2(16, 0); srt.offsetMax = new Vector2(-16, 0);
            var content = scroll.content;

            // ----- Summary card -----
            BuildSummaryCard(content);

            // ----- Per-subject accuracy chart -----
            BuildAccuracyCard(content);

            // ----- Per-subject detail table -----
            BuildSubjectTable(content);

            // ----- Per-grade completion -----
            BuildGradeCompletion(content);

            // Footer (change PIN)
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
            pinBtn.onClick.AddListener(ChangePin);

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
                // Empty state
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

        private void ChangePin()
        {
            PasswordDialog.Show(
                "New Parental PIN",
                "Pick a 4-8 digit PIN.",
                onSubmit: pin =>
                {
                    if (string.IsNullOrEmpty(pin) || pin.Length < 4)
                    {
                        Debug.Log("[Parental] PIN too short.");
                        return;
                    }
                    _profile.parentalPin = pin;
                    GameManager.Instance.SaveProfile();
                    GameManager.Instance.Audio.PlayCorrect();
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
