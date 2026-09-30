using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

//Talks to Supabase: image files go to Storage, placement rows go to the "placements" table via its REST API.
//The anon key is safe to ship in the app; access is controlled by the row level security policies in Backend/supabase_schema.sql.
public class SupabasePlacementApi : MonoBehaviour
{
	public string supabaseUrl = "https://YOUR-PROJECT.supabase.co";
	public string anonKey = ""; // Publishable key (sb_publishable_...) or legacy anon key. Never the secret/service_role key
	public string imageBucket = "placement-images";

	[Serializable]
	private class NearbyQuery
	{
		public double user_lat;
		public double user_lon;
		public float radius_m;
	}

	public IEnumerator UploadImage(byte[] png, Action<string> onSuccess, Action<string> onError)
	{
		string path = Guid.NewGuid() + ".png";
		using (UnityWebRequest req = new UnityWebRequest($"{supabaseUrl}/storage/v1/object/{imageBucket}/{path}", "POST"))
		{
			req.uploadHandler = new UploadHandlerRaw(png);
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Content-Type", "image/png");
			AddAuthHeaders(req);

			yield return req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				onError($"Image upload failed: {req.error} {req.downloadHandler.text}");
				yield break;
			}
			onSuccess($"{supabaseUrl}/storage/v1/object/public/{imageBucket}/{path}");
		}
	}

	public IEnumerator InsertPlacement(PlacementRecord record, Action onSuccess, Action<string> onError)
	{
		using (UnityWebRequest req = PostJson($"{supabaseUrl}/rest/v1/placements", JsonUtility.ToJson(record)))
		{
			req.SetRequestHeader("Prefer", "return=minimal");

			yield return req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				onError($"Insert failed: {req.error} {req.downloadHandler.text}");
				yield break;
			}
			onSuccess();
		}
	}

	public IEnumerator QueryNearby(double lat, double lon, float radiusMeters, Action<PlacementRecord[]> onSuccess, Action<string> onError)
	{
		NearbyQuery query = new NearbyQuery { user_lat = lat, user_lon = lon, radius_m = radiusMeters };
		using (UnityWebRequest req = PostJson($"{supabaseUrl}/rest/v1/rpc/nearby_placements", JsonUtility.ToJson(query)))
		{
			yield return req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				onError($"Nearby query failed: {req.error} {req.downloadHandler.text}");
				yield break;
			}
			PlacementRecordList list = JsonUtility.FromJson<PlacementRecordList>("{\"items\":" + req.downloadHandler.text + "}");
			onSuccess(list.items ?? new PlacementRecord[0]);
		}
	}

	public IEnumerator DownloadImage(string url, Action<Texture2D> onSuccess, Action<string> onError)
	{
		using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
		{
			yield return req.SendWebRequest();

			if (req.result != UnityWebRequest.Result.Success)
			{
				onError($"Image download failed: {req.error}");
				yield break;
			}
			onSuccess(DownloadHandlerTexture.GetContent(req));
		}
	}

	private UnityWebRequest PostJson(string url, string json)
	{
		UnityWebRequest req = new UnityWebRequest(url, "POST");
		req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
		req.downloadHandler = new DownloadHandlerBuffer();
		req.SetRequestHeader("Content-Type", "application/json");
		AddAuthHeaders(req);
		return req;
	}

	private void AddAuthHeaders(UnityWebRequest req)
	{
		req.SetRequestHeader("apikey", anonKey);
		//Legacy anon keys are JWTs and also go in the Authorization header; newer publishable keys (sb_publishable_...) don't
		if (anonKey.StartsWith("eyJ"))
			req.SetRequestHeader("Authorization", "Bearer " + anonKey);
	}
}
