using System;
using System.Collections;
using System.Collections.Generic;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

//Saves placed images as ARCore Cloud Anchors + a database row, and loads nearby saved images on startup.
//Requires the ARCore Extensions package and an ARCoreExtensions object in the scene with Cloud Anchors enabled.
public class CloudAnchorPlacementManager : MonoBehaviour
{
	public ARAnchorManager anchorManager;
	public LocationProvider locationProvider;
	public SupabasePlacementApi api;
	public GameObject double_image_quad;
	public Text statusText; // Optional

	public int anchorTtlDays = 1; // API key auth only allows 1 day; token auth allows up to 365
	public float nearbyRadiusMeters = 50f;
	public float hostingQualityTimeout = 30f;
	public float locationTimeout = 15f;
	public bool loadNearbyOnStart = true;

	//Cloud anchor IDs already in the scene, so reloading doesn't duplicate them
	private HashSet<string> loadedAnchorIds = new HashSet<string>();

	void Start()
	{
		if (loadNearbyOnStart)
			LoadNearby();
	}

	//Call right after the image is parented to its anchor, before anything animates its local transform
	public void SavePlacement(ARAnchor anchor, GameObject imageQuad, Texture2D texture)
	{
		Transform t = imageQuad.transform;
		PlacementRecord record = new PlacementRecord
		{
			pos_x = t.localPosition.x, pos_y = t.localPosition.y, pos_z = t.localPosition.z,
			rot_x = t.localRotation.x, rot_y = t.localRotation.y, rot_z = t.localRotation.z, rot_w = t.localRotation.w,
			scale_x = t.localScale.x, scale_y = t.localScale.y, scale_z = t.localScale.z,
		};
		StartCoroutine(SaveRoutine(anchor, record, texture));
	}

	public void LoadNearby()
	{
		StartCoroutine(LoadNearbyRoutine());
	}

	private IEnumerator SaveRoutine(ARAnchor anchor, PlacementRecord record, Texture2D texture)
	{
		//Local anchor needs to be tracking before it can be hosted
		while (anchor != null && anchor.trackingState != TrackingState.Tracking)
			yield return null;
		if (anchor == null)
			yield break;

		//ARCore needs to have seen the area from a few angles before it can host an anchor there
		FeatureMapQuality quality = GetHostingQuality();
		if (quality == FeatureMapQuality.Insufficient)
		{
			SetStatus("Move your phone slowly around the image to save it...");
			float elapsed = 0f;
			while (quality == FeatureMapQuality.Insufficient && elapsed < hostingQualityTimeout)
			{
				yield return null;
				elapsed += Time.deltaTime;
				quality = GetHostingQuality();
			}

			//Quality is only an estimate, so try hosting anyway rather than giving up
			if (quality == FeatureMapQuality.Insufficient)
				Debug.Log("Visual detail still low, trying to save anyway");
		}
		Debug.Log("Hosting quality: " + quality);

		SetStatus("Saving anchor...");
		var hostPromise = anchorManager.HostCloudAnchorAsync(anchor, anchorTtlDays);
		while (hostPromise.State == PromiseState.Pending)
			yield return null;

		var hostResult = hostPromise.Result;
		if (hostResult.CloudAnchorState != CloudAnchorState.Success)
		{
			SetStatus("Failed to save anchor: " + hostResult.CloudAnchorState);
			yield break;
		}
		record.cloud_anchor_id = hostResult.CloudAnchorId;
		record.anchor_expires_at = DateTime.UtcNow.AddDays(anchorTtlDays).ToString("o");
		loadedAnchorIds.Add(record.cloud_anchor_id);

		yield return locationProvider.WaitUntilReady(locationTimeout);
		if (!locationProvider.IsReady)
		{
			SetStatus("Couldn't get GPS location, image not saved.");
			yield break;
		}
		record.lat = locationProvider.Latitude;
		record.lon = locationProvider.Longitude;
		record.gps_accuracy = locationProvider.HorizontalAccuracy;
		record.heading = locationProvider.Heading;

		SetStatus("Uploading image...");
		string error = null;
		yield return api.UploadImage(texture.EncodeToPNG(), url => record.image_url = url, e => error = e);
		if (error != null)
		{
			SetStatus(error);
			yield break;
		}

		yield return api.InsertPlacement(record, () => { }, e => error = e);
		SetStatus(error ?? "Image saved!");
	}

	private IEnumerator LoadNearbyRoutine()
	{
		while (ARSession.state != ARSessionState.SessionTracking)
			yield return null;

		yield return locationProvider.WaitUntilReady(locationTimeout);
		if (!locationProvider.IsReady)
		{
			SetStatus("Couldn't get GPS location to load nearby images.");
			yield break;
		}

		PlacementRecord[] records = null;
		string error = null;
		yield return api.QueryNearby(locationProvider.Latitude, locationProvider.Longitude, nearbyRadiusMeters, r => records = r, e => error = e);
		if (error != null)
		{
			SetStatus(error);
			yield break;
		}

		Debug.Log($"Found {records.Length} nearby placements");
		foreach (PlacementRecord record in records)
		{
			if (loadedAnchorIds.Add(record.cloud_anchor_id))
				StartCoroutine(ResolveAndSpawn(record));
		}
	}

	private IEnumerator ResolveAndSpawn(PlacementRecord record)
	{
		Texture2D texture = null;
		string error = null;
		yield return api.DownloadImage(record.image_url, t => texture = t, e => error = e);
		if (error != null)
		{
			Debug.LogWarning(error);
			loadedAnchorIds.Remove(record.cloud_anchor_id);
			yield break;
		}

		//Keeps searching until the user points the camera at the area where the anchor was hosted
		var resolvePromise = anchorManager.ResolveCloudAnchorAsync(record.cloud_anchor_id);
		while (resolvePromise.State == PromiseState.Pending)
			yield return null;

		var resolveResult = resolvePromise.Result;
		if (resolveResult.CloudAnchorState != CloudAnchorState.Success)
		{
			Debug.LogWarning($"Couldn't resolve anchor {record.cloud_anchor_id}: {resolveResult.CloudAnchorState}");
			loadedAnchorIds.Remove(record.cloud_anchor_id);
			yield break;
		}

		GameObject quad = Instantiate(double_image_quad, resolveResult.Anchor.transform);
		quad.transform.localPosition = new Vector3(record.pos_x, record.pos_y, record.pos_z);
		quad.transform.localRotation = new Quaternion(record.rot_x, record.rot_y, record.rot_z, record.rot_w);
		quad.transform.localScale = new Vector3(record.scale_x, record.scale_y, record.scale_z);
		ImageQuadUtil.ApplyTexture(quad, texture);
		ImageQuadUtil.SetCornersVisible(quad, false);
	}

	private FeatureMapQuality GetHostingQuality()
	{
		Transform cam = Camera.main.transform;
		return anchorManager.EstimateFeatureMapQualityForHosting(new Pose(cam.position, cam.rotation));
	}

	private void SetStatus(string message)
	{
		Debug.Log(message);
		if (statusText != null)
			statusText.text = message;
	}
}
