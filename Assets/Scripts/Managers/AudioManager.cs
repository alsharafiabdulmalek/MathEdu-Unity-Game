// -----------------------------------------------------------------------------
// AudioManager.cs
// -----------------------------------------------------------------------------
// Plays SFX and music. Generates pleasant procedural tones at runtime so the
// project works with zero packaged audio assets. Once real .wav clips are
// dropped into Assets/Resources/Audio they override the procedural fallbacks.
//
// Named SFX (call AudioManager.PlaySFX(name) or one of the legacy convenience
// methods). All names have a procedural fallback so the call never silently
// no-ops because the clip is missing:
//
//   "correct"        Pleasant ding, high pitch                  PlayCorrect()
//   "wrong"          Low descending buzz                        PlayWrong()
//   "tap"            Soft click for any button press            PlayTap()
//   "levelComplete"  Fanfare jingle                             PlayWin()
//   "starReveal"     Sparkle (called 1-3 times)
//   "timerTick"      Short tick when timer < 5 s                PlayTick()
//   "timerExpire"    Alarm when the timer hits zero
//   "pageTransition" Soft whoosh on scene change
//   "badgeUnlocked"  Triumphant chime
//   "lose"           Sad descending notes                       PlayLose()
//
// Volume control:
//   - PlayerProfile.musicVolume / sfxVolume are the master values (0..1).
//   - PlayerProfile.musicOn / sfxOn are master switches; flipping them off
//     drives the corresponding AudioSource to 0 without forgetting the slider
//     value.
//   - The Settings scene calls SetMusicVolume / SetSfxVolume after every
//     change.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using MathEdu.Data;
using UnityEngine;

namespace MathEdu.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private AudioSource _sfxSource;
        private AudioSource _musicSource;
        private PlayerProfile _profile;

        // Named clip cache. Keys: "correct", "wrong", "tap", ...
        private readonly Dictionary<string, AudioClip> _clips =
            new Dictionary<string, AudioClip>();
        private AudioClip _musicClip;

        public void Init(PlayerProfile profile)
        {
            _profile = profile;

            _sfxSource              = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake  = false;
            _sfxSource.volume       = EffectiveSfxVolume();

            _musicSource             = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop        = true;
            _musicSource.volume      = EffectiveMusicVolume();

            BuildProceduralClips();
            TryLoadFromResources();
        }

        // -------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------

        /// <summary>Play a named SFX clip. Unknown names log a Warning.</summary>
        public void PlaySFX(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!_clips.TryGetValue(name, out var clip) || clip == null)
            {
                Debug.LogWarning($"[AudioManager] Unknown SFX name '{name}'.");
                return;
            }
            Play(clip);
        }

        public bool HasClip(string name) => _clips.ContainsKey(name) && _clips[name] != null;

        // Legacy short-hand methods (still used in places). All delegate to
        // the named map so the procedural fallback is shared.
        public void PlayCorrect() => PlaySFX("correct");
        public void PlayWrong()   => PlaySFX("wrong");
        public void PlayTap()     => PlaySFX("tap");
        public void PlayWin()     => PlaySFX("levelComplete");
        public void PlayLose()    => PlaySFX("lose");
        public void PlayTick()    => PlaySFX("timerTick");

        // -------------------------------------------------------------------
        // Music
        // -------------------------------------------------------------------
        public void PlayMusic(AudioClip clip = null)
        {
            if (clip != null) _musicClip = clip;
            if (_musicClip == null) return;
            if (_musicSource.clip == _musicClip && _musicSource.isPlaying) return;
            _musicSource.clip = _musicClip;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            if (_musicSource != null) _musicSource.Stop();
        }

        // -------------------------------------------------------------------
        // Volume API (drives both PlayerProfile values and live sources)
        // -------------------------------------------------------------------
        public void ApplyVolumeFromProfile()
        {
            if (_profile == null) return;
            _sfxSource.volume   = EffectiveSfxVolume();
            _musicSource.volume = EffectiveMusicVolume();
        }

        public void SetMusicVolume(float v)
        {
            if (_profile != null) _profile.musicVolume = Mathf.Clamp01(v);
            if (_musicSource != null) _musicSource.volume = EffectiveMusicVolume();
        }

        public void SetSfxVolume(float v)
        {
            if (_profile != null) _profile.sfxVolume = Mathf.Clamp01(v);
            if (_sfxSource != null) _sfxSource.volume = EffectiveSfxVolume();
        }

        private float EffectiveMusicVolume()
        {
            if (_profile == null) return 0.7f;
            return _profile.musicOn ? _profile.musicVolume : 0f;
        }

        private float EffectiveSfxVolume()
        {
            if (_profile == null) return 1f;
            return _profile.sfxOn ? _profile.sfxVolume : 0f;
        }

        // -------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------
        private void Play(AudioClip clip)
        {
            if (clip != null && _sfxSource != null && EffectiveSfxVolume() > 0)
                _sfxSource.PlayOneShot(clip);
        }

        private void TryLoadFromResources()
        {
            foreach (var key in new[]
            {
                "correct", "wrong", "tap", "levelComplete",
                "starReveal", "timerTick", "timerExpire",
                "pageTransition", "badgeUnlocked", "lose"
            })
            {
                var c = Resources.Load<AudioClip>($"Audio/sfx_{key}");
                if (c != null) _clips[key] = c;
            }
            var music = Resources.Load<AudioClip>("Audio/music_menu");
            if (music != null) _musicClip = music;
        }

        private void BuildProceduralClips()
        {
            _clips["correct"]        = MakeTone("sfx_correct", 880,  0.18f, 0.10f, addFifth: true);
            _clips["wrong"]          = MakeTone("sfx_wrong",   220,  0.30f, 0.20f, descending: true);
            _clips["tap"]            = MakeTone("sfx_tap",    1320,  0.06f, 0.04f);
            _clips["levelComplete"]  = MakeArpeggio("sfx_win", new[]{ 523, 659, 784, 1046 }, 0.10f);
            _clips["lose"]           = MakeArpeggio("sfx_lose",new[]{ 523, 392, 311, 220  }, 0.12f);
            _clips["timerTick"]      = MakeTone("sfx_tick",   1500,  0.02f, 0.01f);

            // New named clips:
            _clips["starReveal"]     = MakeArpeggio("sfx_star",
                new[]{ 1175, 1568, 2093 }, 0.07f);                       // bright sparkle
            _clips["timerExpire"]    = MakeAlarm("sfx_expire", 660);     // alarm pulse
            _clips["pageTransition"] = MakeWhoosh("sfx_page");           // soft whoosh
            _clips["badgeUnlocked"]  = MakeArpeggio("sfx_badge",
                new[]{ 523, 784, 1046, 1568 }, 0.10f);                    // triumphant chime
        }

        // -------------------------------------------------------------------
        // Procedural waveform helpers
        // -------------------------------------------------------------------
        private const int SampleRate = 22050;

        private static AudioClip MakeTone(string name, float freq, float duration,
                                          float decay, bool addFifth = false,
                                          bool descending = false)
        {
            int total = Mathf.RoundToInt(SampleRate * duration);
            var samples = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t / decay);
                float f = descending ? freq * (1f - 0.4f * t / duration) : freq;
                float v = Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f;
                if (addFifth) v += Mathf.Sin(2f * Mathf.PI * (f * 1.5f) * t) * 0.4f;
                samples[i] = v * env;
            }
            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip MakeArpeggio(string name, int[] freqs, float stepDur)
        {
            int stepSamples = Mathf.RoundToInt(SampleRate * stepDur);
            int total = stepSamples * freqs.Length;
            var samples = new float[total];
            for (int n = 0; n < freqs.Length; n++)
            {
                float freq = freqs[n];
                for (int i = 0; i < stepSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = Mathf.Exp(-t / 0.08f);
                    samples[n * stepSamples + i] =
                        Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f * env;
                }
            }
            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Two-beat alarm. Sounds urgent without being unpleasant.
        /// </summary>
        private static AudioClip MakeAlarm(string name, float freq)
        {
            float dur = 0.7f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var samples = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                // Square-ish wave by tanh of a sine.
                float wave = Mathf.Tan(Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * freq * t) * 3f, -1.4f, 1.4f));
                // Pulse twice across the duration.
                float pulse = (t < 0.10f || (t > 0.35f && t < 0.45f)) ? 1f : 0f;
                samples[i] = Mathf.Clamp(wave * 0.4f * pulse, -0.7f, 0.7f);
            }
            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Soft band-passed white-noise whoosh for scene transitions.</summary>
        private static AudioClip MakeWhoosh(string name)
        {
            float dur = 0.30f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var samples = new float[total];
            var rng = new System.Random(1);
            float prev = 0f;
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float n = (float)(rng.NextDouble() * 2 - 1);
                // Low-pass + attack/release envelope.
                prev = Mathf.Lerp(prev, n, 0.18f);
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur)); // 0→1→0
                samples[i] = prev * 0.35f * env;
            }
            var clip = AudioClip.Create(name, total, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
