// -----------------------------------------------------------------------------
// AudioManager.cs
// -----------------------------------------------------------------------------
// Plays SFX and music. Generates pleasant procedural tones at runtime so the
// project works with zero packaged audio assets. Once real .wav clips are
// dropped into Assets/Resources/Audio, AudioManager will prefer those.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using UnityEngine;

namespace MathEdu.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private PlayerProfile _profile;

        // Procedural tone cache
        private AudioClip _correctClip;
        private AudioClip _wrongClip;
        private AudioClip _tapClip;
        private AudioClip _winClip;
        private AudioClip _loseClip;
        private AudioClip _tickClip;

        public void Init(PlayerProfile profile)
        {
            _profile = profile;

            _sfxSource              = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake  = false;
            _sfxSource.volume       = profile != null ? profile.sfxVolume : 1f;

            _musicSource             = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop        = true;
            _musicSource.volume      = profile != null ? profile.musicVolume : 0.7f;

            BuildProceduralClips();
            TryLoadFromResources();
        }

        public void PlayCorrect() => Play(_correctClip);
        public void PlayWrong()   => Play(_wrongClip);
        public void PlayTap()     => Play(_tapClip);
        public void PlayWin()     => Play(_winClip);
        public void PlayLose()    => Play(_loseClip);
        public void PlayTick()    => Play(_tickClip);

        public void ApplyVolumeFromProfile()
        {
            if (_profile == null) return;
            _sfxSource.volume   = _profile.sfxVolume;
            _musicSource.volume = _profile.musicVolume;
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && _sfxSource != null)
                _sfxSource.PlayOneShot(clip);
        }

        private void TryLoadFromResources()
        {
            var corr = Resources.Load<AudioClip>("Audio/sfx_correct");
            if (corr != null) _correctClip = corr;
            var wrong = Resources.Load<AudioClip>("Audio/sfx_wrong");
            if (wrong != null) _wrongClip = wrong;
            var tap = Resources.Load<AudioClip>("Audio/sfx_tap");
            if (tap != null) _tapClip = tap;
            var win = Resources.Load<AudioClip>("Audio/sfx_win");
            if (win != null) _winClip = win;
            var lose = Resources.Load<AudioClip>("Audio/sfx_lose");
            if (lose != null) _loseClip = lose;
            var tick = Resources.Load<AudioClip>("Audio/sfx_tick");
            if (tick != null) _tickClip = tick;
        }

        private void BuildProceduralClips()
        {
            _correctClip = MakeTone("sfx_correct", 880,  0.18f, 0.10f, addFifth: true);
            _wrongClip   = MakeTone("sfx_wrong",   220,  0.30f, 0.20f, descending: true);
            _tapClip     = MakeTone("sfx_tap",    1320,  0.06f, 0.04f);
            _winClip     = MakeArpeggio("sfx_win", new[]{ 523, 659, 784, 1046 }, 0.10f);
            _loseClip    = MakeArpeggio("sfx_lose",new[]{ 523, 392, 311, 220  }, 0.12f);
            _tickClip    = MakeTone("sfx_tick",   1500,  0.02f, 0.01f);
        }

        private static AudioClip MakeTone(string name, float freq, float duration,
                                          float decay, bool addFifth = false,
                                          bool descending = false)
        {
            const int sampleRate = 22050;
            int total = Mathf.RoundToInt(sampleRate * duration);
            var samples = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Exp(-t / decay);
                float f = descending ? freq * (1f - 0.4f * t / duration) : freq;
                float v = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f;
                if (addFifth) v += Mathf.Sin(2f * Mathf.PI * (f * 1.5f) * t) * 0.4f;
                samples[i] = v * env;
            }
            var clip = AudioClip.Create(name, total, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip MakeArpeggio(string name, int[] freqs, float stepDur)
        {
            const int sampleRate = 22050;
            int stepSamples = Mathf.RoundToInt(sampleRate * stepDur);
            int total = stepSamples * freqs.Length;
            var samples = new float[total];
            for (int n = 0; n < freqs.Length; n++)
            {
                float freq = freqs[n];
                for (int i = 0; i < stepSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = Mathf.Exp(-t / 0.08f);
                    samples[n * stepSamples + i] =
                        Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f * env;
                }
            }
            var clip = AudioClip.Create(name, total, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
