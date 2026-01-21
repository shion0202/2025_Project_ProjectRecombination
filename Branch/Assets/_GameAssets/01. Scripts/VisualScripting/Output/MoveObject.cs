using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace _Project.Scripts.VisualScripting
{
    [Serializable]
    public struct MoveData
    {
        public Transform transform;
        public float time;
    }

    public class MoveObject : ProcessBase
    {
        [SerializeField] private GameObject objectToMove;
        [SerializeField] private List<MoveData> moveTargetPos;
        [SerializeField] private bool repeat;
        [SerializeField] private ProcessBase nextExecute;
        
        private Action _onComplete;

        private void Start()
        {
            if (nextExecute != null)
                _onComplete = nextExecute.Execute;
        }
        
        public override void Execute()
        {
            if (IsOn) return;

            if (CheckNull()) return;

            RunningCoroutine = StartCoroutine(C_Move());
        }

        private bool CheckNull()
        {
            return (objectToMove is null) || (moveTargetPos is null);
        }

        private IEnumerator C_Move()
        {
            IsOn = true;
            // 오브젝트를 주어진 시간 동안 움직인다.
            // 이때 시간이 모두 지났을 때 목표했던 지점에 오브젝트가 도달해야 한다. (해결)
            do
            {
                for (int i = 0; i < moveTargetPos.Count; i++)
                {
                    // 지정된 대상을 타겟으로 이동한다.
                    Vector3 target = moveTargetPos[i].transform.position;
                    // 이동에 걸리는 시간을 받아 온다.
                    float timer = moveTargetPos[i].time;
                    // 현재 오브젝트의 위치를 받아온다.
                    Vector3 startPosition = objectToMove.transform.position;
                    float elapsedTime = 0f;
        
                    while (elapsedTime < timer)
                    {
                        elapsedTime += Time.deltaTime;
                        float t = elapsedTime / timer;
                        // 현재 오브젝트의 위치를 보간하여 이동시킨다.
                        objectToMove.transform.position = Vector3.Lerp(startPosition, target, t);
                        yield return null; // 다음 프레임까지 대기
                    }
        
                    // 마지막 위치를 정확히 설정
                    objectToMove.transform.position = target;
                    yield return null; // 다음 프레임까지 대기
                    _onComplete?.Invoke();
                    yield return new WaitForSeconds(1f);
                }
            } while (repeat);
            IsOn = false;
        }
    }
}
