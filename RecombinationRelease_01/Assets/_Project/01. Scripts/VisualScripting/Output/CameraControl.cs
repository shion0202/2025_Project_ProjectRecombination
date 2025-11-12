using System;
using System.Collections;
using Cinemachine;
using Managers;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    
    public enum CutsceneMode
    {
        FadeInNOut, // 페이드 인 아웃
        CameraMove, // 카메라 이동
    }

    [System.Serializable]
    public struct CinemachineCutsceneData
    {
        public CinemachineVirtualCamera cutsceneVirtualCamera;     // 컷씬 재생할 카메라
        public float cutsceneDuration;                             // 컷씬 재생 시간

        public CinemachineCutsceneData(CinemachineVirtualCamera camera, float duration)
        {
            cutsceneVirtualCamera = camera;
            cutsceneDuration = duration;
        }
    }

    public class CameraControl : ProcessBase
    {
        [Header("Cinemachine Virtual Camera Settings")]
        [SerializeField] private CinemachineVirtualCamera mainVirtualCamera;        // 컷씬 재생 후 되돌아갈 카메라
        // [SerializeField] private CinemachineVirtualCamera cutsceneVirtualCamera;    // 컷씬 재생할 카메라
        // [SerializeField] private float cutsceneDuration = 5f;                       // 컷씬 재생 시간
        [SerializeField] private CinemachineCutsceneData[] cutsceneData;
    
        // [Header("")]
        [Header("Cinemachine Brain Settings")]
        [SerializeField] private CinemachineBrain cinemachineBrain;
        [SerializeField] private CutsceneMode cutsceneMode = CutsceneMode.CameraMove;
        [SerializeField] private float cutsceneBlendTime = 1f; // 컷씬 블렌드 시간
        private Coroutine _cinemachineBlendRoutine = null;

        private void Start()
        {
            // Find 메서드로 다른 스크립트에서 인스턴스 되는 FollowCamera를 찾아서 사용하는 구조인데 안정성이 떨어진다.
            // 인스팩터에서 하이라키에 등록된 FollowCamera를 등록하는 것으로 해결

            if (mainVirtualCamera is not null) return;
            mainVirtualCamera = GameObject.Find("FollowCamera").GetComponent<CinemachineVirtualCamera>();

            if (mainVirtualCamera is not null) return;
            Debug.LogError("No Cinemachine Virtual Camera found in the scene.");
        }
    
        public override void Execute()
        {
            if (IsOn) return;
            BlendToCutscene();                            // 컷씬 모드에 따라 블렌딩 방식이 다르게 설정
            StartCoroutine(SwitchToCutscene());     // ~~컷씬 전환을 GUI Manager에 맞기자.~~ (뭔 소리야 이게 GUI Manager가 Camera를 관리하면 어쩌자는 거야)
            IsOn = true;
        }

        private IEnumerator SwitchToCutscene()
        {
            int originalPriority = mainVirtualCamera.Priority;
            GameManager.Instance.PauseObjects();        // Game Manager에 의해 특정 오브젝트들만 Pause

            // 컷씬 카메라의 우선순위를 높이고, 메인 카메라의 우선순위를 낮춘다.
            for (int i = 0; i < cutsceneData.Length; i++)
            {
                CinemachineCutsceneData cutscene = cutsceneData[i];
                if (cutscene.cutsceneVirtualCamera is null)
                {
                    Debug.LogError($"Cutscene camera at index {i} is not assigned.");
                    continue;
                }
                if (cutsceneMode == CutsceneMode.FadeInNOut) GUIManager.Instance.FadeOut(cutsceneBlendTime);
                cutscene.cutsceneVirtualCamera.Priority = originalPriority + 1 + i;
                if (cutsceneMode == CutsceneMode.FadeInNOut) GUIManager.Instance.FadeIn(cutsceneBlendTime);

                yield return new WaitForSeconds(cutscene.cutsceneDuration);
            }

            foreach (CinemachineCutsceneData cutscene in cutsceneData)
            {
                if (cutsceneMode == CutsceneMode.FadeInNOut) GUIManager.Instance.FadeOut(cutsceneBlendTime);
                cutscene.cutsceneVirtualCamera.Priority = originalPriority - 1;
                if (cutsceneMode == CutsceneMode.FadeInNOut) GUIManager.Instance.FadeIn(cutsceneBlendTime);
            }
            StartCoroutine(CoCheckBlendCamera());
        }

        private void BlendToCutscene()
        {
            if (cinemachineBrain is null) return;
        
            CinemachineBlendDefinition.Style style = cutsceneMode switch
            {
                CutsceneMode.FadeInNOut => CinemachineBlendDefinition.Style.Cut,
                CutsceneMode.CameraMove => CinemachineBlendDefinition.Style.EaseInOut,
                _ => CinemachineBlendDefinition.Style.EaseInOut
            };
        
            // Blend to cutscene camera
            var blend = new CinemachineBlendDefinition
            {
                m_Style = style,
                m_Time = cutsceneBlendTime
            };
        
            cinemachineBrain.m_DefaultBlend = blend;
        }

        private IEnumerator CoCheckBlendCamera()
        {
            while (cinemachineBrain.IsBlending)
            {
                yield return null;

                Debug.Log("카메라 블렌딩 중");
                yield return null;
            }

            GameManager.Instance.UnpauseObjects();
            yield break;
        }
    }
}
