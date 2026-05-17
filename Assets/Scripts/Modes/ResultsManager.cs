// -----------------------------------------------------------------------------
// ResultsManager.cs
// -----------------------------------------------------------------------------
// End-of-level celebration screen. Reads its data exclusively from
// GameSession.lastResult (populated by GameplayManagerBase.Finish before the
// scene transition), so the screen renders identically whether the player
// arrived from gameplay, backgrounded the app, or reloaded the scene.
//
// Visual behaviour (per spec):
//   • Three star widgets start at scale 0.
//   • 0.30 s after build: star #1 pops 0 → 1.3 → 1.0 over 0.25 s.
//   • Inter-star delay 0.15 s before the next star animates.
//   • Stars NOT earned remain at scale 0 (visible "empty" placeholder).
//   • Plays "starReveal" SFX on each animated reveal.
//   • Speed Round shows a "Survived X questions" caption instead of "Correct/Total".
//   • "Next Level" enabled only when nextLevelUnlocked && stars > 0.
//   • Empty-state error panel when GameSession.lastResult is missing.
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
                result.failedEarly ? "Run Ended!" : "Level Complete!",
                72, UIFactory.TextDark, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            // ---- Star row (three discrete star Images) ---------------------
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

            // ---- Body text -------------------------------------------------
            if (result.mode == LearningMode.SpeedRound)
            {
                int survived = Mathf.Max(0, result.correct);
                UIFactory.CreateText((RectTransform)col.transform,
                    $"Survived {survived} questions!", 44,
                    UIFactory.TextDark, TextAlignmentOptions.Center, "Survived")
                    .fontStyle = FontStyles.Bold;
                UIFactory.CreateText((RectTransform)col.transform,
                    $"Longest streak: {result.streak}", 32,
                    UIFactory.TextDark, TextAlignmentOptions.Center, "Streak");
            }
            else
            {
                UIFactory.CreateText((RectTransform)col.transform,
                    $"Correct: {result.correct} / {result.total}", 40,
                    UIFactory.TextDark, TextAlignmentOptions.Center, "Correct");
            }

            UIFactory.CreateText((RectTransform)col.transform,
                $"Score {result.score}     +{result.xpEarned} XP",
                40, UIFactory.Primary, TextAlignmentOptions.Center, "Score")
                .fontStyle = FontStyles.Bold;

            // ---- Newly earned badges --------------------------------------
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
                    "🏅 New badge!", 36,
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

            // ---- Action row -----------------------------------------------
            var actions = new GameObject("Actions",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            actions.transform.SetParent(col.transform, false);
            var arr = (RectTransform)actions.transform;
            var hl = actions.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 24; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth = true;
            actions.GetComponent<LayoutElement>().preferredHeight = 160;

            var menuBtn = UIFactory.CreateButton(arr, "Menu",
                new Color(0.5f, 0.5f, 0.6f), 40, "Menu");
            menuBtn.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            });

            var retryBtn = UIFactory.CreateButton(arr, "Retry",
                UIFactory.Primary, 40, "Retry");
            retryBtn.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(SceneForMode(result.mode));
            });

            var nextBtn = UIFactory.CreateButton(arr, "Next Level",
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

            // ---- Audio + VFX celebration ----------------------------------
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

        // -------------------------------------------------------------------
        // Star animation (one Image per slot, scale 0 → 1.3 → 1.0 in sequence)
        // -------------------------------------------------------------------
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

            // Five-pointed star glyph centred inside the circle.
            var glyph = UIFactory.CreateText(rt,
                filled ? "★" : "☆", 130, Color.white,
                TextAlignmentOptions.Center, "Glyph");
            glyph.fontStyle = FontStyles.Bold;

            // Earned stars start invisible — we'll pop them in via coroutine.
            // Empty stars stay at scale 1 so the player can see the gray slot.
            rt.localScale = filled ? Vector3.zero : Vector3.one;
            return rt;
        }

        private IEnumerator AnimateStars(RectTransform[] stars, int earned)
        {
            yield return new WaitForSeconds(0.30f);
            for (int i = 0; i < stars.Length; i++)
            {
                if (i >= earned) yield break; // stop — unearned stars stay at scale 0
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
                // 0 → 1.3 over first 70%, then 1.3 → 1.0 over last 30%.
                float s = k < 0.7f
                    ? Mathf.Lerp(0f, 1.3f, k / 0.7f)
                    : Mathf.Lerp(1.3f, 1.0f, (k - 0.7f) / 0.3f);
                star.localScale = Vector3.one * s;
                yield return null;
            }
            star.localScale = Vector3.one;
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------
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
                "😅 Oops!", 84,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;
            UIFactory.CreateText((RectTransform)col.transform,
                "We couldn't load this level's results.\nLet's head back to the menu.", 36,
                UIFactory.TextDark, TextAlignmentOptions.Center, "Body");

            var back = UIFactory.CreateButton((RectTransform)col.transform,
                "Back to Menu", UIFactory.Primary, 40, "Back");
            back.gameObject.AddComponent<LayoutElement>().preferredHeight = 140;
            back.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            });
        }
    }
}
