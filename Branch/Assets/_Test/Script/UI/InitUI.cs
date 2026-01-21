using Managers;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace UI
{
    public class InitUI : MonoBehaviour
    {
        [SerializeField] private UIInfo[] uiInfos;

        private void Awake()
        {
            Dictionary<EUIType, GameObject> createdUIs = new ();
            
            try
            {
                foreach (UIInfo ui in uiInfos)
                {
                    if (!ui.uiPrefab) continue;
                    if (createdUIs.ContainsKey(ui.uiType)) continue;

                    GameObject uiInstance = Instantiate(ui.uiPrefab);
                    uiInstance.SetActive(ui.isActive);
                    
                    createdUIs.Add(ui.uiType, uiInstance);
                }
            
                // GUIManager 초기화
                GUIManager.Instance.Init(createdUIs);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }
}

