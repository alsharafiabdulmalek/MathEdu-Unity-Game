// -----------------------------------------------------------------------------
// AudioManager.cs
// -----------------------------------------------------------------------------
// Plays SFX and music. Generates pleasant procedural tones at runtime so the
// project works with zero packaged audio assets. Once real .wav / .ogg clips
// are dropped into Assets/Resources/Audio they override the procedural
// fallbacks (drop in `sfx_correct.wav`, `sfx_wrong.wav`, `music_menu.ogg`,
// `music_play.ogg`, etc. and the runtime auto-picks them up).
//
// Named SFX (call AudioManager.PlaySFX(name) or one of the legacy convenience
// methods). All names have a procedural fallback so the call never silently
// no-ops because the clip is missing:
//
//   "correct"        Pleasant rising 4ths chord with sparkle.   PlayCorrect()
//   "wrong"          Soft "uh-oh" descending two-note buzz.     PlayWrong()
//   "tap"            Smooth UI click with attack.               PlayTap()
//   "hint"           Friendly two-note "?" cue.
//   "levelComplete"  Triumphant five-note major fanfare.        PlayWin()
//   "streak"         Bright ascending arpeggio (3+ in a row).
//   "starReveal"     Two-bell sparkle (called 1-3 times).
//   "timerTick"      Short pizzicato tick (timer < 5 s).        PlayTick()
//   "timerExpire"    Soft alarm pulse (timer hits zero).
//   "pageTransition" Pillowy whoosh on scene change.
//   "badgeUnlocked"  Glowing chime cluster.
//   "lose"           Sad descending phrase.                     PlayLose()
//   "swoosh"         Quick swoosh for menu hover/select.
//
// Music:
//   - PlayMenuMusic() looks for Resources/Audio/music_menu (.ogg/.wav/.mp3).
//   - PlayGameplayMusic() looks for music_play.
//   - When no music clip is found, a calm procedural ambient loop is generated
//     so the player never plays in silence.
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
        private AudioClip _menuMusic;
        private AudioClip _gameplayMusic;

        // Light cooldown so a button mash doesn't drown the mixer.
        private readonly Dictionary<string, float> _lastPlayed =
            new Dictionary<string, float>();
        private const float TapCooldown = 0.04f;   // seconds

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
            // Throttle "tap" so rapid taps don't pile up (sounds harsh on iOS).
            if (name == "tap")
            {
                float now = Time.unscaledTime;
                if (_lastPlayed.TryGetValue(name, out float last) &&
                    now - last < TapCooldown) return;
                _lastPlayed[name] = now;
            }
            // Tiny random pitch variation makes repeated SFX feel less mechanical.
            float pitchJitter = name switch
            {
                "tap"        => Random.Range(0.97f, 1.03f),
                "correct"    => Random.Range(0.985f, 1.015f),
                "starReveal" => Random.Range(0.95f, 1.05f),
                _            => 1f
            };
            PlayWithPitch(clip, pitchJitter);
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

        /// <summary>Play the menu / settings ambient track (looping).</summary>
        public void PlayMenuMusic()
        {
            SwapMusic(_menuMusic);
        }

        /// <summary>Play the in-game ambient track (looping).</summary>
        public void PlayGameplayMusic()
        {
            SwapMusic(_gameplayMusic ?? _menuMusic);
        }

        /// <summary>Backwards-compat: play an explicit clip, or resume menu.</summary>
        public void PlayMusic(AudioClip clip = null)
        {
            if (clip != null) { SwapMusic(clip); return; }
            PlayMenuMusic();
        }

        public void StopMusic()
        {
            if (_musicSource != null) _musicSource.Stop();
        }

        private void SwapMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (_musicSource == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;
            _musicSource.clip = clip;
            _musicSource.Play();
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
            if (_profile == null) return 0.55f;
            return _profile.musicOn ? Mathf.Clamp01(_profile.musicVolume) * 0.7f : 0f;
        }

        private float EffectiveSfxVolume()
        {
            if (_profile == null) return 0.9f;
            return _profile.sfxOn ? Mathf.Clamp01(_profile.sfxVolume) * 0.9f : 0f;
        }

        // -------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------
        private void PlayWithPitch(AudioClip clip, float pitch)
        {
            if (clip == null || _sfxSource == null) return;
            if (EffectiveSfxVolume() <= 0f) return;
            // PlayOneShot ignores AudioSource.pitch in some flows, so we
            // briefly override pitch around the call. Reset afterwards.
            float prev = _sfxSource.pitch;
            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip);
            _sfxSource.pitch = prev;
        }

        private void TryLoadFromResources()
        {
            foreach (var key in new[]
            {
                "correct", "wrong", "tap", "hint", "levelComplete",
                "starReveal", "streak", "timerTick", "timerExpire",
                "pageTransition", "badgeUnlocked", "lose", "swoosh"
            })
            {
                var c = Resources.Load<AudioClip>($"Audio/sfx_{key}");
                if (c != null) _clips[key] = c;
            }
            var menu = Resources.Load<AudioClip>("Audio/music_menu");
            if (menu != null) _menuMusic = menu;
            var play = Resources.Load<AudioClip>("Audio/music_play");
            if (play != null) _gameplayMusic = play;
        }

        private void BuildProceduralClips()
        {
            // Polished UI sounds. Stereo, with ADSR envelopes and small chord
            // stacks so they don't sound like sine-wave testers.
            _clips["correct"]        = MakeCorrect();
            _clips["wrong"]          = MakeWrong();
            _clips["tap"]            = MakeTap();
            _clips["hint"]           = MakeHint();
            _clips["levelComplete"]  = MakeFanfare();
            _clips["lose"]           = MakeArpeggio("sfx_lose",
                new[] { 523f, 392f, 311f, 220f }, 0.14f, decay: 0.18f);
            _clips["timerTick"]      = MakeTick();

            _clips["starReveal"]     = MakeArpeggio("sfx_star",
                new[] { 1175f, 1568f, 2093f }, 0.07f, decay: 0.08f);
            _clips["streak"]         = MakeArpeggio("sfx_streak",
                new[] { 784f, 988f, 1175f, 1568f }, 0.08f, decay: 0.10f);
            _clips["timerExpire"]    = MakeAlarm("sfx_expire", 660);
            _clips["pageTransition"] = MakeWhoosh("sfx_page", 0.30f);
            _clips["swoosh"]         = MakeWhoosh("sfx_swoosh", 0.16f);
            _clips["badgeUnlocked"]  = MakeArpeggio("sfx_badge",
                new[] { 523f, 784f, 1046f, 1568f, 2093f }, 0.10f, decay: 0.18f);

            // Procedural ambient music — calm, looping, in C major. Used if no
            // music_menu / music_play clip is present in Resources/Audio/.
            if (_menuMusic == null) _menuMusic = MakeAmbientLoop("music_menu_proc");
            if (_gameplayMusic == null) _gameplayMusic = _menuMusic;
        }

        // -------------------------------------------------------------------
        // Procedural waveform helpers — stereo, 44.1 kHz
        // -------------------------------------------------------------------
        private const int SampleRate = 44100;

        /// <summary>
        /// "Correct" SFX: a bright two-note major sixth (C5 + A5) with a
        /// shimmering octave harmonic and a smooth attack. Sounds friendly
        /// rather than shrill.
        /// </summary>
        private static AudioClip MakeCorrect()
        {
            const float dur = 0.42f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            float f1 = 880f;        // A5
            float f2 = 1318.5f;     // E6 (major 6th up = colour)
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float env = ADSR(t, dur, attack: 0.012f, decay: 0.10f, sustain: 0.55f, release: 0.20f);
                float v = Mathf.Sin(2 * Mathf.PI * f1 * t) * 0.45f
                        + Mathf.Sin(2 * Mathf.PI * f2 * t) * 0.30f
                        + Mathf.Sin(2 * Mathf.PI * f1 * 2 * t) * 0.10f;
                // Soft saturation for warmth.
                v = Mathf.Tan(Mathf.Clamp(v * 0.6f, -1.2f, 1.2f)) * 0.6f;
                l[i] = v * env;
                r[i] = v * env * 0.98f;
            }
            return MakeStereo("sfx_correct", l, r);
        }

        /// <summary>
        /// "Wrong" SFX: a soft, low descending two-note buzz that says
        /// "uh-oh" without being harsh on kids' ears.
        /// </summary>
        private static AudioClip MakeWrong()
        {
            const float dur = 0.35f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float k = Mathf.Clamp01(t / dur);
                // Glide from 260 Hz → 170 Hz across the clip.
                float f = Mathf.Lerp(260f, 170f, k);
                float env = ADSR(t, dur, 0.01f, 0.10f, 0.6f, 0.18f);
                // Mostly sine with a hint of triangle to feel "rubbery"
                float s = Mathf.Sin(2 * Mathf.PI * f * t);
                float tri = Mathf.Asin(Mathf.Sin(2 * Mathf.PI * (f * 1.5f) * t)) * 0.45f;
                float v = (s * 0.7f + tri * 0.3f) * env * 0.65f;
                l[i] = v; r[i] = v;
            }
            return MakeStereo("sfx_wrong", l, r);
        }

        /// <summary>UI tap: short, soft "click" with quick attack.</summary>
        private static AudioClip MakeTap()
        {
            const float dur = 0.08f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            float f = 1400f;
            var rng = new System.Random(7);
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t / 0.025f);
                // A short sine plus a tiny noise spike at the very front.
                float click = i < 64 ? (float)(rng.NextDouble() * 2 - 1) * 0.35f : 0f;
                float v = (Mathf.Sin(2 * Mathf.PI * f * t) * 0.45f + click) * env;
                l[i] = v; r[i] = v;
            }
            return MakeStereo("sfx_tap", l, r);
        }

        /// <summary>Friendly two-note "hint?" cue (rising minor third).</summary>
        private static AudioClip MakeHint()
        {
            const float dur = 0.28f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float f = t < 0.10f ? 660f : 784f;   // E5 -> G5
                float env = ADSR(t, dur, 0.01f, 0.06f, 0.6f, 0.10f);
                float v = Mathf.Sin(2 * Mathf.PI * f * t) * 0.45f;
                v += Mathf.Sin(2 * Mathf.PI * f * 2 * t) * 0.08f;
                l[i] = v * env; r[i] = v * env;
            }
            return MakeStereo("sfx_hint", l, r);
        }

        /// <summary>Triumphant 5-note major fanfare. Ends with a held chord.</summary>
        private static AudioClip MakeFanfare()
        {
            float[] notes = { 523f, 659f, 784f, 1046f, 1318.5f }; // C5 E5 G5 C6 E6
            float[] dur   = { 0.10f, 0.10f, 0.10f, 0.12f, 0.36f };
            float total = 0f;
            for (int i = 0; i < dur.Length; i++) total += dur[i];
            int totalSamples = Mathf.RoundToInt(SampleRate * total);
            var l = new float[totalSamples]; var r = new float[totalSamples];
            int cursor = 0;
            for (int n = 0; n < notes.Length; n++)
            {
                int len = Mathf.RoundToInt(SampleRate * dur[n]);
                // For the held final chord, layer the full C-major triad.
                bool chord = n == notes.Length - 1;
                for (int i = 0; i < len && cursor + i < totalSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = ADSR(t, dur[n], 0.01f, 0.05f, 0.6f, dur[n] * 0.4f);
                    float v = Mathf.Sin(2 * Mathf.PI * notes[n] * t) * 0.45f;
                    if (chord)
                    {
                        v += Mathf.Sin(2 * Mathf.PI * notes[n] * 1.25f * t) * 0.25f; // E
                        v += Mathf.Sin(2 * Mathf.PI * notes[n] * 1.5f  * t) * 0.20f; // G
                    }
                    else
                    {
                        v += Mathf.Sin(2 * Mathf.PI * notes[n] * 2 * t) * 0.10f;
                    }
                    v *= env;
                    l[cursor + i] += v;
                    r[cursor + i] += v;
                }
                cursor += len;
            }
            return MakeStereo("sfx_win", l, r);
        }

        /// <summary>Short pizzicato tick for low-time timer warning.</summary>
        private static AudioClip MakeTick()
        {
            const float dur = 0.05f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t / 0.015f);
                float v = Mathf.Sin(2 * Mathf.PI * 1900f * t) * 0.55f * env;
                l[i] = v; r[i] = v;
            }
            return MakeStereo("sfx_tick", l, r);
        }

        /// <summary>
        /// Arpeggio helper. Plays each frequency for stepDur seconds with a
        /// configurable per-note decay. Used for win / star / streak / badge.
        /// </summary>
        private static AudioClip MakeArpeggio(string name, float[] freqs,
            float stepDur, float decay = 0.10f)
        {
            int stepSamples = Mathf.RoundToInt(SampleRate * stepDur);
            int total = stepSamples * freqs.Length;
            var l = new float[total]; var r = new float[total];
            for (int n = 0; n < freqs.Length; n++)
            {
                float freq = freqs[n];
                for (int i = 0; i < stepSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = ADSR(t, stepDur, 0.008f, decay, 0.5f, stepDur * 0.4f);
                    float v = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.45f;
                    v += Mathf.Sin(2 * Mathf.PI * freq * 2 * t) * 0.10f;
                    v *= env;
                    int idx = n * stepSamples + i;
                    l[idx] = v;
                    r[idx] = v * 0.96f;
                }
            }
            return MakeStereo(name, l, r);
        }

        /// <summary>
        /// Two-pulse alarm at <paramref name="freq"/> — urgent but not painful.
        /// </summary>
        private static AudioClip MakeAlarm(string name, float freq)
        {
            const float dur = 0.7f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                // Square-ish via tanh of sine.
                float square = Mathf.Tan(Mathf.Clamp(Mathf.Sin(2 * Mathf.PI * freq * t) * 3f, -1.4f, 1.4f));
                // Two short pulses across the duration.
                float pulse = (t < 0.10f || (t > 0.35f && t < 0.45f)) ? 1f : 0f;
                float v = Mathf.Clamp(square * 0.4f * pulse, -0.7f, 0.7f);
                l[i] = v; r[i] = v;
            }
            return MakeStereo(name, l, r);
        }

        /// <summary>Soft band-passed white-noise whoosh for transitions.</summary>
        private static AudioClip MakeWhoosh(string name, float dur)
        {
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            var rng = new System.Random(1);
            float prevL = 0f, prevR = 0f;
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float nL = (float)(rng.NextDouble() * 2 - 1);
                float nR = (float)(rng.NextDouble() * 2 - 1);
                // Low-pass with slight stereo spread.
                prevL = Mathf.Lerp(prevL, nL, 0.18f);
                prevR = Mathf.Lerp(prevR, nR, 0.18f);
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / dur)); // 0→1→0
                l[i] = prevL * 0.35f * env;
                r[i] = prevR * 0.35f * env;
            }
            return MakeStereo(name, l, r);
        }

        /// <summary>
        /// Calm, looping ambient pad. C major triad held with a slow LFO that
        /// pans between channels. ~12 seconds long, loops seamlessly. Used as
        /// the menu / gameplay backing music when no real music asset is
        /// supplied in Resources/Audio/.
        /// </summary>
        private static AudioClip MakeAmbientLoop(string name)
        {
            const float dur = 12.0f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            // C major triad in two octaves + a soft 5th drone.
            float[] notes = { 130.81f, 196f, 261.63f, 329.63f, 392f };
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float lfo = 0.5f + 0.5f * Mathf.Sin(2 * Mathf.PI * (1f / dur) * t);
                float pan = 0.5f + 0.45f * Mathf.Sin(2 * Mathf.PI * 0.07f * t);
                float v = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    // Very gentle vibrato per voice.
                    float vib = 1f + 0.0025f * Mathf.Sin(2 * Mathf.PI * (0.4f + 0.05f * n) * t);
                    v += Mathf.Sin(2 * Mathf.PI * notes[n] * vib * t) * (0.16f - 0.018f * n);
                }
                // Slowly breathe.
                v *= 0.5f + 0.35f * lfo;
                // Light low-pass via 1-pole filter for warmth.
                if (i > 0) v = Mathf.Lerp(l[i - 1] + r[i - 1] * 0.5f, v, 0.35f);
                l[i] = v * (1f - pan);
                r[i] = v * pan;
            }
            // Tiny crossfade at the seams so the loop is seamless.
            int xfade = SampleRate / 4; // 0.25 s
            for (int i = 0; i < xfade && i < total; i++)
            {
                float k = i / (float)xfade;
                int j = total - xfade + i;
                l[i] = Mathf.Lerp(l[j], l[i], k);
                r[i] = Mathf.Lerp(r[j], r[i], k);
            }
            return MakeStereo(name, l, r);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        /// <summary>
        /// Pack two parallel float buffers into a stereo AudioClip.
        /// </summary>
        private static AudioClip MakeStereo(string name, float[] l, float[] r)
        {
            int n = Mathf.Min(l.Length, r.Length);
            var interleaved = new float[n * 2];
            for (int i = 0; i < n; i++)
            {
                interleaved[i * 2]     = Mathf.Clamp(l[i], -1f, 1f);
                interleaved[i * 2 + 1] = Mathf.Clamp(r[i], -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 2, SampleRate, false);
            clip.SetData(interleaved, 0);
            return clip;
        }

        /// <summary>
        /// Attack-Decay-Sustain-Release envelope shaped for short SFX. All
        /// values in seconds (except sustain which is the held level 0..1).
        /// </summary>
        private static float ADSR(float t, float dur,
            float attack, float decay, float sustain, float release)
        {
            if (t < 0) return 0f;
            if (t < attack) return t / attack;                    // attack ramp 0->1
            float ad = attack + decay;
            if (t < ad) return Mathf.Lerp(1f, sustain, (t - attack) / decay);
            float relStart = Mathf.Max(ad, dur - release);
            if (t < relStart) return sustain;
            float k = Mathf.Clamp01((t - relStart) / Mathf.Max(0.0001f, release));
            return Mathf.Lerp(sustain, 0f, k);
        }
    }
}
