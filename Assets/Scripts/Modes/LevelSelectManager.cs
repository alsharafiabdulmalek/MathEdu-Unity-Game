// -----------------------------------------------------------------------------
// LevelSelectManager.cs
// -----------------------------------------------------------------------------
// Shows a grid of level tiles for the currently selected (grade, subject).
// Tapping an unlocked level proceeds to ModeSelect to pick a learning mode.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Managers;
using MathEdu.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Modes
{
    public class LevelSelectManager : MonoBehaviour
    {
        private void Start()
        {
            _ = GameManager.Instance;
            Build();
        }

        private void Build()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[LevelSelectCanvas]");
            UIFactory.CreateGradientBackground(safe, UIFactory.BgTop, UIFactory.BgBottom);

            var subject = GameManager.Instance.CurrentSubject;
            var grade   = GameManager.Instance.CurrentGrade;

            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.88f), new Vector2(1, 1f),
                subject != null ? subject.themeColor : UIFactory.Primary, 0, "Header");

            UIFactory.CreateText(header,
                $"Grade {grade?.gradeNumber ?? 1} - {subject?.displayName ?? ""}",
                56, Color.white, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            var back = UIFactory.CreateIconButton(header, "<", new Color(0, 0, 0, 0.35f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() => GameManager.Instance.UI.Go(UIManager.SceneMainMenu));

            var scroll = UIFactory.CreateScrollView(safe, "LevelScroll");
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0, 0.06f); srt.anchorMax = new Vector2(1, 0.88f);
            srt.offsetMin = new Vector2(24, 0); srt.offsetMax = new Vector2(-24, 0);

            var content = scroll.content;
            Destroy(content.GetComponent<VerticalLayoutGroup>());
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300, 300);
            grid.spacing  = new Vector2(24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.padding = new RectOffset(8, 8, 8, 8);

            if (subject != null)
            {
                foreach (var level in subject.levels)
                    if (level != null) BuildTile(content, subject, level);
            }

            var bottom = UIFactory.CreatePanel(safe,
                new Vector2(0, 0), new Vector2(1, 0.06f),
                new Color(0, 0, 0, 0.35f), 0, "Bottom");
            UIFactory.CreateText(bottom,
                "★ = stars earned    🔒 = locked - beat the previous level to unlock!",
                28, Color.white, TextAlignmentOptions.Center, "Hint");
        }

        private void BuildTile(RectTransform parent, SubjectData subject, LevelData level)
        {
            bool unlocked = GameManager.Instance.Profile.IsUnlocked(level.levelId);
            int stars = GameManager.Instance.Profile.GetStars(level.levelId);

            var card = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                unlocked ? UIFactory.Card : new Color(0.6f, 0.6f, 0.6f, 0.85f),
                28, $"Tile_{level.levelNumber}");

            var col = UIFactory.CreateVerticalLayout(card, 8,
                new RectOffset(16, 16, 16, 16), $"Tile_{level.levelNumber}_Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform,
                $"Level {level.levelNumber}", 48,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Num")
                .fontStyle = FontStyles.Bold;

            UIFactory.CreateText((RectTransform)col.transform,
                level.displayTitle, 28,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Title");

            UIFactory.CreateText((RectTransform)col.transform,
                unlocked ? RenderStars(stars) : "🔒",
                56,
                unlocked ? UIFactory.Accent : UIFactory.TextDark,
                TextAlignmentOptions.Center, "Stars");

            if (unlocked)
            {
                var btn = card.gameObject.AddComponent<Button>();
                var colors = btn.colors;
                colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
                btn.colors = colors;
                btn.onClick.AddListener(() => OnLevelTapped(level));
            }
        }

        private void OnLevelTapped(LevelData level)
        {
            GameManager.Instance.Audio.PlayTap();
            GameManager.Instance.SelectLevel(level.levelNumber);
            GameManager.Instance.UI.Go(UIManager.SceneModeSelect);
        }

        private static string RenderStars(int stars)
        {
            string s = "";
            for (int i = 0; i < 3; i++) s += i < stars ? "★" : "☆";
            return s;
        }
    }
}
