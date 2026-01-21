using UnityEngine;

namespace _Project._01._Scripts.Monster
{
    public enum PartType
    {
        HEAD,
        TORSO,
        ARMS,
        LEGS,
        OTHER
    }
    public class PartialBlow : MonoBehaviour
    {
        public PartType partType;
        public float fValue;
    }
}