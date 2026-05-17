// -----------------------------------------------------------------------------
// SettingsManager.cs
// -----------------------------------------------------------------------------
// Builds the Settings scene at runtime:
//   • Music ON/OFF toggle + volume slider
//   • SFX ON/OFF toggle + volume slider
//   • Haptics toggle
//   • Language placeholder dropdown (English by default)
//   • Reset progress button (PIN-gated)
//   • Back button → Main Menu
//
// All changes are written straight to PlayerProfile and flushed to JSON via
// GameManager.SaveProfile. AudioManager picks up the new volumes via
// ApplyVolumeFromProfile().
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.UI;
using MathEdu.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathEdu.Managers
{
    public class SettingsManager : MonoBehaviour
    {
        private PlayerProfile _profile;
        private Slider _musicSlider;
        private Slider _sfxSlider;
        private ToggleSwitch _musicToggle;
        private ToggleSwitch _sfxToggle;
        private ToggleSwitch _hapticsToggle;

        private void Start()
        {
            _ = GameManager.Instance;
            _profile = GameManager.Instance.Profile;
            Build();
        }

        // -------------------------------------------------------------------
        // UI construction
        // -------------------------------------------------------------------
        private void Build()
        {
            var (canvas, safe) = UIFactory.CreateCanvas("[SettingsCanvas]");
            UIFactory.CreateThemedBackground(safe, "settings");

            // Header
            var header = UIFactory.CreatePanel(safe,
                new Vector2(0, 0.88f), new Vector2(1, 1f),
                UIFactory.Primary, 0, "Header");

            UIFactory.CreateText(header, "Settings", 64, Color.white,
                TextAlignmentOptions.Center, "Title").fontStyle = FontStyles.Bold;

            var back = UIFactory.CreateIconButton(header, "<", new Color(0, 0, 0, 0.35f), "Back");
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(80, 0);
            brt.sizeDelta = new Vector2(110, 110);
            back.onClick.AddListener(() =>
            {
                GameManager.Instance.Audio.PlayTap();
                GameManager.Instance.SaveProfile();
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            });

            // Body
            var card = UIFactory.CreatePanel(safe,
                new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.86f),
                UIFactory.Card, 28, "Card");

            var col = UIFactory.CreateVerticalLayout(card, 28,
                new RectOffset(32, 32, 32, 32), "Col");
            var crt = (RectTransform)col.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;

            // ----- Music -----
            BuildSection((RectTransform)col.transform, "🎵  Music",
                _profile.musicOn, _profile.musicVolume,
                onToggle: v => { _profile.musicOn = v; ApplyVolumes(); },
                onSlider: v => { _profile.musicVolume = v; ApplyVolumes(); },
                out _musicToggle, out _musicSlider);

            // ----- SFX -----
            BuildSection((RectTransform)col.transform, "🔊  Sound Effects",
                _profile.sfxOn, _profile.sfxVolume,
                onToggle: v => { _profile.sfxOn = v; ApplyVolumes(); GameManager.Instance.Audio.PlayTap(); },
                onSlider: v => { _profile.sfxVolume = v; ApplyVolumes(); },
                out _sfxToggle, out _sfxSlider);

            // ----- Haptics -----
            BuildToggleRow((RectTransform)col.transform, "📳  Haptics", _profile.hapticsOn,
                v => _profile.hapticsOn = v, out _hapticsToggle);

            // ----- Language placeholder -----
            BuildLanguageRow((RectTransform)col.transform);

            // ----- Reset Progress -----
            var resetBtn = UIFactory.CreateButton((RectTransform)col.transform,
                "Reset Player Progress…", UIFactory.Danger, 36, "ResetBtn");
            var rle = resetBtn.gameObject.AddComponent<LayoutElement>();
            rle.preferredHeight = 130; rle.minHeight = 100;
            resetBtn.onClick.AddListener(ConfirmReset);

            // About
            var about = UIFactory.CreateText((RectTransform)col.transform,
                "MathEdu • Unity 6000.4.4f1 • Built for kids who love numbers.",
                26, new Color(0.30f, 0.35f, 0.45f), TextAlignmentOptions.Center, "About");
            var ale = about.gameObject.AddComponent<LayoutElement>();
            ale.preferredHeight = 80;
        }

        private static void BuildSection(RectTransform parent, string title,
            bool toggleInitial, float sliderInitial,
            System.Action<bool> onToggle, System.Action<float> onSlider,
            out ToggleSwitch toggle, out Slider slider)
        {
            var row = new GameObject("Section_" + title,
                typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var rt = (RectTransform)row.transform;
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 200; le.minHeight = 180;

            // Title row
            var titleRow = new GameObject("TitleRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            titleRow.transform.SetParent(rt, false);
            var trt = (RectTransform)titleRow.transform;
            trt.anchorMin = new Vector2(0, 0.55f); trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var thl = titleRow.GetComponent<HorizontalLayoutGroup>();
            thl.spacing = 16;
            thl.padding = new RectOffset(0, 0, 0, 0);
            thl.childForceExpandWidth = true;
            thl.childAlignment = TextAnchor.MiddleLeft;

            var titleTxt = UIFactory.CreateText((RectTransform)titleRow.transform, title, 40,
                UIFactory.TextDark, TextAlignmentOptions.MidlineLeft, "Title");
            titleTxt.fontStyle = FontStyles.Bold;
            var tle = titleTxt.gameObject.AddComponent<LayoutElement>();
            tle.flexibleWidth = 1;

            toggle = ToggleSwitch.Spawn((RectTransform)titleRow.transform, toggleInitial, "Toggle");
            var sle = toggle.gameObject.AddComponent<LayoutElement>();
            sle.preferredWidth = 200; sle.preferredHeight = 90;
            toggle.onValueChanged += b => onToggle?.Invoke(b);

            // Slider row
            slider = UIFactory.CreateSlider(rt, sliderInitial, "Slider");
            var srt = (RectTransform)slider.transform;
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 0.55f);
            srt.offsetMin = new Vector2(8, 8); srt.offsetMax = new Vector2(-8, -8);
            slider.onValueChanged.AddListener(v => onSlider?.Invoke(v));
        }

        private static void BuildToggleRow(RectTransform parent, string title, bool initial,
            System.Action<bool> onChanged, out ToggleSwitch toggle)
        {
            var row = new GameObject("Row_" + title,
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 16;
            hl.childForceExpandWidth = true;
            hl.childAlignment = TextAnchor.MiddleLeft;
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 100; le.minHeight = 100;

            var lbl = UIFactory.CreateText((RectTransform)row.transform, title, 40,
                UIFactory.TextDark, TextAlignmentOptions.MidlineLeft, "Lbl");
            lbl.fontStyle = FontStyles.Bold;
            var lle = lbl.gameObject.AddComponent<LayoutElement>();
            lle.flexibleWidth = 1;

            toggle = ToggleSwitch.Spawn((RectTransform)row.transform, initial, "Toggle");
            var tle = toggle.gameObject.AddComponent<LayoutElement>();
            tle.preferredWidth = 200; tle.preferredHeight = 90;
            toggle.onValueChanged += b => onChanged?.Invoke(b);
        }

        private void BuildLanguageRow(RectTransform parent)
        {
            var row = new GameObject("LangRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.spacing = 16;
            hl.childForceExpandWidth = true;
            hl.childAlignment = TextAnchor.MiddleLeft;
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 110; le.minHeight = 100;

            var lbl = UIFactory.CreateText((RectTransform)row.transform, "🌐  Language", 40,
                UIFactory.TextDark, TextAlignmentOptions.MidlineLeft, "Lbl");
            lbl.fontStyle = FontStyles.Bold;
            var lle = lbl.gameObject.AddComponent<LayoutElement>();
            lle.flexibleWidth = 1;

            var btn = UIFactory.CreateButton((RectTransform)row.transform,
                _profile.language == "en" ? "English" : _profile.language,
                UIFactory.Primary, 32, "LangBtn");
            var tle = btn.gameObject.AddComponent<LayoutElement>();
            tle.preferredWidth = 320; tle.preferredHeight = 90;
            btn.onClick.AddListener(() =>
            {
                // Placeholder — real i18n hooks would swap a TMP_FontAsset + dictionary.
                GameManager.Instance.Audio.PlayTap();
            });
        }

        // -------------------------------------------------------------------
        // Behaviour
        // -------------------------------------------------------------------
        private void ApplyVolumes()
        {
            // Toggles take precedence over slider values — flipping off the
            // toggle drives volume to 0 even if the slider is high.
            float music = _profile.musicOn ? _profile.musicVolume : 0f;
            float sfx   = _profile.sfxOn   ? _profile.sfxVolume   : 0f;
            GameManager.Instance.Audio.SetMusicVolume(music);
            GameManager.Instance.Audio.SetSfxVolume(sfx);
            GameManager.Instance.SaveProfile();
        }

        private void ConfirmReset()
        {
            PasswordDialog.Show(
                "Reset Progress?",
                "Parental PIN required.",
                onSubmit: pin =>
                {
                    if (pin == _profile.parentalPin)
                    {
                        SaveSystem.DeleteAll();
                        GameManager.Instance.UI.Go(UIManager.SceneBootstrap);
                    }
                    else
                    {
                        Debug.Log("[Settings] Incorrect PIN.");
                    }
                });
        }
    }
}
