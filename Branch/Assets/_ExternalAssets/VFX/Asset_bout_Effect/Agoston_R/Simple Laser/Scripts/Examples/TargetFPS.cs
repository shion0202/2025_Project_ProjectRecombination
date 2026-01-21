using UnityEngine;

namespace LaserAssetPackage.Tests.LaserAssetPackage.Tests.Runtime.BasicActorTest
{
    public class TargetFPS : MonoBehaviour
    {
        public int targetFPS = 100;

        private void Start()
        {
            Application.targetFrameRate = targetFPS;
        }
    }
}