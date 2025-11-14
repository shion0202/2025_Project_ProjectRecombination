using Controller;
using UnityEditor;

namespace Assets.Agoston_R.Simple_Laser.Scripts.Editor
{
    [CustomEditor(typeof(LaserBeamController))]
    class LaserControllerEditor : UnityEditor.Editor
    {
        LaserBeamController controller;

        private void OnEnable()
        {
            controller = target as LaserBeamController;
        }

        public override void OnInspectorGUI()
        {
            switch (controller.physicsMode)
            {
                case Dto.PhysicsMode.Mode3D:
                    break;
                case Dto.PhysicsMode.Mode2D:
                    controller.minDepth = EditorGUILayout.FloatField("2D raycast min depth", controller.minDepth);
                    controller.maxDepth = EditorGUILayout.FloatField("2D raycast max depth", controller.maxDepth);
                    break;
            }

            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }
    }
}
