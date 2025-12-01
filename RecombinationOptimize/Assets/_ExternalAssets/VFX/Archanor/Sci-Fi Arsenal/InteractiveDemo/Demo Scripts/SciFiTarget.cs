using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace SciFiArsenal
{
    [System.Serializable]
    public class TargetEffects
    {
        public GameObject hitParticle;
        public GameObject respawnParticle;
		public List<GameObject> deathParticles = new List<GameObject>();
    }

    public class SciFiTarget : MonoBehaviour
    {
        public TargetEffects effects;

        [Header("General Settings")]
        public int hitsToDestroy = 5;
        public float respawnTime = 3.0f;

        [Header("Squash & Stretch")]
		public bool enableSquashAndStretch = true;
        public float duration = 0.07f; // Duration of both squash and stretch
        public Vector3 squashScale = new Vector3(0.8f, 1.2f, 1f);
        public Vector3 stretchScale = new Vector3(1.2f, 0.8f, 1f);

        private Renderer targetRenderer;
        private Collider targetCollider;
        private int currentHits = 0;

        private Vector3 originalScale;

        void Start()
        {
            targetRenderer = GetComponent<Renderer>();
            targetCollider = GetComponent<Collider>();
            originalScale = transform.localScale;
        }

        void SpawnTarget()
        {
            targetRenderer.enabled = true; //Shows the target
            targetCollider.enabled = true; //Enables the collider
            GameObject respawnEffect = Instantiate(effects.respawnParticle, transform.position, transform.rotation) as GameObject; //Spawns attached respawn effect
            Destroy(respawnEffect, 3.5f); //Removes attached respawn effect after x seconds
            currentHits = 0; // Reset current hits on respawn
        }

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(respawnTime);
            SpawnTarget();
        }

        public void OnHit()
        {
            currentHits++;

            if (currentHits >= hitsToDestroy)
            {
                DestroyTarget();
            }
            else
            {
                if (effects.hitParticle)
                {
                    GameObject hitEffect = Instantiate(effects.hitParticle, transform.position, transform.rotation) as GameObject; // Spawns hit particle
                    Destroy(hitEffect, 2f); // Removes hit particle after 2 seconds
                }

                if (enableSquashAndStretch)
                {
                    StartCoroutine(SquashAndStretch());
                }
            }
        }

        IEnumerator SquashAndStretch()
        {
            float timeElapsed = 0f;
            Vector3 startScale = transform.localScale;
            Vector3 endScale = squashScale;

            // Squash
            while (timeElapsed < duration)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, timeElapsed / duration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            // Stretch
            timeElapsed = 0f;
            startScale = endScale;
            endScale = stretchScale;
            while (timeElapsed < duration)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, timeElapsed / duration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            // Return to original scale
            timeElapsed = 0f;
            startScale = endScale;
            endScale = originalScale;
            while (timeElapsed < duration)
            {
                transform.localScale = Vector3.Lerp(startScale, endScale, timeElapsed / duration);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
        }

        void DestroyTarget()
        {
            GameObject deathEffect;

            if (effects.deathParticles.Count > 0)
            {
                if (effects.deathParticles.Count == 1)
                {
                    deathEffect = Instantiate(effects.deathParticles[0], transform.position, transform.rotation) as GameObject; // Spawns the only death particle
                }
                else
                {
                    int randomIndex = Random.Range(0, effects.deathParticles.Count);
                    deathEffect = Instantiate(effects.deathParticles[randomIndex], transform.position, transform.rotation) as GameObject; // Spawns a random death particle
                }
            }
            else
            {
                deathEffect = new GameObject("DefaultDeathParticle"); // Create an empty game object if no death particle is available
            }

            Destroy(deathEffect, 2f); // Removes death particle after 2 seconds

            targetRenderer.enabled = false; // Hides the target
            targetCollider.enabled = false; // Disables target collider
            StartCoroutine(Respawn()); // Sets timer for respawning the target
        }
    }
}