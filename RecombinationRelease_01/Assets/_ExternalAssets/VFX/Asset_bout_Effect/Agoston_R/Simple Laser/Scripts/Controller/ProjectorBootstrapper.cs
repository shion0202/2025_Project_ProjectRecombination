using UnityEngine;

namespace Controller
{
    public class ProjectorBootstrapper : MonoBehaviour
    {
        public Material hdrpProjectorMat;
        public Material urpProjectorMat;
        public Material builtInProjectorMat;

        public float projectorSize = 0.04f;
        public float projectorDepth = 0.2f;

        private void Awake()
        {

#if USING_HDRP

            var projector = GetComponent<UnityEngine.Rendering.HighDefinition.DecalProjector>();
            if (projector == null)
            {
                projector = gameObject.AddComponent<UnityEngine.Rendering.HighDefinition.DecalProjector>();
            }

            projector.material = Instantiate(hdrpProjectorMat);
            projector.size = new Vector3(projectorSize, projectorSize, projectorDepth);
            projector.startAngleFade = 0.9f;
            projector.pivot = Vector3.zero;

#elif USING_URP

            var projector = GetComponent<UnityEngine.Rendering.Universal.DecalProjector>();
            if (projector == null)
            {
                projector = gameObject.AddComponent<UnityEngine.Rendering.Universal.DecalProjector>();
            }

            projector.material = Instantiate(urpProjectorMat);
            projector.size = new Vector3(projectorSize, projectorSize, projectorDepth);
            projector.startAngleFade = 0.9f;
            projector.pivot = Vector3.zero;

#else
            var projector = GetComponent<Projector>();
            if (projector == null)
            {
                projector = gameObject.AddComponent<Projector>();
            }

            projector.nearClipPlane = 0.001f;
            projector.farClipPlane = 0.3f;
            projector.fieldOfView = 60f;
            projector.aspectRatio = 1f;
            projector.orthographic = true;
            projector.orthographicSize = projectorSize - 0.01f;
            projector.material = Instantiate(builtInProjectorMat);
            projector.ignoreLayers = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "Water", "UI");
#endif

        }
    }
}

