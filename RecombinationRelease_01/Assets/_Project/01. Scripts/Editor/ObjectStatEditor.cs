using System.Linq;
using UnityEditor;

[CustomEditor(typeof(DamagableObject))]
public class ObjectStatEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DamagableObject dObject = (DamagableObject)target;

        // 기본 인스펙터 그리기
        base.OnInspectorGUI();

        // Enum 값 전체 가져오기
        var statTypes = System.Enum.GetValues(typeof(EStatType)).Cast<EStatType>();

        // 리스트에 누락된 StatType 추가
        foreach (var statType in statTypes)
        {
            if (!dObject.StatList.Any(s => s.StatName == statType.ToString()))
            {
                dObject.StatList.Add(new StatPair { StatName = statType.ToString(), StatValue = 0 });
            }
        }

        // 각 StatType별로 입력 가능하게 표시
        //EditorGUI.BeginChangeCheck();
        //for (int i = 0; i < dObject.StatList.Count; ++i)
        //{
        //    var stat = dObject.StatList[i];
        //    stat.StatValue = EditorGUILayout.FloatField(stat.StatName, stat.StatValue);
        //    dObject.StatList[i] = stat;
        //}
        //if (EditorGUI.EndChangeCheck())
        //{
        //    EditorUtility.SetDirty(dObject);
        //}
    }
}
