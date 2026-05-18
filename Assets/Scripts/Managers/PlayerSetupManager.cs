// -----------------------------------------------------------------------------
// PlayerSetupManager.cs (fully localized)
// -----------------------------------------------------------------------------
// First-launch screen. Every visible string flows through Localization.T()
// so the experience reads naturally in either English or Arabic the moment
// the player lands here.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using MathEdu.Data;
using MathEdu.UI;
using MathEdu.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Managers
{
    public class PlayerSetupManager : MonoBehaviour
    {
        private PlayerProfile _profile;
        private TMP_InputField _nameInput;
        private AvatarTile _selectedTile;
        private string _selectedAvatarId;
        private int _selectedGrade = 1;
        private readonly List<Button> _gradeButtons = new List<Button>();

        private void Start()
        {
            _ = GameManager.Instance;
            _profile = GameManager.Instance.Profile;
            _selectedAvatarId = _profile.avatarId;
            _selectedGrade    = Mathf.Clamp(_profile.selectedGrade, 1, 3);
            Build();
        }

        private void Build()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[PlayerSetupCanvas]");
            UIFactory.CreateThemedBackground(safe, "setup");

            // Header
            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.90f), new Vector2(1, 1f),
                UIFactory.Primary, 0, "Header");
            UIFactory.CreateText(header, Localization.T("setup.welcome"), 64,
                Color.white, TextAlignmentOptions.Center, "Title")
                .fontStyle = FontStyles.Bold;

            var sub = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.84f), new Vector2(1, 0.90f),
                new Color(0, 0, 0, 0.20f), 0, "Sub");
            UIFactory.CreateText(sub, Localization.T("setup.subtitle"), 32,
                Color.white, TextAlignmentOptions.Center, "SubTxt");

            // Name input
            var nameRow = UIFactory.CreatePanel(safe,
                new Vector2(0.05f, 0.74f), new Vector2(0.95f, 0.83f),
                UIFactory.Card, 24, "NameRow");
            var nameCol = new GameObject("NameCol", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            nameCol.transform.SetParent(nameRow, false);
            var nrt = (RectTransform)nameCol.transform;
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
            nrt.offsetMin = new Vector2(24, 16); nrt.offsetMax = new Vector2(-24, -16);
            var nhl = nameCol.GetComponent<HorizontalLayoutGroup>();
            nhl.spacing = 16; nhl.childForceExpandWidth = false;
            nhl.childAlignment = Localization.IsRTL ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

            var nameLbl = UIFactory.CreateText((RectTransform)nameCol.transform,
                Localization.T("setup.name_label"), 38,
                UIFactory.TextDark,
                Localization.IsRTL ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft,
                "Lbl");
            nameLbl.fontStyle = FontStyles.Bold;
            var nle = nameLbl.gameObject.AddComponent<LayoutElement>();
            nle.preferredWidth = 200;

            _nameInput = UIFactory.CreateInputField((RectTransform)nameCol.transform,
                Localization.T("setup.name_placeholder"), 38, "NameInput");
            _nameInput.text = _profile.playerName == "Player" ? "" : _profile.playerName;
            _nameInput.characterLimit = 16;
            var ile = _nameInput.gameObject.AddComponent<LayoutElement>();
            ile.flexibleWidth = 1;
            ile.preferredHeight = 110;

            // Avatar grid label
            var avLbl = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.68f), new Vector2(1, 0.73f),
                new Color(0, 0, 0, 0.15f), 0, "AvLblHolder");
            UIFactory.CreateText(avLbl, Localization.T("setup.pick_avatar"), 36,
                Color.white, TextAlignmentOptions.Center, "AvLbl")
                .fontStyle = FontStyles.Bold;

            // Avatar grid
            var scroll = UIFactory.CreateScrollView(safe, "AvatarScroll");
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0.02f, 0.30f); srt.anchorMax = new Vector2(0.98f, 0.68f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

            var content = scroll.content;
            DestroyImmediate(content.GetComponent<VerticalLayoutGroup>());
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(260, 320);
            grid.spacing  = new Vector2(20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.padding = new RectOffset(12, 12, 12, 12);

            BuildAvatarGrid(content);

            // Grade selector
            var gradeBar = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.20f), new Vector2(1, 0.29f),
                new Color(0, 0, 0, 0.25f), 0, "GradeBar");
            UIFactory.CreateText(gradeBar, Localization.T("setup.choose_grade"), 32,
                Color.white, TextAlignmentOptions.Top, "GradeLbl");

            var gradeRow = new GameObject("GradeRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            gradeRow.transform.SetParent(gradeBar, false);
            var grt = (RectTransform)gradeRow.transform;
            grt.anchorMin = new Vector2(0, 0); grt.anchorMax = new Vector2(1, 0.7f);
            grt.offsetMin = new Vector2(24, 8); grt.offsetMax = new Vector2(-24, -8);
            var ghl = gradeRow.GetComponent<HorizontalLayoutGroup>();
            ghl.spacing = 18; ghl.childForceExpandWidth = true;
            ghl.childAlignment = TextAnchor.MiddleCenter;

            _gradeButtons.Clear();
            for (int g = 1; g <= 3; g++)
            {
                int captured = g;
                var btn = UIFactory.CreateButton((RectTransform)gradeRow.transform,
                    Localization.T("setup.grade_n", g),
                    g == _selectedGrade ? UIFactory.Accent : UIFactory.Primary,
                    40, $"GradeBtn_{g}");
                btn.onClick.AddListener(() => OnGradePicked(captured));
                _gradeButtons.Add(btn);
            }

            // Start playing
            var startBtn = UIFactory.CreateButton(safe, Localization.T("setup.start_playing"),
                UIFactory.Success, 56, "StartBtn");
            var sbrt = (RectTransform)startBtn.transform;
            sbrt.anchorMin = new Vector2(0.10f, 0.06f); sbrt.anchorMax = new Vector2(0.90f, 0.16f);
            sbrt.offsetMin = Vector2.zero; sbrt.offsetMax = Vector2.zero;
            sbrt.sizeDelta = Vector2.zero;
            startBtn.onClick.AddListener(OnStartPlaying);

            var footer = UIFactory.CreateText(safe, Localization.T("setup.footer"),
                26, new Color(1, 1, 1, 0.7f),
                TextAlignmentOptions.Center, "Footer");
            var frt = footer.rectTransform;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 0.05f);
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        }

        private void BuildAvatarGrid(RectTransform content)
        {
            var library = GameManager.Instance.Avatars;
            if (library == null || library.avatars == null || library.avatars.Count == 0)
            {
                Debug.LogWarning("[PlayerSetup] AvatarLibrary missing - using runtime defaults.");
                library = AvatarLibrary.BuildDefault();
                GameManager.Instance.avatarLibrary = library;
            }

            AvatarTile firstTile = null;
            foreach (var avatar in library.avatars)
            {
                if (avatar == null) continue;
                var tile = AvatarTile.Spawn(content, avatar);
                tile.onSelected += OnAvatarPicked;
                if (firstTile == null) firstTile = tile;
                if (avatar.avatarId == _selectedAvatarId)
                {
                    _selectedTile = tile;
                    tile.SetSelected(true);
                }
            }

            if (_selectedTile == null && firstTile != null)
            {
                _selectedTile = firstTile;
                _selectedAvatarId = firstTile.Avatar.avatarId;
                firstTile.SetSelected(true);
            }
        }

        private void OnAvatarPicked(AvatarData avatar)
        {
            GameManager.Instance.Audio.PlaySFX("tap");
            if (_selectedTile != null) _selectedTile.SetSelected(false);

            foreach (var t in FindObjectsByType<AvatarTile>(FindObjectsSortMode.None))
            {
                if (t.Avatar == avatar)
                {
                    _selectedTile = t;
                    t.SetSelected(true);
                    break;
                }
            }
            _selectedAvatarId = avatar.avatarId;
        }

        private void OnGradePicked(int grade)
        {
            GameManager.Instance.Audio.PlaySFX("tap");
            _selectedGrade = grade;
            for (int i = 0; i < _gradeButtons.Count; i++)
            {
                var btn = _gradeButtons[i];
                if (btn == null) continue;
                var img = btn.GetComponent<Image>();
                img.color = (i + 1 == _selectedGrade) ? UIFactory.Accent : UIFactory.Primary;
            }
        }

        private void OnStartPlaying()
        {
            GameManager.Instance.Audio.PlaySFX("levelComplete");

            string nm = (_nameInput != null ? _nameInput.text : "")?.Trim();
            if (string.IsNullOrEmpty(nm)) nm = "Player";

            if (string.IsNullOrEmpty(_selectedAvatarId)
                || GameManager.Instance.Avatars.FindById(_selectedAvatarId) == null)
            {
                var lib = GameManager.Instance.Avatars;
                if (lib != null && lib.avatars.Count > 0 && lib.avatars[0] != null)
                    _selectedAvatarId = lib.avatars[0].avatarId;
            }

            _profile.playerName    = nm;
            _profile.avatarId      = _selectedAvatarId;
            _profile.selectedGrade = _selectedGrade;
            _profile.setupComplete = true;

            GameManager.Instance.SelectGrade(_selectedGrade);
            GameManager.Instance.SaveProfile();
            GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
        }
    }
}
