using UnityEngine;

public interface ILegsMovement
{
    Vector3 GetMoveDirection(Vector2 moveInput, Transform characterTransform, Transform cameraTransform);
}
