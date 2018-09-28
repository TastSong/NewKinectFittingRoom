// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/BlurShader3" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
	    _BlurSizeXY("BlurSizeXY", Range(0,10)) = 0
    }
    
    SubShader {

        // Render the object with the texture generated above
        Pass {

			CGPROGRAM
			#pragma vertex vert1
			#pragma fragment frag1
			#pragma target 3.0
    
            sampler2D _MainTex;
            float _BlurSizeXY;

			struct data {
			    float4 vertex : POSITION;
			    float3 normal : NORMAL;
			};

			struct v2f {
			    float4 position : POSITION;
			    float4 screenPos : TEXCOORD0;
			};

			v2f vert1(data i)
			{
			    v2f o;
			    o.position = UnityObjectToClipPos(i.vertex);
			    o.screenPos = o.position;

			    return o;
			}

			half4 frag1( v2f i ) : COLOR
			{
			    float2 screenPos = i.screenPos.xy / i.screenPos.w;
				float depth = _BlurSizeXY * 0.0005;

			    screenPos.x = (screenPos.x + 1) * 0.5;
			    screenPos.y = 1 - (screenPos.y + 1) * 0.5;

				//horizontal 
			    half4 sum = half4(0.0h,0.0h,0.0h,0.0h);  
			    
			    sum += tex2D( _MainTex, float2(screenPos.x-5.0 * depth, screenPos.y )) * 0.025;    
			    sum += tex2D( _MainTex, float2(screenPos.x+5.0 * depth, screenPos.y )) * 0.025;
			    
			    sum += tex2D( _MainTex, float2(screenPos.x-4.0 * depth, screenPos.y)) * 0.05;
			    sum += tex2D( _MainTex, float2(screenPos.x+4.0 * depth, screenPos.y)) * 0.05;

			    
			    sum += tex2D( _MainTex, float2(screenPos.x-3.0 * depth, screenPos.y)) * 0.09;
			    sum += tex2D( _MainTex, float2(screenPos.x+3.0 * depth, screenPos.y)) * 0.09;
			    
			    sum += tex2D( _MainTex, float2(screenPos.x-2.0 * depth, screenPos.y)) * 0.12;
			    sum += tex2D( _MainTex, float2(screenPos.x+2.0 * depth, screenPos.y)) * 0.12;
			    
			    sum += tex2D( _MainTex, float2(screenPos.x-1.0 * depth, screenPos.y)) *  0.15;
			    sum += tex2D( _MainTex, float2(screenPos.x+1.0 * depth, screenPos.y)) *  0.15;
			    
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y)) *  0.16;
			        
				return sum / 2;
			}

			ENDCG
        }
        
        Pass {
        Blend One One

			CGPROGRAM
			#pragma vertex vert2
			#pragma fragment frag2 
			#pragma target 3.0

            sampler2D _MainTex;
            float _BlurSizeXY;

			struct data {
			    float4 vertex : POSITION;
			    float3 normal : NORMAL;
			};

			struct v2f {
			    float4 position : POSITION;
			    float4 screenPos : TEXCOORD0;
			};

			v2f vert2(data i)
			{
			    v2f o;
			    o.position = UnityObjectToClipPos(i.vertex);
			    o.screenPos = o.position;

			    return o;
			}

			half4 frag2( v2f i ) : COLOR
			{
			    float2 screenPos = i.screenPos.xy / i.screenPos.w;
				float depth = _BlurSizeXY * 0.0005;

			    screenPos.x = (screenPos.x + 1) * 0.5;
			    screenPos.y = 1 - (screenPos.y + 1) * 0.5;
			    
			    //vertical
			    half4 sum = half4(0.0h,0.0h,0.0h,0.0h);
			    
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y+5.0 * depth)) * 0.025;    
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y-5.0 * depth)) * 0.025;
			    
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y+4.0 * depth)) * 0.05;
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y-4.0 * depth)) * 0.05;

			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y+3.0 * depth)) * 0.09;
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y-3.0 * depth)) * 0.09;
			    
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y+2.0 * depth)) * 0.12;
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y-2.0 * depth)) * 0.12;
			    
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y+1.0 * depth)) *  0.15;
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y-1.0 * depth)) *  0.15;	
			    
			    sum += tex2D( _MainTex, float2(screenPos.x, screenPos.y)) *  0.16;

				return sum / 2;
			}

			ENDCG
        }

    }

	Fallback Off
}

