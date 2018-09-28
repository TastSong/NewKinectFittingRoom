﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Kinect/Body2DepthShaderInvNN" {
    Properties {
        _BodyTex ("Body (RGB)", 2D) = "white" {}
        _ColorTex ("Color (RGB)", 2D) = "white" {}
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
			uniform sampler2D _ColorTex;
			
			uniform float _ColorResX;
			uniform float _ColorResY;
			uniform float _DepthResX;
			uniform float _DepthResY;

			StructuredBuffer<float2> _ColorCoords;

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
				float player = tex2D(_BodyTex, i.uv).w;
				float2 ci_uv;
				
				if (player < 0.1)
				{
					int dx = (int)(_DepthResX - i.uv.x * _DepthResX);
					int dy = (int)(_DepthResY - i.uv.y * _DepthResY);
					int di = (int)(dx + dy * _DepthResX);
					
					if (!isinf(_ColorCoords[di].x) && !isinf(_ColorCoords[di].y))
					{
						float ci_index, ci_length;
						ci_index = _ColorCoords[di].x + (_ColorCoords[di].y * _ColorResX);
						ci_length = _ColorResX * _ColorResY;
					
						if(ci_index >= 0 && ci_index < ci_length)
						{
							ci_uv.x = (_ColorResX - _ColorCoords[di].x - 1) / _ColorResX;
							ci_uv.y = (_ColorResY - _ColorCoords[di].y - 1) / _ColorResY;
							
							float4 clr = tex2D (_ColorTex, ci_uv);
							clr.w = 1;
							return clr;
						}
					}

					ci_uv = i.uv;
					return tex2D (_ColorTex, ci_uv);
				}
				
				return float4(0, 0, 0, 0);
			}

			ENDCG
		}
	}

	Fallback Off
}