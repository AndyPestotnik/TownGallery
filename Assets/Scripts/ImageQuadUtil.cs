using UnityEngine;

//Shared setup for double_image_quad prefab instances, used both when placing a new image and when loading a saved one
public static class ImageQuadUtil
{
	public static void ApplyTexture(GameObject doubleQuad, Texture2D texture)
	{
		//Set front quad material
		Material material = doubleQuad.transform.Find("front_quad").GetComponent<Renderer>().material;
		SetupTransparentMaterial(material, texture);

		//set rear quad material, reversed for rear texture
		Material rearMaterial = doubleQuad.transform.Find("rear_quad").GetComponent<Renderer>().material;
		SetupTransparentMaterial(rearMaterial, texture);
		Vector2 currentScale = rearMaterial.mainTextureScale;
		rearMaterial.mainTextureScale = new Vector2(-currentScale.x, currentScale.y);
	}

	public static void SetCornersVisible(GameObject doubleQuad, bool visible)
	{
		Transform cornersTransform = doubleQuad.transform.Find("corner_pieces");
		if (cornersTransform != null)
			cornersTransform.gameObject.SetActive(visible);
	}

	private static void SetupTransparentMaterial(Material material, Texture2D texture)
	{
		material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		material.SetInt("_ZWrite", 0);
		material.DisableKeyword("_ALPHATEST_ON");
		material.EnableKeyword("_ALPHABLEND_ON");
		material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
		material.renderQueue = 3000;

		if (!material.shader.isSupported) // happens when Standard shader is not included in the build
			material.shader = Shader.Find("Legacy Shaders/Diffuse");

		material.SetTexture("_MainTex", texture);
	}
}
