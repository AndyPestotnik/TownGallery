using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

//Menu command that adds the PlacementSystem object (location, Supabase, cloud anchors) to the open scene
//and wires it to Add_Image. Safe to run again; it reuses an existing PlacementSystem.
//Supabase URL and key still need to be pasted into SupabasePlacementApi by hand.
public static class PlacementSystemSetup
{
	[MenuItem("Tools/Capstone AR/Set Up Placement System")]
	private static void SetUp()
	{
		Add_Image addImage = Object.FindFirstObjectByType<Add_Image>(FindObjectsInactive.Include);
		ARAnchorManager anchorManager = Object.FindFirstObjectByType<ARAnchorManager>(FindObjectsInactive.Include);
		if (addImage == null || anchorManager == null)
		{
			EditorUtility.DisplayDialog("Set Up Placement System",
				"Couldn't find Add_Image and/or ARAnchorManager in the open scene. Open AR_Scene and try again.", "OK");
			return;
		}

		CloudAnchorPlacementManager existing = Object.FindFirstObjectByType<CloudAnchorPlacementManager>(FindObjectsInactive.Include);
		GameObject go;
		if (existing != null)
		{
			go = existing.gameObject;
		}
		else
		{
			go = new GameObject("PlacementSystem");
			Undo.RegisterCreatedObjectUndo(go, "Create PlacementSystem");
		}

		LocationProvider location = GetOrAdd<LocationProvider>(go);
		SupabasePlacementApi api = GetOrAdd<SupabasePlacementApi>(go);
		CloudAnchorPlacementManager manager = GetOrAdd<CloudAnchorPlacementManager>(go);

		Undo.RecordObject(manager, "Wire PlacementSystem");
		manager.anchorManager = anchorManager;
		manager.locationProvider = location;
		manager.api = api;
		manager.double_image_quad = addImage.double_image_quad;

		Undo.RecordObject(addImage, "Wire PlacementSystem");
		addImage.placementManager = manager;
		PrefabUtility.RecordPrefabInstancePropertyModifications(addImage);

		EditorSceneManager.MarkSceneDirty(go.scene);
		Selection.activeGameObject = go;
		EditorGUIUtility.PingObject(api);

		Debug.Log("PlacementSystem set up. Paste your Supabase URL and publishable key into Supabase Placement Api, then save the scene.");
	}

	private static T GetOrAdd<T>(GameObject go) where T : Component
	{
		T component = go.GetComponent<T>();
		return component != null ? component : Undo.AddComponent<T>(go);
	}
}
