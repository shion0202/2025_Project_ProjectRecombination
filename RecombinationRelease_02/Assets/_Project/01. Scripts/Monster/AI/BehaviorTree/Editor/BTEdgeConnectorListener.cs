using UnityEditor.Experimental.GraphView;
using UnityEngine;

// namespace AI.BehaviorTree.EditorExtensions
// Unity GraphView의 Edge 연결 이벤트를 처리하는 리스너
public class BTEdgeConnectorListener : IEdgeConnectorListener
{
    public void OnDrop(GraphView graphView, Edge edge)
    {
        graphView.AddElement(edge);
    }

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
        if (edge != null)
        {
            var graphView = edge.GetFirstAncestorOfType<GraphView>();
            if (graphView != null)
                graphView.RemoveElement(edge);
        }
    }
}
