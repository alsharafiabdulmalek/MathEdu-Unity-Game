// -----------------------------------------------------------------------------
// VFXManager.cs
// -----------------------------------------------------------------------------
// Spawns short-lived particle effects from a VFXLibrary ScriptableObject so we
// get juicy feedback (sparkles, electric strikes, etc.) without baking any
// specific VFX asset into the code.
//
// The default mapping plugs straight into the bundled Epic Toon FX prefabs —
// drop your favourite particle prefab into the matching slot on the
// VFXLibrary asset and it lights up on the right event.
//
// Lifecycle:
//   • GameManager looks for `Assets/Resources/VFXLibrary.asset`; if missing
//     this manager logs a soft warning and silently no-ops every event so
//     gameplay is never blocked by missing VFX assets.
//   • Spawned effects auto-destroy after `defaultLifetime` seconds.
// -----------------------------------------------------------------------------

using MathEdu.Data;
using UnityEngine;

namespace MathEdu.Managers
{
    public class VFXManager : MonoBehaviour
    {
        [Tooltip("Optional library asset. Auto-loaded from Resources/VFXLibrary if null.")]
        public VFXLibrary library;

        [Tooltip("Seconds before spawned VFX instances are destroyed.")]
        public float defaultLifetime = 2.5f;

        private Transform _root;

        public void Init()
        {
            if (library == null)
                library = Resources.Load<VFXLibrary>("VFXLibrary");

            var go = new GameObject("[VFXRoot]");
            DontDestroyOnLoad(go);
            _root = go.transform;
        }

        // -------------------------------------------------------------------
        // Public API — single-shot effects at world or canvas positions
        // -------------------------------------------------------------------
        public void PlayCorrect(Vector3? at = null) => Play(library?.correctVFX,  at);
        public void PlayWrong  (Vector3? at = null) => Play(library?.wrongVFX,    at);
        public void PlayWin    (Vector3? at = null) => Play(library?.winVFX,      at);
        public void PlayLose   (Vector3? at = null) => Play(library?.loseVFX,     at);
        public void PlayTap    (Vector3? at = null) => Play(library?.tapVFX,      at, 0.6f);
        public void PlayStar   (Vector3? at = null) => Play(library?.starBurstVFX,at, 1.6f);

        /// <summary>Attach the ambient prefab as a child of the supplied parent (e.g. a Canvas SafeArea).</summary>
        public GameObject AttachAmbient(Transform parent)
        {
            if (library == null || library.ambientVFX == null) return null;
            var inst = Instantiate(library.ambientVFX, parent);
            inst.name = "AmbientVFX";
            return inst;
        }

        // -------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------
        private void Play(GameObject prefab, Vector3? at, float lifeOverride = -1f)
        {
            if (prefab == null) return;
            Vector3 pos = at ?? GetCenterOfScreenInWorld();
            var inst = Instantiate(prefab, pos, Quaternion.identity, _root);
            float life = lifeOverride > 0 ? lifeOverride : defaultLifetime;
            Destroy(inst, life);
        }

        private static Vector3 GetCenterOfScreenInWorld()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.zero;
            return cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Mathf.Abs(cam.transform.position.z)));
        }
    }
}
