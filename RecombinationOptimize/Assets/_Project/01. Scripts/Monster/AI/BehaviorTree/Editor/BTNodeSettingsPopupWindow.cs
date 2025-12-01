using UnityEditor;
using UnityEngine;
using Monster.AI.BehaviorTree.Nodes;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

public class BTNodeSettingsPopupWindow : VisualElement
{
    private BTNode _targetNode;
    private Label _nodeTypeLabel;
    private TextField _nodeNameField;
    private System.Action _onNodeChanged;
    private BTGraphView _graphView;
    private Editor _nodeEditor;
    private IMGUIContainer _inspectorContainer;

    public BTNodeSettingsPopupWindow()
    {
        style.flexDirection = FlexDirection.Column;
        style.paddingLeft = 10;
        style.paddingRight = 10;
        style.paddingTop = 10;
        style.paddingBottom = 10;
        style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        style.borderTopWidth = 1;
        style.borderBottomWidth = 1;
        style.borderLeftWidth = 1;
        style.borderRightWidth = 1;
        style.minWidth = 320;
        style.maxWidth = 320;
        style.minHeight = 120;
        style.maxHeight = 400;
    }

    public void SetTargetNode(BTNode node, BTGraphView graphView)
    {
        _targetNode = node;
        _graphView = graphView;
        CreateUI();
    }

    private void CreateUI()
    {
        Clear();
        if (_targetNode == null)
        {
            Add(new Label("노드가 선택되지 않았습니다."));
            return;
        }
        // UIElements 기반 커스텀 인스팩터 예시
        var nameField = new TextField("노드 이름") { value = _targetNode.name };
        nameField.isDelayed = true;
        nameField.RegisterValueChangedCallback(evt =>
        {
            if (_targetNode.name != evt.newValue)
            {
                _targetNode.name = evt.newValue;
                EditorUtility.SetDirty(_targetNode);
                AssetDatabase.SaveAssets();
                _graphView?.RedrawTree();
                nameField.Focus();
            }
        });
        Add(nameField);

        // 자식 노드 UI (Composite, Decorator)
        if (_targetNode is BTComposite composite)
        {
            var childrenLabel = new Label("자식 노드 목록");
            Add(childrenLabel);
            for (int i = 0; i < composite.children.Count; i++)
            {
                var childField = new ObjectField($"자식 {i + 1}")
                {
                    objectType = typeof(BTNode),
                    value = composite.children[i],
                    allowSceneObjects = false
                };
                int idx = i;
                childField.RegisterValueChangedCallback(evt =>
                {
                    composite.children[idx] = evt.newValue as BTNode;
                    EditorUtility.SetDirty(_targetNode);
                    AssetDatabase.SaveAssets();
                    _graphView?.RedrawTree();
                    childField.Focus();
                });
                Add(childField);
            }
            // 자식 노드 추가 버튼
            var addChildBtn = new Button(() =>
            {
                composite.children.Add(null);
                EditorUtility.SetDirty(_targetNode);
                AssetDatabase.SaveAssets();
                _graphView?.RedrawTree();
            }) { text = "자식 노드 추가" };
            Add(addChildBtn);
            
            // 자식 노드 삭제 버튼
            var removeChildBtn = new Button(() =>
            {
                if (composite.children.Count > 0)
                {
                    composite.children.RemoveAt(composite.children.Count - 1);
                    EditorUtility.SetDirty(_targetNode);
                    AssetDatabase.SaveAssets();
                    _graphView?.RedrawTree();
                }
            }) { text = "자식 노드 삭제" };
            Add(removeChildBtn);
        }
        else if (_targetNode is BTDecorator decorator)
        {
            var childField = new ObjectField("자식 노드")
            {
                objectType = typeof(BTNode),
                value = decorator.child,
                allowSceneObjects = false
            };
            childField.RegisterValueChangedCallback(evt =>
            {
                decorator.child = evt.newValue as BTNode;
                EditorUtility.SetDirty(_targetNode);
                AssetDatabase.SaveAssets();
                _graphView?.RedrawTree();
                childField.Focus();
            });
            Add(childField);
            
            // TODO: 아래에 다른 Decorator 타입에 대한 필드 추가 가능
            
            // 쿨타임 데코레이터
            if (decorator is BTCooldown cooldown)
            {
                var timeField = new FloatField("Cooldown Time") { value = cooldown.cooldownTime };
                timeField.RegisterValueChangedCallback(evt =>
                {
                    cooldown.cooldownTime = evt.newValue;
                    EditorUtility.SetDirty(cooldown);
                    AssetDatabase.SaveAssets();
                });
                Add(timeField);
            }
            
            // 딜레이 타임 데코레이터
            if (decorator is BTDelay delay)
            {
                var timeField = new FloatField("Delay Time") { value = delay.delayTime };
                timeField.RegisterValueChangedCallback(evt =>
                {
                    delay.delayTime = evt.newValue;
                    EditorUtility.SetDirty(delay);
                    AssetDatabase.SaveAssets();
                });
                Add(timeField);
            }
        }
        
        // 컨디션 노드
        else if (_targetNode is BTCondition condition)
        {
            if (condition is BTIsRandom isRandomCondition)
            {
                var randomField = new FloatField("확률 값") { value = isRandomCondition.threshold };
                randomField.RegisterValueChangedCallback(evt =>
                {
                    isRandomCondition.threshold = evt.newValue;
                    EditorUtility.SetDirty(isRandomCondition);
                    AssetDatabase.SaveAssets();
                });
                Add(randomField);
            }
            
            if (condition is BTIsInSkillRange attackRangeCondition)
            {
                var skillIdField = new IntegerField("스킬 ID") { value = attackRangeCondition.skillId };
                skillIdField.RegisterValueChangedCallback(evt =>
                {
                    attackRangeCondition.skillId = evt.newValue;
                    EditorUtility.SetDirty(attackRangeCondition);
                    AssetDatabase.SaveAssets();
                });
                Add(skillIdField);
            }
            
            if (condition is BTIsCooldown cooldownCondition)
            {
                var skillIdField = new IntegerField("스킬 ID") { value = cooldownCondition.skillId };
                skillIdField.RegisterValueChangedCallback(evt =>
                {
                    cooldownCondition.skillId = evt.newValue;
                    EditorUtility.SetDirty(cooldownCondition);
                    AssetDatabase.SaveAssets();
                });
                Add(skillIdField);
            }
            
            if (condition is BTIsHealthLow healthLowCondition)
            {
                var thresholdField = new FloatField("체력 임계값") { value = healthLowCondition.healthThreshold };
                thresholdField.RegisterValueChangedCallback(evt =>
                {
                    healthLowCondition.healthThreshold = evt.newValue;
                    EditorUtility.SetDirty(healthLowCondition);
                    AssetDatabase.SaveAssets();
                });
                Add(thresholdField);
            }
        }
        // 액션 노드
        else if (_targetNode is BTAction action)
        {
            var priorityField = new IntegerField("우선순위") { value = action.priority };
            priorityField.RegisterValueChangedCallback(evt =>
            {
                action.priority = evt.newValue;
                EditorUtility.SetDirty(action);
                AssetDatabase.SaveAssets();
                // _graphView?.RedrawTree();
            });
            Add(priorityField);
            
            if (action is BTActionAttack attackAction)
            {
                var skillIdField = new IntegerField("스킬 ID") { value = attackAction.skillId };
                skillIdField.RegisterValueChangedCallback(evt =>
                {
                    attackAction.skillId = evt.newValue;
                    EditorUtility.SetDirty(attackAction);
                    AssetDatabase.SaveAssets();
                });
                Add(skillIdField);
            }
            
            if (action is BTActionSystemMessage systemMessageAction)
            {
                TextField speakerField = new TextField("이름") { value = systemMessageAction.speaker, isDelayed = true };
                speakerField.RegisterValueChangedCallback(evt =>
                {
                    if (systemMessageAction.speaker != evt.newValue)
                    {
                        systemMessageAction.speaker = evt.newValue;
                        EditorUtility.SetDirty(systemMessageAction);
                        AssetDatabase.SaveAssets();
                        _graphView?.RedrawTree();
                        speakerField.Focus();
                    }
                });
                
                TextField messageField = new TextField("메세지") { value = systemMessageAction.message, isDelayed = true };
                messageField.RegisterValueChangedCallback(evt =>
                {
                    if (systemMessageAction.message != evt.newValue)
                    {
                        systemMessageAction.message = evt.newValue;
                        EditorUtility.SetDirty(systemMessageAction);
                        AssetDatabase.SaveAssets();
                        _graphView?.RedrawTree();
                        messageField.Focus();
                    }
                });
                
                Add(speakerField);
                Add(messageField);
            }
            
            // 상태 전환 노드
            if (action is BTActionChangeState changeStateAction)
            {
                var stateField = new TextField("전환할 상태 이름") { value = changeStateAction.newState, isDelayed = true };
                stateField.RegisterValueChangedCallback(evt =>
                {
                    if (changeStateAction.newState != evt.newValue)
                    {
                        changeStateAction.newState = evt.newValue;
                        EditorUtility.SetDirty(changeStateAction);
                        AssetDatabase.SaveAssets();
                        _graphView?.RedrawTree();
                        stateField.Focus();
                    }
                });
                Add(stateField);
            }
        }
    }
}
