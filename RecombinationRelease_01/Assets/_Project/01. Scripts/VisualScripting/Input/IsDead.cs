using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // DamagablaObject만 지원하고 있으나, 구조상 Player나 Monster도 반영할 수 있으므로 필요하다면 추후 수정
    public class IsDead : ProcessBase
    {
        // 지정된 오브젝트가 사망(Current Hp <= 0)할 경우 알림
        [SerializeField] private List<DamagableObject> cach; // 사망 여부를 참조할 객체
        private int _count;

        // 인스턴스 객체로 부터 생성한 객체를 넘겨 받을 Getter/Setter
        public List<DamagableObject> Cach
        {
            get => cach;
            set => cach = value;
        }
    
        private void Start()
        {
            if (cach.Count <= 0) return;
        
            // 씬 시작 시 함수 등록
            foreach (var obj in cach)
            {
                if (obj is null) continue;

                obj.OnObjectDied -= Execute;
                obj.OnObjectDied += Execute;
                _count++;
            }
        }
    
        // 동적으로 생성된 오브젝트가 삭제되었을 때 실행할 기능을 등록
        public void AddObject(DamagableObject obj)
        {
            obj.OnObjectDied -= Execute;
            obj.OnObjectDied += Execute;
            cach.Add(obj);
            _count++;
        }
    
        // 캐싱된 객체가 파괴될 때 실행할 함수
        public override void Execute()
        {
            Debug.Log("IsDead called");
            if (IsOn) return;   // 한번도 함수가 호출되지 않았으면 실행

            _count--;
        
            if (_count <= 0)
            {
                IsOn = true;        // 함수 호출 여부를 수정
            }
        }
    }
}
