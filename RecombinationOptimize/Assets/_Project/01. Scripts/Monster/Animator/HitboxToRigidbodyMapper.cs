using UnityEngine;

namespace _Project._01._Scripts.Monster.Animator
{
    public class HitboxToRigidbodyMapper : MonoBehaviour
    {
        [SerializeField] private Rigidbody hitRigidbody;
        
        public Rigidbody GetHitRigidbody()
        {
            return hitRigidbody;
        }
    }
}