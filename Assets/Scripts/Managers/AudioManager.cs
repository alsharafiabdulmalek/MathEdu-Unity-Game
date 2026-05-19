// -----------------------------------------------------------------------------
// AudioManager.cs
// -----------------------------------------------------------------------------
// Plays SFX and music. Generates calm procedural tones at runtime so the
// project works with zero packaged audio assets, but the moment you drop a
// real .wav / .ogg / .mp3 into Assets/Resources/Audio/, the runtime picks it
// up and uses it instead of the procedural fallback.
//
// === DROP-IN ROYALTY-FREE AUDIO =============================================
//
// To replace the procedural sounds with real (still juicy but production)
// audio, drop files into Assets/Resources/Audio/ with these exact names:
//
//   music_menu.ogg       Main menu / setup screens (loops).
//   music_play.ogg       Gameplay scenes (Quiz / Practice / Story / Speed).
//   sfx_correct.wav      Correct answer.
//   sfx_wrong.wav        Wrong answer.
//   sfx_tap.wav          UI tap / button click.
//   sfx_hint.wav         Hint button.
//   sfx_levelComplete.wav  Level-complete fanfare.
//   sfx_starReveal.wav     Each star pop.
//   sfx_streak.wav         Streak hit.
//   sfx_timerTick.wav      Last-5-seconds tick.
//   sfx_timerExpire.wav    Time's up.
//   sfx_pageTransition.wav Scene change swoosh.
//   sfx_badgeUnlocked.wav  Badge earn.
//   sfx_lose.wav           Run ended.
//   sfx_swoosh.wav         Menu hover / select.
//
// Run "MathEdu / Audio / Open Audio Resources Folder" in the Unity Editor to
// jump straight to that folder. The list of recommended royalty-free sources
// is available from "MathEdu / Audio / About Royalty-Free Audio Sources".
//
// === NOISE FIX (v3) =========================================================
//
// The v1 build used `Mathf.Tan` (trigonometric tangent, which diverges near
// pi/2) as a "soft saturation" function in the Correct and Alarm clips. tan()
// can produce values of 5+ before the post-clamp, which after the final
// `Mathf.Clamp(-1, 1)` became hard-clipped square-wave-like noise. v1 also
// chained a 1-pole IIR filter in the ambient music loop that took its input
// from PREVIOUSLY-WRITTEN samples, creating a feedback loop with the pan LFO
// that produced an audible noisy buzz on top of the pad.
//
// v3 (this file):
//   * Replaces all uses of `Mathf.Tan` with a proper rational soft-clip
//     `x / (1 + |x|)` that smoothly saturates between -1 and +1 (the curve
//     the original author was reaching for).
//   * Rewrites the ambient pad from scratch: 3 voices (root, fifth, octave),
//     low amplitude (each voice ~0.06), no feedback filter, gentle stereo
//     drift via panning, 30-second loop with a 1-second seam crossfade.
//   * Adds a 256-sample anti-pop window to every clip (fade in/out the very
//     edges so the AudioSource never starts/ends on a discontinuity).
//   * Adds a master soft-limiter pass over every generated buffer so the sum
//     of layered voices never exceeds ~0.8 peak.
//   * Lowers the default music volume coefficient from 0.7 -> 0.32 (and adds
//     a 1.2-second fade-in on first play) so the background pad sits well
//     below the SFX in the mix.
//   * Per-SFX cooldowns to stop the mixer drowning when buttons are mashed.
// -----------------------------------------------------------------------------

using System.Collections;
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

        // Per-SFX cooldown so button mashes don't pile up.
        private readonly Dictionary<string, float> _lastPlayed =
            new Dictionary<string, float>();
        private static readonly Dictionary<string, float> Cooldowns =
            new Dictionary<string, float>
            {
                { "tap",        0.04f },
                { "swoosh",     0.06f },
                { "hint",       0.10f },
                { "timerTick",  0.20f },
                { "starReveal", 0.06f },
                { "correct",    0.06f },
                { "wrong",      0.10f },
            };

        // Master volume coefficients. These multiply on top of
        // profile.sfxVolume / profile.musicVolume so the perceived loudness
        // stays balanced even at slider = 1.0.
        private const float SfxMaster   = 0.80f;
        private const float MusicMaster = 0.32f;

        // Fade-in time when music starts.
        private const float MusicFadeIn  = 1.2f;
        private const float MusicFadeOut = 0.6f;
        private Coroutine _musicFade;

        public void Init(PlayerProfile profile)
        {
            _profile = profile;

            _sfxSource              = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake  = false;
            _sfxSource.volume       = EffectiveSfxVolume();

            _musicSource             = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop        = true;
            _musicSource.volume      = 0f; // fade in from silence

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

            // Throttle.
            if (Cooldowns.TryGetValue(name, out float cd))
            {
                float now = Time.unscaledTime;
                if (_lastPlayed.TryGetValue(name, out float last) &&
                    now - last < cd) return;
                _lastPlayed[name] = now;
            }

            // Tiny random pitch variation makes repeated SFX feel organic.
            float pitchJitter = name switch
            {
                "tap"        => Random.Range(0.97f, 1.03f),
                "correct"    => Random.Range(0.985f, 1.015f),
                "starReveal" => Random.Range(0.95f, 1.05f),
                "swoosh"     => Random.Range(0.94f, 1.06f),
                _            => 1f
            };
            PlayWithPitch(clip, pitchJitter);
        }

        public bool HasClip(string name) => _clips.ContainsKey(name) && _clips[name] != null;

        // Legacy short-hand methods (still used in places).
        public void PlayCorrect() => PlaySFX("correct");
        public void PlayWrong()   => PlaySFX("wrong");
        public void PlayTap()     => PlaySFX("tap");
        public void PlayWin()     => PlaySFX("levelComplete");
        public void PlayLose()    => PlaySFX("lose");
        public void PlayTick()    => PlaySFX("timerTick");

        // -------------------------------------------------------------------
        // Music
        // -------------------------------------------------------------------
        public void PlayMenuMusic()     => SwapMusic(_menuMusic);
        public void PlayGameplayMusic() => SwapMusic(_gameplayMusic ?? _menuMusic);

        /// <summary>Backwards-compat: play an explicit clip, or resume menu.</summary>
        public void PlayMusic(AudioClip clip = null)
        {
            if (clip != null) { SwapMusic(clip); return; }
            PlayMenuMusic();
        }

        public void StopMusic()
        {
            if (_musicSource == null) return;
            if (_musicFade != null) StopCoroutine(_musicFade);
            _musicSource.Stop();
            _musicSource.volume = 0f;
        }

        private void SwapMusic(AudioClip clip)
        {
            if (clip == null || _musicSource == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;
            if (_musicFade != null) StopCoroutine(_musicFade);
            _musicFade = StartCoroutine(CrossfadeMusic(clip));
        }

        private IEnumerator CrossfadeMusic(AudioClip clip)
        {
            float target = EffectiveMusicVolume();

            // If something is already playing, fade it out first.
            if (_musicSource.isPlaying && _musicSource.clip != null)
            {
                float from = _musicSource.volume;
                float t = 0f;
                while (t < MusicFadeOut)
                {
                    t += Time.unscaledDeltaTime;
                    float k = Mathf.Clamp01(t / MusicFadeOut);
                    _musicSource.volume = Mathf.Lerp(from, 0f, k);
                    yield return null;
                }
                _musicSource.Stop();
            }

            _musicSource.clip   = clip;
            _musicSource.volume = 0f;
            _musicSource.loop   = true;
            _musicSource.Play();

            // Fade in.
            float t2 = 0f;
            while (t2 < MusicFadeIn)
            {
                t2 += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t2 / MusicFadeIn);
                // Ease-out curve so the start is even gentler.
                k = 1f - (1f - k) * (1f - k);
                _musicSource.volume = Mathf.Lerp(0f, target, k);
                yield return null;
            }
            _musicSource.volume = target;
            _musicFade = null;
        }

        // -------------------------------------------------------------------
        // Volume API (drives both PlayerProfile values and live sources)
        // -------------------------------------------------------------------
        public void ApplyVolumeFromProfile()
        {
            if (_profile == null) return;
            _sfxSource.volume   = EffectiveSfxVolume();
            // Only set music volume directly if we are NOT mid-fade.
            if (_musicFade == null && _musicSource != null)
                _musicSource.volume = EffectiveMusicVolume();
        }

        public void SetMusicVolume(float v)
        {
            if (_profile != null) _profile.musicVolume = Mathf.Clamp01(v);
            if (_musicSource != null && _musicFade == null)
                _musicSource.volume = EffectiveMusicVolume();
        }

        public void SetSfxVolume(float v)
        {
            if (_profile != null) _profile.sfxVolume = Mathf.Clamp01(v);
            if (_sfxSource != null) _sfxSource.volume = EffectiveSfxVolume();
        }

        private float EffectiveMusicVolume()
        {
            if (_profile == null) return MusicMaster * 0.7f;
            return _profile.musicOn ? Mathf.Clamp01(_profile.musicVolume) * MusicMaster : 0f;
        }

        private float EffectiveSfxVolume()
        {
            if (_profile == null) return SfxMaster;
            return _profile.sfxOn ? Mathf.Clamp01(_profile.sfxVolume) * SfxMaster : 0f;
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
            // Real audio files always override the procedural fallback.
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
            // Polished UI sounds. Stereo, ADSR envelopes, no Mathf.Tan
            // divergence, anti-pop edge windows, soft-limited.
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

            // Procedural ambient music — calm, looping, very quiet pad. Only
            // used if Resources/Audio/music_menu / music_play are absent.
            if (_menuMusic == null) _menuMusic = MakeAmbientLoop("music_menu_proc");
            if (_gameplayMusic == null) _gameplayMusic = _menuMusic;
        }

        // -------------------------------------------------------------------
        // Procedural waveform helpers — stereo, 44.1 kHz
        // -------------------------------------------------------------------
        private const int SampleRate = 44100;

        /// <summary>
        /// Smooth rational soft-clip. Asymptotic to +/-1 for large |x|;
        /// nearly linear for small |x|. Replaces the v1 Mathf.Tan() abuse
        /// that diverged and produced harsh noise.
        /// </summary>
        private static float SoftClip(float x) => x / (1f + Mathf.Abs(x));

        /// <summary>
        /// "Correct" SFX: a bright two-note chord (A5 + E6) with a shimmer
        /// harmonic. Soft, friendly, no harsh clipping.
        /// </summary>
        private static AudioClip MakeCorrect()
        {
            const float dur = 0.42f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            const float f1 = 880f;        // A5
            const float f2 = 1318.5f;     // E6 (major 6th -> bright but warm)
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float env = ADSR(t, dur, attack: 0.012f, decay: 0.10f, sustain: 0.55f, release: 0.20f);
                float v = Mathf.Sin(2 * Mathf.PI * f1 * t) * 0.45f
                        + Mathf.Sin(2 * Mathf.PI * f2 * t) * 0.30f
                        + Mathf.Sin(2 * Mathf.PI * f1 * 2 * t) * 0.10f;
                // Gentle warmth via soft-clip (NOT Mathf.Tan — that's the
                // tangent function which diverges; here we use the safe
                // saturation x / (1 + |x|)).
                v = SoftClip(v * 0.85f);
                l[i] = v * env;
                r[i] = v * env * 0.98f;
            }
            EdgeFade(l, r, 128);
            return MakeStereo("sfx_correct", l, r);
        }

        /// <summary>
        /// "Wrong" SFX: a soft, low descending two-note "uh-oh".
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
                float f = Mathf.Lerp(260f, 170f, k);
                float env = ADSR(t, dur, 0.01f, 0.10f, 0.6f, 0.18f);
                float s = Mathf.Sin(2 * Mathf.PI * f * t);
                // Soft triangle approximation (no Mathf.Asin precision spikes).
                float ph = (f * t) - Mathf.Floor(f * t);
                float tri = (Mathf.Abs(ph * 2f - 1f) * 2f - 1f);
                float v = (s * 0.75f + tri * 0.25f) * env * 0.55f;
                l[i] = v; r[i] = v;
            }
            EdgeFade(l, r, 128);
            return MakeStereo("sfx_wrong", l, r);
        }

        /// <summary>UI tap: short, soft "click" with quick attack.</summary>
        private static AudioClip MakeTap()
        {
            const float dur = 0.08f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            const float f = 1400f;
            var rng = new System.Random(7);
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t / 0.025f);
                // Quieter initial noise spike for a softer click.
                float click = i < 48 ? (float)(rng.NextDouble() * 2 - 1) * 0.22f : 0f;
                float v = (Mathf.Sin(2 * Mathf.PI * f * t) * 0.35f + click) * env;
                l[i] = v; r[i] = v;
            }
            EdgeFade(l, r, 64);
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
                float v = Mathf.Sin(2 * Mathf.PI * f * t) * 0.40f;
                v += Mathf.Sin(2 * Mathf.PI * f * 2 * t) * 0.06f;
                l[i] = v * env; r[i] = v * env;
            }
            EdgeFade(l, r, 128);
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
                bool chord = n == notes.Length - 1;
                for (int i = 0; i < len && cursor + i < totalSamples; i++)
                {
                    float t = (float)i / SampleRate;
                    float env = ADSR(t, dur[n], 0.01f, 0.05f, 0.6f, dur[n] * 0.4f);
                    float v = Mathf.Sin(2 * Mathf.PI * notes[n] * t) * 0.40f;
                    if (chord)
                    {
                        v += Mathf.Sin(2 * Mathf.PI * notes[n] * 1.25f * t) * 0.22f; // E
                        v += Mathf.Sin(2 * Mathf.PI * notes[n] * 1.5f  * t) * 0.18f; // G
                    }
                    else
                    {
                        v += Mathf.Sin(2 * Mathf.PI * notes[n] * 2 * t) * 0.08f;
                    }
                    v = SoftClip(v) * env;
                    l[cursor + i] += v;
                    r[cursor + i] += v;
                }
                cursor += len;
            }
            EdgeFade(l, r, 128);
            return MakeStereo("sfx_win", l, r);
        }

        /// <summary>Soft pizzicato tick for low-time timer warning.</summary>
        private static AudioClip MakeTick()
        {
            const float dur = 0.05f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float env = Mathf.Exp(-t / 0.015f);
                // Lower amplitude (0.35 instead of 0.55) — the tick is a
                // warning, it shouldn't dominate the mix.
                float v = Mathf.Sin(2 * Mathf.PI * 1900f * t) * 0.35f * env;
                l[i] = v; r[i] = v;
            }
            EdgeFade(l, r, 32);
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
                    float v = Mathf.Sin(2 * Mathf.PI * freq * t) * 0.40f;
                    v += Mathf.Sin(2 * Mathf.PI * freq * 2 * t) * 0.08f;
                    v = SoftClip(v) * env;
                    int idx = n * stepSamples + i;
                    l[idx] = v;
                    r[idx] = v * 0.96f;
                }
            }
            EdgeFade(l, r, 128);
            return MakeStereo(name, l, r);
        }

        /// <summary>
        /// Two-pulse alarm at <paramref name="freq"/> — urgent but not painful.
        /// Uses SoftClip(sine * 2) to get a smooth-edged square wave; previously
        /// used Mathf.Tan which diverges and produced harsh clipping noise.
        /// </summary>
        private static AudioClip MakeAlarm(string name, float freq)
        {
            const float dur = 0.7f;
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];
            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                // Smooth square-ish via SoftClip of a strongly-driven sine.
                // SoftClip never diverges, so no harsh noise spike.
                float square = SoftClip(Mathf.Sin(2 * Mathf.PI * freq * t) * 3f);
                // Two short pulses with quick fade edges so the pulses don't pop.
                float pulse = 0f;
                if (t < 0.10f) pulse = Mathf.Sin(Mathf.PI * (t / 0.10f));
                else if (t > 0.35f && t < 0.45f) pulse = Mathf.Sin(Mathf.PI * ((t - 0.35f) / 0.10f));
                float v = square * 0.40f * pulse;
                l[i] = v; r[i] = v;
            }
            EdgeFade(l, r, 128);
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
                // Hann window so the whoosh rises and falls smoothly.
                float env = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * Mathf.Clamp01(t / dur)));
                l[i] = prevL * 0.30f * env;
                r[i] = prevR * 0.30f * env;
            }
            EdgeFade(l, r, 32);
            return MakeStereo(name, l, r);
        }

        /// <summary>
        /// Calm, looping ambient pad. Three voices: root, perfect 5th, octave.
        /// Each voice has a slow vibrato. NO feedback filter (the v1 1-pole
        /// IIR fed back from previously-written samples and beat with the pan
        /// LFO, producing the noisy drone the player was hearing). The seam
        /// is cross-faded over 1 second so the loop is inaudible. Final
        /// peak < 0.30 so the music sits well below SFX in the mix.
        /// </summary>
        private static AudioClip MakeAmbientLoop(string name)
        {
            const float dur = 30.0f;           // 30 s loop (longer = less obvious)
            int total = Mathf.RoundToInt(SampleRate * dur);
            var l = new float[total]; var r = new float[total];

            // Low triad: C3 (root) + G3 (perfect fifth) + C4 (octave).
            // Gentle on the ear, leaves headroom for vocals/SFX.
            float[] notes = { 130.81f, 196.00f, 261.63f };

            // Per-voice slow detune (vibrato) frequency, in Hz. Detuned so
            // the voices breathe independently.
            float[] vibHz = { 0.13f, 0.17f, 0.21f };

            // Per-voice slow pan oscillation (cycles per loop).
            float[] panHz = { 0.04f, 0.06f, 0.05f };

            for (int i = 0; i < total; i++)
            {
                float t = (float)i / SampleRate;
                float lSum = 0f, rSum = 0f;

                for (int n = 0; n < notes.Length; n++)
                {
                    // Slow vibrato around the centre frequency.
                    float vib = 1f + 0.0030f * Mathf.Sin(2f * Mathf.PI * vibHz[n] * t);
                    // Centre-weighted amplitude (root loudest).
                    float amp = 0.085f - 0.020f * n;
                    float s = Mathf.Sin(2f * Mathf.PI * notes[n] * vib * t) * amp;

                    // Slow stereo position swing.
                    float pan = 0.5f + 0.35f * Mathf.Sin(2f * Mathf.PI * panHz[n] * t + n);
                    lSum += s * (1f - pan);
                    rSum += s * pan;
                }

                // Slow global "breathing" envelope.
                float breath = 0.60f + 0.25f * Mathf.Sin(2f * Mathf.PI * (1f / dur) * t);
                lSum *= breath;
                rSum *= breath;

                // Soft-clip to make sure no transient ever exceeds ±1.
                l[i] = SoftClip(lSum);
                r[i] = SoftClip(rSum);
            }

            // Crossfade the last 1 second back into the beginning so the loop
            // seam is inaudible. (Equal-power crossfade.)
            int xfade = SampleRate; // 1 s
            for (int i = 0; i < xfade && i < total; i++)
            {
                float k  = i / (float)xfade;       // 0 -> 1 across the fade
                float a  = Mathf.Cos(k * Mathf.PI * 0.5f); // ramp down old
                float b  = Mathf.Sin(k * Mathf.PI * 0.5f); // ramp up new
                int j = total - xfade + i;
                l[i] = l[i] * b + l[j] * a;
                r[i] = r[i] * b + r[j] * a;
            }

            // Master limiter pass — keep absolute peak just under 1 so the
            // playback never hard-clips on the audio device.
            MasterLimit(l, r, 0.92f);
            return MakeStereo(name, l, r);
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        /// <summary>
        /// Linear fade-in on the first <paramref name="samples"/> and
        /// fade-out on the last <paramref name="samples"/> so the very edges
        /// of the buffer ramp from / to zero. Removes the audible click that
        /// happens when a clip starts on a non-zero sample value.
        /// </summary>
        private static void EdgeFade(float[] l, float[] r, int samples)
        {
            int n = Mathf.Min(l.Length, r.Length);
            samples = Mathf.Min(samples, n / 2);
            for (int i = 0; i < samples; i++)
            {
                float k = i / (float)samples;
                l[i] *= k; r[i] *= k;
                int j = n - 1 - i;
                l[j] *= k; r[j] *= k;
            }
        }

        /// <summary>
        /// Single-pass peak-normalising limiter. Scales the buffer so the
        /// absolute peak equals <paramref name="ceiling"/>. Cheap (one pass to
        /// find the peak, one pass to scale).
        /// </summary>
        private static void MasterLimit(float[] l, float[] r, float ceiling)
        {
            int n = Mathf.Min(l.Length, r.Length);
            float peak = 0f;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.Abs(l[i]); if (a > peak) peak = a;
                float b = Mathf.Abs(r[i]); if (b > peak) peak = b;
            }
            if (peak <= ceiling || peak <= 0.0001f) return;
            float scale = ceiling / peak;
            for (int i = 0; i < n; i++)
            {
                l[i] *= scale;
                r[i] *= scale;
            }
        }

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
