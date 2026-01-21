using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace _Test.Script.Editor
{
    public class LoadAllGameScene
    {
        private const string GameScenesFolderPath = "Assets/_Test/Scene/GameScene";
        
        [MenuItem("Tools/Load All Game Scenes")]
        public static void LoadAllScenes()
        {
            // 1. 게임 씬 폴더 존재 여부 확인
            if (!Directory.Exists(GameScenesFolderPath))
            {
                Debug.LogError($"[SceneTools] 게임 씬 폴더를 찾을 수 없습니다. 경로를 확인해주세요: {GameScenesFolderPath}");
                return;
            }

            // 2. 현재 작업 중인 씬 저장
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // 3. 게임 씬 폴더 내 모든 씬 파일 로드
            string[] sceneFiles = Directory.GetFiles(GameScenesFolderPath, "*.unity", SearchOption.AllDirectories);
            foreach (string sceneFile in sceneFiles)
            {
                EditorSceneManager.OpenScene(sceneFile, OpenSceneMode.Additive);
                Debug.Log($"[SceneTools] 로드된 씬: {sceneFile}");
            }
        }
    }
}