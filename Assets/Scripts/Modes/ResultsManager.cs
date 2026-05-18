// -----------------------------------------------------------------------------
// ResultsManager.cs (localized)
// -----------------------------------------------------------------------------
// End-of-level celebration. All strings flow through Localization.T() so the
// player sees a fully Arabic results screen when language=ar, including
// 'Level Complete!', 'Score X +Y XP', 'Correct: A / B', 'Survived N questions',
// new-badge labels, action buttons, and the empty-state error panel.
// -----------------------------------------------------------------------------

using System.Collections;
using MathEdu.Data;
using MathEdu.Managers;
using MathEdu.UI;
using MathEdu.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Modes
{
    public class ResultsManager : MonoBehaviour
    {
        private void Start()
        {
            _ = GameManager.Instance;
            Build();
        }

        private void Build()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[ResultsCanvas]");
            UIFactory.CreateThemedBackground(safe, "results");

            var session = GameManager.Instance.Session;
            var result  = session?.lastResult;
            if (result == null)
            {
                BuildErrorPanel(safe);
                return;
            }

            var card = UIFactory.CreatePanel(safe,
                new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f),
                UIFactory.Card, 32, "Card");

            var col = UIFactory.CreateVerticalLayout(card, 18,
                new RectOffset(36, 36, 36, 36), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform,
                Localization.T(result.failedEarly ? "results.title_lose" : "results.title_win"),
                72, UIFactory.TextDark, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            var starRow = new GameObject("StarRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            starRow.transform.SetParent(col.transform, false);
            var srt = (RectTransform)starRow.transform;
            var shl = starRow.GetComponent<HorizontalLayoutGroup>();
            shl.spacing = 24;
            shl.padding = new RectOffset(0, 0, 8, 8);
            shl.childForceExpandWidth = false;
            shl.childAlignment = TextAnchor.MiddleCenter;
            starRow.GetComponent<LayoutElement>().preferredHeight = 240;

            var starWidgets = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                starWidgets[i] = BuildStar((RectTransform)starRow.transform, i < result.stars);
            }
            StartCoroutine(AnimateStars(starWidgets, result.stars));

            if (result.mode == LearningMode.SpeedRound)
            {
                int survived = Mathf.Max(0, result.correct);
                UIFactory.CreateText((RectTransform)col.transform,
                    Localization.T("results.survived_format", survived), 44,
                    UIFactory.TextDark, TextAlignmentOptions.Center, "Survived")
                    .fontStyle = FontStyles.Bold;
                UIFactory.CreateText((RectTransform)col.transform,
                    Localization.T("results.streak_format", result.streak), 32,
                    UIFactory.TextDark, TextAlignmentOptions.Center, "Streak");
            }
            else
            {
                UIFactory.CreateText((RectTransform)col.transform,
                    Localization.T("results.correct_format", result.correct, result.total), 40,
                    UIFactory.TextDark, TextAlignmentOptions.Center, "Correct");
            }

            UIFactory.CreateText((RectTransform)col.transform,
                Localization.T("results.score_xp_format", result.score, result.xpEarned),
                40, UIFactory.Primary, TextAlignmentOptions.Center, "Score")
                .fontStyle = FontStyles.Bold;

            if (result.newBadges != null && result.newBadges.Length > 0)
            {
                var badgePanel = UIFactory.CreatePanel((RectTransform)col.transform,
                    Vector2.zero, Vector2.one,
                    new Color(0.95f, 0.85f, 0.30f, 0.20f), 18, "BadgePanel");
                var be = badgePanel.gameObject.AddComponent<LayoutElement>();
                be.preferredHeight = 110 + 50 * result.newBadges.Length;
                be.minHeight = be.preferredHeight;
                var bcol = UIFactory.CreateVerticalLayout(badgePanel, 6,
                    new RectOffset(16, 16, 12, 12), "BCol");
                var bcrt = (RectTransform)bcol.transform;
                bcrt.anchorMin = Vector2.zero; bcrt.anchorMax = Vector2.one;
                bcrt.offsetMin = Vector2.zero; bcrt.offsetMax = Vector2.zero;
                UIFactory.CreateText((RectTransform)bcol.transform,
                    Localization.T("results.new_badge_label"), 36,
                    UIFactory.TextDark, TextAlignmentOptions.Center, "BLbl")
                    .fontStyle = FontStyles.Bold;
                foreach (var id in result.newBadges)
                {
                    UIFactory.CreateText((RectTransform)bcol.transform,
                        ProgressManager.PrettyBadgeName(id), 32,
                        UIFactory.TextDark, TextAlignmentOptions.Center, "B_" + id);
                }
                GameManager.Instance.Audio.PlaySFX("badgeUnlocked");
                HapticManager.Medium();
            }

            var actions = new GameObject("Actions",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            actions.transform.SetParent(col.transform, false);
            var arr = (RectTransform)actions.transform;
            var hl = actions.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 24; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth = true;
            actions.GetComponent<LayoutElement>().preferredHeight = 160;

            var menuBtn = UIFactory.CreateButton(arr, Localization.T("results.menu"),
                new Color(0.5f, 0.5f, 0.6f), 40, "Menu");
            menuBtn.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            });

            var retryBtn = UIFactory.CreateButton(arr, Localization.T("results.retry"),
                UIFactory.Primary, 40, "Retry");
            retryBtn.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(SceneForMode(result.mode));
            });

            var nextBtn = UIFactory.CreateButton(arr, Localization.T("results.next_level"),
                UIFactory.Success, 40, "Next");
            bool nextEnabled = result.nextLevelUnlocked && result.stars > 0;
            nextBtn.interactable = nextEnabled;
            var nextImg = nextBtn.GetComponent<Image>();
            if (!nextEnabled) nextImg.color = new Color(0.6f, 0.7f, 0.6f, 0.6f);
            nextBtn.onClick.AddListener(() =>
            {
                if (!nextEnabled) return;
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.SelectLevel(result.levelNumber + 1);
                GameManager.Instance.UI.Go(SceneForMode(result.mode));
            });

            if (result.stars > 0)
            {
                GameManager.Instance.VFX?.PlayWin();
                HapticManager.Medium();
            }
            else
            {
                GameManager.Instance.Audio.PlaySFX("lose");
                GameManager.Instance.VFX?.PlayLose();
            }
        }

        private RectTransform BuildStar(RectTransform parent, bool filled)
        {
            var go = new GameObject(filled ? "Star_filled" : "Star_empty",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(180, 180);
            go.GetComponent<LayoutElement>().preferredWidth = 180;
            go.GetComponent<LayoutElement>().preferredHeight = 180;

            var img = go.GetComponent<Image>();
            img.sprite = DefaultSprite.Circle();
            img.color  = filled
                ? new Color(0.95f, 0.55f, 0.20f, 1f)
                : new Color(0.7f, 0.7f, 0.7f, 0.35f);

            var glyph = UIFactory.CreateText(rt,
                filled ? "★" : "☆", 130, Color.white,
                TextAlignmentOptions.Center, "Glyph");
            glyph.fontStyle = FontStyles.Bold;

            rt.localScale = filled ? Vector3.zero : Vector3.one;
            return rt;
        }

        private IEnumerator AnimateStars(RectTransform[] stars, int earned)
        {
            yield return new WaitForSeconds(0.30f);
            for (int i = 0; i < stars.Length; i++)
            {
                if (i >= earned) yield break;
                yield return PopStar(stars[i]);
                yield return new WaitForSeconds(0.15f);
            }
        }

        private IEnumerator PopStar(RectTransform star)
        {
            GameManager.Instance.Audio.PlaySFX("starReveal");
            GameManager.Instance.VFX?.PlayStar();
            float t = 0f;
            const float dur = 0.25f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                float s = k < 0.7f
                    ? Mathf.Lerp(0f, 1.3f, k / 0.7f)
                    : Mathf.Lerp(1.3f, 1.0f, (k - 0.7f) / 0.3f);
                star.localScale = Vector3.one * s;
                yield return null;
            }
            star.localScale = Vector3.one;
        }

        private string SceneForMode(LearningMode mode)
        {
            return mode switch
            {
                LearningMode.Learn      => UIManager.SceneLearn,
                LearningMode.Practice   => UIManager.ScenePractice,
                LearningMode.Quiz       => UIManager.SceneQuiz,
                LearningMode.Story      => UIManager.SceneStory,
                LearningMode.SpeedRound => UIManager.SceneSpeed,
                _                       => UIManager.SceneMainMenu
            };
        }

        private void BuildErrorPanel(RectTransform safe)
        {
            var card = UIFactory.CreatePanel(safe,
                new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.70f),
                UIFactory.Card, 28, "ErrorCard");
            var col = UIFactory.CreateVerticalLayout(card, 24,
                new RectOffset(32, 32, 32, 32), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform,
                Localization.T("results.error_title"), 84,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;
            UIFactory.CreateText((RectTransform)col.transform,
                Localization.T("results.error_body"), 36,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Body");

            var back = UIFactory.CreateButton((RectTransform)col.transform,
                Localization.T("results.back_to_menu"), UIFactory.Primary, 40, "Back");
            back.gameObject.AddComponent<LayoutElement>().preferredHeight = 140;
            back.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            });
        }
    }
}
