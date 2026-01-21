using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Monster.AI.BehaviorTree;
using Monster.AI.BehaviorTree.Nodes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public class BTNodeCreatePopupWindow : EditorWindow
{
    private BTNodeView _parentNodeView;
    private int _outPortIndex;
    private BehaviorTree _tree;
    private BTGraphView _graphView;
    private TextField _nameField;
    private PopupField<string> _typeField;
    private List<Type> _nodeTypes;
    private Vector2 _popupPosition;

    public static BTNodeCreatePopupWindow ShowPopup(BTNodeView parentNodeView, int outPortIndex, Vector2 position, BehaviorTree tree, BTGraphView graphView)
    {
        var wndNew = ScriptableObject.CreateInstance<BTNodeCreatePopupWindow>();
        wndNew._parentNodeView = parentNodeView;
        wndNew._outPortIndex = outPortIndex;
        // 그래프 좌표계로 변환
        Vector2 graphPos = position;
        if (graphView != null)
        {
            // position은 화면 좌표이므로, 그래프 뷰의 좌표계로 변환 필요
            graphPos = graphView.contentViewContainer.WorldToLocal(position);
        }
        wndNew._popupPosition = graphPos;
        wndNew._tree = tree;
        wndNew._graphView = graphView;
        wndNew.titleContent = new GUIContent("노드 생성");
        Vector2 screenPos = GUIUtility.GUIToScreenPoint(position);
        wndNew.position = new Rect(screenPos.x, screenPos.y, 320, 160);
        wndNew.CreateUI(); // UI 생성
        wndNew.ShowModalUtility();
        return wndNew;
    }

    private void CreateUI()
    {
        rootVisualElement.Clear();
        _nodeTypes = GetAllNodeTypes();
        var typeNames = _nodeTypes.Select(t => t.Name).ToList();
        _typeField = new PopupField<string>(typeNames, 0);
        rootVisualElement.Add(new Label("노드 타입 선택:"));
        rootVisualElement.Add(_typeField);
        _nameField = new TextField("노드 이름") { value = "NewNode" };
        rootVisualElement.Add(_nameField);
        var createBtn = new Button(OnCreateNode) { text = "생성" };
        rootVisualElement.Add(createBtn);
    }

    private List<Type> GetAllNodeTypes()
    {
        var baseType = typeof(BTNode);
        var asm = baseType.Assembly;
        return asm.GetTypes()
            .Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract && t.IsClass)
            .ToList();
    }

    private void OnCreateNode()
    {
        int idx = _typeField.index;
        if (idx < 0 || idx >= _nodeTypes.Count) return;
        var nodeType = _nodeTypes[idx];
        var node = ScriptableObject.CreateInstance(nodeType) as BTNode;
        if (node == null) return;
        // 노드 생성 위치를 그래프 좌표계로 지정
        node.position = _popupPosition;
        node.guid = System.Guid.NewGuid().ToString();
        node.name = _nameField.value; // BTNode의 name만 입력값으로 설정
        AssetDatabase.AddObjectToAsset(node, _tree);
        AssetDatabase.SaveAssets();
        // 부모 노드에 연결
        if (_parentNodeView != null && _parentNodeView.Node is BTComposite composite)
        {
            if (_outPortIndex < composite.children.Count)
                composite.children[_outPortIndex] = node;
            else
                composite.children.Add(node);
            node.input = _parentNodeView.Node;
        }
        else if (_parentNodeView != null && _parentNodeView.Node is BTDecorator decorator)
        {
            decorator.child = node;
            node.input = _parentNodeView.Node;
        }
        EditorUtility.SetDirty(node);
        if (_parentNodeView != null)
        {
            EditorUtility.SetDirty(_parentNodeView.Node);
        }
        AssetDatabase.SaveAssets();
        // 노드 생성 후 팝업 닫기 및 그래프 뷰 갱신
        Close();
        _graphView?.RedrawTree(); // 화면 갱신
    }

    private void RegisterPopupCallbacks()
    {
        // ESC 키 감지: OnGUI에서 직접 처리
        // 포커스 아웃 감지: EditorApplication.update에서 윈도우 포커스 체크
        EditorApplication.update += CheckFocus;
    }

    private void OnGUI()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            Close();
            Event.current.Use();
        }
    }

    private void OnDisable()
    {
        EditorApplication.update -= CheckFocus;
        // 팝업이 닫힐 때 연결되지 않은 임시 간선 및 아웃 포트 정리
        if (_parentNodeView != null && _outPortIndex >= 0 && _parentNodeView.OutputPorts.Count > _outPortIndex)
        {
            // 연결된 노드 정보만 null로 변경
            if (_parentNodeView.Node is BTComposite composite)
            {
                if (_outPortIndex < composite.children.Count)
                    composite.children[_outPortIndex] = null;
            }
            else if (_parentNodeView.Node is BTDecorator decorator)
            {
                decorator.child = null;
            }
        }
        
        // 그래프 갱신
        if (_graphView != null)
        {
            _graphView.RedrawTree();
        }
    }

    private void CheckFocus()
    {
        if (focusedWindow != this)
        {
            Close();
        }
    }

    protected void OnEnable()
    {
        // OnEnable에서는 콜백 등록하지 않음 (팝업 콜백은 RegisterPopupCallbacks에서 처리)
    }
}
