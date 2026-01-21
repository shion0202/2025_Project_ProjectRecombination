using System.Collections.Generic;
using Monster.AI.BehaviorTree;
using Monster.AI.BehaviorTree.Nodes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class BTEditorWindow : EditorWindow
{
    private BTGraphView _graphView;
    private BehaviorTree _currentTree;
    private BTNode _selectedNode;
    private BTNodeSettingsPopupWindow _settingsPopup;
    private IEnumerable<object> _lastSelection;

    private bool autoSaveEnabled = true; // 자동 저장 여부

    [MenuItem("Window/AI/Behavior Tree Editor")]
    public static void Open()
    {
        GetWindow<BTEditorWindow>("Behavior Tree");
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.update += OnEditorUpdate;
        Refresh();
    }
    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnSelectionChanged()
    {
        Refresh();
    }

    private void OnEditorUpdate()
    {
        if (_graphView != null)
        {
            var currentSelection = _graphView.selection;
            // selection이 변경된 경우에만 처리
            if (_lastSelection == null || !AreSelectionsEqual(currentSelection, _lastSelection))
            {
                _lastSelection = new List<object>(currentSelection);
                OnNodeSelectionChanged(currentSelection);
            }
        }
    }

    // selection 비교 함수
    private bool AreSelectionsEqual(IEnumerable<object> a, IEnumerable<object> b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        var listA = new List<object>(a);
        var listB = new List<object>(b);
        if (listA.Count != listB.Count) return false;
        for (int i = 0; i < listA.Count; i++)
        {
            if (!Equals(listA[i], listB[i])) return false;
        }
        return true;
    }

    private void Refresh()
    {
        rootVisualElement.Clear();

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.flexGrow = 1;

        var topBar = new VisualElement();
        topBar.style.height = 40;
        topBar.style.flexShrink = 0;
        topBar.style.flexDirection = FlexDirection.Row;

        var autoSaveToggle = new Toggle("자동 저장") { value = autoSaveEnabled };
        autoSaveToggle.RegisterValueChangedCallback(evt =>
        {
            autoSaveEnabled = evt.newValue;
        });
        topBar.Add(autoSaveToggle);

        var saveButton = new Button(SaveTreeNodes) { text = "Save Tree" };
        topBar.Add(saveButton);

        var deleteAllOrphanNodesButton = new Button(DeleteAllOrphanNodes) { text = "고아 일괄 삭제" };
        deleteAllOrphanNodesButton.SetEnabled(true);
        topBar.Add(deleteAllOrphanNodesButton);

        container.Add(topBar);

        if (Selection.activeObject is BehaviorTree selectedTree)
        {
            _currentTree = selectedTree;
            _graphView = new BTGraphView(_currentTree);
            _graphView.style.flexGrow = 1;
            // 노드 선택 이벤트 등록
            _graphView.OnNodeSelectionChanged += OnNodeSelectionChanged;
            container.Add(_graphView);
            // 트리 갱신 시 모든 노드가 잘 보이도록 자동 프레이밍
            _graphView.FrameAllNodes();
        }
        else
        {
            var label = new Label("BehaviorTree ScriptableObject를 선택하세요.")
            {
                style = { unityTextAlign = TextAnchor.MiddleCenter, fontSize = 14, marginTop = 20 }
            };
            container.Add(label);
        }
        rootVisualElement.Add(container);

        // 항상 설정 팝업을 오른쪽에 표시
        if (_settingsPopup == null)
        {
            _settingsPopup = new BTNodeSettingsPopupWindow();
            _settingsPopup.style.position = Position.Absolute;
            _settingsPopup.style.right = 10;
            _settingsPopup.style.top = 50;
        }
        rootVisualElement.Add(_settingsPopup);
        // 노드 선택 상태에 따라 정보 갱신
        _settingsPopup.SetTargetNode(_selectedNode, _graphView);
    }

    // 노드 선택 시 설정 팝업만 갱신 (Refresh 호출 제거)
    private void OnNodeSelectionChanged(IEnumerable<object> selection)
    {
        _selectedNode = null;
        foreach (var item in selection)
        {
            if (item is BTNodeView nodeView)
            {
                _selectedNode = nodeView.Node;
                break;
            }
        }
        // 설정 팝업만 갱신
        _settingsPopup?.SetTargetNode(_selectedNode, _graphView);
        // 버튼 상태 갱신 필요시 별도 처리
    }

    private void AddNode(string nodeType)
    {
        if (_currentTree == null) return;
        BTNode newNode = null;
        switch (nodeType)
        {
            case "BTSequence":
                newNode = BTEditorUtils.CreateNode<BTSequence>(_currentTree);
                break;
            case "BTSelector":
                newNode = BTEditorUtils.CreateNode<BTSelector>(_currentTree);
                break;
            case "BTAction":
                newNode = BTEditorUtils.CreateNode<BTAction>(_currentTree);
                break;
            case "BTCondition":
                newNode = BTEditorUtils.CreateNode<BTCondition>(_currentTree);
                break;
            case "BTDecorator":
                newNode = BTEditorUtils.CreateNode<BTDecorator>(_currentTree);
                break;
        }
        if (newNode != null)
        {
            Debug.Log($"노드 추가: {newNode.name}");
            Refresh(); // 그래프 갱신
        }
    }

    // 트리의 모든 노드 정보를 저장하는 함수
    private void SaveTreeNodes()
    {
        if (_currentTree == null || _currentTree.rootNode == null) return;
        var visited = new HashSet<BTNode>();
        SaveNodeRecursive(_currentTree.rootNode, visited);
        EditorUtility.SetDirty(_currentTree);
        AssetDatabase.SaveAssets();
        Debug.Log("[BTEditorWindow] 트리 노드 정보가 저장되었습니다.");
    }

    private void SaveNodeRecursive(BTNode node, HashSet<BTNode> visited)
    {
        if (node == null || !visited.Add(node)) return;
        EditorUtility.SetDirty(node);
        if (node is BTComposite composite && composite.children != null)
        {
            foreach (var child in composite.children)
                SaveNodeRecursive(child, visited);
        }
        else if (node is BTDecorator decorator && decorator.child != null)
        {
            SaveNodeRecursive(decorator.child, visited);
        }
    }

    private void DeleteSelectedNode()
    {
        if (_selectedNode == null || _currentTree == null) return;
        // 부모에서 참조 제거
        if (_selectedNode.input is BTComposite parentComposite)
        {
            parentComposite.children.Remove(_selectedNode);
        }
        else if (_selectedNode.input is BTDecorator parentDecorator)
        {
            if (parentDecorator.child == _selectedNode)
                parentDecorator.child = null;
        }
        // 트리의 루트 노드라면 rootNode도 null로
        if (_currentTree.rootNode == _selectedNode)
        {
            _currentTree.rootNode = null;
        }
        // 에셋에서 제거
        AssetDatabase.RemoveObjectFromAsset(_selectedNode);
        DestroyImmediate(_selectedNode, true);
        AssetDatabase.SaveAssets();
        _selectedNode = null;
        Refresh();
    }

    private bool IsOrphanNode(BTNode node)
    {
        if (node == null) return false;
        if (node.input != null) return false;
        if (node is BTComposite composite && composite.children != null && composite.children.Count > 0)
            return false;
        if (node is BTDecorator decorator && decorator.child != null)
            return false;
        return true;
    }

    private void DeleteOrphanNode()
    {
        if (_selectedNode == null || !IsOrphanNode(_selectedNode)) return;
        AssetDatabase.RemoveObjectFromAsset(_selectedNode);
        DestroyImmediate(_selectedNode, true);
        AssetDatabase.SaveAssets();
        _selectedNode = null;
        Refresh();
    }

    private void DeleteAllOrphanNodes()
    {
        if (_currentTree == null) return;
        string treePath = AssetDatabase.GetAssetPath(_currentTree);
        var assets = AssetDatabase.LoadAllAssetsAtPath(treePath);
        int deleteCount = 0;
        foreach (var asset in assets)
        {
            if (asset is BTNode node && IsOrphanNode(node))
            {
                AssetDatabase.RemoveObjectFromAsset(node);
                DestroyImmediate(node, true);
                deleteCount++;
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"삭제된 고아 노드 수: {deleteCount}");
        Refresh();
    }
}