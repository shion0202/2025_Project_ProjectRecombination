using System;
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace Managers
{
    public class GUIManager : Singleton<GUIManager>
    {
        public GameObject TitleUI { get; private set; }
        public GameObject PrologueUI { get; private set; }
        public GameUIController GameUIController { get; private set; }
        public GameObject EpilogueUI { get; private set; }
        public GameObject LoadingUI { get; private set; }
        public GameObject CreditUI { get; private set; }

        private bool _isInit;

        public void Init(Dictionary<EUIType, GameObject> uiInstances)
        {
            try
            {
                if (uiInstances.TryGetValue(EUIType.Title, out GameObject titleUI) && titleUI != null)
                {
                    TitleUI = titleUI;
                }
                if (uiInstances.TryGetValue(EUIType.Prologue, out GameObject prologueUI) && prologueUI != null)
                {
                    PrologueUI = prologueUI;
                }
                if (uiInstances.TryGetValue(EUIType.GameUIController, out GameObject gameUIController) &&
                    gameUIController != null)
                {
                    GameUIController = gameUIController.GetComponent<GameUIController>();
                }
                if (uiInstances.TryGetValue(EUIType.Epilogue, out GameObject epilogueUI) && epilogueUI != null)
                {
                    EpilogueUI = epilogueUI;
                }
                if (uiInstances.TryGetValue(EUIType.Loading, out GameObject loadingUI) && loadingUI != null)
                {
                    LoadingUI = loadingUI;
                }
                if (uiInstances.TryGetValue(EUIType.Credit, out GameObject creditUI) && creditUI != null)
                {
                    CreditUI = creditUI;
                }
                
                CheckValidation(EUIType.Title, titleUI);
                CheckValidation(EUIType.Prologue, prologueUI);
                CheckValidation(EUIType.GameUIController, gameUIController);
                CheckValidation(EUIType.Epilogue, epilogueUI);
                CheckValidation(EUIType.Loading, loadingUI);
                CheckValidation(EUIType.Credit, creditUI);
                
                _isInit = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        private static void CheckValidation(EUIType uiType, GameObject uiInstance)
        {
            if (uiInstance == null)
            {
                Debug.LogError($"[GUIManager] {uiType} UI instance is null.");
            }
        }

        private void Update()
        {
            if (!_isInit) return;
            
            switch (GameManager.Instance.CurrentState)
            {
                case GameManager.GameState.Title:
                    TitleUI.SetActive(true);
                    PrologueUI.SetActive(false);
                    GameUIController.gameObject.SetActive(false);
                    EpilogueUI.SetActive(false);
                    LoadingUI.SetActive(false);
                    CreditUI.SetActive(false);
                    break;
                case GameManager.GameState.Prologue:
                    TitleUI.SetActive(false);
                    PrologueUI.SetActive(true);
                    GameUIController.gameObject.SetActive(false);
                    EpilogueUI.SetActive(false);
                    LoadingUI.SetActive(false);
                    CreditUI.SetActive(false);
                    break;
                case GameManager.GameState.Playing:
                    TitleUI.SetActive(false);
                    PrologueUI.SetActive(false);
                    GameUIController.gameObject.SetActive(true);
                    EpilogueUI.SetActive(false);
                    LoadingUI.SetActive(false);
                    CreditUI.SetActive(false);
                    break;
                case GameManager.GameState.Epilogue:
                    TitleUI.SetActive(false);
                    PrologueUI.SetActive(false);
                    GameUIController.gameObject.SetActive(false);
                    EpilogueUI.SetActive(true);
                    LoadingUI.SetActive(false);
                    CreditUI.SetActive(false);
                    break;
                case GameManager.GameState.Loading:
                    TitleUI.SetActive(false);
                    PrologueUI.SetActive(false);
                    GameUIController.gameObject.SetActive(false);
                    EpilogueUI.SetActive(false);
                    LoadingUI.SetActive(true);
                    CreditUI.SetActive(false);
                    break;
            }
        }
    }
}