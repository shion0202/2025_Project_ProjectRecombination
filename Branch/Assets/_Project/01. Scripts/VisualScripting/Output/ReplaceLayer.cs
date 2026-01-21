using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 레이어 교체 GOS
    // 주로 Outline 추가/제거 용도로 사용할 예정
    public class ReplaceLayer : ProcessBase
    {
        [SerializeField] private GameObject target;
        [SerializeField] private LayerMask newLayer;

        public override void Execute()
        {
            if (IsOn) return;

            int layerIndex = GetLayerIndexFromMask(newLayer);

            if (layerIndex != -1)
                target.layer = layerIndex;

            IsOn = true;
        }

        int GetLayerIndexFromMask(LayerMask mask)
        {
            uint maskValue = (uint)mask.value;
            // 0이면 잘못된 마스크이므로 -1 반환
            if (maskValue == 0) return -1;

            // single bit만 있다고 가정하고, 비트셋 위치를 찾기
            int layerIndex = 0;
            while ((maskValue & 1) == 0)
            {
                maskValue >>= 1;
                layerIndex++;
            }
            return layerIndex;
        }
    }
}
