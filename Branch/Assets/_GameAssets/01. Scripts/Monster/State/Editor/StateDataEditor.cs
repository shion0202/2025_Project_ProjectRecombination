using System.Linq;
using UnityEditor;
using UnityEngine;
using Monster;

[CustomEditor(typeof(MonsterStateData))]
public class StateDataEditor : Editor
{
    private string newStateName = "";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("사용자 지정 상태 추가", EditorStyles.boldLabel);
        newStateName = EditorGUILayout.TextField("상태 이름", newStateName);

        if (GUILayout.Button("상태 추가") && !string.IsNullOrWhiteSpace(newStateName))
        {
            var stateData = (MonsterStateData)target;
            // 기본 상태와 중복 체크
            bool isDuplicate = stateData.states != null &&
                               stateData.states.Any(s => s.stateName == newStateName);
            if (!isDuplicate)
            {
                // 사용자 지정 상태 추가
                var newStates = stateData.states == null ?
                    new MonsterStateData.StateDefinition[1] :
                    new MonsterStateData.StateDefinition[stateData.states.Length + 1];
                if (stateData.states != null)
                    stateData.states.CopyTo(newStates, 0);
                newStates[newStates.Length - 1] = new MonsterStateData.StateDefinition
                {
                    stateName = newStateName,
                    bitIndex = 0 // OnValidate에서 자동 할당됨
                };
                stateData.states = newStates;
                newStateName = "";
                EditorUtility.SetDirty(stateData);
                // OnValidate 강제 호출
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
            else
            {
                EditorUtility.DisplayDialog("중복 오류", "이미 존재하는 상태 이름입니다.", "확인");
            }
        }
    }
}