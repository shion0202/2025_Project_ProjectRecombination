using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [1회 작업용] 프로젝트 음원을 쓰는 AudioSource의 볼륨을 1로 되돌린다. (Tools > Audio Volume Reset)
///
/// 음원마다 크기가 달라 AudioSource 볼륨을 귀로 하나씩 낮춰두었는데,
/// 음원 파일 자체를 라우드니스 정규화로 맞추면서 개별 볼륨 조정이 필요 없어졌다.
/// 남겨두면 정규화 결과와 겹쳐 이중으로 작아지므로 전부 1로 통일한다.
///
/// 대상: 06. Audio 폴더의 클립을 쓰고 볼륨이 1이 아닌 AudioSource (프리팹, 04. Scenes 아래 씬)
/// 씬 파일을 열고 저장하므로 라이트 베이크 중에는 실행하지 않는다.
/// </summary>
public class AudioVolumeResetTool : EditorWindow
{
    private const string AudioFolder = "Assets/_GameAssets/05. Art/06. Audio/";
    private const string PrefabFolder = "Assets/_GameAssets";
    private const string SceneFolder = "Assets/_GameAssets/04. Scenes";

    private class Target
    {
        public string assetPath;     // 프리팹 또는 씬 경로
        public string objectPath;    // 계층 경로
        public string clipName;
        public float volume;
        public bool isScene;
    }

    private readonly List<Target> _targets = new();
    private Vector2 _scroll;
    private bool _hasScanned;

    [MenuItem("Tools/Audio Volume Reset")]
    private static void Open()
    {
        GetWindow<AudioVolumeResetTool>("Audio Volume Reset");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "06. Audio 폴더의 음원을 쓰는 AudioSource 중 볼륨이 1이 아닌 것을 찾아 1로 바꾼다.\n" +
            "씬을 열고 저장하므로 저장하지 않은 변경은 먼저 저장하고, 라이트 베이크가 끝난 뒤 실행한다.",
            MessageType.None);

        bool isBaking = Lightmapping.isRunning;
        if (isBaking)
        {
            EditorGUILayout.HelpBox("라이트 베이크 진행 중에는 실행할 수 없다.", MessageType.Warning);
        }

        GUI.enabled = !isBaking;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("대상 찾기", GUILayout.Height(24)))
            {
                Scan();
                GUIUtility.ExitGUI();   // 목록이 바뀐 채로 이번 GUI 패스를 계속 그리면 레이아웃 불일치 에러가 난다.
            }

            GUI.enabled = !isBaking && _targets.Count > 0;
            if (GUILayout.Button("전부 1로 적용", GUILayout.Height(24)) &&
                EditorUtility.DisplayDialog("볼륨 초기화", $"AudioSource {_targets.Count}개의 볼륨을 1로 바꾸고 프리팹과 씬을 저장한다.", "적용", "취소"))
            {
                Apply();
                GUIUtility.ExitGUI();
            }
        }
        GUI.enabled = true;

        if (!_hasScanned) return;

        EditorGUILayout.LabelField($"대상 {_targets.Count}개", EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (IGrouping<string, Target> group in _targets.GroupBy(target => target.assetPath))
        {
            EditorGUILayout.LabelField(group.Key, EditorStyles.miniBoldLabel);
            foreach (Target target in group)
            {
                EditorGUILayout.LabelField($"    {target.objectPath}  |  {target.clipName}  |  {target.volume:0.###}");
            }
        }
        EditorGUILayout.EndScrollView();
    }

    #region Scan

    private void Scan()
    {
        // 씬을 열고 닫은 뒤 원래 구성으로 되돌릴 때 디스크에서 다시 불러오므로, 저장하지 않은 변경이 있으면 먼저 저장한다.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        _targets.Clear();
        _hasScanned = true;

        ScanPrefabs();
        ForEachScene(false, (scenePath, scene) => CollectTargets(scene.GetRootGameObjects(), scenePath, true));

        Debug.Log($"[AudioVolumeResetTool] 대상 {_targets.Count}개");
    }

    private void ScanPrefabs()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            CollectTargets(new[] { prefab }, path, false);
        }
    }

    private void CollectTargets(IEnumerable<GameObject> roots, string assetPath, bool isScene)
    {
        foreach (GameObject root in roots)
        {
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (!NeedsReset(source)) continue;

                _targets.Add(new Target
                {
                    assetPath = assetPath,
                    objectPath = GetHierarchyPath(source.transform),
                    clipName = source.clip.name,
                    volume = source.volume,
                    isScene = isScene
                });
            }
        }
    }

    private static bool NeedsReset(AudioSource source)
    {
        if (source.clip == null) return false;
        if (Mathf.Approximately(source.volume, 1.0f)) return false;

        return AssetDatabase.GetAssetPath(source.clip).StartsWith(AudioFolder);
    }

    #endregion

    #region Apply

    private void Apply()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int changed = 0;

        // 프리팹: 대상이 있는 프리팹만 열어서 수정한다.
        foreach (string prefabPath in _targets.Where(target => !target.isScene).Select(target => target.assetPath).Distinct())
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                int count = ResetVolumes(contents.GetComponentsInChildren<AudioSource>(true));
                if (count > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                    changed += count;
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        // 씬: 대상이 있는 씬만 수정하고 저장한다.
        HashSet<string> scenePaths = new(_targets.Where(target => target.isScene).Select(target => target.assetPath));
        ForEachScene(true, (scenePath, scene) =>
        {
            if (!scenePaths.Contains(scenePath)) return;

            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                count += ResetVolumes(root.GetComponentsInChildren<AudioSource>(true));
            }

            if (count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                changed += count;
            }
        });

        AssetDatabase.SaveAssets();
        Debug.Log($"[AudioVolumeResetTool] AudioSource {changed}개의 볼륨을 1로 변경");

        Scan();   // 남은 대상이 없는지 확인용
    }

    private static int ResetVolumes(IEnumerable<AudioSource> sources)
    {
        int count = 0;
        foreach (AudioSource source in sources)
        {
            if (!NeedsReset(source)) continue;

            // SerializedObject로 수정해야 프리팹 인스턴스의 오버라이드도 올바르게 기록된다.
            SerializedObject serialized = new SerializedObject(source);
            serialized.FindProperty("m_Volume").floatValue = 1.0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ++count;
        }
        return count;
    }

    #endregion

    /// <summary>
    /// 04. Scenes 아래 씬을 하나씩 순회한다. 열려 있지 않은 씬은 추가로 열었다가 닫고,
    /// 끝나면 원래 열려 있던 씬 구성(Active Scene 포함)으로 되돌린다.
    /// </summary>
    private static void ForEachScene(bool allowSave, System.Action<string, Scene> action)
    {
        SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
        string[] scenePaths = Directory.GetFiles(SceneFolder, "*.unity", SearchOption.AllDirectories)
            .Select(path => path.Replace('\\', '/'))
            .ToArray();

        try
        {
            for (int i = 0; i < scenePaths.Length; ++i)
            {
                string path = scenePaths[i];
                EditorUtility.DisplayProgressBar("Audio Volume Reset", path, (float)i / scenePaths.Length);

                Scene scene = SceneManager.GetSceneByPath(path);
                bool openedHere = !scene.IsValid() || !scene.isLoaded;
                if (openedHere)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }

                action(path, scene);

                if (openedHere && SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            if (originalSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
