using UnityEditor;
using UnityEngine;

// 선택한 게임오브젝트의 Bone 데이터를 스크립터블 오브젝트로 저장하는 에디터 스크립트
public class BoneLoader : MonoBehaviour
{
    [MenuItem("GameObject/Utility/Save Bone Data To Scriptable Object", priority = 100)]
    public static void SaveBoneDataToSO()
    {
        Object selectedObj = Selection.activeObject;
        if (selectedObj == null)
        {
            Debug.LogWarning("오브젝트를 선택하세요.");
            return;
        }

        GameObject targetGO = null;

        // 1. 프로젝트 창에서 프리팹 에셋 선택 시
        if (PrefabUtility.IsPartOfPrefabAsset(selectedObj))
        {
            targetGO = PrefabUtility.InstantiatePrefab(selectedObj) as GameObject;
        }
        // 2. 씬에서 GameObject 선택 시
        else if (selectedObj is GameObject)
        {
            targetGO = selectedObj as GameObject;
        }

        if (targetGO == null)
        {
            Debug.LogWarning("GameObject를 찾을 수 없습니다.");
            return;
        }

        // 저장 경로 입력
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Bone Data",
            "BoneData",
            "asset",
            "ScriptableObject를 저장할 경로와 파일명을 입력하세요."
        );
        if (string.IsNullOrEmpty(path))
            return;

        // ScriptableObject 생성 및 데이터 입력
        CharacterBoneData boneMapSO = ScriptableObject.CreateInstance<CharacterBoneData>();

        var part = targetGO.GetComponentInChildren<PartBase>();
        var smr = part.gameObject.GetComponent<SkinnedMeshRenderer>();
        foreach (var bone in smr.bones)
        {
            if (bone == null)
            {
                boneMapSO.boneNames.Add("");
                continue;
            }

            boneMapSO.boneNames.Add(bone.name);
        }

        AssetDatabase.CreateAsset(boneMapSO, path);
        AssetDatabase.SaveAssets();

        // 프리팹 에셋에서 임시로 인스턴스화한 경우 삭제
        if (PrefabUtility.IsPartOfPrefabAsset(selectedObj) && targetGO != null)
        {
            GameObject.DestroyImmediate(targetGO);
        }

        Debug.Log($"Bone Data 저장 완료: {path}");
        AssetDatabase.Refresh();
    }
}
