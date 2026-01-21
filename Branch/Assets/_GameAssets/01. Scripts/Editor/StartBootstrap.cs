using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _Project._01._Scripts.Editor
{
    public class StartBootstrap
    {
        // [설정 필요] 부트스트랩 씬의 실제 경로를 입력하세요.
        // 프로젝트 창에서 해당 씬을 우클릭 -> 'Copy Path'를 하면 쉽게 경로를 얻을 수 있습니다.
        private const string BootstrapScenePath = "Assets/_Project/04. Scenes/Bootstrap.unity";

        // 단축키 설정: 윈도우(Ctrl+F5), 맥(Cmd+F5)
        // % = Ctrl(Win)/Cmd(Mac), # = Shift, & = Alt
        [MenuItem("Tools/Play Bootstrap Scene")]
        public static void PlayBootstrap()
        {
            // 0. 플레이 모드 중이라면 정지 (선택 사항)
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            // 1. 부트스트랩 씬 파일 존재 여부 확인
            if (!File.Exists(BootstrapScenePath))
            {
                Debug.LogError($"[SceneTools] 부트스트랩 씬을 찾을 수 없습니다. 경로를 확인해주세요: {BootstrapScenePath}");
                return;
            }

            // 2. 현재 작업 중인 씬 저장
            // 사용자가 취소를 누르면(false 반환) 실행을 중단합니다.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // 3. 부트스트랩 씬 열기
            EditorSceneManager.OpenScene(BootstrapScenePath);

            // 4. 에디터 실행 (플레이 모드 진입)
            EditorApplication.isPlaying = true;
        }
    }
}