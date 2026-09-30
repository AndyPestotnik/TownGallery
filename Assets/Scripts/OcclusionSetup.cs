using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

//Hides AR images behind real-world surfaces.
//LiDAR devices: AROcclusionManager environment depth gives pixel-accurate occlusion (applied by ARCameraBackground).
//Other devices: each detected plane gets an invisible depth-only mesh, so detected walls/floors hide images behind them.
//Plane occluders turn off automatically whenever environment depth is active.
public class OcclusionSetup : MonoBehaviour
{
	public ARPlaneManager planeManager;
	public AROcclusionManager occlusionManager; // On the AR camera; optional
	public Shader occluderShader; // Custom/DepthOnlyOccluder

	private Material occluderMaterial;
	private bool usingEnvironmentDepth;

	void Awake()
	{
		if (occluderShader == null)
			occluderShader = Shader.Find("Custom/DepthOnlyOccluder"); // only works in builds if the shader is referenced somewhere
		occluderMaterial = new Material(occluderShader);
	}

	void OnEnable()
	{
		planeManager.trackablesChanged.AddListener(OnPlanesChanged);
	}

	void OnDisable()
	{
		planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
	}

	void Update()
	{
		bool depthActive = occlusionManager != null && occlusionManager.enabled
			&& occlusionManager.currentEnvironmentDepthMode != EnvironmentDepthMode.Disabled;

		if (depthActive != usingEnvironmentDepth)
		{
			usingEnvironmentDepth = depthActive;
			//All plane occluders share this material, so this switches them all on/off at once
			occluderMaterial.SetFloat("_ZWrite", usingEnvironmentDepth ? 0f : 1f);
			Debug.Log(usingEnvironmentDepth ? "Using LiDAR depth occlusion" : "Using plane occlusion");
		}
	}

	private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
	{
		foreach (ARPlane plane in args.added)
			AddOccluder(plane);
	}

	private void AddOccluder(ARPlane plane)
	{
		if (plane.GetComponent<MeshRenderer>() != null)
			return; // plane prefab already has its own renderer

		plane.gameObject.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = plane.gameObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = occluderMaterial;
		meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
		meshRenderer.receiveShadows = false;

		//Builds and updates the mesh from the plane's boundary as ARKit refines it
		plane.gameObject.AddComponent<ARPlaneMeshVisualizer>();
	}
}
