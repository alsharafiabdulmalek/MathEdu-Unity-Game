// -----------------------------------------------------------------------------
// AudioImportMenu.cs
// -----------------------------------------------------------------------------
// Editor-only helper menu that makes it trivial to drop real royalty-free
// audio files into the game. Lives under "MathEdu / Audio".
//
//   * About Royalty-Free Audio Sources -- popup that lists curated sites
//     where the project owner can grab CC0 / CC-BY / royalty-free music and
//     SFX (with the exact file names the runtime auto-picks up).
//   * Open Audio Resources Folder -- creates Assets/Resources/Audio if it
//     does not exist, then reveals it in Finder / Explorer.
//   * Print Expected File Names -- dumps the list of file names the runtime
//     loads from Resources/Audio/ to the Console for quick copy-paste.
//
// No automated download from the Unity Editor: pulling files from the web
// inside the Editor is fragile (firewalls, retired URLs, licence drift) so
// this helper just guides the user to the right places and shows the exact
// names to save the downloaded files under.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MathEdu.EditorTools
{
    public static class AudioImportMenu
    {
        private const string AudioDir = "Assets/Resources/Audio";

        // ===================================================================
        //                       ROYALTY-FREE SOURCES
        // ===================================================================

        [MenuItem("MathEdu/Audio/About Royalty-Free Audio Sources", priority = 0)]
        public static void AboutRoyaltyFreeSources()
        {
            string body =
                "Drop high-quality, royalty-free audio into\n" +
                "    Assets/Resources/Audio/\n" +
                "with the file names listed in 'Print Expected File Names' (or in\n" +
                "the docstring at the top of AudioManager.cs).\n\n" +
                "RECOMMENDED CC0 / FREE-FOR-COMMERCIAL SOURCES:\n\n" +
                "  - Pixabay Music & SFX  -- https://pixabay.com/music\n" +
                "                            https://pixabay.com/sound-effects\n" +
                "    Pixabay licence; free for commercial use, no attribution required.\n\n" +
                "  - Mixkit               -- https://mixkit.co/free-sound-effects\n" +
                "                            https://mixkit.co/free-stock-music\n" +
                "    Mixkit licence; free for commercial use, no attribution required.\n\n" +
                "  - Kenney Audio Packs   -- https://kenney.nl/assets?category=audio\n" +
                "    CC0; pre-made cartoon/UI SFX, perfect for kids' games.\n\n" +
                "  - OpenGameArt.org      -- https://opengameart.org/art-search-advanced?field_art_type_tid%5B%5D=12\n" +
                "    Mix of CC0 / CC-BY / GPL. ALWAYS check the per-asset licence.\n\n" +
                "  - Freesound.org        -- https://freesound.org\n" +
                "    Per-asset CC licence. Filter by 'Creative Commons 0' for safest use.\n\n" +
                "  - Incompetech (Kevin MacLeod) -- https://incompetech.com/music\n" +
                "    CC-BY; great for friendly background music. Credit the author.\n\n" +
                "  - Chosic Music         -- https://www.chosic.com/free-music/all/\n" +
                "    Filters by licence; lots of CC0 background tracks.\n\n" +
                "FORMAT NOTES\n" +
                "  - .ogg is best for music (small + Unity decodes natively).\n" +
                "  - .wav is best for short SFX (no decode delay on first play).\n" +
                "  - Avoid stereo files > 1 minute for SFX; they bloat the build.\n" +
                "  - For music, set Unity Import Settings -> Load Type = Streaming.\n" +
                "  - For SFX, leave it on Decompress On Load (default).\n\n" +
                "After saving the file, just press Play -- AudioManager.TryLoadFromResources()\n" +
                "picks up the new clip automatically; no inspector wiring needed.";
            EditorUtility.DisplayDialog("MathEdu - Royalty-Free Audio Sources",
                body, "Got it");
        }

        // ===================================================================
        //                       OPEN / CREATE FOLDER
        // ===================================================================

        [MenuItem("MathEdu/Audio/Open Audio Resources Folder", priority = 10)]
        public static void OpenAudioFolder()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(AudioDir);
            AssetDatabase.Refresh();
            // Reveal in Finder / Explorer.
            string fullPath = Path.GetFullPath(AudioDir);
            EditorUtility.RevealInFinder(fullPath);
        }

        // ===================================================================
        //                       LIST EXPECTED FILE NAMES
        // ===================================================================

        [MenuItem("MathEdu/Audio/Print Expected File Names", priority = 20)]
        public static void PrintExpectedFileNames()
        {
            string msg =
                "Drop these files into Assets/Resources/Audio/ and AudioManager " +
                "will pick them up automatically:\n\n" +

                "  -- MUSIC --\n" +
                "    music_menu.ogg        Main menu / setup screens (loops).\n" +
                "    music_play.ogg        Gameplay scenes (loops).\n\n" +

                "  -- SFX --\n" +
                "    sfx_correct.wav       Correct answer.\n" +
                "    sfx_wrong.wav         Wrong answer.\n" +
                "    sfx_tap.wav           UI tap / button click.\n" +
                "    sfx_hint.wav          Hint button.\n" +
                "    sfx_levelComplete.wav Level-complete fanfare.\n" +
                "    sfx_starReveal.wav    Each star pop.\n" +
                "    sfx_streak.wav        Streak hit.\n" +
                "    sfx_timerTick.wav     Last-5-seconds tick.\n" +
                "    sfx_timerExpire.wav   Time's up.\n" +
                "    sfx_pageTransition.wav Scene change swoosh.\n" +
                "    sfx_badgeUnlocked.wav Badge earn.\n" +
                "    sfx_lose.wav          Run ended.\n" +
                "    sfx_swoosh.wav        Menu hover / select.\n\n" +

                ".wav and .mp3 are also accepted by Unity; .ogg is recommended for music.";
            Debug.Log("[MathEdu] " + msg);
            EditorUtility.DisplayDialog("MathEdu - Audio File Names",
                msg, "OK");
        }

        // ===================================================================
        //                       MUSIC ON / OFF SHORTCUTS
        // ===================================================================

        [MenuItem("MathEdu/Audio/Disable Background Music (for this save)", priority = 30)]
        public static void DisableMusic()
        {
            if (!EditorUtility.DisplayDialog("MathEdu - Music",
                "Turn the in-game music slider to 0 for the currently-saved\n" +
                "player profile? You can re-enable it from the in-game Settings\n" +
                "screen at any time.\n\n" +
                "This flips PlayerProfile.musicOn = false and writes the save.",
                "Yes, mute music", "Cancel")) return;

            // Use the same PlayerPrefs keys SaveSystem mirrors; toggling on
            // the live profile happens on next play.
            var key = "mathedu.profile";
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json))
            {
                EditorUtility.DisplayDialog("MathEdu",
                    "No saved profile found yet. Run the game once to create one,\n" +
                    "then come back to this menu (or just turn music off in the\n" +
                    "in-game Settings screen).",
                    "OK");
                return;
            }
            try
            {
                var prof = JsonUtility.FromJson<MathEdu.Data.PlayerProfile>(json);
                prof.musicOn = false;
                string updated = JsonUtility.ToJson(prof, prettyPrint: true);
                PlayerPrefs.SetString(key, updated);
                PlayerPrefs.SetString("mathedu.profile.backup", updated);
                PlayerPrefs.Save();
                EditorUtility.DisplayDialog("MathEdu - Music muted",
                    "PlayerProfile.musicOn set to false in the saved profile.\n" +
                    "Press Play again and the game will start in silence.\n" +
                    "Re-enable from in-game Settings > Music whenever you like.",
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MathEdu] Could not patch saved profile: {e.Message}");
            }
        }

        [MenuItem("MathEdu/Audio/Enable Background Music (for this save)", priority = 31)]
        public static void EnableMusic()
        {
            var key = "mathedu.profile";
            string json = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(json))
            {
                EditorUtility.DisplayDialog("MathEdu",
                    "No saved profile found yet. Run the game once first.", "OK");
                return;
            }
            try
            {
                var prof = JsonUtility.FromJson<MathEdu.Data.PlayerProfile>(json);
                prof.musicOn = true;
                string updated = JsonUtility.ToJson(prof, prettyPrint: true);
                PlayerPrefs.SetString(key, updated);
                PlayerPrefs.SetString("mathedu.profile.backup", updated);
                PlayerPrefs.Save();
                EditorUtility.DisplayDialog("MathEdu - Music enabled",
                    "PlayerProfile.musicOn set to true. Music will play on next Play.",
                    "OK");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MathEdu] Could not patch saved profile: {e.Message}");
            }
        }

        // ===================================================================
        //                              Internal
        // ===================================================================
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            string leaf   = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
