using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 멀티 씬 오클루전 컬링 작업용 에디터 창. (Tools > Occlusion Culling Tool)
///
/// 1. 씬 구성: Persistent를 Active Scene으로 열고 선택한 스테이지를 함께 연다.
///    오클루전 데이터는 Active Scene 폴더에 하나로 저장되고 함께 연 씬들만 참조하므로,
///    런타임과 같은 조합(Active Scene = Persistent)으로 열어야 한다. (라이트 베이크와 같은 방식)
/// 2. Occluder 점검: 열린 스테이지에서 Occluder로 쓰면 안 될 가능성이 높은 오브젝트를 찾는다.
///    (투명/알파 클리핑 머티리얼, 움직일 수 있는 오브젝트)
/// 3. 베이크: 열린 씬 조합으로 오클루전을 굽고 저장한다.
/// </summary>
public class OcclusionCullingTool : EditorWindow
{
    private const string PersistentScenePath = "Assets/_GameAssets/04. Scenes/Persistent.unity";
    private const string StageSceneFolder = "Assets/_GameAssets/04. Scenes/GameScene";
    private const string DemoStageName = "0th_Demo_Room";
    private const string SelectionPrefsKey = "OcclusionCullingTool.SelectedStages";

    private readonly List<string> _stagePaths = new();
    private readonly HashSet<string> _selectedStages = new();

    private readonly List<Suspect> _suspects = new();
    private Vector2 _scroll;
    private bool _hasScanned;

    private class Suspect
    {
        public GameObject target;
        public string sceneName;
        public string reason;
    }

    [MenuItem("Tools/Occlusion Culling Tool")]
    private static void Open()
    {
        GetWindow<OcclusionCullingTool>("Occlusion Culling");
    }

    private void OnEnable()
    {
        _stagePaths.Clear();
        if (Directory.Exists(StageSceneFolder))
        {
            _stagePaths.AddRange(Directory.GetFiles(StageSceneFolder, "*.unity")
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path));
        }

        // 기본 선택은 데모 빌드 기준. 마지막 선택은 에디터에 기억한다.
        _selectedStages.Clear();
        string saved = EditorPrefs.GetString(SelectionPrefsKey, DemoStageName);
        foreach (string name in saved.Split('|'))
        {
            if (!string.IsNullOrEmpty(name)) _selectedStages.Add(name);
        }
    }

    private void OnGUI()
    {
        DrawSceneSetup();
        EditorGUILayout.Space(8);
        DrawBake();
        EditorGUILayout.Space(8);
        DrawAudit();
    }

    #region 1. 씬 구성

    private void DrawSceneSetup()
    {
        EditorGUILayout.LabelField("1. 씬 구성", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Persistent는 항상 Active Scene으로 포함된다.\n" +
            "베이크하지 않은 스테이지는 오클루전 데이터가 맞지 않아 그 스테이지에서는 컬링이 정상 동작하지 않는다.",
            MessageType.None);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("데모 빌드만"))
            {
                _selectedStages.Clear();
                _selectedStages.Add(DemoStageName);
                SaveSelection();
            }
            if (GUILayout.Button("전체 선택"))
            {
                _selectedStages.Clear();
                foreach (string path in _stagePaths) _selectedStages.Add(Path.GetFileNameWithoutExtension(path));
                SaveSelection();
            }
            if (GUILayout.Button("전체 해제"))
            {
                _selectedStages.Clear();
                SaveSelection();
            }
        }

        foreach (string path in _stagePaths)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            bool isSelected = _selectedStages.Contains(name);
            bool toggled = EditorGUILayout.ToggleLeft(name, isSelected);
            if (toggled != isSelected)
            {
                if (toggled) _selectedStages.Add(name);
                else _selectedStages.Remove(name);
                SaveSelection();
            }
        }

        if (GUILayout.Button("선택한 조합으로 씬 열기", GUILayout.Height(24)))
        {
            OpenSelectedScenes();
        }

        Scene active = SceneManager.GetActiveScene();
        bool isActiveCorrect = active.path == PersistentScenePath;
        EditorGUILayout.HelpBox(
            $"현재 Active Scene: {(string.IsNullOrEmpty(active.name) ? "(없음)" : active.name)}" +
            (isActiveCorrect ? "" : "\nPersistent가 Active Scene이 아니다. 이 상태로 베이크하면 런타임에 데이터가 맞지 않는다."),
            isActiveCorrect ? MessageType.Info : MessageType.Warning);
    }

    private void SaveSelection()
    {
        EditorPrefs.SetString(SelectionPrefsKey, string.Join("|", _selectedStages));
    }

    private void OpenSelectedScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene persistent = EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
        foreach (string path in _stagePaths)
        {
            if (_selectedStages.Contains(Path.GetFileNameWithoutExtension(path)))
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            }
        }
        SceneManager.SetActiveScene(persistent);

        _suspects.Clear();
        _hasScanned = false;
    }

    #endregion

    #region 2. 베이크

    private void DrawBake()
    {
        EditorGUILayout.LabelField("2. 베이크", EditorStyles.boldLabel);

        // 베이크 설정은 Active Scene에 저장된다. Unity의 Occlusion Culling 창 Bake 탭과 같은 값이다.
        EditorGUI.BeginChangeCheck();
        float smallestOccluder = EditorGUILayout.FloatField(
            new GUIContent("Smallest Occluder", "다른 물체를 가릴 수 있는 물체의 최소 크기(m). 벽 조각보다 조금 작게 둔다."),
            StaticOcclusionCulling.smallestOccluder);
        float smallestHole = EditorGUILayout.FloatField(
            new GUIContent("Smallest Hole", "카메라가 들여다볼 수 있는 가장 작은 틈의 지름(m)."),
            StaticOcclusionCulling.smallestHole);
        float backfaceThreshold = EditorGUILayout.Slider(
            new GUIContent("Backface Threshold", "100 미만이면 벽 안쪽/뒤쪽 데이터를 잘라낸다. 카메라가 벽 너머로 가는 경우가 있어 100을 유지한다."),
            StaticOcclusionCulling.backfaceThreshold, 5.0f, 100.0f);
        if (EditorGUI.EndChangeCheck())
        {
            StaticOcclusionCulling.smallestOccluder = Mathf.Max(0.01f, smallestOccluder);
            StaticOcclusionCulling.smallestHole = Mathf.Max(0.01f, smallestHole);
            StaticOcclusionCulling.backfaceThreshold = backfaceThreshold;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("베이크 설정 창 열기"))
            {
                EditorApplication.ExecuteMenuItem("Window/Rendering/Occlusion Culling");
            }

            GUI.enabled = SceneManager.GetActiveScene().path == PersistentScenePath;
            if (GUILayout.Button("열린 씬으로 베이크 후 저장", GUILayout.Height(24)))
            {
                Bake();
            }
            GUI.enabled = true;
        }
    }

    private void Bake()
    {
        string openScenes = string.Join("\n", OpenScenes().Select(scene => scene.name));
        if (!EditorUtility.DisplayDialog("오클루전 베이크",
                $"다음 씬 조합으로 오클루전을 굽고 씬을 저장한다.\n\n{openScenes}", "베이크", "취소"))
        {
            return;
        }

        // Compute는 완료될 때까지 에디터를 멈춘다. 결과는 Active Scene 폴더에 저장되고 열린 씬들이 참조한다.
        if (!StaticOcclusionCulling.Compute())
        {
            Debug.LogError("[OcclusionCullingTool] 오클루전 베이크 실패");
            return;
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[OcclusionCullingTool] 오클루전 베이크 완료\n{openScenes}");
    }

    private static IEnumerable<Scene> OpenScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; ++i)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded) yield return scene;
        }
    }

    #endregion

    #region 3. Occluder 점검

    private void DrawAudit()
    {
        EditorGUILayout.LabelField("3. Occluder 점검 (열린 스테이지 대상)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Occluder Static인데 뒤가 보여야 하거나 움직일 수 있는 오브젝트를 찾는다.\n" +
            "목록은 후보일 뿐이므로 직접 확인하고 해제한다. 해제는 Occluder Static만 끄고 다른 Static 플래그는 유지한다.",
            MessageType.None);

        if (GUILayout.Button("점검 실행", GUILayout.Height(24)))
        {
            Scan();
        }

        if (!_hasScanned) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"후보 {_suspects.Count}개");
            GUI.enabled = _suspects.Count > 0;
            if (GUILayout.Button("목록 전체 Occluder 해제", GUILayout.Width(170)) &&
                EditorUtility.DisplayDialog("Occluder 일괄 해제", $"후보 {_suspects.Count}개의 Occluder Static을 끈다.", "해제", "취소"))
            {
                foreach (Suspect suspect in _suspects.ToList()) RemoveOccluderFlag(suspect);
                GUIUtility.ExitGUI();   // 목록이 바뀐 채로 이번 GUI 패스를 계속 그리면 레이아웃 불일치 에러가 난다.
            }
            GUI.enabled = true;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (Suspect suspect in _suspects.ToList())
        {
            if (suspect.target == null) continue;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField($"[{suspect.sceneName}] {suspect.target.name}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(suspect.reason, EditorStyles.wordWrappedMiniLabel);
                }

                if (GUILayout.Button("선택", GUILayout.Width(50)))
                {
                    Selection.activeGameObject = suspect.target;
                    EditorGUIUtility.PingObject(suspect.target);
                    SceneView.FrameLastActiveSceneView();
                }
                if (GUILayout.Button("해제", GUILayout.Width(50)))
                {
                    RemoveOccluderFlag(suspect);
                    GUIUtility.ExitGUI();
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        _suspects.Clear();
        _hasScanned = true;

        foreach (Scene scene in OpenScenes())
        {
            if (scene.path == PersistentScenePath) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    GameObject go = renderer.gameObject;
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                    if ((flags & StaticEditorFlags.OccluderStatic) == 0) continue;

                    List<string> reasons = FindReasons(renderer);
                    if (reasons.Count == 0) continue;

                    _suspects.Add(new Suspect
                    {
                        target = go,
                        sceneName = scene.name,
                        reason = string.Join(" / ", reasons)
                    });
                }
            }
        }

        Debug.Log($"[OcclusionCullingTool] Occluder 점검 완료: 후보 {_suspects.Count}개");
    }

    private static List<string> FindReasons(Renderer renderer)
    {
        List<string> reasons = new();

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null) continue;

            // URP Lit 계열: _Surface 1 = Transparent. 그 외 셰이더는 렌더 큐로 판단한다.
            bool isTransparent = (material.HasProperty("_Surface") && material.GetFloat("_Surface") >= 1.0f)
                                 || material.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.GeometryLast + 1;
            // 알파 클리핑은 구멍 뚫린 형태(철망, 창살 등)라 뒤가 보여야 한다.
            bool isAlphaClipped = (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") >= 1.0f)
                                  || material.IsKeywordEnabled("_ALPHATEST_ON");

            if (isTransparent) reasons.Add($"투명 머티리얼({material.name})");
            else if (isAlphaClipped) reasons.Add($"알파 클리핑 머티리얼({material.name})");
        }

        // 움직일 수 있으면 베이크 당시 위치 기준으로 가려서, 문을 열어도 안쪽이 안 보이는 식의 문제가 생긴다.
        Animator animator = renderer.GetComponentInParent<Animator>(true);
        if (animator != null) reasons.Add($"Animator로 움직일 수 있음({animator.name})");

        Animation animation = renderer.GetComponentInParent<Animation>(true);
        if (animation != null) reasons.Add($"Animation으로 움직일 수 있음({animation.name})");

        Rigidbody rigidbody = renderer.GetComponentInParent<Rigidbody>(true);
        if (rigidbody != null) reasons.Add($"Rigidbody가 있음({rigidbody.name})");

        return reasons;
    }

    private void RemoveOccluderFlag(Suspect suspect)
    {
        if (suspect.target == null) return;

        Undo.RecordObject(suspect.target, "Remove Occluder Static");
        StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(suspect.target);
        GameObjectUtility.SetStaticEditorFlags(suspect.target, flags & ~StaticEditorFlags.OccluderStatic);
        EditorSceneManager.MarkSceneDirty(suspect.target.scene);

        _suspects.Remove(suspect);
    }

    #endregion
}
