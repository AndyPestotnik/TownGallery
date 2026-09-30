using System.Collections;
using UnityEngine;

//Starts GPS + compass and exposes the latest reading.
//iOS requires "Location Usage Description" to be filled in under Player Settings > iOS > Other Settings.
public class LocationProvider : MonoBehaviour
{
	public float desiredAccuracyMeters = 5f;
	public float updateDistanceMeters = 1f;

	public bool IsReady => Input.location.status == LocationServiceStatus.Running;
	public double Latitude => Input.location.lastData.latitude;
	public double Longitude => Input.location.lastData.longitude;
	public float HorizontalAccuracy => Input.location.lastData.horizontalAccuracy;
	public float Heading => Input.compass.trueHeading;

	void Start()
	{
		Input.compass.enabled = true;
		Input.location.Start(desiredAccuracyMeters, updateDistanceMeters); // triggers the iOS permission prompt
	}

	//Yield on this from a coroutine; check IsReady afterwards
	public IEnumerator WaitUntilReady(float timeoutSeconds)
	{
		float elapsed = 0f;
		while (!IsReady && Input.location.status != LocationServiceStatus.Failed && elapsed < timeoutSeconds)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}

		if (!IsReady)
			Debug.LogWarning($"Location not available (status: {Input.location.status})");
	}

	void OnDestroy()
	{
		Input.location.Stop();
		Input.compass.enabled = false;
	}
}
