using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 선택한 게임오브젝트 및 프리팹에서 Missing Script를 제거하는 에디터 클래스
#if UNITY_EDITOR
public class RemoveMissingScripts : MonoBehaviour
{
    // 메뉴 항목을 통해 선택한 프리팹 또는 게임오브젝트에서 Missing Script를 제거
    [MenuItem("GameObject/Utility/Remove Missing Scripts", priority = 100)]
    public static void DeleteMissingScriptInHierarchy()
    {
        List<GameObject> selectedObjects = new List<GameObject>(Selection.gameObjects);
        if (selectedObjects.Count > 0)
            DeleteInternal(selectedObjects);
    }

    // 우클릭 메뉴를 통해 선택한 프리팹에서 Missing Script를 제거
    [MenuItem("Assets/Remove Missing Scripts", priority = 100)]
    public static void RemoveMissingScriptsInSelectedPrefabs()
    {
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".prefab"))
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                DeleteMissingScriptsRecursive(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void DeleteMissingScriptsRecursive(GameObject obj)
    {
        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
        if (missingCount > 0)
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);

        foreach (Transform child in obj.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }
    }

    private static void DeleteInternal(List<GameObject> list)
    {
        int deleteCount = 0;
        foreach (GameObject obj in list)
            RecursiveDeleteMissingScript(obj, ref deleteCount);

        if (deleteCount > 0)
        {
            Debug.Log($"{nameof(RemoveMissingScripts)} : Delete Missing Script Count {deleteCount} ");
            AssetDatabase.SaveAssets();
        }
    }

    private static void RecursiveDeleteMissingScript(GameObject obj, ref int deleteCount)
    {
        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
        if (missingCount > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            deleteCount++;
        }
        foreach (Transform childTransform in obj.GetComponentsInChildren<Transform>(true))
        {
            GameObject child = childTransform.gameObject;
            missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child);
            if (missingCount > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child);
                deleteCount++;
            }
        }
    }
}
#endif
