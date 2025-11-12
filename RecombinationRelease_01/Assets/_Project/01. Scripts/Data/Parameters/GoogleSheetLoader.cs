using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif

[System.Serializable]
public struct SheetInfo
{
    public string sheetKey;     // 시트 식별 이름 (ex: "CharacterStats")
    public string sheetLink;    // 구글 스프레드시트 ID
    public string workSheetGid; // 시트 GID
}

[System.Serializable]
public class SheetData
{
    public string sheetKey;                             // 시트 이름
    public List<RowData> rows = new List<RowData>();    // 해당 시트의 데이터 목록
}

[System.Serializable]
public class RowData
{
    [System.Serializable]
    public struct StatEntry
    {
        public EStatType key;
        public StatData value;
    }

    [SerializeField] private List<StatEntry> statEntries = new List<StatEntry>();
    private StatDictionary _stats;
    private bool _isDirty = true;

    public StatDictionary Stats
    {
        get
        {
            if (_isDirty || _stats == null)
            {
                BuildDictionary();
            }
            return _stats;
        }
    }

    private void BuildDictionary()
    {
        _stats = new StatDictionary();
        foreach (var entry in statEntries)
        {
            _stats.SetStat(entry.value);
        }
        _isDirty = false;
    }

    public float GetStat(EStatType key)
    {
        var stat = Stats[key];
        return stat != null ? stat.value : 0.0f;
    }

    public string GetStringStat(EStatType key)
    {
        var stat = Stats[key];
        return stat != null ? stat.stringValue : string.Empty;
    }

    public void SetStat(EStatType key, float value)
    {
        var idx = statEntries.FindIndex(e => e.key == key);
        var newData = new StatData(key, value);

        if (idx >= 0)
        {
            statEntries[idx] = new StatEntry { key = key, value = newData };
        }
        else
        {
            statEntries.Add(new StatEntry { key = key, value = newData });
        }

        _isDirty = true;
    }

    public void SetStat(EStatType key, string value)
    {
        var idx = statEntries.FindIndex(e => e.key == key);
        var newData = new StatData(key, 0f, value);

        if (idx >= 0)
        {
            statEntries[idx] = new StatEntry { key = key, value = newData };
        }
        else
        {
            statEntries.Add(new StatEntry { key = key, value = newData });
        }

        _isDirty = true;
    }
    
    // 문자열로 출력하는 메서드
    public override string ToString()
    {
        // 각 StatEntry를 key: value 형태로 나열
        string result = "{ ";
        foreach (var entry in statEntries)
        {
            string valStr = !string.IsNullOrEmpty(entry.value.stringValue) ? entry.value.stringValue : entry.value.value.ToString("F2");
            result += $"{entry.key}: {valStr}, ";
        }
        result = result.TrimEnd(',', ' ') + " }";
        return result;
    }
}

[CreateAssetMenu(fileName = "ParamDatas", menuName = "Scriptable Object/Parameter Datas", order = 21)]
public class GoogleSheetLoader : ScriptableObject
{
    // Stat Type Enum과 String을 매핑
    private static readonly Dictionary<string, EStatType> headerToStatType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "id", EStatType.ID },
            { "name", EStatType.Name },
            { "maxHp", EStatType.MaxHp },
            { "damage", EStatType.Damage },
            { "walkSpeed", EStatType.WalkSpeed },
            { "runSpeed", EStatType.RunSpeed },
            { "Defence", EStatType.Defence },
            { "maxDetectiveRange", EStatType.MaxDetectiveRange },
            { "minDetectiveRange", EStatType.MinDetectiveRange },
            { "partsType", EStatType.PartType },
            { "intervalBetweenShots", EStatType.IntervalBetweenShots },
            { "addHp", EStatType.AddHp },
            { "addDefence", EStatType.AddDefence },
            { "addMoveSpeed", EStatType.AddMoveSpeed },
            { "damageReductionRate", EStatType.DamageReductionRate },
            { "cooldownReduction", EStatType.CooldownReduction },
            { "statusEffectType", EStatType.StatusEffectType },
            // { "addDefence", EStatType.Defence },
            // { "addSpeed", EStatType.BaseMoveSpeed },
            // { "cooldownDecrease", EStatType.CooldownDecrease },
            // { "attackSkillDamage", EStatType.SkillDamage },
            // { "skillSpeed", EStatType.SkillSpeed },
            // { "skillCount", EStatType.SkillCount },
            // { "skillCooldown", EStatType.SkillCooldown },
            // { "attackAilment", EStatType.Ailment },

            // 몬스터 스킬 관련
            { "skillType", EStatType.SkillType },           // 스킬 타입
            { "range", EStatType.Range },                   // 스킬 사거리
            { "cooldownTime", EStatType.CooldownTime },     // 스킬 쿨타임
            { "animSpeed", EStatType.AnimSpeed },           // 애니메이션 속도
            
            // monster2Skill
            { "charId", EStatType.ID},
            { "monsterSkillsId", EStatType.IdArray}
            
            // TODO: 필요하면 계속 추가
        };

    [Header("시트 목록")]
    [SerializeField] private List<SheetInfo> sheets = new();

    [Header("불러온 데이터 목록")]
    [SerializeField] private List<SheetData> sheetDatas = new();

    // 런타임 전용 Dictionary (Index → RowData)
    private Dictionary<string, Dictionary<int, RowData>> _dataDict = new();
    public Dictionary<string, Dictionary<int, RowData>> DataDict => _dataDict;

    private void OnEnable()
    {
        if (sheetDatas != null && sheetDatas.Count > 0)
        {
            SyncListToDict();
        }
        else
        {
            Debug.Log($"[GoogleSheetLoader] '{name}' : sheetDatas가 비어있어 딕셔너리를 빌드하지 않았습니다.");
        }
    }

    public void SyncListToDict()
    {
        _dataDict.Clear();
        if (sheetDatas == null) return;

        foreach (var sheet in sheetDatas)
        {
            if (string.IsNullOrEmpty(sheet.sheetKey)) continue;

            if (_dataDict.ContainsKey(sheet.sheetKey))
            {
                Debug.LogWarning($"[GoogleSheetLoader] '{sheet.sheetKey}' 키가 중복됩니다. 마지막 항목만 사용됩니다.");
            }

            var dict = new Dictionary<int, RowData>();
            foreach (var row in sheet.rows)
            {
                int idx = (int)row.GetStat(EStatType.ID);
                dict[idx] = row;
            }
            _dataDict[sheet.sheetKey] = dict;
        }
    }

    public void SyncDictToList()
    {
        sheetDatas.Clear();
        foreach (var kv in _dataDict)
        {
            sheetDatas.Add(new SheetData
            {
                sheetKey = kv.Key,
                rows = new List<RowData>(kv.Value.Values)
            });
        }
    }

    public RowData GetRow(string sheetKey, int index)
    {
        if (_dataDict.TryGetValue(sheetKey, out var dict) && dict.TryGetValue(index, out var row))
            return row;
        return null;
    }

    private string GetSheetURL(string sheetLink, string gid)
    {
        return $"https://docs.google.com/spreadsheets/d/{sheetLink}/export?format=csv&gid={gid}";
    }

    private void ParseCsv(string sheetKey, string csv)
    {
        var sheet = sheetDatas.Find(s => s.sheetKey == sheetKey);
        if (sheet == null)
        {
            sheet = new SheetData { sheetKey = sheetKey };
            sheetDatas.Add(sheet);
        }
        sheet.rows.Clear();

        string[] lines = csv.Replace("\r", "").Split('\n');
        if (lines.Length < 2) return;
        string[] headers = lines[0].Trim().Split(',');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var values = line.Split(',');
            //var values = ParseCsvLine(line);
            var row = new RowData();

            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                // 매핑 딕셔너리 우선 사용
                // 매핑에 없으면 Enum.TryParse 시도
                if (!headerToStatType.TryGetValue(headers[j], out var statType) && !Enum.TryParse(headers[j], true, out statType))
                {
                    // Debug.Log($"[Row {i}] 알 수 없는 StatType: {headers[j]}");
                    continue;
                }

                if (float.TryParse(values[j], out float f))
                {
                    row.SetStat(statType, f);
                }
                else
                {
                    row.SetStat(statType, values[j]);
                    //Debug.LogWarning($"[Row {i}] '{headers[j]}' 값 '{values[j]}'은 숫자가 아닙니다.");
                }
            }

            sheet.rows.Add(row);
        }

        SyncListToDict();
    }
    
    // CSV 파싱 헬퍼 메서드 (따옴표 처리를 지원하는 메서드)
    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string value = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(value);
                value = "";
            }
            else
            {
                value += c;
            }
        }
        result.Add(value);
        return result;
    }

#if UNITY_EDITOR
    // 에디터 버튼에서 실행할 메서드
    public void LoadDataFromSpreadSheet_Editor()
    {
        EditorCoroutineUtility.StartCoroutineOwnerless(CoLoadDataFromSpreadSheet());
    }

    public void ClearData_Editor()
    {
        sheetDatas.Clear();
        _dataDict.Clear();
        EditorUtility.SetDirty(this);
    }
#endif

    private IEnumerator CoLoadDataFromSpreadSheet()
    {
        foreach (var sheet in sheets)
        {
            using var www = UnityWebRequest.Get(GetSheetURL(sheet.sheetLink, sheet.workSheetGid));
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ParseCsv(sheet.sheetKey, www.downloadHandler.text);

                var loadedSheet = sheetDatas.Find(s => s.sheetKey == sheet.sheetKey);
                int rowCount = loadedSheet != null ? loadedSheet.rows.Count : 0;

                Debug.Log($"[GoogleSheetLoader] '{sheet.sheetKey}' 로드 완료 (행 {rowCount}개)");

#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
            else
            {
                Debug.LogError($"[GoogleSheetLoader] '{sheet.sheetKey}' 로드 실패: {www.error}");
            }
        }
    }

    public override string ToString()
    {
        string result = $"GoogleSheetLoader '{name}':\n";
        foreach (var sheet in sheetDatas)
        {
            result += $"- Sheet '{sheet.sheetKey}' ({sheet.rows.Count} rows)\n";
            // 너무 길어지면 한두 개의 RowData만 출력하도록 조절 가능
            for (int i = 0; i < Mathf.Min(sheet.rows.Count, 3); i++)
            {
                result += $"  Row {i}: {sheet.rows[i]}\n"; // RowData.ToString() 호출
            }
            if (sheet.rows.Count > 3)
                result += $"  ...and {sheet.rows.Count - 3} more rows\n";
        }
        return result;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GoogleSheetLoader))]
public class GoogleSheetDataSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var so = (GoogleSheetLoader)target;

        GUILayout.Space(10);
        if (GUILayout.Button("모든 시트 불러오기"))
            so.LoadDataFromSpreadSheet_Editor();

        GUILayout.Space(5);
        if (GUILayout.Button("List → Dict 동기화"))
            so.SyncListToDict();

        if (GUILayout.Button("Dict → List 동기화"))
            so.SyncDictToList();

        GUILayout.Space(5);
        if (GUILayout.Button("데이터 초기화"))
        {
            if (EditorUtility.DisplayDialog("데이터 초기화", "모든 시트 데이터를 삭제할까요?", "네", "아니요"))
                so.ClearData_Editor();
        }
    }
}
#endif
