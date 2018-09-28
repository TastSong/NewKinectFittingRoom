// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Kinect/Body2ColorShader" {
    Properties {
        _BodyTex ("BodyTex (RGB)", 2D) = "white" {}
        _ColorTex ("ColorTex (RGB)", 2D) = "white" {}
        _GradientTex ("GradientTex (RGB)", 2D) = "white" {}
        _GradientColor ("GradientColor", Color) = (1, 1, 1, 1)
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
			uniform sampler2D _GradientTex;
			uniform fixed4 _GradientColor;


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

			fixed4 frag (v2f i) : COLOR
			{
				fixed playerA = tex2D(_BodyTex, i.uv).w;
				if(playerA > 0.8) playerA = 1.0;

				fixed gradientA = tex2D(_GradientTex, i.uv).w;
				//return tex2D(_GradientTex, i.uv);

				fixed4 clr = gradientA > 0.5 && _GradientColor.a > 0.0 ? _GradientColor : tex2D(_ColorTex, i.uv);
				clr.w = playerA;

				return clr;
			}

			ENDCG
		}
	}

	Fallback Off
}