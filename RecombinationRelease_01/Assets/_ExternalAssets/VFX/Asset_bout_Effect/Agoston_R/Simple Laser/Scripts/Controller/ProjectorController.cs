using UnityEngine;

namespace Controller
{
    /// <summary>
    /// Controls the projector for lasers that project a texture on hit instead of playing particles.
    /// Contains logic for URP, HDRP and the built in pipeline as these use separate projection methods.
    /// </summary>
    public class ProjectorController : MonoBehaviour, IHitParticlesController
    {
        private const float Delta = 0.03f;

        private static readonly int ColorMultiplierProperty = Shader.PropertyToID("_ColorMultiplier");

        [Header("Settings")]
        [Tooltip("Set the color multiplier for the projected texture")]
        public Color colorMutliplier;

#if USING_HDRP
        private UnityEngine.Rendering.HighDefinition.DecalProjector hdrpDecalProjector;
#elif USING_URP
        private UnityEngine.Rendering.Universal.DecalProjector urpDecalProjector;
#else
        private Projector builtInProjector;
#endif

        void Start()
        {
            SetUpProjectors();
        }

        public void Play(LaserHit hit)
        {
            transform.position = hit.HitPoint + (hit.Origin - hit.HitPoint).normalized * Delta;
#if USING_HDRP
            hdrpDecalProjector.enabled = true;
            hdrpDecalProjector.material.SetColor(ColorMultiplierProperty, colorMutliplier);
#elif USING_URP
            urpDecalProjector.enabled = true;
            urpDecalProjector.material.SetColor(ColorMultiplierProperty, colorMutliplier);
#else
            builtInProjector.enabled = true;
            builtInProjector.material.color = colorMutliplier;
#endif
        }

        public void Stop()
        {
#if USING_HDRP
            hdrpDecalProjector.enabled = false;
#elif USING_URP
            urpDecalProjector.enabled = false;
#else
            builtInProjector.enabled = false;
#endif
        }

        private void SetUpProjectors()
        {
#if USING_HDRP
            hdrpDecalProjector = GetComponent<UnityEngine.Rendering.HighDefinition.DecalProjector>();
#elif USING_URP
            urpDecalProjector = GetComponent<UnityEngine.Rendering.Universal.DecalProjector>();
#else
            builtInProjector = GetComponent<Projector>();
#endif
        }
    }

}
