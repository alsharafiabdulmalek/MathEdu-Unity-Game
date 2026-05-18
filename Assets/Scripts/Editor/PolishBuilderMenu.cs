// -----------------------------------------------------------------------------
// PolishBuilderMenu.cs
// -----------------------------------------------------------------------------
// Editor-only utility that creates a fully wired UITheme.asset and IconLibrary.
// asset in Assets/Resources/ by *finding the GUI Pro - Casual Game pack* assets
// already present in the project (or any other PNG/sprite assets the artist
// added). This means every screen automatically uses the GUI Pro buttons,
// frames, popups, sliders, toggles, and pictoicons without any per-screen
// inspector wiring.
//
// Resolution strategy:
//   • For each named "role" (button, panel, toggle on, gear icon, …) the menu
//     defines an ordered list of *search patterns* (case-insensitive substring
//     match on the asset path). The first matching Sprite asset wins.
//   • If a pattern doesn't match, the slot is left empty — the runtime falls
//     back to the procedural DefaultSprite for that slot, so nothing breaks.
//
// Re-run safe: the menu deletes and re-creates the assets each run so it stays
// in sync with whatever sprites the artist added since the last build.
//
// Entry points:
//   MathEdu / Polish / Build Default UI Theme & Icon Library
//   MathEdu / Polish / Run Full Polish Setup (scenes + DB + theme + icons)
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.IO;
using MathEdu.Data;
using UnityEditor;
using UnityEngine;

namespace MathEdu.EditorTools
{
    public static class PolishBuilderMenu
    {
        private const string ResourcesDir   = "Assets/Resources";
        private const string ThemePath      = "Assets/Resources/UITheme.asset";
        private const string IconLibPath    = "Assets/Resources/IconLibrary.asset";

        // ---------------------------------------------------------------------
        // Top-level entries
        // ---------------------------------------------------------------------

        /// <summary>
        /// THE one-stop polish entry: builds DB, avatars, scenes, theme, icons.
        /// On a fresh clone, run this and press Play.
        /// </summary>
        [MenuItem("MathEdu/Polish/✨ Run Full Polish Setup (scenes + theme + icons)", priority = 1)]
        public static void RunFullPolish()
        {
            DatabaseBuilderMenu.QuickStart();
            BuildPolishAssets();
            EditorUtility.DisplayDialog("MathEdu — Full Polish Setup",
                "Polish setup complete!\n\n" +
                "• Scenes built\n" +
                "• Avatar library built\n" +
                "• UITheme.asset created with GUI Pro sprites where available\n" +
                "• IconLibrary.asset created with PictoIcon mappings\n\n" +
                "Open Assets/Scenes/Bootstrap.unity and press ▶ Play.",
                "OK");
        }

        [MenuItem("MathEdu/Polish/Build Default UI Theme & Icon Library", priority = 11)]
        public static void BuildPolishAssets()
        {
            EnsureFolder(ResourcesDir);
            int themeFilled  = BuildTheme();
            int iconsFilled  = BuildIcons();
            int avatarsWired = WireAvatarSprites();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Polish] UITheme slots wired: {themeFilled}, IconLibrary slots wired: {iconsFilled}, Avatar sprites wired: {avatarsWired}.");
            EditorUtility.DisplayDialog("MathEdu — Polish Assets Built",
                $"UITheme.asset and IconLibrary.asset are now in Assets/Resources/.\n\n" +
                $"UI sprites wired: {themeFilled}\n" +
                $"Icons wired:     {iconsFilled}\n" +
                $"Avatars wired:   {avatarsWired}\n\n" +
                "Every screen auto-detects them on play — no extra wiring needed.",
                "OK");
        }

        [MenuItem("MathEdu/Polish/Wipe Polish Assets", priority = 20)]
        public static void WipePolish()
        {
            if (File.Exists(ThemePath))   AssetDatabase.DeleteAsset(ThemePath);
            if (File.Exists(IconLibPath)) AssetDatabase.DeleteAsset(IconLibPath);
            AssetDatabase.Refresh();
        }

        // ---------------------------------------------------------------------
        // UITheme
        // ---------------------------------------------------------------------
        private static int BuildTheme()
        {
            if (File.Exists(ThemePath)) AssetDatabase.DeleteAsset(ThemePath);
            var theme = ScriptableObject.CreateInstance<UITheme>();

            // Force a kid-friendly palette by default
            theme.overrideColours = true;
            theme.bgTop    = new Color(0.32f, 0.55f, 0.95f);
            theme.bgBottom = new Color(0.60f, 0.85f, 1.00f);
            theme.primary  = new Color(0.30f, 0.65f, 0.95f);
            theme.success  = new Color(0.30f, 0.80f, 0.45f);
            theme.danger   = new Color(0.95f, 0.40f, 0.40f);
            theme.accent   = new Color(1.00f, 0.55f, 0.20f);

            int filled = 0;

            // -------- Buttons / panels / cards / pills --------
            filled += TryAssign(ref theme.buttonSprite, "Button01_175_Blue", "Button01_145_Blue");
            filled += TryAssign(ref theme.panelSprite,  "Popoup01~03_White_Bg", "BasicFrame_Round20");
            filled += TryAssign(ref theme.cardSprite,   "BasicFrame_Round20", "BannerFrame01_White_Bg");
            filled += TryAssign(ref theme.pillSprite,   "Button01_175_Green", "Button01_145_Green");
            filled += TryAssign(ref theme.headerSprite, "Popoup01~03_White_Bg_TopBasic", "BannerFrame02_White_Bg");

            // -------- Icons (legacy slots on UITheme; IconLibrary is the full mapping) --------
            filled += TryAssign(ref theme.starFilled,   "Pictoicon_Star");
            filled += TryAssign(ref theme.starEmpty,    "Toggle02_CheckBox_White_bg", "BasicFrame_Round12");
            filled += TryAssign(ref theme.lockIcon,     "Pictoicon_Lock");
            filled += TryAssign(ref theme.settingsIcon, "Pictoicon_Setting");
            filled += TryAssign(ref theme.backArrow,    "Pictoicon_Arrow_Prev", "Pictoicon_Arrow_Backward");
            filled += TryAssign(ref theme.chartIcon,    "Pictoicon_Chart_Bar", "Pictoicon_Trophy_0");
            filled += TryAssign(ref theme.coinIcon,     "Pictoicon_Coin_Star", "Pictoicon_Coin_Crown");

            // -------- Toggles --------
            filled += TryAssign(ref theme.toggleOnSprite,  "Switch_Bg_On", "Toggle01_Check_Green");
            filled += TryAssign(ref theme.toggleOffSprite, "Switch_Bg_Off", "Toggle02_CheckBox_Off");

            // -------- Slider --------
            filled += TryAssign(ref theme.sliderBackground, "Slider_Basic01_Bg", "Slider_Basic02_Bg");
            filled += TryAssign(ref theme.sliderFill,       "Slider_Basic01_Fill_Blue", "Slider_Basic01_Fill_White");
            filled += TryAssign(ref theme.sliderHandle,     "Switch_Handle_White", "Switch_Handle_On");

            AssetDatabase.CreateAsset(theme, ThemePath);
            return filled;
        }

        // ---------------------------------------------------------------------
        // IconLibrary
        // ---------------------------------------------------------------------
        private static int BuildIcons()
        {
            if (File.Exists(IconLibPath)) AssetDatabase.DeleteAsset(IconLibPath);
            var lib = ScriptableObject.CreateInstance<IconLibrary>();
            int filled = 0;

            filled += TryAssign(ref lib.gear,        "Pictoicon_Setting");
            filled += TryAssign(ref lib.parent,      "Pictoicon_Account", "Pictoicon_Profile");
            filled += TryAssign(ref lib.back,        "Pictoicon_Arrow_Prev", "Pictoicon_Arrow_Backward");
            filled += TryAssign(ref lib.home,        "Pictoicon_Home_0", "Pictoicon_Home_1");
            filled += TryAssign(ref lib.play,        "Pictoicon_Control_Play");
            filled += TryAssign(ref lib.pause,       "Pictoicon_Control_Pause");
            filled += TryAssign(ref lib.next,        "Pictoicon_Arrow_Next", "Pictoicon_Arrow_Forward");
            filled += TryAssign(ref lib.refresh,     "Pictoicon_Refresh", "Pictoicon_Restart");

            filled += TryAssign(ref lib.star,        "Pictoicon_Star", "Pictoicon_Coin_Star");
            filled += TryAssign(ref lib.trophy,      "Pictoicon_Trophy_0", "Pictoicon_Trophy_1");
            filled += TryAssign(ref lib.crown,       "Pictoicon_Crown");
            filled += TryAssign(ref lib.gem,         "Icon_ImageIcon_Gem01_l", "Pictoicon_Crystal");
            filled += TryAssign(ref lib.medal,       "Pictoicon_Medal", "Pictoicon_Achieve");
            filled += TryAssign(ref lib.badge,       "Icon_ImageIcon_Badge", "Pictoicon_Award");

            filled += TryAssign(ref lib.lightbulb,   "Pictoicon_Bulb", "Pictoicon_Light");
            filled += TryAssign(ref lib.heart,       "Pictoicon_Like", "Pictoicon_Love");
            filled += TryAssign(ref lib.clock,       "Pictoicon_Clock", "Pictoicon_Time");
            filled += TryAssign(ref lib.check,       "Pictoicon_Check");
            filled += TryAssign(ref lib.cross,       "Pictoicon_Cross", "Pictoicon_Close");
            filled += TryAssign(ref lib.lockClosed,  "Pictoicon_Lock");
            filled += TryAssign(ref lib.lockOpen,    "Pictoicon_Unlock");
            filled += TryAssign(ref lib.profile,     "Pictoicon_Profile", "Pictoicon_User");

            filled += TryAssign(ref lib.musicOn,     "Pictoicon_Music");
            filled += TryAssign(ref lib.musicOff,    "Pictoicon_Music_Off");
            filled += TryAssign(ref lib.soundOn,     "Pictoicon_Sound");
            filled += TryAssign(ref lib.soundOff,    "Pictoicon_Sound_Off");

            filled += TryAssign(ref lib.emojiSmile,  "Pictoicon_Emoji_Smile");
            filled += TryAssign(ref lib.emojiSad,    "Pictoicon_Emoji_Sad");
            filled += TryAssign(ref lib.emojiAngry,  "Pictoicon_Emoji_Angry");
            filled += TryAssign(ref lib.emojiWow,    "Pictoicon_Emoji_Wow");
            filled += TryAssign(ref lib.emojiCool,   "Pictoicon_Emoji_Cool", "Pictoicon_Emoji_Smile");

            filled += TryAssign(ref lib.fire,        "Pictoicon_Fire");
            filled += TryAssign(ref lib.sparkle,     "Pictoicon_Sparkle", "Pictoicon_Star");
            filled += TryAssign(ref lib.sun,         "Pictoicon_Sun");
            filled += TryAssign(ref lib.flower,      "Pictoicon_Flower");
            filled += TryAssign(ref lib.leaf,        "Pictoicon_Leaf");

            AssetDatabase.CreateAsset(lib, IconLibPath);
            return filled;
        }

        // ---------------------------------------------------------------------
        // Avatar wiring — point existing AvatarData assets at GUI Pro character
        // sprites so the picker shows real cartoon characters instead of plain
        // coloured circles with emojis. Falls back gracefully when sprites are
        // missing.
        // ---------------------------------------------------------------------
        private static int WireAvatarSprites()
        {
            // Pair each avatar id with one or more GUI Pro character sprite
            // names to try, in priority order.
            var pairs = new (string id, string[] patterns)[]
            {
                ("fox",     new[] { "Character_Sample01_l", "Character_Sample01" }),
                ("panda",   new[] { "Character_Sample03_l", "Character_Sample03" }),
                ("rabbit",  new[] { "Character_Sample05_l", "Character_Sample05" }),
                ("owl",     new[] { "Character_Sample06_l", "Character_Sample06" }),
                ("monkey",  new[] { "Character_Sample07_l", "Character_Sample07" }),
                ("cat",     new[] { "Character_Sample02"   }),
                ("dog",     new[] { "Character_Sample04"   }),
                ("unicorn", new[] { "Character_Sample08"   }),
                ("dragon",  new[] { "Character_Sample09"   }),
                ("astro",   new[] { "Character_Sample12"   }),
            };

            int wired = 0;
            foreach (var pair in pairs)
            {
                var assetPath = $"Assets/ScriptableObjects/Avatars/Avatar_{pair.id}.asset";
                var avatar = AssetDatabase.LoadAssetAtPath<AvatarData>(assetPath);
                if (avatar == null) continue;
                Sprite chosen = null;
                foreach (var p in pair.patterns)
                {
                    chosen = FindSprite(p);
                    if (chosen != null) break;
                }
                if (chosen != null)
                {
                    avatar.sprite = chosen;
                    EditorUtility.SetDirty(avatar);
                    wired++;
                }
            }
            return wired;
        }

        // ---------------------------------------------------------------------
        // Asset search
        // ---------------------------------------------------------------------

        /// <summary>
        /// Try each pattern in order. If a Sprite asset matches (substring,
        /// case-insensitive, on the asset path), assign it to <paramref name="slot"/>
        /// and return 1. Otherwise return 0.
        /// </summary>
        private static int TryAssign(ref Sprite slot, params string[] patterns)
        {
            foreach (var p in patterns)
            {
                var hit = FindSprite(p);
                if (hit != null) { slot = hit; return 1; }
            }
            return 0;
        }

        private static Sprite FindSprite(string substring)
        {
            // Search for any sprite whose path or name contains `substring`,
            // case-insensitively. Prefer assets inside the Sprites/ folder so
            // that unrelated TMP / sample icons don't shadow the real art.
            var guids = AssetDatabase.FindAssets($"{substring} t:Sprite");
            string match = null;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                if (path.IndexOf(substring, System.StringComparison.OrdinalIgnoreCase) < 0
                    && Path.GetFileNameWithoutExtension(path).IndexOf(substring,
                        System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                bool preferred = path.IndexOf("/Sprites/", System.StringComparison.OrdinalIgnoreCase) >= 0;
                if (preferred) { match = path; break; }
                match ??= path; // remember as fallback
            }
            if (match == null) return null;
            return AssetDatabase.LoadAssetAtPath<Sprite>(match);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path);
            var folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
