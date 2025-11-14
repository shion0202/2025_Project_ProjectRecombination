using System;
using System.Linq;
using UnityEngine;

namespace Monster
{
    [CreateAssetMenu(fileName = "StateData", menuName = "Monster/State Data")]
    public class MonsterStateData : ScriptableObject
    {
        [Serializable]
        public class StateDefinition
        {
            public string stateName;
            [Range(0, 31)]
            public int bitIndex;
        }
        
        public StateDefinition[] states;
        
        // 기본 상태 정의 (수정 불가)
        private static readonly StateDefinition[] DefaultStates = {
            new() { stateName = "None", bitIndex = 0 },
            new() { stateName = "Idle", bitIndex = 1 },
            new() { stateName = "Chase", bitIndex = 2 },
            new() { stateName = "Attack", bitIndex = 3 },
            new() { stateName = "Hit", bitIndex = 4 },
            new() { stateName = "Death", bitIndex = 5 },
            new() { stateName = "Patrol", bitIndex = 6 },
            new() { stateName = "Wander", bitIndex = 7 },
            new() { stateName = "Spawn", bitIndex = 8 }
        };
        
        private void OnValidate()
        {
            // 기본 상태가 states에 없으면 추가, 기존 기본 상태는 수정 불가
            states ??= Array.Empty<StateDefinition>();

            // 사용자 정의 상태 추출 (기본 상태와 이름이 중복되지 않은 것만)
            var userStates = states
                .Where(s => DefaultStates.All(d => d.stateName != s.stateName))
                .Select((s, idx) => new StateDefinition {
                    stateName = s.stateName,
                    bitIndex = DefaultStates.Length + idx // 9부터 시작
                })
                .ToArray();

            // states 배열 재구성: 기본 상태 + 사용자 정의 상태
            states = new StateDefinition[DefaultStates.Length + userStates.Length];
            Array.Copy(DefaultStates, states, DefaultStates.Length);
            Array.Copy(userStates, 0, states, DefaultStates.Length, userStates.Length);
        }
    }
}