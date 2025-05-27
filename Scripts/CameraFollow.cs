using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;         // The drone (target) the camera follows
    public Vector3 offset;           // Offset between the camera and the drone
    public float followSpeed = 5f;   // Speed of the camera's movement (smooth follow)
    public float rotationSpeed = 5f; // Speed of rotation smoothing

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("No target assigned to CameraFollow script!");
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Smoothly move the camera to follow the drone with an offset
        Vector3 desiredPosition = target.position + target.rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Smoothly rotate the camera to match the drone’s rotation (yaw)
        Quaternion targetRotation = Quaternion.Euler(0, target.eulerAngles.y, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Ensure the camera always looks at the drone
        transform.LookAt(target.position);
    }
}
