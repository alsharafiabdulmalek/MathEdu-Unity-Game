// -----------------------------------------------------------------------------
// VFXLibrary.cs
// -----------------------------------------------------------------------------
// Holds GameObject prefab references used by VFXManager to spawn celebration
// particles. The default mapping plugs straight into the bundled Epic Toon FX
// asset — wire any prefab into the inspector slots and VFXManager picks them
// up at runtime.
//
// If the asset is missing, VFXManager silently no-ops, so gameplay always
// works whether or not Epic Toon FX is imported.
//
// Suggested mappings from the Epic Toon FX pack:
//   correctVFX  → Lighting Strick.prefab  (or any sparkle / star burst)
//   wrongVFX    → Electric Strick.prefab  (or a quick poof)
//   winVFX      → Epic Toon FX/Prefabs/Other/EFFECT_Stars_xxx
//   loseVFX     → Epic Toon FX/Prefabs/Other/EFFECT_Smoke_xxx
//   tapVFX      → small sparkle prefab
//   ambientVFX  → SnowStorm.prefab for atmospheric scenes (optional)
// -----------------------------------------------------------------------------

using UnityEngine;

namespace MathEdu.Data
{
    [CreateAssetMenu(
        fileName = "VFXLibrary",
        menuName = "MathEdu/VFX Library",
        order    = 60)]
    public class VFXLibrary : ScriptableObject
    {
        [Header("Answer feedback")]
        [Tooltip("Plays at the centre of the screen on a correct answer.")]
        public GameObject correctVFX;
        [Tooltip("Plays at the centre of the screen on a wrong answer.")]
        public GameObject wrongVFX;

        [Header("Level result")]
        [Tooltip("Plays when the Results screen opens with at least 1 star.")]
        public GameObject winVFX;
        [Tooltip("Plays when the Results screen opens with 0 stars.")]
        public GameObject loseVFX;

        [Header("UI")]
        [Tooltip("Plays at the press location of an answer button.")]
        public GameObject tapVFX;
        [Tooltip("Optional ambient effect (snow, sparkles) attached to backgrounds.")]
        public GameObject ambientVFX;

        [Header("Star milestone")]
        [Tooltip("Plays once per star as the Results screen animates the count-up.")]
        public GameObject starBurstVFX;
    }
}
