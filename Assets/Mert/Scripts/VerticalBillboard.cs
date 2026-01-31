using UnityEngine;

public class VerticalBillboard : MonoBehaviour
{
    private Transform camTransform;

    void Start()
    {
        // Cache the camera transform for performance
        camTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        // 1. Get the Camera's position
        Vector3 targetPosition = camTransform.position;

        // 2. Lock the Y position (Height)
        // We set the target's height to be the same as the object's height.
        // This forces the "LookAt" vector to be flat on the ground.
        targetPosition.y = transform.position.y;

        // 3. Look at that spot
        transform.LookAt(targetPosition);
    }
}
