using UnityEngine;

namespace ArcadeVP
{
    public class InputManager_ArcadeVP : MonoBehaviour
    {
        public ArcadeVehicleController arcadeVehicleController;

        [HideInInspector] public float Horizontal;
        [HideInInspector] public float Vertical;
        [HideInInspector] public float Jump;

        private void Update()
        {
            Horizontal = Input.GetAxis("Horizontal");
            Vertical = Input.GetAxis("Vertical");
            Jump = Input.GetAxis("Jump");

            arcadeVehicleController.ProvideInputs(Horizontal, Vertical, Jump);
        }
    } 
}
