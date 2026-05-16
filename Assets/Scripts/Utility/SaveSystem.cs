// -----------------------------------------------------------------------------
// SaveSystem.cs
// -----------------------------------------------------------------------------
// Single source of truth for persistence. Writes the player profile as a JSON
// file under Application.persistentDataPath, with a PlayerPrefs fallback for
// platforms that occasionally fail to write the file (older Android variants).
// -----------------------------------------------------------------------------

using System.IO;
using MathEdu.Data;
using UnityEngine;

namespace MathEdu.Utility
{
    public static class SaveSystem
    {
        private const string FileName       = "player_profile.json";
        private const string PrefsKey       = "mathedu.profile";
        private const string PrefsBackupKey = "mathedu.profile.backup";

        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, FileName);

        public static void Save(PlayerProfile profile)
        {
            if (profile == null) return;
            string json = JsonUtility.ToJson(profile, prettyPrint: true);

            try
            {
                File.WriteAllText(FilePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] File save failed ({e.Message}); falling back to PlayerPrefs.");
            }

            // Always mirror to PlayerPrefs as a safety net.
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.SetString(PrefsBackupKey, json);
            PlayerPrefs.Save();
        }

        public static PlayerProfile Load()
        {
            // 1) Try JSON file
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    var prof = JsonUtility.FromJson<PlayerProfile>(json);
                    if (prof != null) return prof;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] File load failed: {e.Message}");
            }

            // 2) Fall back to PlayerPrefs
            string pp = PlayerPrefs.GetString(PrefsKey, "");
            if (!string.IsNullOrEmpty(pp))
            {
                try
                {
                    var prof = JsonUtility.FromJson<PlayerProfile>(pp);
                    if (prof != null) return prof;
                }
                catch { /* ignore */ }
            }

            // 3) Fresh profile
            return new PlayerProfile();
        }

        public static void DeleteAll()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.DeleteKey(PrefsBackupKey);
            PlayerPrefs.Save();
        }
    }
}
