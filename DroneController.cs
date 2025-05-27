using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    
    public float speed = 5f;
    public float liftForce = 10f;
    public float rotationSpeed = 100f;
    public Transform cameraTransform; // Reference to the camera
    private Rigidbody rb;

    // Vicon Data
    private float viconX = 0f, viconY = 0f, viconZ = 0f;  // Store Vicon position
    private float x1 = 0, x2 = 0, x3 = 0, x4 = 0;
    private float y1 = 0, y2 = 0, y3 = 0, y4 = 0;
    private float z1 = 0, z2 = 0, z3 = 0, z4 = 0;
    private UdpClient udpClient;
    private int port = 5005; // Port to listen for data from MATLAB
    private string receivedData;

    void Start()
    {
        rb = GetComponent<Rigidbody>(); //NEEDED

        // Set up UDP client to listen on the same port that MATLAB is sending to
        udpClient = new UdpClient(port);
        udpClient.EnableBroadcast = true;

        // Start listening for incoming data in a separate thread
        System.Threading.Thread receiveThread = new System.Threading.Thread(new System.Threading.ThreadStart(ListenForData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("Camera reference missing in DroneController!");
            return;
        }

        // Get camera's forward direction (ignore Y component to prevent unwanted vertical movement)
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0; // Flatten to only consider horizontal movement
        cameraForward.Normalize();

        // Get camera's right direction
        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0; // Flatten to prevent unwanted vertical movement
        cameraRight.Normalize();

        // Movement inputs relative to the camera
        float movex = Input.GetAxis("Horizontal");  // Left (-1) / Right (+1)
        float movez = Input.GetAxis("Vertical");    // Forward (+1) / Backward (-1)

        if (z1 + 15 < z2 && z3 + 15 < z4) movez = 1;
        if (z2 + 15 < z1 && z4 + 15 < z3) movez = -1;
        if (z1 + 15 < z3 && z2 + 15 < z4) movex = 1;                       //GOAL: CHANGE INPUT TO VICON DATA
        if (z3 + 15 < z1 && z4 + 15 < z2) movex = -1;

        Vector3 movement = (cameraForward * movez + cameraRight * movex) * speed;

        // Lift controls
        float moveY = 0;
        if (z1 > 1700) moveY = liftForce; // Ascend           //GOAL: CHANGE INPUT TO VICON DATA
        if (z1 == 0) moveY = -liftForce; // Descend     //GOAL: CHANGE INPUT TO VICON DATA

        movement.y = moveY;

        // Apply movement with acceleration force
        rb.AddForce(movement, ForceMode.Acceleration);

        // Rotation inputs (Y-axis)
        float rotateY = 0f;
        float targetRotateX = 0f;
        float targetRotateZ = 0f;
        if (z3 + 60 <= z1 && z4 + 60 <= z2) rotateY = -rotationSpeed;                  //GOAL: CHANGE INPUT TO VICON DATA
        else if (z1 + 60 <= z3 && z2 + 60 <= z4) rotateY = rotationSpeed;                   //GOAL: CHANGE INPUT TO VICON DATA
        else
        {
            if (z1 + 15 < z2 && z3 + 15 < z4) targetRotateX = 10;                        //GOAL: CHANGE INPUT TO VICON DATA
            if (z2 + 15 < z1 && z4 + 15 < z3) targetRotateX = -10;                       //GOAL: CHANGE INPUT TO VICON DATA
            if (z1 + 15 < z3 && z2 + 15 < z4) targetRotateZ = -10;                       //GOAL: CHANGE INPUT TO VICON DATA
            if (z3 + 15 < z1 && z4 + 15 < z2) targetRotateZ = 10;
        }
        // Apply Y-axis rotation
        transform.Rotate(0, rotateY * Time.deltaTime, 0);

        // Smoothly interpolate tilt
        Vector3 targetRotation = new Vector3(targetRotateX, transform.eulerAngles.y, targetRotateZ);
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(targetRotation), Time.deltaTime * 5f);
    }

    // Method to listen for incoming UDP data
    private void ListenForData()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);

        int i = 0;

        while (true)
        {
            try
            {
                // Receive bytes and convert to string
                byte[] data = udpClient.Receive(ref endPoint);
                receivedData = System.Text.Encoding.UTF8.GetString(data);

                // Debug the raw data to verify incoming format
                //Debug.Log($"Received Data: {receivedData}");

                // Confirm marker format by logging the index
                string markerPrefix = $"Marker Hand{i + 1}: ";
                if (!receivedData.StartsWith(markerPrefix))
                {
                    Debug.LogWarning($"Unexpected marker format. Expected: '{markerPrefix}' but received: '{receivedData}'");
                    continue; // Skip if the marker name doesn't match
                }

                // Remove marker prefix and extract position data
                string markerData = receivedData.Replace(markerPrefix, "").Trim();
                string[] parts = markerData.Split(' ');

                // Ensure we have exactly 3 position components
                if (parts.Length != 3)
                {
                    Debug.LogError("Invalid data format. Expected 3 components (X, Y, Z).");
                    continue;
                }

                // Safely parse X, Y, Z values
                if (float.TryParse(parts[0].Split('=')[1], out viconX) &&
                    float.TryParse(parts[1].Split('=')[1], out viconY) &&
                    float.TryParse(parts[2].Split('=')[1], out viconZ))
                {
                    // Store values in the correct marker
                    i++;
                    switch (i)
                    {
                        case 1:
                            (x1, y1, z1) = (viconX, viconY, viconZ);
                            Debug.Log($"Marker 1 - X1: {x1}, Y1: {y1}, Z1: {z1}");
                            break;
                        case 2:
                            (x2, y2, z2) = (viconX, viconY, viconZ);
                            Debug.Log($"Marker 2 - X2: {x2}, Y2: {y2}, Z2: {z2}");
                            break;
                        case 3:
                            (x3, y3, z3) = (viconX, viconY, viconZ);
                            Debug.Log($"Marker 3 - X3: {x3}, Y3: {y3}, Z3: {z3}");
                            break;
                        case 4:
                            (x4, y4, z4) = (viconX, viconY, viconZ);
                            Debug.Log($"Marker 4 - X4: {x4}, Y4: {y4}, Z4: {z4}");
                            i = 0; // Reset after 4 markers
                            break;
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse X, Y, Z values from marker data.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error receiving data: {e.Message}");
            }
        }
    }

    // Clean up the UDP client when the application quits
    private void OnApplicationQuit()
    {
        udpClient.Close();
    }
}
