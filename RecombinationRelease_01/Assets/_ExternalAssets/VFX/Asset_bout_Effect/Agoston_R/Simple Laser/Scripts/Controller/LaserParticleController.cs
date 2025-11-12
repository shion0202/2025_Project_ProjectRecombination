using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Controller
{
    /// <summary>
    /// Controls the particle system invocation to play on laser hit. 
    /// Finds particle in children gameobjects of the one this script is assigned to.
    /// </summary>
    public class LaserParticleController : MonoBehaviour, IHitParticlesController
    {
        private const float Epsilon = 0.04f;
        private static readonly int ParticleMainColor = Shader.PropertyToID("_Color");

        [Header("Settings")]
        [Tooltip("Particle system color")]
        public Color ParticleColor;

        private IList<ParticleSystem> particles;
        private IList<ParticleSystem> colorableParticles;
        private IList<Material> particleMaterials;

        private void Start()
        {
            SetUpParticles();
            SetUpMaterials();
        }

        public void Play(LaserHit hit)
        {
            SetParticleColors();
            SetParticleSystemTransforms(hit);
            PlayParticleEffect();
        }

        private void SetParticleColors()
        {
            SetStartColor();
            SetMaterialColor();
        }

        private void SetMaterialColor()
        {
            foreach (var material in particleMaterials)
            {
                material.SetColor(ParticleMainColor, ParticleColor);
            }
        }

        private void SetStartColor()
        {
            foreach (var particle in colorableParticles)
            {
                var main = particle.main;
                main.startColor = ParticleColor;
            }
        }

        private void PlayParticleEffect()
        {
            foreach (var particle in particles)
            {
                if (!particle.isPlaying)
                {
                    particle.Play();
                }
            }
        }

        private void SetParticleSystemTransforms(LaserHit hit)
        {
            transform.position = TranslateForward(hit);
            transform.LookAt(hit.Origin);
        }

        private Vector3 TranslateForward(LaserHit hit)
        {
            // moving the particles forward so they aren't blocked halfway by the mesh they hit
            return hit.HitPoint + hit.Normal * Epsilon;
        }

        public void Stop()
        {
            foreach (var particle in particles)
            {
                particle.Stop
                (
                    withChildren: true,
                    ParticleSystemStopBehavior.StopEmitting
                );
            }
        }

        private void SetUpMaterials()
        {
            particleMaterials = colorableParticles != null ? colorableParticles.Select(p => p.GetComponent<Renderer>().material).ToList() : new List<Material>();
        }

        private void SetUpParticles()
        {
            particles = GetComponentsInChildren<ParticleSystem>();

            if (particles == null)
            {
                particles = new List<ParticleSystem>();
                Debug.LogWarning($"No particle systems found on object {name}. No particles will be emitted on laser hit. ");
            }

            colorableParticles = particles.Count != 0 ? particles.Where(p => p.GetComponent<IgnoreColoring>() == null).ToList() : new List<ParticleSystem>();
        }
    }

}
