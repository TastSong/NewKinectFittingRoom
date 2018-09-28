// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Kinect/Depth2BodyShaderNN" {
    Properties {
        _BodyTex ("Body (RGB)", 2D) = "white" {}
    }
    
	SubShader {
		Pass {
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
		
			CGPROGRAM
			#pragma target 5.0
			//#pragma enable_d3d11_debug_symbols

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform sampler2D _BodyTex;
			
			uniform float _DepthResX;
			uniform float _DepthResY;

			struct v2f {
				float4 pos : SV_POSITION;
			    float2 uv : TEXCOORD0;
			};

			v2f vert (appdata_base v)
			{
				v2f o;
				
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.texcoord;
				
				return o;
			}

			float4 frag (v2f i) : COLOR
			{
				float2 di_uv;
				di_uv.x = 1.0 - i.uv.x - (1.0 / _DepthResX);
				di_uv.y = 1.0 - i.uv.y - (1.0 / _DepthResY);
				
				return tex2D(_BodyTex, di_uv);
			}

			ENDCG
		}
	}

	Fallback Off
}