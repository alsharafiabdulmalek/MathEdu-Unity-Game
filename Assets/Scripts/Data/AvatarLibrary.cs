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
        /// Build a small runtime AvatarLibrary so the Player Setup picker is
        /// fully functional on a fresh clone (or when Resources/AvatarLibrary
        /// is missing). Ahmed + Eleen come first to match the production
        /// library; the remaining 10 are emoji-on-colour fallbacks.
        ///
        /// Sprites for Ahmed and Eleen are looked up via `Resources.Load`
        /// from `Resources/Avatars/Ahmed` and `Resources/Avatars/Eleen` so
        /// they only render when the user has copied the PNG into Resources.
        /// In the production build path, the photos load via the explicit
        /// sprite reference baked into Avatar_ahmed.asset / Avatar_eleen.asset
        /// inside Assets/ScriptableObjects/Avatars/, so the player always
        /// sees the real portrait regardless of which path the loader took.
        /// </summary>
        public static AvatarLibrary BuildDefault()
        {
            var lib = ScriptableObject.CreateInstance<AvatarLibrary>();
            lib.name = "AvatarLibrary (Runtime)";

            (string id, string name, string emoji, Color tint, string spritePath)[] seeds =
            {
                ("ahmed",   "Ahmed",    "\U0001F468", new Color(0.30f, 0.65f, 0.95f), "Avatars/Ahmed"),
                ("eleen",   "Eleen",    "\U0001F469", new Color(0.95f, 0.55f, 0.75f), "Avatars/Eleen"),
                ("fox",     "Fox",      "\U0001F98A", new Color(0.95f, 0.55f, 0.20f), null),
                ("panda",   "Panda",    "\U0001F43C", new Color(0.55f, 0.55f, 0.60f), null),
                ("rabbit",  "Rabbit",   "\U0001F430", new Color(0.95f, 0.78f, 0.90f), null),
                ("owl",     "Owl",      "\U0001F989", new Color(0.45f, 0.55f, 0.75f), null),
                ("monkey",  "Monkey",   "\U0001F435", new Color(0.85f, 0.65f, 0.45f), null),
                ("cat",     "Cat",      "\U0001F431", new Color(0.95f, 0.75f, 0.35f), null),
                ("dog",     "Dog",      "\U0001F436", new Color(0.85f, 0.60f, 0.35f), null),
                ("unicorn", "Unicorn",  "\U0001F984", new Color(0.85f, 0.55f, 0.90f), null),
                ("dragon",  "Dragon",   "\U0001F432", new Color(0.40f, 0.75f, 0.45f), null),
                ("astro",   "Astro",    "\U0001F680", new Color(0.35f, 0.50f, 0.85f), null),
            };

            foreach (var s in seeds)
            {
                var a = ScriptableObject.CreateInstance<AvatarData>();
                a.avatarId    = s.id;
                a.displayName = s.name;
                a.emoji       = s.emoji;
                a.tint        = s.tint;
                // Try to attach a Resources sprite when one was provided so
                // the runtime-built fallback can still show photos when they
                // happen to live inside Assets/Resources/Avatars/.
                if (!string.IsNullOrEmpty(s.spritePath))
                    a.sprite = Resources.Load<Sprite>(s.spritePath);
                lib.avatars.Add(a);
            }
            return lib;
        }
    }
}
