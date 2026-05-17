// -----------------------------------------------------------------------------
// GameManager.cs
// -----------------------------------------------------------------------------
// Persistent root singleton. Holds the MathDatabase, the current PlayerProfile,
// the active GameSession, and the manager references (Audio, Progress, UI,
// VFX). Auto-creates itself the first time anyone calls GameManager.Instance
// so scenes can be played standalone in the editor.
//
// New responsibilities since the initial release:
//   • Loads (and exposes) the optional AvatarLibrary for the Player Setup
//     screen — falls back to a runtime-built default library.
//   • Owns a VFXManager so any scene can fire Epic Toon FX prefabs through
//     `GameManager.Instance.VFX.PlayCorrect()`.
//   • Tracks per-subject statistics through ProgressManager so the Parental
//     Dashboard always has fresh numbers.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using MathEdu.Utility;
using UnityEngine;

namespace MathEdu.Managers
{
    public class GameManager : MonoBehaviour
    {
        // ------------------------------------------------------ singleton --
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[GameManager]");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // ------------------------------------------------------ state ------
        [Header("Master Data (optional, auto-built if null)")]
        public MathDatabase   database;
        public AvatarLibrary  avatarLibrary;

        public PlayerProfile  Profile { get; private set; }
        public GameSession    Session { get; private set; } = new GameSession();
        public AvatarLibrary  Avatars => avatarLibrary;

        public AudioManager    Audio    { get; private set; }
        public ProgressManager Progress { get; private set; }
        public UIManager       UI       { get; private set; }
        public VFXManager      VFX      { get; private set; }

        // ------------------------------------------------------ lifecycle --
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureDatabase();
            EnsureAvatarLibrary();
            Profile  = SaveSystem.Load();
            UnlockStartingLevels();

            Audio    = gameObject.AddComponent<AudioManager>();
            Progress = gameObject.AddComponent<ProgressManager>();
            UI       = gameObject.AddComponent<UIManager>();
            VFX      = gameObject.AddComponent<VFXManager>();

            Audio.Init(Profile);
            VFX.Init();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Save(Profile);
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Save(Profile);
        }

        // ------------------------------------------------------ helpers ----
        private void EnsureDatabase()
        {
            if (database != null && database.grades != null && database.grades.Count > 0)
                return;

            // Try Resources first so a build-time asset wins.
            var fromResources = Resources.Load<MathDatabase>("MathDatabase");
            if (fromResources != null && fromResources.grades.Count > 0)
            {
                database = fromResources;
                return;
            }

            // Fallback: procedural runtime database.
            database = DatabaseBootstrapper.BuildInMemory();
            Debug.Log($"[GameManager] Built runtime MathDatabase with {database.TotalQuestionCount} questions.");
        }

        private void EnsureAvatarLibrary()
        {
            if (avatarLibrary != null && avatarLibrary.avatars.Count > 0) return;

            var fromResources = Resources.Load<AvatarLibrary>("AvatarLibrary");
            if (fromResources != null && fromResources.avatars.Count > 0)
            {
                avatarLibrary = fromResources;
                return;
            }
            avatarLibrary = AvatarLibrary.BuildDefault();
        }

        private void UnlockStartingLevels()
        {
            if (Profile == null || database == null) return;
            foreach (var grade in database.grades)
            {
                foreach (var subject in grade.subjects)
                {
                    if (subject.levels.Count > 0 && subject.levels[0] != null)
                        Profile.Unlock(subject.levels[0].levelId);
                }
            }
            SaveSystem.Save(Profile);
        }

        // ------------------------------------------------------ API --------
        public void SelectGrade(int g)        { Session.selectedGrade = g; Profile.selectedGrade = g; }
        public void SelectSubject(MathSubject s) { Session.selectedSubject = s; }
        public void SelectLevel(int l)        { Session.selectedLevel = l; }
        public void SelectMode(LearningMode m){ Session.selectedMode = m; }

        public LevelData CurrentLevel =>
            database != null
                ? database.GetLevel(Session.selectedGrade, Session.selectedSubject, Session.selectedLevel)
                : null;

        public SubjectData CurrentSubject =>
            database != null
                ? database.GetSubject(Session.selectedGrade, Session.selectedSubject)
                : null;

        public GradeData CurrentGrade =>
            database != null ? database.GetGrade(Session.selectedGrade) : null;

        public void SaveProfile() => SaveSystem.Save(Profile);
    }
}
