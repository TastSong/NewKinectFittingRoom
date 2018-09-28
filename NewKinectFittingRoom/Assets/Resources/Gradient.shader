// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Gradient" 
{
	Properties
	{
		_MainTex ("_MainTex", 2D) = "white" {}
		_ErodeTex ("_ErodeTex", 2D) = "white" {}
	}

	SubShader 
	{

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform sampler2D _ErodeTex;

			float4 _MainTex_ST; 


			struct v2f 
			{
			   float4 pos : SV_POSITION;
			   float2 uv : TEXCOORD0;
			};

			v2f vert (appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : COLOR
			{
				fixed4 texMain = tex2D(_MainTex, i.uv);
				fixed4 texErode = tex2D(_ErodeTex, i.uv);

				fixed4 texGradient = fixed4(texMain.rgb, max(texMain.a - texErode.a, 0.0));
				//if(texGradient.a < 0.2) texGradient.a = 0.0;

				return texGradient;
			}
			ENDCG

		}
	}
}
