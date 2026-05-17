// -----------------------------------------------------------------------------
// HapticManager.cs
// -----------------------------------------------------------------------------
// Thin static wrapper around platform haptics. Used by AnswerButton on correct
// answers and ResultsManager on level completion. Each call is a no-op when
// the player has disabled haptics from the Settings screen (PlayerProfile.
// hapticsOn == false).
//
// Notes:
//   • Android uses UnityEngine.Handheld.Vibrate(), which is a coarse 1-second
//     buzz on most devices. It's the cleanest API that ships with the engine.
//   • iOS does not expose Core Haptics (the modern Taptic Engine API) through
//     the built-in modules. Without a native plugin the best we can do is the
//     same Handheld.Vibrate() call, which iOS interprets as a "peek/pop"
//     style notification. Replace this method with a UIImpactFeedbackGenerator
//     bridge if richer feedback is required.
// -----------------------------------------------------------------------------

using MathEdu.Managers;
using UnityEngine;

namespace MathEdu.Managers
{
    public static class HapticManager
    {
        /// <summary>
        /// Short, sharp tap. Called on correct answers and confirmation taps.
        /// </summary>
        public static void Light()
        {
            if (!IsEnabled()) return;
#if UNITY_ANDROID || UNITY_IOS
            // Handheld.Vibrate works on both platforms via the built-in modules.
            // TODO: iOS — wire UIImpactFeedbackGenerator (style: .light) via a
            // native plugin for proper Taptic Engine feedback.
            Handheld.Vibrate();
#endif
        }

        /// <summary>Stronger buzz used for level-complete or badge unlock.</summary>
        public static void Medium()
        {
            if (!IsEnabled()) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        /// <summary>Triple-buzz for celebration moments.</summary>
        public static void Heavy()
        {
            if (!IsEnabled()) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        private static bool IsEnabled()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Profile == null) return false;
            return gm.Profile.hapticsOn;
        }
    }
}
