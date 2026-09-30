using UnityEngine;

public class OscillatingRotation : MonoBehaviour
{
    public float minRotationAngle = 10f;
    public float maxRotationAngle = 50f;
    public float minRotationSpeed = 15f;
    public float maxRotationSpeed = 45f;

    private float rotationYSpeed = 30f; // Degrees per second
    public float rotationZSpeed = 20f;
    public float maxYAngle = 45f;      // Maximum angle from the starting rotation
    public float maxZAngle = 30f;

    // private bool yWasIncreasing = true;
    // private bool zWasIncreasing = true;

    // private float previousYAngle = 0;
    // private float previousZAngle = 0;

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation; // Store the object's initial rotation relative to its parent (anchor)
        rotationYSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        rotationZSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);

        maxYAngle = Random.Range(minRotationAngle, maxRotationAngle);
        maxZAngle = Random.Range(minRotationAngle, maxRotationAngle);

    }

    void Update()
    {
        // Calculate the current angle using Mathf.PingPong
        // Time.time * rotationSpeed creates a continuously increasing value
        // maxAngle * 2 defines the range of the PingPong function (e.g., 0 to 90 for a maxAngle of 45)
        float currentYAngle = Mathf.PingPong(Time.time * rotationYSpeed, maxYAngle * 2) - maxYAngle;
        float currentZAngle = Mathf.PingPong(Time.time * rotationZSpeed, maxZAngle * 2) - maxZAngle;

        // Apply the rotation around the Y-axis relative to the initial rotation
        // Use localRotation so the parent anchor's tracking corrections are preserved
        transform.localRotation = initialRotation * Quaternion.Euler(0, currentYAngle, currentZAngle);

        // bool yIsIncreasing = currentYAngle > previousYAngle;
        // bool zIsIncreasing = currentZAngle > previousZAngle;

        // previousZAngle = currentZAngle;
        // previousYAngle = currentYAngle;

        // if (zIsIncreasing != zWasIncreasing)
        // {
        //     rotationZSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        //     maxZAngle = Random.Range(minRotationAngle, maxRotationAngle);
        // }

        // if (yIsIncreasing != yWasIncreasing)
        // {
        //     rotationYSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        //     maxYAngle = Random.Range(minRotationAngle, maxRotationAngle);
        // }

        // yWasIncreasing = yIsIncreasing;
        // zWasIncreasing = zIsIncreasing;
        
    }
}