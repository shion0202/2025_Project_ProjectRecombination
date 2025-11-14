using UnityEngine;

namespace Kamgam.PowerPivot
{
    partial class PowerPivotTool
    {
        void debugDrawBounds(Bounds bounds, float duration = 0.1f)
        {
            var center = bounds.center;
            float wHalf = new Vector3(bounds.size.x, 0f, 0f).magnitude * 0.5f;
            float hHalf = new Vector3(0f, bounds.size.y, 0f).magnitude * 0.5f;
            float dHalf = new Vector3(0f, 0f, bounds.size.z).magnitude * 0.5f;

            Vector3 frontTopLeftCorner = new Vector3(center.x - wHalf, center.y + hHalf, center.z - dHalf);
            Vector3 frontTopRightCorner = new Vector3(center.x + wHalf, center.y + hHalf, center.z - dHalf);
            Vector3 frontBottomLeftCorner = new Vector3(center.x - wHalf, center.y - hHalf, center.z - dHalf);
            Vector3 frontBottomRightCorner = new Vector3(center.x + wHalf, center.y - hHalf, center.z - dHalf);

            Vector3 backTopLeftCorner = new Vector3(center.x - wHalf, center.y + hHalf, center.z + dHalf);
            Vector3 backTopRightCorner = new Vector3(center.x + wHalf, center.y + hHalf, center.z + dHalf);
            Vector3 backBottomLeftCorner = new Vector3(center.x - wHalf, center.y - hHalf, center.z + dHalf);
            Vector3 backBottomRightCorner = new Vector3(center.x + wHalf, center.y - hHalf, center.z + dHalf);

            Debug.DrawLine(frontTopLeftCorner, frontTopRightCorner, Color.blue, duration);
            Debug.DrawLine(frontTopRightCorner, frontBottomRightCorner, Color.blue, duration);
            Debug.DrawLine(frontBottomRightCorner, frontBottomLeftCorner, Color.blue, duration);
            Debug.DrawLine(frontBottomLeftCorner, frontTopLeftCorner, Color.blue, duration);

            Debug.DrawLine(frontTopLeftCorner, backTopLeftCorner, Color.blue, duration);
            Debug.DrawLine(frontTopRightCorner, backTopRightCorner, Color.blue, duration);
            Debug.DrawLine(frontBottomLeftCorner, backBottomLeftCorner, Color.blue, duration);
            Debug.DrawLine(frontBottomRightCorner, backBottomRightCorner, Color.blue, duration);

            Debug.DrawLine(backTopLeftCorner, backTopRightCorner, Color.blue, duration);
            Debug.DrawLine(backTopRightCorner, backBottomRightCorner, Color.blue, duration);
            Debug.DrawLine(backBottomRightCorner, backBottomLeftCorner, Color.blue, duration);
            Debug.DrawLine(backBottomLeftCorner, backTopLeftCorner, Color.blue, duration);
        }
    }
}
