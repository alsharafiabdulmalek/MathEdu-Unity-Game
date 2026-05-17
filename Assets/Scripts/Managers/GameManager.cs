// -----------------------------------------------------------------------------
// GameManager.cs
// -----------------------------------------------------------------------------
// Persistent root singleton. Holds the MathDatabase, the current PlayerProfile,
// the active GameSession, and the manager references (Audio, Progress, UI,
// VFX). Auto-creates itself the first time anyone calls GameManager.Instance
// so scenes can be played standalone in the editor.
//
// Standalone-scene safety:
//   • If a scene is opened directly without going through Bootstrap, the
//     first call to GameManager.Instance lazily creates the singleton,
//     loads the saved profile (or builds a default), and registers
//     fallback Subject/Grade/Level selections so the gameplay managers
//     never crash on null lookups.
//   • Every helper property (CurrentLevel/Subject/Grade) returns null only
//     when the database itself is missing; callers always handle null.
//
// Lazy database fill:
//   • When the in-memory database (DatabaseBootstrapper.BuildInMemory) is in
//     use, levels are created as skeletons. CurrentLevel populates each
//     level's questions / lesson text / story text on first access via
//     DatabaseBootstrapper.EnsureLevelContent(). This keeps Bootstrap.unity
//     instant on constrained machines and spreads the question-generation
//     cost over the course of normal play.
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
                        // Awake handles DontDestroyOnLoad + child managers.
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

        private bool _initialized;

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
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            EnsureDatabase();
            EnsureAvatarLibrary();

            Profile = SaveSystem.Load() ?? new PlayerProfile();

            // Initialise sensible defaults on a brand-new profile so the
            // gameplay scenes can run standalone without going through
            // PlayerSetup → MainMenu first.
            if (Profile.selectedGrade <= 0) Profile.selectedGrade = 1;
            if (Session == null) Session = new GameSession();
            Session.selectedGrade   = Profile.selectedGrade;
            if (database != null && database.grades.Count > 0)
            {
                var g = database.GetGrade(Session.selectedGrade);
                if (g != null && g.subjects.Count > 0)
                    Session.selectedSubject = g.subjects[0].subject;
            }
            if (Session.selectedLevel < 1) Session.selectedLevel = 1;

            UnlockStartingLevels();

            // Composite managers (added once per GameManager instance).
            Audio    = gameObject.AddComponent<AudioManager>();
            Progress = gameObject.AddComponent<ProgressManager>();
            UI       = gameObject.AddComponent<UIManager>();
            VFX      = gameObject.AddComponent<VFXManager>();

            Audio.Init(Profile);
            VFX.Init();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && Profile != null) SaveSystem.Save(Profile);
        }

        private void OnApplicationQuit()
        {
            if (Profile != null) SaveSystem.Save(Profile);
        }

        // ------------------------------------------------------ helpers ----
        private void EnsureDatabase()
        {
            if (database != null && database.grades != null && database.grades.Count > 0)
                return;

            // Try Resources first so a build-time asset wins.
            var fromResources = Resources.Load<MathDatabase>("MathDatabase");
            if (fromResources != null && fromResources.grades != null
                && fromResources.grades.Count > 0)
            {
                database = fromResources;
                return;
            }

            // Fallback: procedural runtime database — skeleton only, levels
            // are filled with questions / text lazily on first access via
            // CurrentLevel below. Cost here: < 100 ms even on a low-RAM Mac.
            database = DatabaseBootstrapper.BuildInMemory();
            Debug.Log("[GameManager] Built runtime MathDatabase (lazy skeleton — " +
                      "questions generated on demand).");
        }

        private void EnsureAvatarLibrary()
        {
            if (avatarLibrary != null && avatarLibrary.avatars != null
                && avatarLibrary.avatars.Count > 0)
                return;

            var fromResources = Resources.Load<AvatarLibrary>("AvatarLibrary");
            if (fromResources != null && fromResources.avatars != null
                && fromResources.avatars.Count > 0)
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
                if (grade == null) continue;
                foreach (var subject in grade.subjects)
                {
                    if (subject == null) continue;
                    if (subject.levels != null && subject.levels.Count > 0 && subject.levels[0] != null)
                    {
                        Profile.Unlock(subject.levels[0].levelId);
                        Profile.RecordSubjectHighestUnlocked(subject.SubjectKey, 1);
                    }
                }
            }
            SaveSystem.Save(Profile);
        }

        // ------------------------------------------------------ API --------
        public void SelectGrade(int g)
        {
            Session.selectedGrade = g;
            if (Profile != null) Profile.selectedGrade = g;
        }
        public void SelectSubject(MathSubject s) { Session.selectedSubject = s; }
        public void SelectLevel(int l)        { Session.selectedLevel = l; }
        public void SelectMode(LearningMode m){ Session.selectedMode = m; }

        /// <summary>
        /// Returns the currently selected LevelData, populating its
        /// questions / lesson text / story text on first access via the
        /// lazy DatabaseBootstrapper.EnsureLevelContent() helper. Reading
        /// CurrentLevel from the same level multiple times is cheap — the
        /// second hit is a single null/empty check.
        /// </summary>
        public LevelData CurrentLevel
        {
            get
            {
                if (database == null) return null;
                var level = database.GetLevel(
                    Session.selectedGrade,
                    Session.selectedSubject,
                    Session.selectedLevel);
                if (level != null)
                {
                    DatabaseBootstrapper.EnsureLevelContent(
                        level, Session.selectedGrade, Session.selectedSubject);
                }
                return level;
            }
        }

        public SubjectData CurrentSubject =>
            database != null
                ? database.GetSubject(Session.selectedGrade, Session.selectedSubject)
                : null;

        public GradeData CurrentGrade =>
            database != null ? database.GetGrade(Session.selectedGrade) : null;

        public void SaveProfile()
        {
            if (Profile != null) SaveSystem.Save(Profile);
        }
    }
}
