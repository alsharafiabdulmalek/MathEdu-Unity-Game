// -----------------------------------------------------------------------------
// SaveSystem.cs
// -----------------------------------------------------------------------------
// Single source of truth for persistence. Writes the player profile as a JSON
// file under Application.persistentDataPath, with a PlayerPrefs fallback for
// platforms that occasionally fail to write the file (older Android variants).
//
// Defensive notes
// ---------------
// On some platforms (notably Windows + Unity Editor) Application.persistent-
// DataPath can throw an IOException if the resolved directory name contains
// disallowed characters such as a trailing space (e.g. when productName is
// "Ahmed and Elain Math "). This used to spam the console every frame and
// prevent the profile from loading. We now:
//
//   1. Cache the resolved directory exactly once.
//   2. Catch any exception from Application.persistentDataPath and fall back
//      to Application.temporaryCachePath.
//   3. If both throw, fall back to PlayerPrefs only (no file I/O).
//   4. Always mirror the JSON into PlayerPrefs so a corrupted directory can
//      never cause data loss.
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

        // ----------------------------------------------------- path cache --
        private static bool   _dirResolved;
        private static string _resolvedDir;     // null => use PlayerPrefs only
        private static bool   _warned;

        /// <summary>
        /// Resolves a writable directory exactly once and survives the
        /// "directory name not valid" exceptions Unity throws when the
        /// productName/companyName contains characters the host OS rejects
        /// (trailing spaces, control chars, reserved Win32 names, etc.).
        /// Returns null when no usable directory is available — in that
        /// case the caller skips file I/O and uses PlayerPrefs only.
        /// </summary>
        private static string ResolveDirectory()
        {
            if (_dirResolved) return _resolvedDir;
            _dirResolved = true;

            // 1) Preferred: persistentDataPath
            string dir = TryGetDirectory(() => Application.persistentDataPath);
            if (!string.IsNullOrEmpty(dir))
            {
                _resolvedDir = dir;
                return _resolvedDir;
            }

            // 2) Fallback: temporaryCachePath (less ideal but usually valid)
            dir = TryGetDirectory(() => Application.temporaryCachePath);
            if (!string.IsNullOrEmpty(dir))
            {
                if (!_warned)
                {
                    Debug.LogWarning(
                        "[SaveSystem] persistentDataPath unavailable; falling back to temporaryCachePath. " +
                        "Check PlayerSettings -> Product Name for invalid characters (e.g. trailing spaces).");
                    _warned = true;
                }
                _resolvedDir = dir;
                return _resolvedDir;
            }

            // 3) Give up on file I/O entirely. PlayerPrefs still works.
            if (!_warned)
            {
                Debug.LogWarning(
                    "[SaveSystem] No writable directory available. " +
                    "Profile will be saved to PlayerPrefs only.");
                _warned = true;
            }
            _resolvedDir = null;
            return _resolvedDir;
        }

        private static string TryGetDirectory(System.Func<string> getter)
        {
            string raw;
            try { raw = getter(); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Path getter threw: {e.Message}");
                return null;
            }

            if (string.IsNullOrEmpty(raw)) return null;

            // Trim trailing whitespace from each path segment — Windows
            // refuses to create directories that end with a space.
            string sanitized = SanitizePath(raw);

            try
            {
                if (!Directory.Exists(sanitized))
                    Directory.CreateDirectory(sanitized);
                // Probe write access with a tiny temp file.
                string probe = Path.Combine(sanitized, ".write_probe");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return sanitized;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Directory unusable ({sanitized}): {e.Message}");
                return null;
            }
        }

        private static string SanitizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            // Split on either separator (handles mixed Windows/Unix paths)
            char[] seps = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            var parts = path.Split(seps);
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                // Strip trailing/leading whitespace and dots, but only on
                // segments — never touch a leading drive letter ("C:").
                string trimmed = parts[i].TrimEnd(' ', '.');
                if (i > 0) trimmed = trimmed.TrimStart(' ');
                if (!string.IsNullOrEmpty(trimmed)) parts[i] = trimmed;
            }
            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
        }

        private static string FilePathOrNull
        {
            get
            {
                var dir = ResolveDirectory();
                return string.IsNullOrEmpty(dir) ? null : Path.Combine(dir, FileName);
            }
        }

        // ----------------------------------------------------- public API --
        public static void Save(PlayerProfile profile)
        {
            if (profile == null) return;
            string json = JsonUtility.ToJson(profile, prettyPrint: true);

            string path = FilePathOrNull;
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    File.WriteAllText(path, json);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] File save failed ({e.Message}); using PlayerPrefs.");
                }
            }

            // Always mirror to PlayerPrefs as a safety net.
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.SetString(PrefsBackupKey, json);
            PlayerPrefs.Save();
        }

        public static PlayerProfile Load()
        {
            // 1) Try JSON file (only if we have a usable directory)
            string path = FilePathOrNull;
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        var prof = JsonUtility.FromJson<PlayerProfile>(json);
                        if (prof != null) return prof;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SaveSystem] File load failed: {e.Message}");
                }
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

            // 3) Try the backup mirror.
            string backup = PlayerPrefs.GetString(PrefsBackupKey, "");
            if (!string.IsNullOrEmpty(backup))
            {
                try
                {
                    var prof = JsonUtility.FromJson<PlayerProfile>(backup);
                    if (prof != null) return prof;
                }
                catch { /* ignore */ }
            }

            // 4) Fresh profile
            return new PlayerProfile();
        }

        public static void DeleteAll()
        {
            string path = FilePathOrNull;
            if (!string.IsNullOrEmpty(path))
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
            }
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.DeleteKey(PrefsBackupKey);
            PlayerPrefs.Save();
        }
    }
}
