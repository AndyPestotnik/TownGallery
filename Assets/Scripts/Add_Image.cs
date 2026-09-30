using UnityEngine;
using System.IO;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.UI; // Required for Button component


public class Add_Image : MonoBehaviour
{
	public GameObject double_image_quad;

	public CloudAnchorPlacementManager placementManager; // Optional; saves placed images to the cloud

	public ARRaycastManager aRRaycastManager; // Assign in Inspector

	public float placementDistance = 1f; // Distance in front of camera to place image
	public float minPlacementDistance = 0.2f; // Never place closer than this
	public float surfaceOffset = 0.05f; // Keep image this far in front of a detected surface

	private List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();

	private GameObject currentImageQuad;
	private Texture2D currentTexture;
	bool isAdding = false;

	private float fingerScale = 1f;
	private float aspectRatio = 1f;

	public float maxScale = 5f;
	public float minScale = 0.2f;
	public float scaleSpeed = 0.002f;

	//UI elements

	public Button addButton;
	public Button okButton;
	public Button cancelButton;

	 
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		if(isAdding)
		{
			//Get zoom scale from fingers
			if (Input.touchCount == 2)
        {
            Touch tZero = Input.GetTouch(0);
            Touch tOne = Input.GetTouch(1);

            Vector2 tZeroPrevious = tZero.position - tZero.deltaPosition;
            Vector2 tOnePrevious = tOne.position - tOne.deltaPosition;

            float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
            float currentTouchDistance = Vector2.Distance(tZero.position, tOne.position);

            float deltaDistance = currentTouchDistance - oldTouchDistance;

            fingerScale += deltaDistance * scaleSpeed;
            fingerScale = Mathf.Clamp(fingerScale, minScale, maxScale);
        }

            if (currentImageQuad != null)
			{
				currentImageQuad.transform.position = Camera.main.transform.position + Camera.main.transform.forward * GetPlacementDistance();
				currentImageQuad.transform.forward = Camera.main.transform.forward;
				currentImageQuad.transform.localScale = new Vector3(1f, aspectRatio, 1f) * fingerScale;
            }
        }
	}

	public void PickImage(int maxSize)
	{
		NativeGallery.GetImageFromGallery((path) =>
		{
			Debug.Log("Image path: " + path);
			if (path != null)
			{


				if (!File.Exists(path))
				{
					Debug.LogError("Image file not found at: " + path);
					return;
				}

				// // Create a new Texture2D
				Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize, false);
				if (texture == null)
				{
					Debug.Log("Couldn't load texture from " + path);
					return;
				}
				aspectRatio = texture.height / (float)texture.width;

				// This simulates the 'Alpha is Transparency' setting behavior
				Color[] pixels = texture.GetPixels();
				for (int i = 0; i < pixels.Length; i++)
				{
					// If a pixel's alpha is very close to 0, make its color data undefined (black)
					// This helps prevent filtering artifacts at the edges of transparent areas
					if (Mathf.Approximately(pixels[i].a, 0f))
					{
						pixels[i] = new Color(0, 0, 0, 0); // Set to transparent black
					}
				}
				texture.SetPixels(pixels);
				texture.Apply(); // Apply changes to the texture

				isAdding = true;

				showHideButtons(isAdding);

				//Create a Quad with the new texture
				GameObject double_quad = Instantiate(double_image_quad);
				double_quad.transform.position = Camera.main.transform.position + Camera.main.transform.forward * GetPlacementDistance();
				double_quad.transform.forward = Camera.main.transform.forward;
				double_quad.transform.localScale = new Vector3(1f, texture.height / (float)texture.width, 1f);

				ImageQuadUtil.ApplyTexture(double_quad, texture);

				//set current image as quad
				currentImageQuad = double_quad;
				currentTexture = texture;
				currentImageQuad.GetComponent<OscillatingRotation>().enabled = false;
			}
		});
	}

	public void setQuadPosition()
	{
		//Create anchor at the image's exact pose, on a separate parent object.
		//ARKit drives the anchor's transform; the image (and its oscillation) lives underneath as a child
		//so the two never fight over the same transform.
		GameObject anchorObject = new GameObject("ImageAnchor");
		anchorObject.transform.SetPositionAndRotation(currentImageQuad.transform.position, currentImageQuad.transform.rotation);
		ARAnchor anchor = anchorObject.AddComponent<ARAnchor>();
		currentImageQuad.transform.SetParent(anchorObject.transform, true);

		//Save before oscillation starts so the stored local pose is the resting pose
		if (placementManager != null)
			placementManager.SavePlacement(anchor, currentImageQuad, currentTexture);

		isAdding = false;
		showHideButtons(isAdding);
		fingerScale = 1f;

		ImageQuadUtil.SetCornersVisible(currentImageQuad, false);
		//turn on oscillation
		currentImageQuad.GetComponent<OscillatingRotation>().enabled = true;
	}

	//Returns placementDistance, or less if a detected surface (e.g. a wall) is closer than that
	private float GetPlacementDistance()
	{
		if (aRRaycastManager == null)
			return placementDistance;

		Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
		if (aRRaycastManager.Raycast(screenCenter, m_Hits, TrackableType.PlaneWithinPolygon))
		{
			// Raycast hits are sorted by distance, so the first one is the closest.
			float hitDistance = Vector3.Distance(Camera.main.transform.position, m_Hits[0].pose.position);
			if (hitDistance - surfaceOffset < placementDistance)
				return Mathf.Max(hitDistance - surfaceOffset, minPlacementDistance);
		}
		return placementDistance;
	}

	public void cancelSetting()
	{
		Destroy(currentImageQuad);
		isAdding = false;
		showHideButtons(isAdding);
		fingerScale = 1f;
	}
	
	private void showHideButtons(bool isAdding)
    {
		if (isAdding)
		{
			//show and hide the correct buttons
			addButton.gameObject.SetActive(false);
			okButton.gameObject.SetActive(true);
			cancelButton.gameObject.SetActive(true);
		}
		else
        {
            addButton.gameObject.SetActive(true);
			okButton.gameObject.SetActive(false);
			cancelButton.gameObject.SetActive(false);
        }
    }

}
