// -----------------------------------------------------------------------------
// GameManager.cs
// -----------------------------------------------------------------------------
// Persistent root singleton. Holds the MathDatabase, the current PlayerProfile,
// the active GameSession, and the manager references (Audio, Progress, UI,
// VFX). Auto-creates itself the first time anyone calls GameManager.Instance
// so scenes can be played standalone in the editor.
//
// Lazy database fill, standalone-scene safety, and i18n init all live here.
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
                    }
                }
                return _instance;
            }
        }

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

            // i18n: apply the persisted language as soon as the profile is
            // loaded so the very first UI string the player sees is already
            // in their chosen language.
            Localization.SetFromCode(Profile.language);

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

        private void EnsureDatabase()
        {
            if (database != null && database.grades != null && database.grades.Count > 0)
                return;

            var fromResources = Resources.Load<MathDatabase>("MathDatabase");
            if (fromResources != null && fromResources.grades != null
                && fromResources.grades.Count > 0)
            {
                database = fromResources;
                return;
            }

            database = DatabaseBootstrapper.BuildInMemory();
            Debug.Log("[GameManager] Built runtime MathDatabase (lazy skeleton).");
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

        public void SelectGrade(int g)
        {
            Session.selectedGrade = g;
            if (Profile != null) Profile.selectedGrade = g;
        }
        public void SelectSubject(MathSubject s) { Session.selectedSubject = s; }
        public void SelectLevel(int l)        { Session.selectedLevel = l; }
        public void SelectMode(LearningMode m){ Session.selectedMode = m; }

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
