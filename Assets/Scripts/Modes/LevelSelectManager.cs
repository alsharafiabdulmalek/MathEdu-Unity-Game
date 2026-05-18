// -----------------------------------------------------------------------------
// LevelSelectManager.cs (localized)
// -----------------------------------------------------------------------------
// 20-tile level grid for the current (grade, subject). Titles, locked/unlocked
// labels, and footer hint all flow through Localization.T().
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Managers;
using MathEdu.UI;
using MathEdu.Utility;
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
                subject != null
                    ? Localization.T("levelsel.header",
                        grade?.gradeNumber ?? 1,
                        MainMenuManager.SubjectName(subject.subject))
                    : Localization.T("levelsel.title_no_subject"),
                56, Color.white, TextAlignmentOptions.Center, "Title")
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

            var scroll = UIFactory.CreateScrollView(safe, "LevelScroll");
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0, 0.06f); srt.anchorMax = new Vector2(1, 0.88f);
            srt.offsetMin = new Vector2(24, 0); srt.offsetMax = new Vector2(-24, 0);

            var content = scroll.content;
            DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300, 320);
            grid.spacing  = new Vector2(24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.padding = new RectOffset(8, 8, 8, 8);

            if (subject != null && subject.levels != null)
            {
                foreach (var level in subject.levels)
                    if (level != null) BuildTile(content, subject, level);
            }
            else
            {
                UIFactory.CreateText(content, Localization.T("levelsel.empty"),
                    36, Color.white, TextAlignmentOptions.Center, "Empty");
            }

            var bottom = UIFactory.CreatePanel(safe,
                new Vector2(0, 0), new Vector2(1, 0.06f),
                new Color(0, 0, 0, 0.35f), 0, "Bottom");
            UIFactory.CreateText(bottom, Localization.T("levelsel.hint"),
                26, Color.white, TextAlignmentOptions.Center, "Hint");
        }

        private void BuildTile(RectTransform parent, SubjectData subject, LevelData level)
        {
            bool unlocked = GameManager.Instance.Progress.IsLevelUnlocked(subject, level.levelNumber);
            int stars = GameManager.Instance.Profile.GetStars(level.levelId);

            var card = UIFactory.CreatePanel(parent, Vector2.zero, Vector2.one,
                unlocked ? UIFactory.Card : new Color(0.55f, 0.55f, 0.6f, 0.85f),
                28, $"Tile_{level.levelNumber}");

            var col = UIFactory.CreateVerticalLayout(card, 6,
                new RectOffset(16, 16, 14, 14), $"Tile_{level.levelNumber}_Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform,
                Localization.T("levelsel.level_n", level.levelNumber), 52,
                unlocked ? UIFactory.TextDark : Color.white,
                TextAlignmentOptions.Center, "Num")
                .fontStyle = FontStyles.Bold;

            UIFactory.CreateText((RectTransform)col.transform,
                level.displayTitle, 26,
                unlocked ? UIFactory.TextDark : new Color(1, 1, 1, 0.85f),
                TextAlignmentOptions.Center, "Title");

            if (unlocked)
            {
                BuildStarRow((RectTransform)col.transform, stars);
            }
            else
            {
                BuildLockBadge((RectTransform)col.transform);
            }

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
            GameManager.Instance.Audio.PlaySFX("tap");
            GameManager.Instance.SelectLevel(level.levelNumber);
            GameManager.Instance.UI.Go(UIManager.SceneModeSelect);
        }

        private static string RenderStars(int stars)
        {
            string s = "";
            for (int i = 0; i < 3; i++) s += i < stars ? "★" : "☆";
            return s;
        }

        /// <summary>
        /// Render a row of three star icons, filled or hollow according to
        /// <paramref name="stars"/>. Uses the IconLibrary's star sprite if
        /// available, otherwise the procedural PolishSprites.Star.
        /// </summary>
        private static void BuildStarRow(RectTransform parent, int stars)
        {
            var row = new GameObject("Stars",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 8; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth = false; hl.childForceExpandHeight = true;
            row.GetComponent<LayoutElement>().preferredHeight = 72;

            var sprite = IconService.Get("star");
            for (int i = 0; i < 3; i++)
            {
                bool filled = i < stars;
                var go = new GameObject($"S_{i}", typeof(Image), typeof(LayoutElement));
                go.transform.SetParent(row.transform, false);
                var img = go.GetComponent<Image>();
                img.sprite = sprite != null ? sprite : PolishSprites.Star();
                img.preserveAspect = true;
                img.color = filled
                    ? new Color(1.00f, 0.78f, 0.20f, 1f)
                    : new Color(0.60f, 0.60f, 0.60f, 0.40f);
                img.raycastTarget = false;
                var le = go.GetComponent<LayoutElement>();
                le.preferredWidth = 60; le.preferredHeight = 60;
            }
        }

        private static void BuildLockBadge(RectTransform parent)
        {
            var sprite = IconService.Get("lock");
            var go = new GameObject("Lock", typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 80;

            if (sprite != null)
            {
                var imgGo = new GameObject("LockIcon", typeof(Image));
                imgGo.transform.SetParent(go.transform, false);
                var rt = (RectTransform)imgGo.transform;
                rt.anchorMin = new Vector2(0.30f, 0); rt.anchorMax = new Vector2(0.70f, 1);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var img = imgGo.GetComponent<Image>();
                img.sprite = sprite;
                img.color  = Color.white;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            else
            {
                UIFactory.CreateText((RectTransform)go.transform, "🔒", 70, Color.white,
                    TextAlignmentOptions.Center, "Glyph").fontStyle = FontStyles.Bold;
            }
        }
    }
}
