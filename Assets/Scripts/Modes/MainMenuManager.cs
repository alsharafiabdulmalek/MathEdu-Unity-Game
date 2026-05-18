// -----------------------------------------------------------------------------
// MainMenuManager.cs (fully localized)
// -----------------------------------------------------------------------------
// Builds the Main Menu screen. Every visible string flows through
// Localization.T(). Subject card names use SubjectName() which maps each
// MathSubject enum value to its localized string key.
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
    public class MainMenuManager : MonoBehaviour
    {
        private RectTransform _safeArea;
        private RectTransform _subjectGridParent;
        private TextMeshProUGUI _xpLabel;
        private TextMeshProUGUI _starsLabel;
        private TextMeshProUGUI _badgesLabel;
        private TextMeshProUGUI _titleLabel;

        private int _selectedGrade = 1;

        public static string SubjectName(MathSubject s) => s switch
        {
            MathSubject.Counting       => Localization.T("subj.counting"),
            MathSubject.Addition       => Localization.T("subj.addition"),
            MathSubject.Subtraction    => Localization.T("subj.subtraction"),
            MathSubject.Multiplication => Localization.T("subj.multiplication"),
            MathSubject.Division       => Localization.T("subj.division"),
            MathSubject.Shapes         => Localization.T("subj.shapes"),
            MathSubject.Patterns       => Localization.T("subj.patterns"),
            MathSubject.Fractions      => Localization.T("subj.fractions"),
            MathSubject.Measurement    => Localization.T("subj.measurement"),
            MathSubject.Time           => Localization.T("subj.time"),
            MathSubject.Money          => Localization.T("subj.money"),
            _                          => s.ToString()
        };

        private void Start()
        {
            var gm = GameManager.Instance;
            _selectedGrade = gm.Profile.selectedGrade > 0 ? gm.Profile.selectedGrade : 1;
            Build();
        }

        private void Build()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[MainMenuCanvas]");
            _safeArea = safe;
            UIFactory.CreateThemedBackground(safe, "menu");

            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.86f), new Vector2(1, 1f),
                new Color(0, 0, 0, 0.25f), 0, "Header");

            BuildAvatarMini(header);

            var profile = GameManager.Instance.Profile;
            var nameLbl = UIFactory.CreateText(header,
                Localization.T("menu.hi", profile.playerName), 38,
                Color.white,
                Localization.IsRTL ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft,
                "Welcome");
            nameLbl.fontStyle = FontStyles.Bold;
            var nrt = nameLbl.rectTransform;
            nrt.anchorMin = new Vector2(0, 0); nrt.anchorMax = new Vector2(0.65f, 0.55f);
            nrt.offsetMin = new Vector2(200, 0); nrt.offsetMax = new Vector2(0, 0);

            _titleLabel = UIFactory.CreateText(header, Localization.T("menu.title"), 56,
                Color.white,
                Localization.IsRTL ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft,
                "Title");
            _titleLabel.fontStyle = FontStyles.Bold;
            var trt = _titleLabel.rectTransform;
            trt.anchorMin = new Vector2(0, 0.55f); trt.anchorMax = new Vector2(0.65f, 1);
            trt.offsetMin = new Vector2(200, 0); trt.offsetMax = new Vector2(0, 0);

            var statsHolder = new GameObject("Stats", typeof(RectTransform));
            statsHolder.transform.SetParent(header, false);
            var sh = (RectTransform)statsHolder.transform;
            sh.anchorMin = new Vector2(0.55f, 0);
            sh.anchorMax = new Vector2(1, 1f);
            sh.offsetMin = new Vector2(0, 0); sh.offsetMax = new Vector2(-24, 0);
            var hl = statsHolder.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleRight;
            hl.spacing = 18;
            hl.childForceExpandWidth = false;

            int badgeCount = profile.badges != null ? profile.badges.Count : 0;
            _badgesLabel = UIFactory.CreateText(sh, $"🏅 {badgeCount}",
                34, Color.white, TextAlignmentOptions.MidlineRight, "BadgesLabel");
            _starsLabel = UIFactory.CreateText(sh, $"★ {profile.totalStars}",
                34, UIFactory.Accent, TextAlignmentOptions.MidlineRight, "StarsLabel");
            _xpLabel    = UIFactory.CreateText(sh, $"XP {profile.xp}",
                34, Color.white, TextAlignmentOptions.MidlineRight, "XPLabel");

            var gradeStrip = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.78f), new Vector2(1, 0.86f),
                new Color(0, 0, 0, 0.20f), 0, "GradeStrip");

            var gradeLayout = UIFactory.CreateHorizontalLayout(gradeStrip, 24,
                new RectOffset(48, 48, 16, 16), "GradeLayout");
            ((RectTransform)gradeLayout.transform).anchorMin = Vector2.zero;
            ((RectTransform)gradeLayout.transform).anchorMax = Vector2.one;
            ((RectTransform)gradeLayout.transform).offsetMin = Vector2.zero;
            ((RectTransform)gradeLayout.transform).offsetMax = Vector2.zero;

            UIFactory.CreateText((RectTransform)gradeLayout.transform,
                Localization.T("menu.choose_grade"),
                40, Color.white,
                Localization.IsRTL ? TextAlignmentOptions.Right : TextAlignmentOptions.Left,
                "GradeLabel");

            for (int g = 1; g <= 3; g++)
            {
                int captured = g;
                var btn = UIFactory.CreateButton((RectTransform)gradeLayout.transform,
                    Localization.T("setup.grade_n", g),
                    g == _selectedGrade ? UIFactory.Accent : UIFactory.Primary,
                    44, $"GradeBtn_{g}");
                btn.onClick.AddListener(() => OnGradeSelected(captured));
                var le = btn.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 240; le.preferredHeight = 120;
            }

            var gridScroll = UIFactory.CreateScrollView(safe, "SubjectScroll");
            var grt = (RectTransform)gridScroll.transform;
            grt.anchorMin = new Vector2(0, 0.12f); grt.anchorMax = new Vector2(1, 0.78f);
            grt.offsetMin = new Vector2(24, 0); grt.offsetMax = new Vector2(-24, 0);

            _subjectGridParent = gridScroll.content;
            DestroyImmediate(_subjectGridParent.GetComponent<VerticalLayoutGroup>());
            var grid = _subjectGridParent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(460, 320);
            grid.spacing  = new Vector2(24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.childAlignment = TextAnchor.UpperCenter;

            RebuildSubjectGrid();

            var bottom = UIFactory.CreatePanel(safe,
                new Vector2(0, 0), new Vector2(1, 0.12f),
                new Color(0, 0, 0, 0.30f), 0, "Bottom");

            var hLayout = UIFactory.CreateHorizontalLayout(bottom, 24,
                new RectOffset(32, 32, 16, 16), "BottomLayout");
            ((RectTransform)hLayout.transform).anchorMin = Vector2.zero;
            ((RectTransform)hLayout.transform).anchorMax = Vector2.one;
            ((RectTransform)hLayout.transform).offsetMin = Vector2.zero;
            ((RectTransform)hLayout.transform).offsetMax = Vector2.zero;

            var continueBtn = UIFactory.CreateButton((RectTransform)hLayout.transform,
                Localization.T("menu.continue"), UIFactory.Success, 48, "ContinueBtn");
            continueBtn.onClick.AddListener(OnContinue);

            // Settings & Parental Dashboard entry points — try a real GUI Pro
            // icon first, fall back to a unicode glyph so the buttons are
            // always recognisable even without the polish assets built.
            var settingsBtn = IconService.IconButton((RectTransform)hLayout.transform,
                "gear", "⚙", new Color(0.30f, 0.35f, 0.45f), "SettingsBtn");
            settingsBtn.onClick.AddListener(OnSettings);

            var parentBtn = IconService.IconButton((RectTransform)hLayout.transform,
                "parent", "👪", new Color(0.50f, 0.30f, 0.55f), "ParentBtn");
            parentBtn.onClick.AddListener(OnParent);
        }

        private void BuildAvatarMini(RectTransform header)
        {
            var avatarMini = new GameObject("AvatarMini", typeof(Image));
            avatarMini.transform.SetParent(header, false);
            var amrt = (RectTransform)avatarMini.transform;
            amrt.anchorMin = new Vector2(0, 0); amrt.anchorMax = new Vector2(0, 1);
            amrt.pivot = new Vector2(0, 0.5f);
            amrt.sizeDelta = new Vector2(160, 0);
            amrt.anchoredPosition = new Vector2(24, 0);
            var amImg = avatarMini.GetComponent<Image>();
            amImg.sprite = DefaultSprite.Circle();

            var profile = GameManager.Instance.Profile;
            var avatar  = GameManager.Instance.Avatars?.FindById(profile.avatarId);

            if (avatar == null)
            {
                var lib = GameManager.Instance.Avatars;
                if (lib != null && lib.avatars.Count > 0) avatar = lib.avatars[0];
            }

            amImg.color  = avatar?.tint ?? UIFactory.Accent;
            if (avatar != null && avatar.sprite != null)
            {
                amImg.sprite = avatar.sprite;
                amImg.color  = Color.white;
                amImg.preserveAspect = true;
            }
            else if (avatar != null)
            {
                var em = UIFactory.CreateText(amrt, avatar.emoji, 80, Color.white,
                    TextAlignmentOptions.Center, "Em");
                em.fontStyle = FontStyles.Bold;
            }
        }

        private void RebuildSubjectGrid()
        {
            for (int i = _subjectGridParent.childCount - 1; i >= 0; i--)
                Destroy(_subjectGridParent.GetChild(i).gameObject);

            var grade = GameManager.Instance.database?.GetGrade(_selectedGrade);
            if (grade == null) return;

            foreach (var subject in grade.subjects)
            {
                if (subject != null) BuildSubjectCard(subject);
            }
        }

        private void BuildSubjectCard(SubjectData subject)
        {
            var card = UIFactory.CreatePanel(_subjectGridParent,
                Vector2.zero, Vector2.one, UIFactory.Card, 32, $"Card_{subject.subject}");
            card.GetComponent<Image>().color = subject.themeColor;

            var col = new GameObject("Col", typeof(RectTransform), typeof(VerticalLayoutGroup));
            col.transform.SetParent(card, false);
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var v = col.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(20, 20, 14, 14);
            v.spacing = 8;
            v.childForceExpandWidth = true;
            v.childAlignment = TextAnchor.UpperCenter;

            var emoji = UIFactory.CreateText((RectTransform)col.transform,
                subject.iconEmoji, 70, Color.white,
                TextAlignmentOptions.Center, "Emoji");
            emoji.fontStyle = FontStyles.Bold;
            emoji.gameObject.AddComponent<LayoutElement>().preferredHeight = 90;

            var name = UIFactory.CreateText((RectTransform)col.transform,
                SubjectName(subject.subject), 44, Color.white,
                TextAlignmentOptions.Center, "Name");
            name.fontStyle = FontStyles.Bold;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;

            int stars       = GameManager.Instance.Progress.StarsForSubject(subject);
            int highest     = GameManager.Instance.Progress.HighestLevelReached(subject);
            int totalLevels = subject.levels != null ? Mathf.Max(1, subject.levels.Count) : 20;
            bool started    = stars > 0 || highest > 1 || HasAnyProgress(subject);

            var progRow = new GameObject("ProgRow",
                typeof(RectTransform), typeof(LayoutElement));
            progRow.transform.SetParent(col.transform, false);
            progRow.GetComponent<LayoutElement>().preferredHeight = 28;
            var prt = (RectTransform)progRow.transform;
            prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(1, 1);
            var bar = ProgressBar.Spawn(prt, 22,
                new Color(0, 0, 0, 0.30f), new Color(1f, 1f, 1f, 0.85f));
            bar.SetValue((float)highest / totalLevels);
            var brt = (RectTransform)bar.transform;
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

            string lvlText = started
                ? Localization.T("menu.level_progress", highest, totalLevels, stars)
                : Localization.T("menu.tap_to_start");
            var sub = UIFactory.CreateText((RectTransform)col.transform,
                lvlText, 28,
                started ? new Color(1, 1, 1, 0.95f) : new Color(1, 1, 1, 0.60f),
                TextAlignmentOptions.Center, "LvlText");
            sub.fontStyle = started ? FontStyles.Bold : FontStyles.Italic;

            var btn = card.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1);
            btn.colors = colors;
            var capturedSubject = subject;
            btn.onClick.AddListener(() => OnSubjectSelected(capturedSubject));
        }

        private bool HasAnyProgress(SubjectData subject)
        {
            if (subject == null) return false;
            var profile = GameManager.Instance.Profile;
            if (profile == null) return false;
            foreach (var lv in subject.levels)
            {
                if (lv == null) continue;
                var p = profile.GetOrCreate(lv.levelId);
                if (p.timesPlayed > 0) return true;
            }
            return false;
        }

        private void OnGradeSelected(int g)
        {
            GameManager.Instance.Audio.PlaySFX("tap");
            _selectedGrade = g;
            GameManager.Instance.SelectGrade(g);
            GameManager.Instance.SaveProfile();

            var oldCanvas = GameObject.Find("[MainMenuCanvas]");
            if (oldCanvas != null) DestroyImmediate(oldCanvas);
            Build();
        }

        private void OnSubjectSelected(SubjectData subject)
        {
            GameManager.Instance.Audio.PlaySFX("tap");
            GameManager.Instance.SelectSubject(subject.subject);
            GameManager.Instance.UI.Go(UIManager.SceneLevelSelect);
        }

        private void OnContinue()
        {
            GameManager.Instance.Audio.PlaySFX("tap");
            var grade = GameManager.Instance.database?.GetGrade(_selectedGrade);
            if (grade != null && grade.subjects.Count > 0)
            {
                GameManager.Instance.SelectSubject(grade.subjects[0].subject);
                GameManager.Instance.UI.Go(UIManager.SceneLevelSelect);
            }
        }

        private void OnSettings()
        {
            GameManager.Instance.Audio.PlaySFX("tap");
            GameManager.Instance.UI.Go(UIManager.SceneSettings);
        }

        private void OnParent()
        {
            GameManager.Instance.Audio.PlaySFX("tap");
            GameManager.Instance.UI.Go(UIManager.SceneParentalDashboard);
        }
    }
}
