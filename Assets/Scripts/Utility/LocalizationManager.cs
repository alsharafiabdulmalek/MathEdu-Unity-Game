// -----------------------------------------------------------------------------
// LocalizationManager.cs (Arabic font auto-loading + TMP global fallback)
// -----------------------------------------------------------------------------
// Same string tables, but the ArabicFont property now searches three sources
// for a usable Arabic TTF and registers what it finds as a TMP global fallback
// so every TMP text in the project can render Arabic glyphs.
//
// Square-box problem: TMP_FontAsset.CreateFontAsset(font) needs the actual
// TTF byte data to build the SDF atlas. The font returned by
// Font.CreateDynamicFontFromOSFont is just an OS handle, so on Android/iOS
// TMP can't extract glyph outlines and every Arabic character falls back
// to the tofu box.
//
// Fix: drop a real TTF into Assets/Resources/Fonts/ (see
// Docs/ARABIC_FONT_SETUP.md for the one-minute walkthrough). The code below
// auto-detects it.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MathEdu.Utility
{
    public static class Localization
    {
        public enum Lang { English, Arabic }

        public static Lang Current { get; private set; } = Lang.English;
        public static bool IsRTL => Current == Lang.Arabic;
        public static string CurrentCode => Current == Lang.Arabic ? "ar" : "en";

        public static event System.Action OnLanguageChanged;

        private static TMP_FontAsset _arabicFont;
        private static bool _fallbackRegistered;

        // -------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------

        public static void SetLanguage(Lang lang)
        {
            if (Current == lang) return;
            Current = lang;
            OnLanguageChanged?.Invoke();
        }

        public static void SetFromCode(string code)
        {
            SetLanguage(string.Equals(code, "ar", System.StringComparison.OrdinalIgnoreCase)
                ? Lang.Arabic : Lang.English);
        }

        public static string T(string key, params object[] args)
        {
            var dict = Current == Lang.Arabic ? Ar : En;
            if (!dict.TryGetValue(key, out string val))
            {
                if (!En.TryGetValue(key, out val))
                    val = key;
            }
            if (args != null && args.Length > 0)
            {
                try { val = string.Format(val, args); }
                catch { /* malformed format string -> keep raw */ }
            }
            return val;
        }

        public static TMP_FontAsset ArabicFont
        {
            get
            {
                if (_arabicFont != null) return _arabicFont;

                // Path 1: preauthored TMP_FontAsset (best quality)
                _arabicFont = Resources.Load<TMP_FontAsset>("Fonts/Arabic SDF");
                if (_arabicFont != null)
                {
                    Debug.Log("[Localization] Loaded preauthored 'Fonts/Arabic SDF' TMP asset.");
                    RegisterAsTmpFallback(_arabicFont);
                    return _arabicFont;
                }

                // Path 2: raw TTF in Resources/Fonts/, convert at runtime
                string[] ttfNames = {
                    "Fonts/NotoSansArabic-Regular",
                    "Fonts/NotoSansArabic",
                    "Fonts/Cairo-Regular",
                    "Fonts/Cairo",
                    "Fonts/Amiri-Regular",
                    "Fonts/Amiri",
                    "Fonts/NotoNaskhArabic-Regular",
                    "Fonts/NotoNaskhArabic",
                    "Fonts/Arabic"
                };
                foreach (var name in ttfNames)
                {
                    var ttf = Resources.Load<Font>(name);
                    if (ttf == null) continue;
                    try
                    {
                        _arabicFont = TMP_FontAsset.CreateFontAsset(ttf);
                        if (_arabicFont != null)
                        {
                            _arabicFont.name = name + " SDF (Runtime)";
                            Debug.Log("[Localization] Created TMP font from " + name);
                            RegisterAsTmpFallback(_arabicFont);
                            return _arabicFont;
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning("[Localization] CreateFontAsset(" + name + ") failed: " + ex.Message);
                    }
                }

                // Path 3: OS dynamic font (last resort - usually fails on mobile)
                try
                {
                    string[] osCandidates = {
                        "Noto Sans Arabic", "Tahoma", "Arial",
                        "Geeza Pro", "Helvetica", "Amiri", "Cairo"
                    };
                    var sysFont = Font.CreateDynamicFontFromOSFont(osCandidates, 48);
                    if (sysFont != null)
                    {
                        _arabicFont = TMP_FontAsset.CreateFontAsset(sysFont);
                        if (_arabicFont != null)
                        {
                            _arabicFont.name = "OS Arabic SDF (Runtime)";
                            Debug.LogWarning(
                                "[Localization] Using OS dynamic font for Arabic. " +
                                "Arabic glyphs may appear as squares on mobile because " +
                                "TMP needs the actual TTF data. " +
                                "Add a real TTF to Assets/Resources/Fonts/ to fix " +
                                "(see Docs/ARABIC_FONT_SETUP.md).");
                            RegisterAsTmpFallback(_arabicFont);
                            return _arabicFont;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[Localization] OS Arabic font creation failed: " + ex.Message);
                }

                Debug.LogError(ArabicFontMissingMessage);
                return null;
            }
        }

        public static void Apply(TMP_Text text)
        {
            if (text == null) return;
            if (Current == Lang.Arabic)
            {
                var fa = ArabicFont;
                if (fa != null) text.font = fa;
                text.isRightToLeftText = true;
            }
            else
            {
                text.isRightToLeftText = false;
            }
        }

        // -------------------------------------------------------------------
        // Internals
        // -------------------------------------------------------------------

        private static void RegisterAsTmpFallback(TMP_FontAsset font)
        {
            if (font == null || _fallbackRegistered) return;
            try
            {
                // TMP_Settings.fallbackFontAssets is a STATIC property
                // (it forwards to TMP_Settings.instance.m_fallbackFontAssets
                // internally), so it must be qualified with the type name,
                // not an instance reference. Earlier versions of this code
                // used `settings.fallbackFontAssets` which caused CS0176.
                var fallbacks = TMP_Settings.fallbackFontAssets;
                if (fallbacks == null) return;
                if (!fallbacks.Contains(font))
                {
                    fallbacks.Add(font);
                    Debug.Log("[Localization] Registered " + font.name +
                              " as TMP global fallback. All TMP texts can now render Arabic glyphs.");
                }
                _fallbackRegistered = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Localization] Could not register TMP fallback: " + ex.Message);
            }
        }

        public const string ArabicFontMissingMessage =
            "*** Arabic text will appear as SQUARE BOXES ***\n" +
            "No Arabic-capable TMP font asset found in Resources/Fonts/.\n\n" +
            "QUICK FIX (one-time, ~1 minute):\n" +
            "  1. Open https://fonts.google.com/noto/specimen/Noto+Sans+Arabic\n" +
            "  2. Click 'Get font' > 'Download all' and unzip.\n" +
            "  3. In Unity, create folder Assets/Resources/Fonts/ if needed.\n" +
            "  4. Drag NotoSansArabic-Regular.ttf into that folder.\n" +
            "  5. Stop and restart Play mode (or rebuild for Android/iOS).\n\n" +
            "Or in the Unity menu: MathEdu > Localization > Open Arabic Font Setup Guide.\n" +
            "See Docs/ARABIC_FONT_SETUP.md for full details.";

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "common.ok", "OK" }, { "common.cancel", "Cancel" }, { "common.back", "Back" },
            { "common.continue", "Continue" }, { "common.next", "Next" }, { "common.retry", "Retry" },
            { "common.menu", "Menu" }, { "common.quit", "Quit" }, { "common.start", "Start" },
            { "common.done", "Done!" }, { "common.save", "Save" }, { "common.delete", "Delete" },
            { "boot.app_name", "MathEdu" },
            { "boot.tagline", "Learn. Play. Win." },
            { "setup.welcome", "Welcome!" },
            { "setup.subtitle", "Let's set up your player profile." },
            { "setup.name_label", "Name:" },
            { "setup.name_placeholder", "What's your name?" },
            { "setup.pick_avatar", "Pick an avatar:" },
            { "setup.choose_grade", "Choose your grade:" },
            { "setup.start_playing", "Start Playing!" },
            { "setup.footer", "You can change these later in Settings." },
            { "setup.grade_n", "Grade {0}" },
            { "menu.hi", "Hi {0}!" },
            { "menu.title", "MathEdu - Learn. Play. Win." },
            { "menu.choose_grade", "Choose grade:" },
            { "menu.continue", "Continue" },
            { "menu.tap_to_start", "Tap to start!" },
            { "menu.level_progress", "Level {0} / {1}    {2} \u2605" },
            { "subj.counting", "Counting" }, { "subj.addition", "Addition" },
            { "subj.subtraction", "Subtraction" }, { "subj.multiplication", "Multiplication" },
            { "subj.division", "Division" }, { "subj.shapes", "Shapes" },
            { "subj.patterns", "Patterns" }, { "subj.fractions", "Fractions" },
            { "subj.measurement", "Measurement" }, { "subj.time", "Time" }, { "subj.money", "Money" },
            { "levelsel.header", "Grade {0} - {1}" },
            { "levelsel.title_no_subject", "Pick a Subject" },
            { "levelsel.level_n", "Level {0}" },
            { "levelsel.hint", "\u2605 = stars earned    \ud83d\udd12 = locked - beat the previous level to unlock!" },
            { "levelsel.empty", "No levels available for this subject.\nTap < to return." },
            { "modesel.title", "Grade {0} \u2022 {1} \u2022 Level {2}" },
            { "modesel.learn", "Learn" },
            { "modesel.learn_sub", "Step-by-step lesson with an example." },
            { "modesel.practice", "Practice" },
            { "modesel.practice_sub", "Untimed multiple choice. Take your time." },
            { "modesel.quiz", "Quiz" },
            { "modesel.quiz_sub", "Timed challenge. Score = correct + speed." },
            { "modesel.story", "Story" },
            { "modesel.story_sub", "Math adventure with characters." },
            { "modesel.speed", "Speed Round" },
            { "modesel.speed_sub", "Fast-fire questions. How many in a row?" },
            { "gp.header_format", "{0} - {1} L{2}" },
            { "gp.score", "Score: {0}" },
            { "gp.hint_btn", "\ud83d\udca1 Hint" },
            { "gp.correct", "Correct!" }, { "gp.great_job", "Great job!" },
            { "gp.you_got_it", "You got it!" }, { "gp.awesome", "Awesome!" },
            { "gp.brilliant", "Brilliant!" }, { "gp.yes_excl", "Yes!" },
            { "gp.wrong_answer_was", "Answer was {0}" },
            { "gp.try_again", "Try again" }, { "gp.time_up", "Time's up!" },
            { "pause.paused", "Paused" }, { "pause.resume", "Resume" },
            { "pause.restart", "Restart" }, { "pause.quit_level", "Quit Level" },
            { "quit.title", "Quit this level?" },
            { "quit.body", "Your progress on this level will be lost." },
            { "quit.keep_playing", "Keep playing" }, { "quit.quit", "Quit" },
            { "learn.lesson", "Lesson" },
            { "learn.example_x_of_y", "Example {0} / {1}" },
            { "learn.practice", "Practice" },
            { "learn.practice_x_of_y", "Practice {0} / {1}" },
            { "learn.your_turn", "Now it's YOUR turn! \ud83d\udcaa" },
            { "learn.done", "Done!" },
            { "learn.done_body", "Great job - you finished the lesson!" },
            { "learn.done_sub", "Try Practice or Quiz mode next." },
            { "learn.back_to_modes", "Back to modes" },
            { "learn.practice_now", "Practice now" },
            { "results.title_win", "Level Complete!" },
            { "results.title_lose", "Run Ended!" },
            { "results.correct_format", "Correct: {0} / {1}" },
            { "results.survived_format", "Survived {0} questions!" },
            { "results.streak_format", "Longest streak: {0}" },
            { "results.score_xp_format", "Score {0}     +{1} XP" },
            { "results.new_badge_label", "\ud83c\udfc5 New badge!" },
            { "results.menu", "Menu" }, { "results.retry", "Retry" },
            { "results.next_level", "Next Level" },
            { "results.error_title", "\ud83d\ude05 Oops!" },
            { "results.error_body", "We couldn't load this level's results.\nLet's head back to the menu." },
            { "results.back_to_menu", "Back to Menu" },
            { "settings.title", "Settings" },
            { "settings.music", "\ud83c\udfb5  Music" },
            { "settings.sfx", "\ud83d\udd0a  Sound Effects" },
            { "settings.haptics", "\ud83d\udcf3  Haptics" },
            { "settings.language", "\ud83c\udf10  Language" },
            { "settings.lang_en", "English" },
            { "settings.lang_ar", "\u0627\u0644\u0639\u0631\u0628\u064a\u0629" },
            { "settings.change_pin", "\ud83d\udd10  Change Parental PIN\u2026" },
            { "settings.reset_progress", "Reset Player Progress\u2026" },
            { "settings.about", "MathEdu \u2022 Unity 6000.4.4f1 \u2022 Built for kids who love numbers." },
            { "settings.pin_current_title", "Enter current PIN" },
            { "settings.pin_current_body", "Verify the parental PIN before changing it." },
            { "settings.pin_new_title", "New PIN" },
            { "settings.pin_new_body", "Pick a 4-8 digit PIN." },
            { "settings.pin_confirm_title", "Confirm PIN" },
            { "settings.pin_confirm_body", "Type the new PIN again." },
            { "settings.reset_title", "Reset Progress?" },
            { "settings.reset_body", "Parental PIN required." },
            { "parental.title", "Parental Dashboard" },
            { "parental.for_parents", "\ud83d\udd12 For Parents" },
            { "parental.enter_pin", "Enter your PIN. Default is 0000." },
            { "parental.try_again_n", "Try again in {0} s" },
            { "parental.progress_format", "{0}'s Progress" },
            { "parental.stars", "\u2b50 Stars" }, { "parental.xp", "\ud83c\udfaf XP" },
            { "parental.badges_emoji", "\ud83c\udfc6 Badges" },
            { "parental.levels", "\ud83d\udcda Levels" }, { "parental.time", "\u23f1 Time" },
            { "parental.grade", "\ud83c\udf93 Grade" },
            { "parental.accuracy_title", "Accuracy by Subject" },
            { "parental.accuracy_empty", "Accuracy stats appear after the first session." },
            { "parental.subject_details", "Subject Details" },
            { "parental.no_subjects", "No subjects played yet." },
            { "parental.tbl_subject", "Subject" }, { "parental.tbl_q", "Q" },
            { "parental.tbl_correct", "Correct" }, { "parental.tbl_stars", "Stars" },
            { "parental.tbl_levels", "Levels" }, { "parental.tbl_time", "Time" },
            { "parental.badges_title", "Badges" },
            { "parental.no_badges", "No badges yet - earn one by clearing a level!" },
            { "parental.grade_completion", "Grade Completion" },
            { "parental.grade_n", "Grade {0}" }, { "parental.change_pin", "Change PIN" },
            { "parental.reset_progress", "Reset Progress" },
            { "parental.reset_confirm_title", "Confirm Reset" },
            { "parental.reset_confirm_body", "Type the parental PIN to wipe all progress." },
            { "parental.new_pin", "New Parental PIN" },
            { "parental.pick_pin", "Pick a 4-8 digit PIN." },
            { "badge.first_step", "\ud83c\udf31 First Step" },
            { "badge.half_way", "\ud83d\udee4 Half Way There" },
            { "badge.speed_demon", "\u26a1 Speed Demon" },
            { "badge.perfect_score", "\ud83d\udcaf Perfect Score" },
            { "badge.early_bird", "\ud83c\udf05 Early Bird" },
            { "badge.dedicated", "\ud83d\udcc5 Dedicated" },
            { "badge.apprentice_fmt", "\ud83c\udf93 {0} Apprentice (G{1})" },
            { "badge.master_fmt", "\ud83c\udfc6 {0} Master (G{1})" },
        };

        private static readonly Dictionary<string, string> Ar = new Dictionary<string, string>
        {
            { "common.ok", "\u0645\u0648\u0627\u0641\u0642" },
            { "common.cancel", "\u0625\u0644\u063a\u0627\u0621" },
            { "common.back", "\u0631\u062c\u0648\u0639" },
            { "common.continue", "\u0645\u062a\u0627\u0628\u0639\u0629" },
            { "common.next", "\u0627\u0644\u062a\u0627\u0644\u064a" },
            { "common.retry", "\u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u0645\u062d\u0627\u0648\u0644\u0629" },
            { "common.menu", "\u0627\u0644\u0642\u0627\u0626\u0645\u0629" },
            { "common.quit", "\u062e\u0631\u0648\u062c" },
            { "common.start", "\u0627\u0628\u062f\u0623" },
            { "common.done", "\u062a\u0645\u0651!" },
            { "common.save", "\u062d\u0641\u0638" },
            { "common.delete", "\u062d\u0630\u0641" },
            { "boot.app_name", "\u0645\u0627\u062b \u0625\u064a\u062f\u0648" },
            { "boot.tagline", "\u062a\u0639\u0644\u0651\u0645. \u0627\u0644\u0639\u0628. \u0627\u0631\u0628\u062d." },
            { "setup.welcome", "\u0645\u0631\u062d\u0628\u064b\u0627!" },
            { "setup.subtitle", "\u0647\u064a\u0651\u0627 \u0646\u064f\u062c\u0647\u0651\u0632 \u0645\u0644\u0641 \u0627\u0644\u0644\u0627\u0639\u0628." },
            { "setup.name_label", "\u0627\u0644\u0627\u0633\u0645:" },
            { "setup.name_placeholder", "\u0645\u0627 \u0627\u0633\u0645\u0643\u061f" },
            { "setup.pick_avatar", "\u0627\u062e\u062a\u0631 \u0634\u062e\u0635\u064a\u062a\u0643:" },
            { "setup.choose_grade", "\u0627\u062e\u062a\u0631 \u0635\u0641\u0651\u0643:" },
            { "setup.start_playing", "\u0627\u0628\u062f\u0623 \u0627\u0644\u0644\u0639\u0628!" },
            { "setup.footer", "\u064a\u0645\u0643\u0646\u0643 \u062a\u063a\u064a\u064a\u0631 \u0630\u0644\u0643 \u0644\u0627\u062d\u0642\u064b\u0627 \u0641\u064a \u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a." },
            { "setup.grade_n", "\u0627\u0644\u0635\u0641 {0}" },
            { "menu.hi", "\u0645\u0631\u062d\u0628\u064b\u0627 {0}!" },
            { "menu.title", "\u0645\u0627\u062b \u0625\u064a\u062f\u0648 - \u062a\u0639\u0644\u0651\u0645. \u0627\u0644\u0639\u0628. \u0627\u0631\u0628\u062d." },
            { "menu.choose_grade", "\u0627\u062e\u062a\u0631 \u0627\u0644\u0635\u0641:" },
            { "menu.continue", "\u0645\u062a\u0627\u0628\u0639\u0629" },
            { "menu.tap_to_start", "\u0627\u0636\u063a\u0637 \u0644\u0644\u0628\u062f\u0621!" },
            { "menu.level_progress", "\u0627\u0644\u0645\u0633\u062a\u0648\u0649 {0} / {1}    {2} \u2605" },
            { "subj.counting", "\u0627\u0644\u0639\u062f\u0651" },
            { "subj.addition", "\u0627\u0644\u062c\u0645\u0639" },
            { "subj.subtraction", "\u0627\u0644\u0637\u0631\u062d" },
            { "subj.multiplication", "\u0627\u0644\u0636\u0631\u0628" },
            { "subj.division", "\u0627\u0644\u0642\u0633\u0645\u0629" },
            { "subj.shapes", "\u0627\u0644\u0623\u0634\u0643\u0627\u0644" },
            { "subj.patterns", "\u0627\u0644\u0623\u0646\u0645\u0627\u0637" },
            { "subj.fractions", "\u0627\u0644\u0643\u0633\u0648\u0631" },
            { "subj.measurement", "\u0627\u0644\u0642\u064a\u0627\u0633" },
            { "subj.time", "\u0627\u0644\u0648\u0642\u062a" },
            { "subj.money", "\u0627\u0644\u0646\u0642\u0648\u062f" },
            { "levelsel.header", "\u0627\u0644\u0635\u0641 {0} - {1}" },
            { "levelsel.title_no_subject", "\u0627\u062e\u062a\u0631 \u0645\u0627\u062f\u0651\u0629" },
            { "levelsel.level_n", "\u0627\u0644\u0645\u0633\u062a\u0648\u0649 {0}" },
            { "levelsel.hint", "\u2605 = \u0627\u0644\u0646\u062c\u0648\u0645 \u0627\u0644\u0645\u0643\u062a\u0633\u0628\u0629    \ud83d\udd12 = \u0645\u0642\u0641\u0644 - \u0623\u0646\u0647\u0650 \u0627\u0644\u0645\u0633\u062a\u0648\u0649 \u0627\u0644\u0633\u0627\u0628\u0642 \u0644\u0641\u062a\u062d\u0647!" },
            { "levelsel.empty", "\u0644\u0627 \u062a\u0648\u062c\u062f \u0645\u0633\u062a\u0648\u064a\u0627\u062a \u0644\u0647\u0630\u0647 \u0627\u0644\u0645\u0627\u062f\u0651\u0629.\n\u0627\u0636\u063a\u0637 < \u0644\u0644\u0639\u0648\u062f\u0629." },
            { "modesel.title", "\u0627\u0644\u0635\u0641 {0} \u2022 {1} \u2022 \u0627\u0644\u0645\u0633\u062a\u0648\u0649 {2}" },
            { "modesel.learn", "\u062a\u0639\u0644\u0651\u0645" },
            { "modesel.learn_sub", "\u062f\u0631\u0633 \u062e\u0637\u0648\u0629 \u0628\u062e\u0637\u0648\u0629 \u0645\u0639 \u0645\u062b\u0627\u0644." },
            { "modesel.practice", "\u062a\u062f\u0631\u064a\u0628" },
            { "modesel.practice_sub", "\u0627\u062e\u062a\u064a\u0627\u0631 \u0645\u062a\u0639\u062f\u062f \u0628\u062f\u0648\u0646 \u0648\u0642\u062a. \u062e\u0630 \u0648\u0642\u062a\u0643." },
            { "modesel.quiz", "\u0627\u062e\u062a\u0628\u0627\u0631" },
            { "modesel.quiz_sub", "\u062a\u062d\u062f\u0651\u064d \u0628\u0627\u0644\u0648\u0642\u062a. \u0627\u0644\u0646\u062a\u064a\u062c\u0629 = \u0635\u062d\u064a\u062d + \u0633\u0631\u0639\u0629." },
            { "modesel.story", "\u0642\u0635\u0651\u0629" },
            { "modesel.story_sub", "\u0645\u063a\u0627\u0645\u0631\u0629 \u0631\u064a\u0627\u0636\u064a\u0629 \u0645\u0639 \u0634\u062e\u0635\u064a\u0627\u062a." },
            { "modesel.speed", "\u062c\u0648\u0644\u0629 \u0633\u0631\u064a\u0639\u0629" },
            { "modesel.speed_sub", "\u0623\u0633\u0626\u0644\u0629 \u0633\u0631\u064a\u0639\u0629. \u0643\u0645 \u0625\u062c\u0627\u0628\u0629 \u0635\u062d\u064a\u062d\u0629 \u0645\u062a\u062a\u0627\u0644\u064a\u0629\u061f" },
            { "gp.header_format", "{0} - {1} \u0627\u0644\u0645\u0633\u062a\u0648\u0649 {2}" },
            { "gp.score", "\u0627\u0644\u0646\u062a\u064a\u062c\u0629: {0}" },
            { "gp.hint_btn", "\ud83d\udca1 \u062a\u0644\u0645\u064a\u062d" },
            { "gp.correct", "\u0635\u062d\u064a\u062d!" },
            { "gp.great_job", "\u0639\u0645\u0644 \u0631\u0627\u0626\u0639!" },
            { "gp.you_got_it", "\u0623\u062d\u0633\u0646\u062a!" },
            { "gp.awesome", "\u0645\u0645\u062a\u0627\u0632!" },
            { "gp.brilliant", "\u0631\u0627\u0626\u0639!" },
            { "gp.yes_excl", "\u0646\u0639\u0645!" },
            { "gp.wrong_answer_was", "\u0627\u0644\u0625\u062c\u0627\u0628\u0629 \u0647\u064a {0}" },
            { "gp.try_again", "\u062d\u0627\u0648\u0644 \u0645\u0631\u0651\u0629 \u0623\u062e\u0631\u0649" },
            { "gp.time_up", "\u0627\u0646\u062a\u0647\u0649 \u0627\u0644\u0648\u0642\u062a!" },
            { "pause.paused", "\u0645\u062a\u0648\u0642\u0641" },
            { "pause.resume", "\u0627\u0633\u062a\u0645\u0631\u0627\u0631" },
            { "pause.restart", "\u0625\u0639\u0627\u062f\u0629 \u0627\u0644\u062a\u0634\u063a\u064a\u0644" },
            { "pause.quit_level", "\u0627\u0644\u062e\u0631\u0648\u062c \u0645\u0646 \u0627\u0644\u0645\u0633\u062a\u0648\u0649" },
            { "quit.title", "\u0627\u0644\u062e\u0631\u0648\u062c \u0645\u0646 \u0627\u0644\u0645\u0633\u062a\u0648\u0649\u061f" },
            { "quit.body", "\u0633\u062a\u0641\u0642\u062f \u062a\u0642\u062f\u0651\u0645\u0643 \u0641\u064a \u0647\u0630\u0627 \u0627\u0644\u0645\u0633\u062a\u0648\u0649." },
            { "quit.keep_playing", "\u0645\u062a\u0627\u0628\u0639\u0629 \u0627\u0644\u0644\u0639\u0628" },
            { "quit.quit", "\u062e\u0631\u0648\u062c" },
            { "learn.lesson", "\u062f\u0631\u0633" },
            { "learn.example_x_of_y", "\u0645\u062b\u0627\u0644 {0} / {1}" },
            { "learn.practice", "\u062a\u062f\u0631\u064a\u0628" },
            { "learn.practice_x_of_y", "\u062a\u062f\u0631\u064a\u0628 {0} / {1}" },
            { "learn.your_turn", "\u0627\u0644\u0622\u0646 \u062c\u0627\u0621 \u062f\u0648\u0631\u0643! \ud83d\udcaa" },
            { "learn.done", "\u062a\u0645\u0651!" },
            { "learn.done_body", "\u0623\u062d\u0633\u0646\u062a - \u0623\u0646\u0647\u064a\u062a \u0627\u0644\u062f\u0631\u0633!" },
            { "learn.done_sub", "\u062c\u0631\u0651\u0628 \u0627\u0644\u062a\u062f\u0631\u064a\u0628 \u0623\u0648 \u0627\u0644\u0627\u062e\u062a\u0628\u0627\u0631 \u0628\u0639\u062f \u0630\u0644\u0643." },
            { "learn.back_to_modes", "\u0627\u0644\u0639\u0648\u062f\u0629 \u0644\u0644\u0623\u0648\u0636\u0627\u0639" },
            { "learn.practice_now", "\u062a\u062f\u0631\u064a\u0628 \u0627\u0644\u0622\u0646" },
            { "results.title_win", "\u062a\u0645\u0651 \u0625\u0643\u0645\u0627\u0644 \u0627\u0644\u0645\u0633\u062a\u0648\u0649!" },
            { "results.title_lose", "\u0627\u0646\u062a\u0647\u062a \u0627\u0644\u062c\u0648\u0644\u0629!" },
            { "results.correct_format", "\u0627\u0644\u0635\u062d\u064a\u062d: {0} / {1}" },
            { "results.survived_format", "\u0635\u0645\u062f\u062a \u0644\u0640 {0} \u0633\u0624\u0627\u0644\u0627\u064b!" },
            { "results.streak_format", "\u0623\u0637\u0648\u0644 \u0633\u0644\u0633\u0644\u0629: {0}" },
            { "results.score_xp_format", "\u0627\u0644\u0646\u062a\u064a\u062c\u0629 {0}     +{1} \u062e\u0628\u0631\u0629" },
            { "results.new_badge_label", "\ud83c\udfc5 \u0634\u0627\u0631\u0629 \u062c\u062f\u064a\u062f\u0629!" },
            { "results.menu", "\u0627\u0644\u0642\u0627\u0626\u0645\u0629" },
            { "results.retry", "\u0625\u0639\u0627\u062f\u0629" },
            { "results.next_level", "\u0627\u0644\u0645\u0633\u062a\u0648\u0649 \u0627\u0644\u062a\u0627\u0644\u064a" },
            { "results.error_title", "\ud83d\ude05 \u0639\u0630\u0631\u064b\u0627!" },
            { "results.error_body", "\u0644\u0645 \u0646\u062a\u0645\u0643\u0651\u0646 \u0645\u0646 \u062a\u062d\u0645\u064a\u0644 \u0646\u062a\u0627\u0626\u062c \u0627\u0644\u0645\u0633\u062a\u0648\u0649.\n\u0644\u0646\u0639\u062f \u0625\u0644\u0649 \u0627\u0644\u0642\u0627\u0626\u0645\u0629." },
            { "results.back_to_menu", "\u0627\u0644\u0639\u0648\u062f\u0629 \u0625\u0644\u0649 \u0627\u0644\u0642\u0627\u0626\u0645\u0629" },
            { "settings.title", "\u0627\u0644\u0625\u0639\u062f\u0627\u062f\u0627\u062a" },
            { "settings.music", "\ud83c\udfb5  \u0627\u0644\u0645\u0648\u0633\u064a\u0642\u0649" },
            { "settings.sfx", "\ud83d\udd0a  \u0627\u0644\u0645\u0624\u062b\u0631\u0627\u062a \u0627\u0644\u0635\u0648\u062a\u064a\u0629" },
            { "settings.haptics", "\ud83d\udcf3  \u0627\u0644\u0627\u0647\u062a\u0632\u0627\u0632" },
            { "settings.language", "\ud83c\udf10  \u0627\u0644\u0644\u063a\u0629" },
            { "settings.lang_en", "English" },
            { "settings.lang_ar", "\u0627\u0644\u0639\u0631\u0628\u064a\u0629" },
            { "settings.change_pin", "\ud83d\udd10  \u062a\u063a\u064a\u064a\u0631 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a \u0644\u0644\u0648\u0627\u0644\u062f\u064a\u0646\u2026" },
            { "settings.reset_progress", "\u0625\u0639\u0627\u062f\u0629 \u062a\u0639\u064a\u064a\u0646 \u062a\u0642\u062f\u0651\u0645 \u0627\u0644\u0644\u0627\u0639\u0628\u2026" },
            { "settings.about", "\u0645\u0627\u062b \u0625\u064a\u062f\u0648 \u2022 Unity 6000.4.4f1 \u2022 \u0644\u0644\u0623\u0637\u0641\u0627\u0644 \u0627\u0644\u0630\u064a\u0646 \u064a\u062d\u0628\u0651\u0648\u0646 \u0627\u0644\u0623\u0631\u0642\u0627\u0645." },
            { "settings.pin_current_title", "\u0623\u062f\u062e\u0644 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a \u0627\u0644\u062d\u0627\u0644\u064a" },
            { "settings.pin_current_body", "\u062a\u062d\u0642\u0651\u0642 \u0645\u0646 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a \u0642\u0628\u0644 \u062a\u063a\u064a\u064a\u0631\u0647." },
            { "settings.pin_new_title", "\u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a \u0627\u0644\u062c\u062f\u064a\u062f" },
            { "settings.pin_new_body", "\u0627\u062e\u062a\u0631 \u0631\u0642\u0645\u064b\u0627 \u0645\u0646 4 \u0625\u0644\u0649 8 \u0623\u0631\u0642\u0627\u0645." },
            { "settings.pin_confirm_title", "\u062a\u0623\u0643\u064a\u062f \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a" },
            { "settings.pin_confirm_body", "\u0627\u0643\u062a\u0628 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a \u0645\u0631\u0651\u0629 \u0623\u062e\u0631\u0649." },
            { "settings.reset_title", "\u0625\u0639\u0627\u062f\u0629 \u062a\u0639\u064a\u064a\u0646 \u0627\u0644\u062a\u0642\u062f\u0651\u0645\u061f" },
            { "settings.reset_body", "\u0645\u0637\u0644\u0648\u0628 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a \u0644\u0644\u0648\u0627\u0644\u062f\u064a\u0646." },
            { "parental.title", "\u0644\u0648\u062d\u0629 \u0627\u0644\u0648\u0627\u0644\u062f\u064a\u0646" },
            { "parental.for_parents", "\ud83d\udd12 \u0644\u0644\u0648\u0627\u0644\u062f\u064a\u0646" },
            { "parental.enter_pin", "\u0623\u062f\u062e\u0644 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a. \u0627\u0644\u0627\u0641\u062a\u0631\u0627\u0636\u064a 0000." },
            { "parental.try_again_n", "\u062d\u0627\u0648\u0644 \u0628\u0639\u062f {0} \u062b" },
            { "parental.progress_format", "\u062a\u0642\u062f\u0651\u0645 {0}" },
            { "parental.stars", "\u2b50 \u0627\u0644\u0646\u062c\u0648\u0645" },
            { "parental.xp", "\ud83c\udfaf \u0627\u0644\u062e\u0628\u0631\u0629" },
            { "parental.badges_emoji", "\ud83c\udfc6 \u0627\u0644\u0634\u0627\u0631\u0627\u062a" },
            { "parental.levels", "\ud83d\udcda \u0627\u0644\u0645\u0633\u062a\u0648\u064a\u0627\u062a" },
            { "parental.time", "\u23f1 \u0627\u0644\u0648\u0642\u062a" },
            { "parental.grade", "\ud83c\udf93 \u0627\u0644\u0635\u0641" },
            { "parental.accuracy_title", "\u0627\u0644\u062f\u0642\u0651\u0629 \u062d\u0633\u0628 \u0627\u0644\u0645\u0627\u062f\u0651\u0629" },
            { "parental.accuracy_empty", "\u062a\u0638\u0647\u0631 \u0625\u062d\u0635\u0627\u0626\u064a\u0651\u0627\u062a \u0627\u0644\u062f\u0642\u0651\u0629 \u0628\u0639\u062f \u0627\u0644\u062c\u0644\u0633\u0629 \u0627\u0644\u0623\u0648\u0644\u0649." },
            { "parental.subject_details", "\u062a\u0641\u0627\u0635\u064a\u0644 \u0627\u0644\u0645\u0648\u0627\u062f\u0651" },
            { "parental.no_subjects", "\u0644\u0645 \u062a\u064f\u0644\u0639\u0628 \u0623\u064a \u0645\u0627\u062f\u0651\u0629 \u0628\u0639\u062f." },
            { "parental.tbl_subject", "\u0627\u0644\u0645\u0627\u062f\u0651\u0629" },
            { "parental.tbl_q", "\u0623\u0633\u0626\u0644\u0629" },
            { "parental.tbl_correct", "\u0635\u062d\u064a\u062d" },
            { "parental.tbl_stars", "\u0646\u062c\u0648\u0645" },
            { "parental.tbl_levels", "\u0645\u0633\u062a\u0648\u064a\u0627\u062a" },
            { "parental.tbl_time", "\u0627\u0644\u0648\u0642\u062a" },
            { "parental.badges_title", "\u0627\u0644\u0634\u0627\u0631\u0627\u062a" },
            { "parental.no_badges", "\u0644\u0627 \u062a\u0648\u062c\u062f \u0634\u0627\u0631\u0627\u062a \u0628\u0639\u062f - \u0627\u0643\u0633\u0628 \u0648\u0627\u062d\u062f\u0629 \u0628\u0625\u0646\u0647\u0627\u0621 \u0645\u0633\u062a\u0648\u0649!" },
            { "parental.grade_completion", "\u0625\u0646\u062c\u0627\u0632 \u0627\u0644\u0635\u0641" },
            { "parental.grade_n", "\u0627\u0644\u0635\u0641 {0}" },
            { "parental.change_pin", "\u062a\u063a\u064a\u064a\u0631 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a" },
            { "parental.reset_progress", "\u0625\u0639\u0627\u062f\u0629 \u062a\u0639\u064a\u064a\u0646 \u0627\u0644\u062a\u0642\u062f\u0651\u0645" },
            { "parental.reset_confirm_title", "\u062a\u0623\u0643\u064a\u062f \u0627\u0644\u0625\u0639\u0627\u062f\u0629" },
            { "parental.reset_confirm_body", "\u0627\u0643\u062a\u0628 \u0627\u0644\u0631\u0642\u0645 \u0627\u0644\u0633\u0631\u0651\u064a \u0644\u0645\u0633\u062d \u0643\u0644\u0651 \u0627\u0644\u062a\u0642\u062f\u0651\u0645." },
            { "parental.new_pin", "\u0631\u0642\u0645 \u0633\u0631\u0651\u064a \u062c\u062f\u064a\u062f \u0644\u0644\u0648\u0627\u0644\u062f\u064a\u0646" },
            { "parental.pick_pin", "\u0627\u062e\u062a\u0631 \u0631\u0642\u0645\u064b\u0627 \u0645\u0646 4 \u0625\u0644\u0649 8 \u0623\u0631\u0642\u0627\u0645." },
            { "badge.first_step", "\ud83c\udf31 \u0627\u0644\u062e\u0637\u0648\u0629 \u0627\u0644\u0623\u0648\u0644\u0649" },
            { "badge.half_way", "\ud83d\udee4 \u0645\u0646\u062a\u0635\u0641 \u0627\u0644\u0637\u0631\u064a\u0642" },
            { "badge.speed_demon", "\u26a1 \u0634\u064a\u0637\u0627\u0646 \u0627\u0644\u0633\u0631\u0639\u0629" },
            { "badge.perfect_score", "\ud83d\udcaf \u062f\u0631\u062c\u0629 \u0643\u0627\u0645\u0644\u0629" },
            { "badge.early_bird", "\ud83c\udf05 \u0627\u0644\u0641\u062c\u0631 \u0627\u0644\u0645\u0628\u0643\u0651\u0631" },
            { "badge.dedicated", "\ud83d\udcc5 \u0645\u0644\u062a\u0632\u0645" },
            { "badge.apprentice_fmt", "\ud83c\udf93 \u0645\u062a\u062f\u0631\u0651\u0628 {0} (\u0627\u0644\u0635\u0641 {1})" },
            { "badge.master_fmt", "\ud83c\udfc6 \u062e\u0628\u064a\u0631 {0} (\u0627\u0644\u0635\u0641 {1})" },
        };
    }
}
