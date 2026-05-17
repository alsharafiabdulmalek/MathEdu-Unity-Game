// -----------------------------------------------------------------------------
// ResultsManager.cs
// -----------------------------------------------------------------------------
// End-of-level celebration screen. Animates an incremental star count from 0
// to earned, shows the final score, fires a VFX burst per star, and offers
// retry / next-level / menu CTAs.
// -----------------------------------------------------------------------------

using System.Collections;
using MathEdu.Data;
using MathEdu.Managers;
using MathEdu.UI;
using TMPro;
using UnityEngine;

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
            var level   = GameManager.Instance.CurrentLevel;
            if (level == null) return;
            int total = session.correctCount + session.wrongCount;
            int stars = level.ComputeStars(session.correctCount, total);

            var card = UIFactory.CreatePanel(safe,
                new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.82f),
                UIFactory.Card, 32, "Card");

            var col = UIFactory.CreateVerticalLayout(card, 24,
                new RectOffset(32, 32, 32, 32), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)col.transform, "Level Complete!",
                72, UIFactory.TextDark, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            var stars3 = StarRating.Spawn((RectTransform)col.transform, 0, 120);
            StartCoroutine(AnimateStars(stars3, stars));

            UIFactory.CreateText((RectTransform)col.transform,
                $"Correct: {session.correctCount}/{total}",
                40, UIFactory.TextDark, TextAlignmentOptions.Center, "Correct");
            UIFactory.CreateText((RectTransform)col.transform,
                $"Score: {session.score}",
                40, UIFactory.Primary, TextAlignmentOptions.Center, "Score")
                .fontStyle = FontStyles.Bold;

            var actions = new GameObject("Actions", typeof(RectTransform));
            actions.transform.SetParent(col.transform, false);
            var arr = (RectTransform)actions.transform;
            arr.sizeDelta = new Vector2(0, 160);
            var hl = actions.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hl.spacing = 24; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childForceExpandWidth = true;

            var menuBtn = UIFactory.CreateButton(arr, "Menu",
                new Color(0.5f, 0.5f, 0.6f), 40, "Menu");
            menuBtn.onClick.AddListener(() => GameManager.Instance.UI.Go(UIManager.SceneMainMenu));

            var retryBtn = UIFactory.CreateButton(arr, "Retry",
                UIFactory.Primary, 40, "Retry");
            retryBtn.onClick.AddListener(() => GameManager.Instance.UI.Go(SceneForMode()));

            var nextBtn = UIFactory.CreateButton(arr, "Next",
                UIFactory.Success, 40, "Next");
            nextBtn.onClick.AddListener(NextLevel);

            // Audio + VFX celebration
            if (stars > 0)
            {
                GameManager.Instance.Audio.PlayWin();
                GameManager.Instance.VFX?.PlayWin();
            }
            else
            {
                GameManager.Instance.Audio.PlayLose();
                GameManager.Instance.VFX?.PlayLose();
            }
        }

        private IEnumerator AnimateStars(StarRating widget, int stars)
        {
            for (int i = 0; i <= stars; i++)
            {
                widget.SetStars(i);
                if (i > 0) GameManager.Instance.VFX?.PlayStar();
                yield return new WaitForSeconds(0.35f);
            }
        }

        private string SceneForMode()
        {
            return GameManager.Instance.Session.selectedMode switch
            {
                LearningMode.Learn      => UIManager.SceneLearn,
                LearningMode.Practice   => UIManager.ScenePractice,
                LearningMode.Quiz       => UIManager.SceneQuiz,
                LearningMode.Story      => UIManager.SceneStory,
                LearningMode.SpeedRound => UIManager.SceneSpeed,
                _                       => UIManager.SceneMainMenu
            };
        }

        private void NextLevel()
        {
            var subject = GameManager.Instance.CurrentSubject;
            int current = GameManager.Instance.Session.selectedLevel;
            if (subject != null && current < subject.levels.Count)
            {
                GameManager.Instance.SelectLevel(current + 1);
                GameManager.Instance.UI.Go(SceneForMode());
            }
            else
            {
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            }
        }
    }
}
