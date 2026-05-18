// -----------------------------------------------------------------------------
// IconLibrary.cs
// -----------------------------------------------------------------------------
// Optional ScriptableObject that maps every gameplay-meaningful icon to a
// concrete Sprite. Drop in the GUI Pro - Casual Game pack's PictoIcon set (or
// any other equivalently-sized PNG/SVG art) and the entire UI lights up with
// real iconography instead of glyph fallbacks.
//
// Storage:
//   Place the configured IconLibrary.asset in `Assets/Resources/IconLibrary.asset`.
//   `IconService` looks it up at runtime. If the asset is missing or any slot is
//   empty, the corresponding consumer falls back to its emoji/glyph string —
//   nothing crashes.
//
// Suggested wiring (using the bundled GUI Pro - Casual Game pictoicons):
//   gear            → Pictoicon_Setting
//   parent          → Pictoicon_Account
//   back            → Pictoicon_Arrow_Prev
//   home            → Pictoicon_Home_0
//   pause           → Pictoicon_Control_Pause
//   play            → Pictoicon_Control_Play
//   star            → Pictoicon_Star
//   trophy          → Pictoicon_Trophy_0
//   crown           → Pictoicon_Crown
//   lightbulb       → Pictoicon_Bulb
//   musicOn / Off   → Pictoicon_Music / Pictoicon_Music_Off
//   soundOn / Off   → Pictoicon_Sound / Pictoicon_Sound_Off
//   lock / unlock   → Pictoicon_Lock / Pictoicon_Unlock
//   profile         → Pictoicon_Profile
//   heart           → Pictoicon_Like (or similar)
//   emojiSmile      → Pictoicon_Emoji_Smile
//   emojiSad        → Pictoicon_Emoji_Sad
//   emojiAngry      → Pictoicon_Emoji_Angry
//   emojiWow        → Pictoicon_Emoji_Wow
// -----------------------------------------------------------------------------

using UnityEngine;

namespace MathEdu.Data
{
    [CreateAssetMenu(
        fileName = "IconLibrary",
        menuName = "MathEdu/Icon Library",
        order    = 60)]
    public class IconLibrary : ScriptableObject
    {
        [Header("Navigation / Chrome")]
        public Sprite gear;
        public Sprite parent;
        public Sprite back;
        public Sprite home;
        public Sprite play;
        public Sprite pause;
        public Sprite next;
        public Sprite refresh;

        [Header("Rewards")]
        public Sprite star;
        public Sprite trophy;
        public Sprite crown;
        public Sprite gem;
        public Sprite medal;
        public Sprite badge;

        [Header("Gameplay")]
        public Sprite lightbulb;     // hints
        public Sprite heart;          // lives
        public Sprite clock;          // timer
        public Sprite check;          // correct
        public Sprite cross;          // wrong
        public Sprite lockClosed;
        public Sprite lockOpen;
        public Sprite profile;

        [Header("Audio")]
        public Sprite musicOn;
        public Sprite musicOff;
        public Sprite soundOn;
        public Sprite soundOff;

        [Header("Emoji Reactions")]
        public Sprite emojiSmile;
        public Sprite emojiSad;
        public Sprite emojiAngry;
        public Sprite emojiWow;
        public Sprite emojiCool;     // optional — falls back to smile

        [Header("Ambient (used by polish FX)")]
        public Sprite fire;
        public Sprite sparkle;
        public Sprite sun;
        public Sprite flower;
        public Sprite leaf;
    }
}
