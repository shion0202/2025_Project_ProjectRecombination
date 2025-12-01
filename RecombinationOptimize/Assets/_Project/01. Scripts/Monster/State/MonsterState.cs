using System;
using System.Collections.Generic;
using UnityEngine;

namespace Monster
{
    public static class DynamicState
    {
        private static Dictionary<string, int> _stateNameToMask = new Dictionary<string, int>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize()
        {
            _stateNameToMask.Clear();
            MonsterStateData data = Resources.Load<MonsterStateData>("StateData");

            if (data == null)
            {
                Debug.LogError("MonsterStateData asset not found in Resources folder.");
                return;
            }

            foreach (var def in data.states)
            {
                // 비트 인덱스를 비트 마스크 값으로 변환하여 딕셔너리에 저장
                if (!_stateNameToMask.ContainsKey(def.stateName))
                {
                    int bitMask = 1 << def.bitIndex;
                    _stateNameToMask.Add(def.stateName, bitMask);
                }
            }
            
            // Debug.Log("DynamicState Init Complete");
        }
        
        // 상태 이름으로 비트 마스크 값을 가져오는 메서드
        public static int GetStateMask(string stateName)
        {
            if (_stateNameToMask.TryGetValue(stateName, out int mask))
            {
                return mask;
            }
            Debug.LogWarning($"State name '{stateName}' not found.");
            return 0;
        }
        
        // 비트 마스크 값으로 상태 이름을 가져오는 메서드
        public static string GetStateName(int mask)
        {
            foreach (var kvp in _stateNameToMask)
            {
                if (kvp.Value == mask)
                {
                    return kvp.Key;
                }
            }

            return null;
        }
        
        // 특정 비트 마스크를 사용할 수 있는지 확인하는 메서드
        public static bool IsValidMask(string stateName)
        {
            return _stateNameToMask.ContainsKey(stateName);
        }
    }

    [Serializable]
    public class MonsterState
    {
        public int Value { get; set; }

        public void AddState(string stateName)
        {
            int mask = DynamicState.GetStateMask(stateName);
            if (mask != 0) Value |= mask;
        }
        
        public void AddState(int mask)
        {
            if (mask != 0) Value |= mask;
        }

        public void RemoveState(string stateName)
        {
            int mask = DynamicState.GetStateMask(stateName);
            if (mask != 0) Value &= ~mask;
        }

        public void RemoveState(int mask)
        {
            if (mask != 0) Value &= ~mask;
        }

        public void SetState(int mask)
        {
            Value = 0;
            if (mask != 0) Value |= mask;
        }

        public void SetState(string stateName)
        {
            Value = 0;
            int mask = DynamicState.GetStateMask(stateName);
            if (mask != 0) Value |= mask;
        }

        public bool HasState(string stateName)
        {
            int mask = DynamicState.GetStateMask(stateName);
            if (mask != 0) return (Value & mask) == mask;
            return false;
        }
        
        public bool HasState(int mask)
        {
            if (mask != 0) return (Value & mask) == mask;
            return false;
        }

        public string GetStates()
        {
            string result = "";
            for (int i = 0; i < 32; i++)
            {
                if (Value == 0) break;
                int mask = 1 << i;
                if ((Value & mask) != mask) continue;

                string stateName = DynamicState.GetStateName(mask);
                if (stateName != null)
                    result += stateName + " ";
            }
            
            result = result.Trim();
            return result;
        }
        
        public void ClearStates()
        {
            Value = 0;
        }

        public override string ToString()
        {
            // 비트 마스크 값을 비트 단위로 나눠서 상태 이름을 반환
            string result = "";
            for (int i = 0; i < 32; i++)
            {
                int mask = 1 << i;
                if ((Value & mask) == mask)
                {
                    string stateName = DynamicState.GetStateName(mask);
                    if (stateName != null)
                        // stateName.Add(stateName);
                        result += stateName + " ";
                }
            }

            return result;
        }
    }
}