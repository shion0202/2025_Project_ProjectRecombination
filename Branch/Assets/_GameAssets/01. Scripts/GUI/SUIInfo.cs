using System;
using UnityEngine;

namespace UI
{
    [Serializable]
    public struct UIInfo
    {
        public EUIType uiType;
        public GameObject uiPrefab;
        public bool isActive;
    }
}