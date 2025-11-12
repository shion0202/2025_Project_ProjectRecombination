using UnityEditor;
using UnityEngine;

public class HierarchyEditor
{ 
    [MenuItem("Tools/Unparent to Parent Level %&E")]
    private static void UnparentToParentLevel()
    {
        var selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("Unparent할 GameObject를 선택하세요.");
            return;
        }

        foreach (var obj in selectedObjects)
        {
            var current = obj.transform;
            var parent = current.parent;

            if (parent == null)
            {
                Debug.Log($"{obj.name}은 이미 루트 오브젝트입니다.");
                continue;
            }

            var grandParent = parent.parent;
            if (grandParent != null)
            {
                current.SetParent(grandParent, true);
                Debug.Log($"{obj.name}이 {grandParent.name}의 자식으로 이동되었습니다.");
            }
            else
            {
                current.SetParent(null, true);
                Debug.Log($"{obj.name}이 루트 오브젝트로 이동되었습니다.");
            }

            // 부모 드롭다운 닫기
            CollapseHierarchy(parent.gameObject);
        }
    }

    private static void CollapseHierarchy(GameObject target)
    {
        var hierarchyType = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        if (hierarchyType == null)
        {
            Debug.LogWarning("SceneHierarchyWindow 클래스를 찾을 수 없습니다.");
            return;
        }

        var windows = Resources.FindObjectsOfTypeAll(hierarchyType);
        if (windows.Length == 0)
        {
            Debug.LogWarning("Hierarchy 창을 찾을 수 없습니다.");
            return;
        }

        var window = windows[0];
        var setExpandedMethod = hierarchyType.GetMethod(
            "SetExpanded", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        if (setExpandedMethod == null)
        {
            Debug.LogWarning("SetExpanded 메서드를 찾을 수 없습니다.");
            return;
        }

        CollapseRecursive(target.transform, setExpandedMethod, window);
    }

    private static void CollapseRecursive(Transform target, System.Reflection.MethodInfo method, object window)
    {
        method.Invoke(window, new object[] { target.gameObject.GetInstanceID(), false });

        for (int i = 0; i < target.childCount; i++)
        {
            CollapseRecursive(target.GetChild(i), method, window);
        }
    }
}