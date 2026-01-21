using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

namespace Monster.AI.BehaviorTree.Nodes
{
    public enum NodeState
    {
        Success,
        Failure,
        Running
    }

    // BT Node Base Class
    // [CreateAssetMenu(fileName = "New BT Node", menuName = "Behavior Tree/Node")]
    public abstract class BTNode : ScriptableObject
    {
        [HideInInspector] public NodeState state;                   // 노드 상태
        
        #region GraphView Setting

        public new string name;                   // 노드 이름
        
        [HideInInspector] public string guid;                       // 노드 GUI
        [HideInInspector] public Vector2 position;                  // 노드 좌표
        [HideInInspector] public BTNode input;                      // 입력 (부모 노드)
        [HideInInspector] public List<BTNode> outputs;              // 출력

        #endregion

        protected BTNode(NodeState state = NodeState.Success, string name = "Root", string guid = "", Vector2? position = null, BTNode input = null, List<BTNode> outputs = null)
        {
            this.state = state;
            this.name = name;
            this.guid = string.IsNullOrEmpty(guid) ? System.Guid.NewGuid().ToString() : guid;
            this.position = position ?? Vector2.zero;
            this.input = input;
            this.outputs = outputs ?? new List<BTNode>();
        }
        
        // 노드 활동 정의
        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Reset() { }
        
        public abstract NodeState Evaluate(NodeContext context, HashSet<BTNode> visited);
        public virtual void OnValidateNode() { }
        
        // 순환 참조 발생을 점검하고 결과 반환하는 함수
        protected bool CheckCycle(HashSet<BTNode> visited)
        {
            if (visited.Contains(this))
            {
                Debug.LogError($"[BehaviorTree] 순환 참조 감지: {name}");
                return true;
            }
            visited.Add(this);
            return false;
        }
    }
}
