Shader "Hidden/SceneViewOutline"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		CGINCLUDE
		#include "UnityCG.cginc"
		struct appdata_t
		{
			float4 pos : POSITION;
			float2 uv : TEXCOORD0;
		};
 
		struct v2f
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
		#if UNITY_UV_STARTS_AT_TOP
			float2 uv2 : TEXCOORD1;
		#endif
		};

		float4 _MainTex_TexelSize;
		float4 _MainTex_ST;
 
		v2f vertex(appdata_t i)
		{
			v2f o;
 
			o.pos = UnityObjectToClipPos(i.pos);
			o.uv = UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST);
		#if UNITY_UV_STARTS_AT_TOP
			o.uv2 = o.uv;
			if (_MainTex_TexelSize.y < 0.0)
				o.uv.y = 1.0 - o.uv.y;
		#endif
			return o;
		}
		ENDCG

		// No culling or depth
		// Cull Off ZWrite Off ZTest Always

		// Pass 0: Fill id
		Pass
		{
			Cull Off

			CGPROGRAM
			#pragma vertex vertex
			#pragma fragment fragment			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _Tex;
			fixed4 _ID;

			fixed4 fragment(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);			
				if (col.a < 0.1) discard;
				return _ID;
			}
			ENDCG
		}

		// Pass 1: Compare id
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
		   
			CGPROGRAM
			#pragma vertex vertex
			#pragma fragment fragment
			#pragma target 3.0
			#include "UnityCG.cginc"
 
			sampler2D _MainTex;

			static const half2 kOffsets[8] = {
				half2(-1,-1),
				half2(0,-1),
				half2(1,-1),
				half2(-1,0),
				half2(1,0),
				half2(-1,1),
				half2(0,1),
				half2(1,1)
			};

			fixed4 fragment(v2f i) : SV_Target
			{
				fixed4 col1 = tex2D(_MainTex, i.uv);
				if (all(col1.rg == 0))
					return fixed4(0, 0, 0, 0);

				fixed4 col = fixed4(1, 1, 1, 1);
				fixed4 col2;
				float id;
				[unroll(8)]
				for (int tap = 0; tap < 8; ++tap)
				{
					col2 = tex2D(_MainTex, i.uv + (kOffsets[tap] * _MainTex_TexelSize.xy));
					if (any(col2.rg != 0) && (any(col2.rg != col1.rg)))
						col = fixed4(0, 0, 0, 0);
				}
				return col;
			}
			ENDCG
		}

		// Pass 2: Blur outline
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off
		   
			CGPROGRAM
			#pragma vertex vertex
			#pragma fragment fragment
			#pragma target 3.0
			#include "UnityCG.cginc"
 
			float2 _Direction;
			sampler2D _MainTex;

			static const half kCurveWeights[9] = { 0.0204001988,0.0577929595,0.1215916882,0.1899858519,0.2204586031,0.1899858519,0.1215916882,0.0577929595,0.0204001988 };

			half4 fragment(v2f i) : SV_Target
			{
				float2 step = _MainTex_TexelSize.xy * _Direction;
				half4 col = 0;
				float2 uv = i.uv - step * 4;
				for (int tap = 0; tap < 9; ++tap)
				{
					col += tex2D(_MainTex, uv) * kCurveWeights[tap];
					uv += step;
				}
				return col;
			}
			ENDCG
		}

		// Pass 3: Combine outline
		Pass
		{
			ZTest Always
			Cull Off
			ZWrite Off

			CGPROGRAM
			#pragma vertex vertex
			#pragma fragment fragment			
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _OutlineMap;
			sampler2D _FillMap;
			fixed4 _OutlineColor;

			fixed4 fragment(v2f i) : SV_Target
			{
			#if UNITY_UV_STARTS_AT_TOP
				fixed4 col = tex2D(_MainTex, i.uv2);
			#else
				fixed4 col = tex2D(_MainTex, i.uv);
			#endif
				fixed4 oul = tex2D(_OutlineMap, i.uv);
				fixed4 fil = tex2D(_FillMap, i.uv);
				float c = oul.r > 0.05 ? 1 : 0;
				c = fil.r > 0 ? _OutlineColor.a : c;
				col.rgb = col.rgb * (1 - c) + _OutlineColor.rgb * c;
				col.a = 1;
				return col;
			}
			ENDCG
		}
	}
}
