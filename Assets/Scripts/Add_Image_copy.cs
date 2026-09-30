// using UnityEngine;
// using System.IO;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using System.Collections.Generic;
// using UnityEngine.UI; // Required for Button component


// public class Add_Image : MonoBehaviour
// {
// 	public ARAnchorManager anchorManager;
// 	public ARRaycastManager aRRaycastManager;
// 	public ARPlaneManager m_PlaneManager; // Assign in Inspector

// 	public GameObject imageQuad;

// 	private GameObject currentImageQuad;
// 	bool isAdding = false;

// 	private float fingerScale = 1f;
// 	private float aspectRatio = 1f;

// 	public float maxScale = 5f;
// 	public float minScale = 0.2f;
// 	public float scaleSpeed = 0.002f;

// 	private List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();

// 	//UI elements

// 	public Button addButton;
// 	public Button okButton;
// 	public Button cancelButton;

	 
// 	// Start is called once before the first execution of Update after the MonoBehaviour is created
// 	void Start()
// 	{

// 	}

// 	// Update is called once per frame
// 	void Update()
// 	{
// 		if(isAdding)
// 		{
// 			//Get zoom scale from fingers
// 			if (Input.touchCount == 2)
//         {
//             Touch tZero = Input.GetTouch(0);
//             Touch tOne = Input.GetTouch(1);

//             Vector2 tZeroPrevious = tZero.position - tZero.deltaPosition;
//             Vector2 tOnePrevious = tOne.position - tOne.deltaPosition;

//             float oldTouchDistance = Vector2.Distance(tZeroPrevious, tOnePrevious);
//             float currentTouchDistance = Vector2.Distance(tZero.position, tOne.position);

//             float deltaDistance = currentTouchDistance - oldTouchDistance;

//             fingerScale += deltaDistance * scaleSpeed;
//             fingerScale = Mathf.Clamp(fingerScale, minScale, maxScale);
//         }

//             if (currentImageQuad != null)
// 			{
// 				currentImageQuad.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2.5f;
// 				currentImageQuad.transform.forward = Camera.main.transform.forward;
// 				currentImageQuad.transform.localScale = new Vector3(1f, aspectRatio, 1f) * fingerScale;
//             }
//         }
// 	}

// 	public void PickImage(int maxSize)
// 	{
// 		NativeGallery.GetImageFromGallery((path) =>
// 		{
// 			Debug.Log("Image path: " + path);
// 			if (path != null)
// 			{


// 				if (!File.Exists(path))
// 				{
// 					Debug.LogError("Image file not found at: " + path);
// 					return;
// 				}

// 				// // Create a new Texture2D
// 				Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize, false);
// 				aspectRatio = texture.height / (float)texture.width;

// 				// This simulates the 'Alpha is Transparency' setting behavior
// 				Color[] pixels = texture.GetPixels();
// 				for (int i = 0; i < pixels.Length; i++)
// 				{
// 					// If a pixel's alpha is very close to 0, make its color data undefined (black)
// 					// This helps prevent filtering artifacts at the edges of transparent areas
// 					if (Mathf.Approximately(pixels[i].a, 0f))
// 					{
// 						pixels[i] = new Color(0, 0, 0, 0); // Set to transparent black
// 					}
// 				}
// 				texture.SetPixels(pixels);
// 				texture.Apply(); // Apply changes to the texture

// 				if (texture == null)
// 				{
// 					Debug.Log("Couldn't load texture from " + path);
// 					return;
// 				}

// 				isAdding = true;

// 				showHideButtons(isAdding);

// 				//Create a Quad with the new texture
// 				// GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
// 				GameObject quad = Instantiate(imageQuad);
// 				quad.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2.5f;
// 				quad.transform.forward = Camera.main.transform.forward;
// 				quad.transform.localScale = new Vector3(1f, texture.height / (float)texture.width, 1f);

// 				Material material = quad.GetComponent<Renderer>().material;

// 				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
// 				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
// 				material.SetInt("_ZWrite", 0);
// 				material.DisableKeyword("_ALPHATEST_ON");
// 				material.EnableKeyword("_ALPHABLEND_ON");
// 				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
// 				material.renderQueue = 3000;


// 				if (!material.shader.isSupported) // happens when Standard shader is not included in the build
// 					material.shader = Shader.Find("Legacy Shaders/Diffuse");

// 				// material.mainTexture = texture;
// 				material.SetTexture("_MainTex", texture);
// 				//set current image as quad
// 				currentImageQuad = quad;
// 				currentImageQuad.GetComponent<OscillatingRotation>().enabled = false;
// 			}
// 		});
// 	}

// 	public void setQuadPosition()
// 	{
// 		//Look for plane to anchor to
// 		if (aRRaycastManager.Raycast(new Vector2(Camera.main.transform.forward.x, Camera.main.transform.forward.y), m_Hits, TrackableType.Planes))
// 		{
// 			// Raycast hits are sorted by distance, so the first one is the closest.
// 			ARRaycastHit hit = m_Hits[0];

// 			// Check if the hit is a plane
// 			if ((hit.hitType & TrackableType.Planes) != 0)
// 			{
// 				// Get the ARPlane using the ARPlaneManager and the hit's trackableId
// 				ARPlane plane = m_PlaneManager.GetPlane(hit.trackableId);

// 				if (plane != null)
// 				{
// 					Debug.Log($"Hit a plane with alignment: {plane.alignment}");
// 					ARAnchor newAnchor = anchorManager.AttachAnchor(plane, hit.pose);
// 					currentImageQuad.transform.SetParent(newAnchor.transform);
// 					currentImageQuad.GetComponent<OscillatingRotation>().enabled = true;
// 					// Now you can work with the 'plane' object
// 				}
// 			}
// 		}
// 		else

// 		{
// 			//just add anchor directly to object if no plane available
// 			ARAnchor otherAnchor = currentImageQuad.AddComponent<ARAnchor>();
// 		}
// 		isAdding = false;
// 		showHideButtons(isAdding);
// 		fingerScale = 1f;
// 	}

// 	public void cancelSetting()
// 	{
// 		Destroy(currentImageQuad);
// 		isAdding = false;
// 		showHideButtons(isAdding);
// 		fingerScale = 1f;
// 	}
	
// 	private void showHideButtons(bool isAdding)
//     {
// 		if (isAdding)
// 		{
// 			//show and hide the correct buttons
// 			addButton.gameObject.SetActive(false);
// 			okButton.gameObject.SetActive(true);
// 			cancelButton.gameObject.SetActive(true);
// 		}
// 		else
//         {
//             addButton.gameObject.SetActive(true);
// 			okButton.gameObject.SetActive(false);
// 			cancelButton.gameObject.SetActive(false);
//         }
//     }

// }
