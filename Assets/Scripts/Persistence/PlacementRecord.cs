using System;

//One saved image placement. Field names match the columns of the "placements" table (see Backend/supabase_schema.sql).
//Pose/scale are the image's LOCAL transform relative to its cloud anchor, not Unity world space.
[Serializable]
public class PlacementRecord
{
	public string cloud_anchor_id;
	public string image_url;
	public string anchor_expires_at; // ISO 8601; cloud anchors can't be resolved after this

	public double lat;
	public double lon;
	public float gps_accuracy;
	public float heading;

	public float pos_x, pos_y, pos_z;
	public float rot_x, rot_y, rot_z, rot_w;
	public float scale_x, scale_y, scale_z;
}

//JsonUtility can't parse top-level arrays, so query results get wrapped in this
[Serializable]
public class PlacementRecordList
{
	public PlacementRecord[] items;
}
