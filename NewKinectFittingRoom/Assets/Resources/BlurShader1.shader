// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/BlurShader1" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_PixOffset ("Pixel Offset", Range(0, 10)) = 3
		_PixStep ("Pixel Step", Range(1, 5)) = 2
	}
	
	CGINCLUDE
	#include "UnityCG.cginc"
	
	sampler2D _MainTex;
	half4 _MainTex_TexelSize;

	int _PixOffset;
	int _PixStep;

	struct v2f_off {
		float4 pos : POSITION;
		half2 uvc : TEXCOORD0;
	};
	
	v2f_off vertOff (appdata_img v)
	{
		v2f_off o;

		o.pos = UnityObjectToClipPos (v.vertex);
		//o.uvc = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
		o.uvc = v.texcoord.xy;

		return o;
	}
	
	half4 fragOff (v2f_off i) : COLOR
	{
		int rcCount = 2 * _PixOffset + 1;
		half2 texUv = i.uvc + half2(_MainTex_TexelSize.x * -_PixOffset, _MainTex_TexelSize.y * -_PixOffset);

		half4 newCol = 0;
		int pixCount = 0;

		for (int iY = -_PixOffset; iY < rcCount; iY += _PixStep) 
		{
			for (int iX = -_PixOffset; iX < rcCount; iX += _PixStep)
			{
				half4 texCol = tex2D(_MainTex, texUv);
				newCol += texCol * 2.0 * texCol.a;
				pixCount += 1 + (int)texCol.a;

				texUv.x += _MainTex_TexelSize.x * _PixStep;
			}

			texUv.x = i.uvc.x + (_MainTex_TexelSize.x * -_PixOffset);
			texUv.y += _MainTex_TexelSize.y * _PixStep;
		}

		newCol = newCol / pixCount;

		return newCol;
	}
	
	ENDCG
	
	SubShader {
		ZTest Always Cull Off ZWrite Off
		pass {
			CGPROGRAM
			#pragma vertex vertOff
			#pragma fragment fragOff
//			#pragma target 3.0
			ENDCG
		}
	}
}
