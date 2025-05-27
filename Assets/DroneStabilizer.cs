using UnityEngine;

public class DroneStabilizer : MonoBehaviour
{
    public float stabilizationStrength = 5f;  // Strength of stabilization forces
    public float maxTiltAngle = 45f;         // Maximum tilt angle allowed before correcting
    public float correctionSpeed = 3f;       // Speed of correction when tilting

    private Rigidbody rb; // Reference to the existing Rigidbody

    void Start()
    {
        // Get the Rigidbody component of the drone (this should already be attached)
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Check the current tilt of the drone by getting its rotation in Euler angles
        Vector3 currentRotation = transform.rotation.eulerAngles;

        // Get the roll and pitch angles (we're ignoring yaw for self-stabilization)
        float roll = NormalizeAngle(currentRotation.x);
        float pitch = NormalizeAngle(currentRotation.z);

        // Apply stabilization if the drone is tilted beyond a threshold
        if (Mathf.Abs(roll) > maxTiltAngle || Mathf.Abs(pitch) > maxTiltAngle)
        {
            StabilizeDrone(roll, pitch);
        }
    }

    void StabilizeDrone(float roll, float pitch)
    {
        // Compute the target rotation to self-correct the tilt
        Quaternion targetRotation = Quaternion.Euler(
            Mathf.LerpAngle(transform.rotation.eulerAngles.x, 0, correctionSpeed * Time.deltaTime),
            transform.rotation.eulerAngles.y,  // Keep yaw intact
            Mathf.LerpAngle(transform.rotation.eulerAngles.z, 0, correctionSpeed * Time.deltaTime)
        );

        // Apply torque to stabilize by modifying angular velocity and applying forces
        Vector3 stabilizationTorque = Vector3.zero;

        if (Mathf.Abs(roll) > maxTiltAngle)
        {
            stabilizationTorque.x = Mathf.Sign(roll) * stabilizationStrength;
        }

        if (Mathf.Abs(pitch) > maxTiltAngle)
        {
            stabilizationTorque.z = Mathf.Sign(pitch) * stabilizationStrength;
        }

        // Apply the torque to the existing Rigidbody (using ForceMode.Acceleration for smooth physics)
        rb.AddTorque(stabilizationTorque, ForceMode.Acceleration);
    }

    // Normalize angles to be between -180 and 180 degrees
    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            return angle - 360f;
        return angle;
    }
}