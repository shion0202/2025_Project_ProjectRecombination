using UnityEngine;

namespace Monster.AI.Blackboard
{
    // 정적 키 선언
    public static class StaticBBKey
    {
        [Header("Monster Current Stats")]
        public static readonly BBKey<float> Health = new("health");
        public static readonly BBKey<float> MaxHealth = new("maxHealth");
        public static readonly BBKey<float> WalkSpeed = new("walkSpeed");
        public static readonly BBKey<float> RunSpeed = new("runSpeed");
        public static readonly BBKey<float> Damage = new("damage");
        // public static readonly BBKey<float> AttackSpeed = new("AttackSpeed");
        public static readonly BBKey<float> Defence = new("defence");
        
        [Header("Monster Config")]
        public static readonly BBKey<float> MinDetectionRange = new("minDetectionRange");
        public static readonly BBKey<float> MaxDetectionRange = new("maxDetectionRange");
        // public static readonly BBKey<float> RotationSpeed = new("RotationSpeed");
    }
}