// Invisible material that only writes depth, so AR content behind it (e.g. behind a detected wall) is hidden.
// Draws before regular geometry and transparent images (queue 3000) so they depth-test against it.
Shader "Custom/DepthOnlyOccluder"
{
	Properties
	{
		// Pushes the occluder this many meters away from the camera, so images placed just in front of
		// a slightly misestimated wall don't get clipped
		_PushBack ("Push Back (m)", Float) = 0.03
		// Set to 0 at runtime to turn occluders off (e.g. when LiDAR depth occlusion is active)
		[Toggle] _ZWrite ("ZWrite", Float) = 1
	}
	SubShader
	{
		Tags { "Queue" = "Geometry-10" "RenderType" = "Opaque" }

		Pass
		{
			ZWrite [_ZWrite]
			ColorMask 0
			Cull Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float _PushBack;

			struct appdata
			{
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert (appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 viewPos = UnityObjectToViewPos(v.vertex);
				viewPos += normalize(viewPos) * _PushBack;
				o.pos = mul(UNITY_MATRIX_P, float4(viewPos, 1.0));
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return 0;
			}
			ENDCG
		}
	}
}
