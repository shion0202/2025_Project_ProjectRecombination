using UnityEditor;
using UnityEngine;

public class DeleteEmptyGameObject
{
    [MenuItem("Tools/Delete Empty GameObjects")]
    private static void DeleteEmptyGameObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("삭제할 빈 GameObject를 선택하세요.");
            return;
        }

        foreach (var obj in selectedObjects)
        {
            if (obj.transform.childCount == 0 && obj.GetComponents<Component>().Length == 1)
            {
                Debug.Log($"빈 GameObject '{obj.name}'가 삭제되었습니다.");
                Object.DestroyImmediate(obj);
            }
            else
            {
                Debug.Log($"GameObject '{obj.name}'는 빈 GameObject가 아닙니다.");
            }
        }
    }
}