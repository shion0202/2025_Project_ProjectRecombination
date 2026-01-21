using System.Collections.Generic;
using Monster.AI.BehaviorTree;
using UnityEditor;
using UnityEngine;
using Monster.AI.BehaviorTree.Nodes;

public static class BTEditorUtils
{
    // ScriptableObject 생성 및 에셋 등록 로직 통합
    private static T CreateAndAddAsset<T>(Object parent, string name = null) where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        asset.name = name ?? typeof(T).Name;
        AssetDatabase.AddObjectToAsset(asset, parent);
        AssetDatabase.SaveAssets();
        return asset;
    }

    public static T CreateNodeAsset<T>(string name = null) where T : ScriptableObject
    {
        return CreateAndAddAsset<T>(Selection.activeObject, name);
    }

    // public static T CreateNode<T>(BehaviorTree.BehaviorTree tree, string name = null) where T : BTNode
    // {
    //     var node = CreateAndAddAsset<T>(tree, name);
    //     if (node == null) return null;
    //     node.guid = System.Guid.NewGuid().ToString();
    //     node.name = $"{(name ?? typeof(T).Name)}_{node.guid}";
    //     return node;
    // }

    // 루트 노드는 반드시 구체 타입으로 생성해야 함
    public static T CreateRootNode<T>(BehaviorTree tree, string name = null) where T : BTNode
    {
        var node = CreateAndAddAsset<T>(tree, name);
        if (node == null) return null;
        node.guid = System.Guid.NewGuid().ToString();
        node.name = $"{(name ?? typeof(T).Name)}_{node.guid}";
        return node;
    }

    public static string GenerateGUID() => System.Guid.NewGuid().ToString();

    public static Color GetColorByNodeType(System.Type type)
    {
        if (type == typeof(BTSequence)) return new Color(0.21f, 0.36f, 0.49f); // 파랑
        if (type == typeof(BTSelector)) return new Color(0.42f, 0.36f, 0.48f); // 보라
        if (type == typeof(BTAction)) return new Color(0.75f, 0.42f, 0.52f); // 핑크
        return Color.gray;
    }

    // 노드 생성 시 반드시 ScriptableObject 에셋 인스턴스만 반환하고, 트리 구조에서 해당 인스턴스만 참조하도록 통일
    public static T CreateNode<T>(BehaviorTree tree, string name = null) where T : BTNode
    {
        var node = ScriptableObject.CreateInstance<T>();
        if (node == null)
        {
            Debug.LogError(
                $"[CreateNode] Failed to create ScriptableObject of type {typeof(T)}. Make sure it is a non-abstract ScriptableObject.");
            return null;
        }

        node.guid = System.Guid.NewGuid().ToString();
        string assetName = $"{(name ?? typeof(T).Name)}_{node.guid}";
        AssetDatabase.AddObjectToAsset(node, tree);
        node.name = assetName;
        AssetDatabase.SaveAssets();
        Debug.Log($"[BTEditorUtils] 노드 생성: {node.name} ({node.guid})");
        return node;
    }

    public static BTNode CreateRootNode(BehaviorTree tree, string name = null)
    {
        var node = ScriptableObject.CreateInstance<BTNode>();
        if (node == null)
        {
            Debug.LogError(
                $"[CreateNode] Failed to create ScriptableObject of type {typeof(BTNode)}. Make sure it is a non-abstract ScriptableObject.");
            return null;
        }

        node.guid = System.Guid.NewGuid().ToString();
        string assetName = $"{(name ?? typeof(BTNode).Name)}_{node.guid}";
        node.name = assetName;
        Debug.Log($"[BTEditorUtils] 루트 노드 생성: {node.name} ({node.guid})");
        return node;
    }

    // input Node == Parent Node
    public static BTComposite GetParent(BTNode node)
    {
        return node.input as BTComposite;
    }

    public static List<BTNode> GetChildren(BTNode node)
    {
        var result = new List<BTNode>();
        if (node is BTComposite composite)
            result.AddRange(composite.children);
        else if (node is BTDecorator decorator && decorator.child != null)
            result.Add(decorator.child);
        return result;
    }

    // 노드에서 트리의 루트 노드를 반환
    public static BTNode GetRootNode(BTNode node)
    {
        if (node == null) return null;
        // 부모를 따라 최상위까지 이동
        BTNode current = node;
        while (current.input != null)
        {
            current = current.input;
        }

        return current;
    }
}