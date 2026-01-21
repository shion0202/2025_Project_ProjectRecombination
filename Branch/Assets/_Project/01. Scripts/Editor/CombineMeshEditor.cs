using UnityEditor;
using UnityEngine;

public class CombineMeshEditor
{
    [MenuItem("Tools/Combine Meshes")]
    private static void CombineMesh()
    {
        GameObject[] objs = Selection.gameObjects;

        if (objs.Length == 0)
        {
            Debug.LogWarning("GameObject를 하나 이상 선택하세요.");
            return;
        }
        
        var combinedObj = new GameObject("CombinedMesh");
        var combinedFilter = combinedObj.AddComponent<MeshFilter>();
        var combinedRenderer = combinedObj.AddComponent<MeshRenderer>();

        if (objs[0].TryGetComponent(out MeshRenderer firstRenderer))
        {
            combinedRenderer.sharedMaterials = firstRenderer.sharedMaterials;
        }
        
        var meshFilters = new MeshFilter[objs.Length];
        var combine = new CombineInstance[objs.Length];
        
        for (var i = 0; i < objs.Length; i++)
        {
            var mf = objs[i].GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"GameObject '{objs[i].name}'에 MeshFilter가 없습니다.");
                continue;
            }
            
            meshFilters[i] = mf;
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = objs[i].transform.localToWorldMatrix;
        }
        
        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine, true, true);
        combinedFilter.sharedMesh = combinedMesh;
        combinedObj.AddComponent<MeshCollider>();
        
        // 리소스 저장
        var path = "Assets/CombinedMesh.asset";
        AssetDatabase.CreateAsset(combinedMesh, AssetDatabase.GenerateUniqueAssetPath(path));
        AssetDatabase.SaveAssets();

        Debug.Log("Mesh가 성공적으로 결합되었습니다: " + combinedObj.name);
    }
}
