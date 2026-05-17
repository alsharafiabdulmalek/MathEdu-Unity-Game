// -----------------------------------------------------------------------------
// AvatarData.cs
// -----------------------------------------------------------------------------
// One selectable avatar shown on the Player Setup screen. Each AvatarData is a
// small ScriptableObject so designers can drop in new character art without
// editing code.
//
// If no Sprite is assigned, the Player Setup screen falls back to a coloured
// circle with the emoji glyph so the picker is always functional.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace MathEdu.Data
{
    [CreateAssetMenu(
        fileName = "Avatar_",
        menuName = "MathEdu/Avatar Data",
        order    = 40)]
    public class AvatarData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable id saved to PlayerProfile.avatarId.")]
        public string avatarId = "default";

        [Tooltip("Player-facing name shown under the avatar.")]
        public string displayName = "Friend";

        [Header("Visuals")]
        [Tooltip("Optional sprite. If null, an emoji on a coloured circle is used.")]
        public Sprite sprite;

        [Tooltip("Emoji shown when no sprite is assigned.")]
        public string emoji = "🦊";

        [Tooltip("Background colour for the circle when no sprite is assigned.")]
        public Color tint = new Color(0.95f, 0.55f, 0.20f);
    }
}
