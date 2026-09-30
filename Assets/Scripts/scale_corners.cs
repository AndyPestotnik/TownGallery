using UnityEngine;

public class scale_corners : MonoBehaviour
{
    public Transform parentTransform;
    Vector3 startingScale;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startingScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 inverseScale = new Vector3(
            1f / parentTransform.localScale.x * startingScale.x,
            1f / parentTransform.localScale.y * startingScale.y,
            1f / parentTransform.localScale.z * startingScale.z
        );
        transform.localScale = inverseScale;
    }
}
