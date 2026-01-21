using UnityEngine;

namespace _Project.Scripts.GUI.TitleScene
{
    public class DTBase : MonoBehaviour
    {
        public bool isStarted = false;
        public bool isRunning = false;
        
        public void AnimaStart()
        {
            isStarted = true;
        }
    }
}