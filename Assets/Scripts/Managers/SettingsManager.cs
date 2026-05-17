// -----------------------------------------------------------------------------
// SettingsManager.cs
// -----------------------------------------------------------------------------
// Builds the Settings scene at runtime:
//   • Music ON/OFF toggle + volume slider
//   • SFX ON/OFF toggle + volume slider
//   • Haptics toggle
//   • Language placeholder (English by default)
//   • Change Parental PIN (current → new → confirm flow)
//   • Reset Player Progress (PIN-gated)
//   • Back button → Main Menu
//
// All changes are written straight to PlayerProfile and flushed to JSON via
// GameManager.SaveProfile *immediately* on every toggle/slider event, so the
// player never has to back out to persist a setting.
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
                GameManager.Instance.Audio.PlaySFX("tap");
                GameManager.Instance.SaveProfile();
                GameManager.Instance.UI.Go(UIManager.SceneMainMenu);
            });

            // Body — scroll view so all rows fit on small phones
            var scroll = UIFactory.CreateScrollView(safe, "SettingsScroll");
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0.05f, 0.06f); srt.anchorMax = new Vector2(0.95f, 0.88f);
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

            var content = scroll.content;

            // ----- Music -----
            BuildSection(content, "🎵  Music",
                _profile.musicOn, _profile.musicVolume,
                onToggle: v => { _profile.musicOn = v; ApplyVolumes(); Save(); },
                onSlider: v => { _profile.musicVolume = v; ApplyVolumes(); Save(); },
                out _musicToggle, out _musicSlider);

            // ----- SFX -----
            BuildSection(content, "🔊  Sound Effects",
                _profile.sfxOn, _profile.sfxVolume,
                onToggle: v =>
                {
                    _profile.sfxOn = v;
                    ApplyVolumes();
                    Save();
                    GameManager.Instance.Audio.PlaySFX("tap");
                },
                onSlider: v => { _profile.sfxVolume = v; ApplyVolumes(); Save(); },
                out _sfxToggle, out _sfxSlider);

            // ----- Haptics -----
            BuildToggleRow(content, "📳  Haptics", _profile.hapticsOn,
                v => { _profile.hapticsOn = v; Save(); }, out _hapticsToggle);

            // ----- Language placeholder -----
            BuildLanguageRow(content);

            // ----- Change PIN -----
            var pinBtn = UIFactory.CreateButton(content,
                "🔐  Change Parental PIN…", UIFactory.Primary, 36, "PinBtn");
            pinBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 130;
            pinBtn.onClick.AddListener(BeginChangePin);

            // ----- Reset Progress -----
            var resetBtn = UIFactory.CreateButton(content,
                "Reset Player Progress…", UIFactory.Danger, 36, "ResetBtn");
            var rle = resetBtn.gameObject.AddComponent<LayoutElement>();
            rle.preferredHeight = 130; rle.minHeight = 100;
            resetBtn.onClick.AddListener(ConfirmReset);

            // About
            var about = UIFactory.CreateText(content,
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
                GameManager.Instance.Audio.PlaySFX("tap");
            });
        }

        // -------------------------------------------------------------------
        // Behaviour
        // -------------------------------------------------------------------
        private void ApplyVolumes()
        {
            float music = _profile.musicOn ? _profile.musicVolume : 0f;
            float sfx   = _profile.sfxOn   ? _profile.sfxVolume   : 0f;
            GameManager.Instance.Audio.SetMusicVolume(music);
            GameManager.Instance.Audio.SetSfxVolume(sfx);
        }

        private void Save() => GameManager.Instance.SaveProfile();

        // -------------------------------------------------------------------
        // PIN change flow: current → new → confirm
        // -------------------------------------------------------------------
        private void BeginChangePin()
        {
            GameManager.Instance.Audio.PlaySFX("tap");
            PasswordDialog.Show(
                "Enter current PIN",
                "Verify the parental PIN before changing it.",
                onSubmit: current =>
                {
                    if (current != _profile.parentalPin)
                    {
                        GameManager.Instance.Audio.PlaySFX("wrong");
                        return;
                    }
                    PromptNewPin();
                });
        }

        private void PromptNewPin()
        {
            PasswordDialog.Show(
                "New PIN",
                "Pick a 4–8 digit PIN.",
                onSubmit: newPin =>
                {
                    if (string.IsNullOrEmpty(newPin) || newPin.Length < 4)
                    {
                        GameManager.Instance.Audio.PlaySFX("wrong");
                        return;
                    }
                    PromptConfirmPin(newPin);
                });
        }

        private void PromptConfirmPin(string newPin)
        {
            PasswordDialog.Show(
                "Confirm PIN",
                "Type the new PIN again.",
                onSubmit: confirm =>
                {
                    if (confirm == newPin)
                    {
                        _profile.parentalPin = newPin;
                        Save();
                        GameManager.Instance.Audio.PlaySFX("correct");
                    }
                    else
                    {
                        GameManager.Instance.Audio.PlaySFX("wrong");
                    }
                });
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
                        GameManager.Instance.Audio.PlaySFX("wrong");
                    }
                });
        }
    }
}
