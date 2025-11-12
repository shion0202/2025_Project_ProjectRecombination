using UnityEngine;
using System.Collections;

namespace Forge3D
{
    public class MeshColliderChild : MonoBehaviour
    {
        private Vector3 positionDiff = Vector3.zero;
       // private Vector3 rotationDiff = Vector3.zero;
        private Vector3 scaleDiff = Vector3.zero;

        private Transform transf;
        public Transform parent;

        void Start()
        {
            transf = this.transform;
            if (parent != null)
                Install(parent);
        }

        public void Install(Transform _parent)
        {
            if (transf == null)
                transf = this.transform;

            if (_parent == null)
            {
                Debug.Log("PARENT NULL");
                return;
            }

            parent = _parent;

            transf.parent = _parent.parent;
            positionDiff = _parent.position - transf.position;
          //  rotationDiff = _parent.localEulerAngles - transf.localEulerAngles;
            scaleDiff = new Vector3(_parent.localScale.x / transf.localScale.x, _parent.localScale.y / transf.localScale.y,
                _parent.localScale.z / transf.localScale.z);
            // Debug.Log(positionDiff);
            // Debug.Log(rotationDiff);

        }

        void LateUpdate()
        {
            if (parent == null)
                return;

            transf.position = parent.position - positionDiff;
            transf.localEulerAngles = parent.localEulerAngles;// - rotationDiff;
            transf.localScale = new Vector3(parent.localScale.x / scaleDiff.x, parent.localScale.y / scaleDiff.y,
                parent.localScale.z / scaleDiff.z);

            if (transf.gameObject.activeSelf && !parent.gameObject.activeSelf)
                transf.gameObject.SetActive(false);
            else if (transf.gameObject.activeSelf && !parent.gameObject.activeSelf)
                transf.gameObject.SetActive(true);

            //Debug.Log(parent.localEulerAngles - transf.localEulerAngles + "UPD");

        }
    }
}