using System;
using System.Collections.Generic;

namespace Script.CustomCollections
{
    // 델리게이트를 활용한 우선순위 큐
    public class PriorityQueue<TElement>
    {
        #region Parameter(파리미터)

        private List<(TElement item, int priority)> _heap;  // 완전 이진트리 (힙)
        private Comparison<(TElement item, int priority)> _comparer; // 델리게이트 추가

        public int Count => _heap.Count;

        #endregion

        #region Constructor(생성자)

        public PriorityQueue(Comparison<(TElement item, int priority)> customComparer)
        {
            if (customComparer == null)
            {
                throw new ArgumentNullException(nameof(customComparer), "Comparer cannot be null.");
            }
            _heap = new List<(TElement item, int priority)>();  // 힙 생성
            _comparer = customComparer;                         // 델리게이트 저장
        }
        
        // Min-Heap 생성자
        public static PriorityQueue<TElement> CreateMinPriorityQueue()
        {
            return new PriorityQueue<TElement>((a, b) => a.priority.CompareTo(b.priority));
        }

        // Max-Heap 생성자
        public static PriorityQueue<TElement> CreateMaxPriorityQueue()
        {
            return new PriorityQueue<TElement>((a, b) => b.priority.CompareTo(a.priority));
        }

        #endregion

        #region Queue 기본 함수

        // 요소 삽입 (Enqueue)
        public void Enqueue(TElement item, int priority)
        {
            _heap.Add((item, priority));    // 리스트 마지막에 우선순위 값 추가
            HeapifyUp(_heap.Count - 1);     // 힙 규칙에 맞게 트리 조정
        }

        // 최상위 요소 추출 (Dequeue)
        public TElement Dequeue()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("PriorityQueue is empty.");
            }

            // 루트(가장 높은 우선순위) 요소 가져오기
            var topItem = _heap[0].item;

            // 마지막 요소를 루트로 이동하고 힙 크기 줄이기
            int lastIndex = _heap.Count - 1;
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);

            // 새 루트를 올바른 위치로 정렬 (HeapifyDown)
            if (_heap.Count > 0)
            {
                HeapifyDown(0);
            }

            return topItem;
        }

        public (TElement item, int priority) DequeueRow()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("PriorityQueue is empty.");
            }
            
            TElement item = _heap[0].item;
            int priority = _heap[0].priority;
            
            // 마지막 요소를 루트로 이동하고 힙 크기 줄이기
            int lastIndex = _heap.Count - 1;
            _heap[0] = _heap[lastIndex];
            _heap.RemoveAt(lastIndex);

            // 새 루트를 올바른 위치로 정렬 (HeapifyDown)
            if (_heap.Count > 0)
            {
                HeapifyDown(0);
            }
            
            return (item, priority);
        }

        // 최상위 요소 확인 (Peek)
        public TElement Peek()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("PriorityQueue is empty.");
            }
            return _heap[0].item;
        }

        // 큐 청소
        public void CleanUp()
        {
            // 힙 초기화
            _heap.Clear();
        }

        #endregion

        #region 힙 조정 함수

        // 힙 상향 조정 (새 요소 삽입 시)
        private void HeapifyUp(int index)
        {
            int parentIndex = (index - 1) / 2;
            while (index > 0 && _comparer(_heap[index], _heap[parentIndex]) < 0)
            {
                Swap(index, parentIndex);
                index = parentIndex;
                parentIndex = (index - 1) / 2;
            }
        }

        // 힙 하향 조정 (요소 추출 시)
        private void HeapifyDown(int index)
        {
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;
            int targetIndex = index;

            // 왼쪽 자식과 비교
            if (leftChild < _heap.Count && _comparer(_heap[leftChild], _heap[targetIndex]) < 0)
            {
                targetIndex = leftChild;
            }

            // 오른쪽 자식과 비교
            if (rightChild < _heap.Count && _comparer(_heap[rightChild], _heap[targetIndex]) < 0)
            {
                targetIndex = rightChild;
            }

            // 가장 작은 요소가 현재 인덱스가 아니면 스왑하고 재귀 호출
            if (targetIndex != index)
            {
                Swap(index, targetIndex);
                HeapifyDown(targetIndex);
            }
        }

        #endregion

        #region ETC

        // 두 요소 위치 스왑
        private void Swap(int i, int j)
        {
            (_heap[i], _heap[j]) = (_heap[j], _heap[i]);
        }

        public void Clear()
        {
            _heap.Clear();
        }

        #endregion
        
        public override string ToString()
        {
            string result = "PriorityQueue: [";
            for (int i = 0; i < _heap.Count; i++)
            {
                result += $"{i}: {_heap[i].item}\n";
            }
            return $"PriorityQueue with {_heap.Count} elements.\n{result}]";
        }
    }    
}