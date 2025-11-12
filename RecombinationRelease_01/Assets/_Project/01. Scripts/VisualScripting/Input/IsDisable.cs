using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class IsDisable : ProcessBase
    {
        [SerializeField] private List<GlobalGameObject> cach;
        private int _count;
        
        public List<GlobalGameObject> Cach
        {
            get => cach;
            set => cach = value;
        }

        private void Start()
        {
            if (cach?.Count <= 0) return;

            foreach (var item in cach)
            {
                if (item is null) continue;
                item.OnObjectDisabled += Execute;
                _count++;
            }
        }
        
        public void AddObject(GlobalGameObject obj)
        {
            obj.OnObjectDisabled += Execute;
            cach.Add(obj);
            _count++;
        }
        
        // 캐릭터(또는 몬스터)가 사망했을 경우 알림
        public override void Execute()
        {
            Debug.Log("IsDisable called");
            if (IsOn) return;   // 한번도 함수가 호출되지 않았으면 실행
            _count--;
            if (_count > 0) return;

            cach.Clear();
            IsOn = true;        // 함수 호출 여부를 수정
        }
    }
}