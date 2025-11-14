using Assets.Agoston_R.Simple_Laser.Scripts.Dto;
using Assets.Agoston_R.Simple_Laser.Scripts.Raycaster;
using UnityEngine;

namespace Controller
{
    /// <summary>
    /// Controls the the raycasts and particle invocation for the beam.
    /// </summary>
    public class LaserBeamController : MonoBehaviour
    {
        [Header("Damage")]
        [SerializeField] float damagePerSecond = 0.0f;

        private const float Eps = 0.001f;
        private static readonly int LineDistance = Shader.PropertyToID("_lineDistance");
        private static readonly int WidthConstant = Shader.PropertyToID("_widthConstant");
        private static readonly int ColorConstant = Shader.PropertyToID("_Color");
        private readonly Vector3[] linePositionsLocalSpace = new Vector3[2];

        [Header("Setup")]
        [Tooltip("Switch to activate or deactivate the laser.")]
        public bool isLaserActivated;

        [Header("Physics mode")]
        [Tooltip("Select to use either 3D or 2D physics mode.")]
        public PhysicsMode physicsMode = PhysicsMode.Mode3D;

        [Space]
        [Header("Controls")]
        [Tooltip("The color of the laser beam.")]
        [ColorUsage(true, true)]
        public Color LaserColor;

        [Tooltip("The maximum distance of the line renderer that draws the beam.")]
        public float MaxLaserDistance = 50f;

        [Tooltip("The layers that the laser doesn't ignore. Hitting one stops the laser.")]
        public LayerMask layersThatStopLaser;

        [HideInInspector] public float minDepth = -Mathf.Infinity;
        [HideInInspector] public float maxDepth = Mathf.Infinity;

        private IHitParticlesController hitController;
        private LineRenderer beam;
        private IPhysicsRaycaster raycaster;

        private void Awake()
        {
            SetUpBeam();
            SetUpParticles();
            SetUpRaycaster();

            if (layersThatStopLaser == 0 || layersThatStopLaser == LayerMask.NameToLayer("Default"))
            {
                // 레이어 설정이 없거나, 기본 값인 Default로만 설정되어 있을 경우
                layersThatStopLaser = 0;
                layersThatStopLaser |= (1 << LayerMask.NameToLayer("Default"));
                layersThatStopLaser |= (1 << LayerMask.NameToLayer("Player"));
                layersThatStopLaser |= (1 << LayerMask.NameToLayer("Enemy"));
            }

            if (damagePerSecond <= 0.0f)
            {
                // 데미지 설정이 없을 경우 기본 값
                damagePerSecond = 100.0f;
            }
        }

        private void Start()
        {
            SetUVScaleBasedOnWidth();
        }

        private void LateUpdate()
        {
            if (isLaserActivated)
            {
                ShootRaycastToDraw();
            }
            else if (IsBeamActive())
            {
                EraseBeam();
                StopParticles();
            }
        }

        private void ShootRaycastToDraw()
        {
            if (raycaster.Raycast(transform, MaxLaserDistance, layersThatStopLaser, out var hit))
            {
                OnRaycastHit(hit);
            }
            else
            {
                var position = transform.position;
                var direction = SelectForwardDirection();
                OnRaycastMiss(new LaserHit(null, position, direction * MaxLaserDistance + position, transform.TransformDirection(Vector3.back).normalized));
            }
        }

        private void OnRaycastHit(LaserHit hit)
        {
            DrawBeam(hit.HitPoint);
            PlayParticles(hit);
            TakeDamage(hit, damagePerSecond * Time.deltaTime);
        }

        private void OnRaycastMiss(LaserHit hit)
        {
            DrawBeam(hit.HitPoint);
            StopParticles();
        }

        private void DrawBeam(Vector3 endPointWorldSpace)
        {
            var position = transform.position;
            SetLineColor(LaserColor);
            SetLineRendererPositions(position, endPointWorldSpace);
            UpdateEdgeTaperByLineDistance(position, endPointWorldSpace);
        }

        private void EraseBeam()
        {
            beam.positionCount = 0;
        }

        private void SetLineColor(Color tint)
        {
            beam.material.SetColor(ColorConstant, tint);
        }

        private void SetLineRendererPositions(Vector3 originWorldSpace, Vector3 endpointWorldSpace)
        {
            beam.positionCount = linePositionsLocalSpace.Length;
            linePositionsLocalSpace[0] = transform.InverseTransformPoint(originWorldSpace);
            linePositionsLocalSpace[1] = transform.InverseTransformPoint(endpointWorldSpace);
            beam.SetPositions(linePositionsLocalSpace);
        }

        private void UpdateEdgeTaperByLineDistance(Vector3 originWorldSpace, Vector3 endpointWorldSpace)
        {
            float distance = Vector3.Distance(originWorldSpace, endpointWorldSpace);
            distance = distance > Eps ? distance : 1f;
            beam.material.SetFloat(LineDistance, distance);
        }

        private void SetUVScaleBasedOnWidth()
        {
            beam.material.SetFloat(WidthConstant, beam.widthCurve.keys[0].value);
        }

        private void SetUpBeam()
        {
            beam = GetComponent<LineRenderer>();
            ValidateBeamWithMaterialNotNull();
        }

        private void PlayParticles(LaserHit hit)
        {
            hitController.Play(hit);
        }

        private void StopParticles()
        {
            hitController.Stop();
        }

        private bool IsBeamActive()
        {
            return beam.positionCount > 0;
        }

        private void SetUpRaycaster()
        {
            switch (physicsMode)
            {
                case PhysicsMode.Mode3D:
                    raycaster = new PhysicsRaycaster3D(QueryTriggerInteraction.Ignore);
                    break;
                case PhysicsMode.Mode2D:
                    raycaster = new PhysicsRaycaster2D(minDepth, maxDepth);
                    break;
            }
        }

        private void SetUpParticles()
        {
            hitController = GetComponentInChildren<IHitParticlesController>();
            if (hitController == null)
            {
                Debug.Log("No hit controller found in children. On laser hit no particles will play.");
                hitController = EmptyParticles.Instance;
            }
        }

        private Vector3 SelectForwardDirection()
        {
            return physicsMode == PhysicsMode.Mode3D ? transform.forward : transform.right;
        }

        private void ValidateBeamWithMaterialNotNull()
        {
            if (beam != null && beam.material != null)
            {
                return;
            }

            throw new MissingComponentException($"Laser object {name}: line renderer or its material is missing.");
        }

        private void TakeDamage(LaserHit hit, float damage)
        {
            IDamagable damagable = hit.Target.GetComponent<IDamagable>();
            if (damagable == null)
            {
                damagable = hit.Target.GetComponentInChildren<IDamagable>();
            }

            if (damagable != null)
            {
                damagable.ApplyDamage(damage, layersThatStopLaser, Time.deltaTime, 1.0f);
            }
        }
    }
}
