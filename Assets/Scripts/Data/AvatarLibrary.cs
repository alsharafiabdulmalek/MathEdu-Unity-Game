// -----------------------------------------------------------------------------
// AvatarLibrary.cs
// -----------------------------------------------------------------------------
// Holds the ordered list of AvatarData assets shown in the Player Setup grid.
// GameManager looks for one at Resources/AvatarLibrary; if none is found a
// runtime fallback is built procedurally so the picker always has options.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace MathEdu.Data
{
    [CreateAssetMenu(
        fileName = "AvatarLibrary",
        menuName = "MathEdu/Avatar Library",
        order    = 41)]
    public class AvatarLibrary : ScriptableObject
    {
        [Tooltip("Every avatar the player can choose. Order is the grid order.")]
        public List<AvatarData> avatars = new List<AvatarData>();

        public AvatarData FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var a in avatars)
                if (a != null && a.avatarId == id)
                    return a;
            return null;
        }

        /// <summary>
        /// Build a small runtime AvatarLibrary with 8 emoji-on-colour avatars so
        /// the Player Setup picker is fully functional on a fresh clone.
        /// </summary>
        public static AvatarLibrary BuildDefault()
        {
            var lib = ScriptableObject.CreateInstance<AvatarLibrary>();
            lib.name = "AvatarLibrary (Runtime)";

            (string id, string name, string emoji, Color tint)[] seeds =
            {
                ("fox",     "Fox",      "🦊", new Color(0.95f, 0.55f, 0.20f)),
                ("panda",   "Panda",    "🐼", new Color(0.55f, 0.55f, 0.60f)),
                ("rabbit",  "Rabbit",   "🐰", new Color(0.95f, 0.78f, 0.90f)),
                ("owl",     "Owl",      "🦉", new Color(0.45f, 0.55f, 0.75f)),
                ("monkey",  "Monkey",   "🐵", new Color(0.85f, 0.65f, 0.45f)),
                ("cat",     "Cat",      "🐱", new Color(0.95f, 0.75f, 0.35f)),
                ("dog",     "Dog",      "🐶", new Color(0.85f, 0.60f, 0.35f)),
                ("unicorn", "Unicorn",  "🦄", new Color(0.85f, 0.55f, 0.90f)),
                ("dragon",  "Dragon",   "🐲", new Color(0.40f, 0.75f, 0.45f)),
                ("astro",   "Astro",    "🚀", new Color(0.35f, 0.50f, 0.85f)),
            };

            foreach (var s in seeds)
            {
                var a = ScriptableObject.CreateInstance<AvatarData>();
                a.avatarId    = s.id;
                a.displayName = s.name;
                a.emoji       = s.emoji;
                a.tint        = s.tint;
                lib.avatars.Add(a);
            }
            return lib;
        }
    }
}
