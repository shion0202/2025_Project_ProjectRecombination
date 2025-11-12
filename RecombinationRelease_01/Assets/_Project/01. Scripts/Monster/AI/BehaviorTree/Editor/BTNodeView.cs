using System.Collections.Generic;
using System.Linq;
using Monster.AI.BehaviorTree.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class BTNodeView : Node
{
    public BTNode Node;
    public Port InputPort;
    public List<Port> OutputPorts = new List<Port>();
    private BTGraphView _graphView;
    // private int _lastDragPortIndex = -1;

    public BTNodeView(BTNode node, BTNode rootNode, BTGraphView graphView)
    {
        Node = node;
        _graphView = graphView;
        title = node.name ?? node.GetType().Name;
        viewDataKey = node.guid;
        style.left = node.position.x;
        style.top = node.position.y;

        // 노드가 그래프 뷰에 표시될 때 GUID 등록
        // _graphView?.RegisterNodeDisplay(node.guid);

        if (node != rootNode)
        {
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(bool));
            InputPort.portName = "In";
            InputPort.AddManipulator(new EdgeConnector<Edge>(new BTEdgeConnectorListener()));
            inputContainer.Add(InputPort);
        }

        OutputPorts.Clear();
        outputContainer.Clear();
        if (node is BTComposite composite && composite.children != null)
        {
            for (int i = 0; i < composite.children.Count; i++)
            {
                var outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                outPort.portName = $"Out {i + 1}";
                var edgeConnector = new EdgeConnector<Edge>(new EdgeConnectorListenerWithPopup(this, i));
                outPort.AddManipulator(edgeConnector);
                outputContainer.Add(outPort);
                OutputPorts.Add(outPort);
            }
        }
        else if (node is BTDecorator decorator)
        {
            var outPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outPort.portName = "Out";
            // 팝업 활성화 리스너로 변경
            var edgeConnector = new EdgeConnector<Edge>(new EdgeConnectorListenerWithPopup(this, 0));
            outPort.AddManipulator(edgeConnector);
            outputContainer.Add(outPort);
            OutputPorts.Add(outPort);
        }

        switch (node.state)
        {
            case NodeState.Success:
                AddToClassList("bt-node-success");
                break;
            case NodeState.Failure:
                AddToClassList("bt-node-failure");
                break;
            case NodeState.Running:
                AddToClassList("bt-node-running");
                break;
        }

        var label = node switch
        {
            BTSequence => "sequence",
            BTSelector => "selector",
            BTAction => "action",
            BTCondition => "condition",
            BTDecorator => "decorator",
            _ => ""
        };
        
        AddToClassList("node");
        
        if (!string.IsNullOrEmpty(label))
            AddToClassList(label);

        RefreshExpandedState();
        RefreshPorts();

        // 노드 클릭 시 선택 이벤트 전달 (삭제)
        // RegisterCallback<MouseDownEvent>(evt =>
        // {
        //     _graphView?.UpdateNodeSelection();
        // });

        // 노드가 드래그로 이동 가능하도록 설정
        this.capabilities |= Capabilities.Movable;

        // 노드 이동 종료 시 저장 처리
        RegisterCallback<MouseUpEvent>(evt =>
        {
            var editorWindow = UnityEditor.EditorWindow.GetWindow<BTEditorWindow>();
            var autoSaveField = editorWindow.GetType().GetField("autoSaveEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool autoSave = autoSaveField != null && (bool)autoSaveField.GetValue(editorWindow);
            UnityEditor.EditorUtility.SetDirty(Node);
            if (autoSave)
            {
                UnityEditor.AssetDatabase.SaveAssets();
            }
        });

        // 우클릭 컨텍스트 메뉴 등록
        this.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 1 && evt.clickCount == 1)
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("노드 삭제"), false, () => {
                    // BTEditorWindow에서 노드 삭제 함수 호출
                    var editorWindow = UnityEditor.EditorWindow.GetWindow<BTEditorWindow>();
                    editorWindow.GetType().GetMethod("DeleteSelectedNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(editorWindow, null);
                });
                menu.ShowAsContext();
                evt.StopPropagation();
            }
        });
    }

    private void OnNodeClicked(MouseDownEvent evt)
    {
        // 노드 클릭 시 에디터 윈도우의 _selectedNode를 갱신하고 Refresh() 호출
        if (_graphView != null && Node != null)
        {
            var editorWindow = UnityEditor.EditorWindow.GetWindow<BTEditorWindow>();
            if (editorWindow != null)
            {
                editorWindow.GetType().GetField("_selectedNode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(editorWindow, Node);
                editorWindow.Repaint();
                editorWindow.GetType().GetMethod("Refresh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(editorWindow, null);
            }
        }
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);
        Node.position = newPos.position;
        // 저장 로직 제거
        // 노드 이동 후 선택 이벤트 전달 (설정 팝업 갱신)
        _graphView?.UpdateNodeSelection();
    }

    public BTGraphView GraphView => _graphView;
}

// EdgeConnectorListener 확장: 빈 공간에 드롭 시 노드 생성 팝업 호출
public class EdgeConnectorListenerWithPopup : IEdgeConnectorListener
{
    private BTNodeView _nodeView;
    private int _portIndex;
    public EdgeConnectorListenerWithPopup(BTNodeView nodeView, int portIndex)
    {
        _nodeView = nodeView;
        _portIndex = portIndex;
    }
    public void OnDrop(GraphView graphView, Edge edge)
    {
        graphView.AddElement(edge);
        // 간선이 포트에 연결된 경우 트리 구조도 갱신
        if (graphView is BTGraphView btGraphView)
        {
            btGraphView.OnEdgeConnected(edge);
        }
    }
    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
        var graphView = edge.GetFirstAncestorOfType<BTGraphView>();
        var pointerPos = graphView.contentViewContainer.WorldToLocal(position);
        // Debug.Log($"OnDropOutsidePort called. Screen Position: {position}, contentViewContainer 기준: {pointerPos}");
        if (graphView != null)
        {
            BTNodeView targetNodeView = null;
            float minDist = float.MaxValue;
            // 마우스 포인터 좌표를 contentViewContainer 기준으로 통일
            foreach (var nodeView in graphView.contentViewContainer.Query<BTNodeView>().ToList())
            {
                float width = nodeView.layout.width > 0 ? nodeView.layout.width : 200f;
                float height = nodeView.layout.height > 0 ? nodeView.layout.height : 100f;
                var nodeRect = new Rect(nodeView.Node.position, new Vector2(width, height));
                // Debug.Log($"Node: {nodeView.Node.name}, GUID: {nodeView.Node.guid}, position(contentViewContainer): {nodeView.Node.position}, nodeRect: {nodeRect}, layout: {nodeView.layout}");
                if (nodeRect.Contains(pointerPos))
                {
                    float dist = Vector2.Distance(nodeRect.center, pointerPos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        targetNodeView = nodeView;
                    }
                }
            }
            if (targetNodeView != null)
            {
                // Debug.Log($"Target node found: {targetNodeView.Node.name} (GUID: {targetNodeView.Node.guid})");
                // // 이미 연결된 부모가 있으면 중복 연결 방지
                // if (targetNodeView.Node.input == _nodeView.Node)
                // {
                //     Debug.Log("Node already connected as child. No action taken.");
                //     graphView.RemoveElement(edge);
                //     return;
                // }
                if (_nodeView.Node is BTComposite composite)
                {
                    if (_portIndex < 0) _portIndex = 0;
                    if (_portIndex < composite.children.Count)
                    {
                        if (composite.children[_portIndex] != targetNodeView.Node)
                            composite.children[_portIndex] = targetNodeView.Node;
                    }
                    else
                    {
                        composite.children.Add(targetNodeView.Node);
                    }
                    targetNodeView.Node.input = _nodeView.Node;
                    // Debug.Log($"Added child to composite: {_nodeView.Node.name} -> {targetNodeView.Node.name}");
                }
                else if (_nodeView.Node is BTDecorator decorator)
                {
                    if (decorator.child != targetNodeView.Node)
                    {
                        decorator.child = targetNodeView.Node;
                        targetNodeView.Node.input = _nodeView.Node;
                        // Debug.Log($"Set decorator child: {_nodeView.Node.name} -> {targetNodeView.Node.name}");
                    }
                }
                UnityEditor.EditorUtility.SetDirty(_nodeView.Node);
                UnityEditor.EditorUtility.SetDirty(targetNodeView.Node);
                UnityEditor.AssetDatabase.SaveAssets();
                graphView.RedrawTree();
                // Debug.Log("RedrawTree called.");
                return;
            }
            else
            {
                // Debug.LogWarning("No target node found at drop position.");
                // 빈 공간에 드롭 시 노드 생성 팝업 표시
                graphView.ShowNodeCreatePopup(_nodeView, _portIndex, position);
                // Debug.Log("ShowNodeCreatePopup called.");
            }
            graphView.RemoveElement(edge);
        }
        else
        {
            Debug.LogError("GraphView is null in OnDropOutsidePort.");
        }
    }
}