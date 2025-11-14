using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 머터리얼 교체 GOS
    public class ReplaceMaterial : ProcessBase
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private int index = 0;             // 여러 개의 머터리얼로 이루어진 렌더러일 경우
        [SerializeField] private Material newMaterial;
        private Material[] _targetMaterials;

        private void Start()
        {
            Material[] _targetMaterials = targetRenderer.materials;

            if (index < _targetMaterials.Length)
            {
                _targetMaterials[index] = newMaterial;
            }
        }

        public override void Execute()
        {
            if (IsOn) return;

            targetRenderer.materials = _targetMaterials;

            IsOn = true;
        }
    }
}