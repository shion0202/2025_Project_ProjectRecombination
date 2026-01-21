using System.Collections.Generic;
using System.Linq;
using Monster.AI.BehaviorTree;
using Monster.AI.BehaviorTree.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
// using UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class BTGraphView : GraphView
{
    private BehaviorTree _tree;
    private Dictionary<string, BTNodeView> _nodeViews = new();

    public BTGraphView(BehaviorTree tree)
    {
        _tree = tree;
        var styleSheet = Resources.Load<StyleSheet>("BTStyles");
        if (styleSheet != null)
            styleSheets.Add(styleSheet);

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        // EdgeConnector는 포트에 직접 할당해야 하므로 GraphView에는 추가하지 않음

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // Edge 연결/해제 이벤트 등록
        this.graphViewChanged += OnGraphViewChanged;

        if (_tree == null || _tree.rootNode == null) return;
        DrawTree();

        this.RegisterCallback<MouseDownEvent>(evt => {
            if (evt.button == 1 && evt.clickCount == 1) // 우클릭 단일 클릭
            {
                Vector2 mousePos = evt.mousePosition;
                // 팝업 위치를 실제 마우스 좌표로 지정
                ShowNodeCreatePopup(null, -1, mousePos);
                // Debug.Log($"GraphView 우클릭 팝업: {mousePos}");
                evt.StopPropagation();
            }
        });
    }

    // Edge 연결/해제 이벤트 핸들러
    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        // Edge 연결
        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                OnEdgeConnected(edge);
            }
        }
        // Edge 해제 (elementsToRemove에서 Edge만 처리)
        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    OnEdgeDisconnected(edge);
                }
            }
        }
        return change;
    }

    private void DrawTree()
    {
        // Debug.Log($"[BTGraphView] DrawTree 호출됨. 트리: {_tree}, 루트: {_tree?.rootNode}");
        // 기존 노드/엣지 완전 제거
        foreach (var view in _nodeViews.Values)
            RemoveElement(view);
        foreach (var edge in edges.ToList())
            RemoveElement(edge);
        _nodeViews.Clear();

        if (_tree == null) {
            // Debug.LogWarning("[BTGraphView] _tree가 null입니다.");
            return;
        }
        var allNodes = _tree.GetAllAssetNodes();
        // Debug.Log($"[BTGraphView] 트리 내 노드 개수: {allNodes.Count}");
        // 트리 에셋에 포함된 모든 노드 표시 (고아 노드 포함)
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                // Debug.Log($"[BTGraphView] 노드 추가: {node.name}, GUID: {node.guid}, position(contentViewContainer): {node.position}");
                // composite.children에서 null 제거 (자동 삭제 금지)
                // if (node is BTComposite composite && composite.children != null)
                // {
                //     composite.children.RemoveAll(child => child == null);
                // }
                // 노드의 position을 contentViewContainer 기준 좌표로 통일
                var view = new BTNodeView(node, _tree.rootNode, this);
                view.SetPosition(new Rect(node.position, new Vector2(200, 100)));
                // Debug.Log($"[BTGraphView] NodeView Rect: {new Rect(node.position, new Vector2(200, 100))}");
                _nodeViews[node.guid] = view;
                AddElement(view);
            }
            else
            {
                // Debug.LogWarning("[BTGraphView] null 노드 발견");
            }
        }
        // Debug.Log($"[BTGraphView] 최종 노드뷰 개수: {_nodeViews.Count}");
        DrawEdges();
        FrameAllNodes();
    }

    public void RedrawTree()
    {
        // 현재 선택된 노드 guid 저장
        string selectedGuid = null;
        if (selection != null)
        {
            foreach (var item in selection)
            {
                if (item is BTNodeView nodeView)
                {
                    selectedGuid = nodeView.Node.guid;
                    break;
                }
            }
        }
        DrawTree();
        // DrawTree 후 선택 복원
        if (!string.IsNullOrEmpty(selectedGuid) && _nodeViews.TryGetValue(selectedGuid, out var selectedView))
        {
            ClearSelection();
            AddToSelection(selectedView);
        }
    }

    public void FrameAllNodes()
    {
        if (_nodeViews.Count == 0) return;
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var view in _nodeViews.Values)
        {
            Vector2 pos = view.Node.position;
            float width = view.layout.width > 0 ? view.layout.width : 200f;
            float height = view.layout.height > 0 ? view.layout.height : 100f;
            minX = Mathf.Min(minX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxX = Mathf.Max(maxX, pos.x + width);
            maxY = Mathf.Max(maxY, pos.y + height);
        }
        var bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
        float viewWidth = this.layout.width > 0 ? this.layout.width : 800f;
        float viewHeight = this.layout.height > 0 ? this.layout.height : 600f;
        Vector2 center = bounds.center;
        Vector2 viewCenter = new Vector2(viewWidth / 2, viewHeight / 2);
        Vector3 targetPos = viewCenter - center;
        UpdateViewTransform(targetPos, Vector3.one);
    }

    private void Traverse(BTNode node, HashSet<BTNode> visited)
    {
        if (node == null || visited.Contains(node)) return;
        visited.Add(node);
        var view = new BTNodeView(node, _tree.rootNode, this);
        _nodeViews[node.guid] = view;
        AddElement(view);
        if (node is BTComposite composite && composite.children != null)
        {
            foreach (var child in composite.children)
                Traverse(child, visited);
        }
        else if (node is BTDecorator decorator && decorator.child != null)
        {
            Traverse(decorator.child, visited);
        }
    }

    private void DrawEdges()
    {
        foreach (var kvp in _nodeViews)
        {
            var node = kvp.Value.Node;
            var parentView = kvp.Value;
            if (node is BTComposite composite && composite.children != null)
            {
                for (int i = 0; i < composite.children.Count; i++)
                {
                    var child = composite.children[i];
                    if (child == null || !_nodeViews.ContainsKey(child.guid)) continue;

                    var childView = _nodeViews[child.guid];
                    if (parentView.OutputPorts.Count <= i || childView.InputPort == null) continue;

                    var edge = parentView.OutputPorts[i].ConnectTo(childView.InputPort);
                    AddElement(edge);
                }
            }
            else if (node is BTDecorator decorator && decorator.child != null)
            {
                if (!_nodeViews.ContainsKey(decorator.child.guid)) continue;

                var childView = _nodeViews[decorator.child.guid];
                if (parentView.OutputPorts.Count <= 0 || childView.InputPort == null) continue;

                var edge = parentView.OutputPorts[0].ConnectTo(childView.InputPort);
                AddElement(edge);
            }
        }
    }

    public void OnEdgeConnected(Edge edge)
    {
        if (edge == null || edge.input == null || edge.output == null) return;
        var parentView = edge.output.node as BTNodeView;
        var childView = edge.input.node as BTNodeView;
        if (parentView == null || childView == null) return;
        var parentNode = parentView.Node;
        var childNode = childView.Node;
        // 자기 자신 연결 금지
        if (parentNode == childNode)
        {
            // Debug.LogWarning("자기 자신을 연결할 수 없습니다.");
            RemoveElement(edge);
            return;
        }
        // 순환 참조 체크
        var visited = new HashSet<BTNode>();
        bool hasCycle = CheckCycle(parentNode, childNode, visited);
        if (hasCycle)
        {
            // Debug.LogWarning("순환 참조가 발생하므로 연결할 수 없습니다.");
            RemoveElement(edge);
            return;
        }
        // 실제 트리 구조에 반영
        if (parentNode is BTComposite composite)
        {
            // children이 null이면 초기화
            if (composite.children == null)
                composite.children = new List<BTNode>();
            // 이미 연결된 경우 중복 방지
            if (!composite.children.Contains(childNode))
            {
                composite.children.Add(childNode);
                childNode.input = parentNode;
            }
        }
        else if (parentNode is BTDecorator decorator)
        {
            // child가 null이거나 다르면 연결
            if (decorator.child != childNode)
            {
                if (decorator.child == null)
                {
                    decorator.child = childNode;
                    childNode.input = parentNode;
                }
                else
                {
                    // 기존 child가 있는데 다른 노드 연결 시 기존 연결 해제 후 새로 연결
                    decorator.child.input = null;
                    decorator.child = childNode;
                    childNode.input = parentNode;
                }
            }
        }
        // 포트 연결 정보 갱신
        // 부모의 Out 포트와 자식의 Input 포트 연결
        if (parentView.OutputPorts != null && parentView.OutputPorts.Count > 0 && childView.InputPort != null)
        {
            // 연결된 포트에 edge 추가
            foreach (var outPort in parentView.OutputPorts)
            {
                if (!outPort.connections.Contains(edge) && (edge.output == outPort || edge.input == childView.InputPort))
                {
                    outPort.Connect(edge);
                }
            }
            if (!childView.InputPort.connections.Contains(edge))
            {
                childView.InputPort.Connect(edge);
            }
        }
        EditorUtility.SetDirty(parentNode);
        EditorUtility.SetDirty(childNode);
        // 자동 저장이 켜져 있을 때만 저장
        var editorWindow = UnityEditor.EditorWindow.GetWindow<BTEditorWindow>();
        var autoSaveField = editorWindow.GetType().GetField("autoSaveEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool autoSave = autoSaveField != null && (bool)autoSaveField.GetValue(editorWindow);
        if (autoSave)
        {
            AssetDatabase.SaveAssets();
        }
        RedrawTree();
    }

    private void OnEdgeDisconnected(Edge edge)
    {
        if (edge == null || edge.input == null || edge.output == null) return;
        var parentView = edge.output.node as BTNodeView;
        var childView = edge.input.node as BTNodeView;
        if (parentView == null || childView == null) return;
        var parentNode = parentView.Node;
        var childNode = childView.Node;
        // 트리 구조에서 연결 해제
        if (parentNode is BTComposite composite)
        {
            int idx = composite.children.IndexOf(childNode);
            if (idx >= 0)
                composite.children[idx] = null;
            childNode.input = null;
        }
        else if (parentNode is BTDecorator decorator)
        {
            if (decorator.child == childNode)
            {
                decorator.child = null;
                childNode.input = null;
            }
        }
        // 연결된 포트 중 삭제된 간선과 연결된 포트만 null로 처리
        if (parentView.OutputPorts != null && parentView.OutputPorts.Count > 0)
        {
            // 부모 노드의 Out 포트 중 childView의 InputPort와 연결된 포트만 해제
            for (int i = 0; i < parentView.OutputPorts.Count; i++)
            {
                var outPort = parentView.OutputPorts[i];
                if (outPort != null)
                {
                    foreach (var e in outPort.connections.ToList())
                    {
                        if (e.input == childView.InputPort || e.output == outPort)
                        {
                            outPort.Disconnect(e);
                        }
                    }
                }
            }
        }
        if (childView.InputPort != null)
        {
            foreach (var e in childView.InputPort.connections.ToList())
            {
                if (e.output == parentView.OutputPorts.Find(p => p.connections.Contains(e)) || e.input == childView.InputPort)
                {
                    childView.InputPort.Disconnect(e);
                }
            }
        }
        EditorUtility.SetDirty(parentNode);
        EditorUtility.SetDirty(childNode);
        // 자동 저장이 켜져 있을 때만 저장
        var editorWindow = UnityEditor.EditorWindow.GetWindow<BTEditorWindow>();
        var autoSaveField = editorWindow.GetType().GetField("autoSaveEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool autoSave = autoSaveField != null && (bool)autoSaveField.GetValue(editorWindow);
        if (autoSave)
        {
            AssetDatabase.SaveAssets();
        }
        RedrawTree();
    }

    private bool CheckCycle(BTNode parent, BTNode child, HashSet<BTNode> visited)
    {
        if (parent == null || child == null) return false;
        if (parent == child) return true;
        if (!visited.Add(child)) return true;
        var children = BTEditorUtils.GetChildren(child);
        foreach (var c in children)
        {
            if (CheckCycle(parent, c, visited)) return true;
        }
        return false;
    }

    // 노드 생성 팝업을 띄우는 메서드
    public void ShowNodeCreatePopup(BTNodeView parentNodeView, int outPortIndex, Vector2 position)
    {
        var wnd = BTNodeCreatePopupWindow.ShowPopup(parentNodeView, outPortIndex, position, _tree, this);
    }

    // 노드 선택 이벤트를 외부에서 등록할 수 있도록 델리게이트 정의
    public System.Action<IEnumerable<object>> OnNodeSelectionChanged;

    public BTNodeView GetSelectedNodeView()
    {
        if (selection == null) return null;
        foreach (var item in selection)
        {
            if (item is BTNodeView nodeView)
                return nodeView;
        }
        return null;
    }

    public void UpdateNodeSelection()
    {
        OnNodeSelectionChanged?.Invoke(selection);
    }
}
