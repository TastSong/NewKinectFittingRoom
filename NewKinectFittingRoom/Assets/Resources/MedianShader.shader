﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*
5x5 Median
 
GLSL 1.0
Morgan McGuire and Kyle Whitson, 2006
Williams College
http://graphics.cs.williams.edu
 
Copyright (c) Morgan McGuire and Williams College, 2006
All rights reserved.
 
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are
met:
 
Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.
 
Redistributions in binary form must reproduce the above copyright
notice, this list of conditions and the following disclaimer in the
documentation and/or other materials provided with the distribution.
 
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
 
Shader "Custom/Median5x5"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Amount ("_Amount", Float) = 1.0
    }
       
    SubShader
    {
        //Tags { "Queue"="Transparent" "RenderType" = "Opaque" }
    
        //Cull Off
        //Blend SrcAlpha OneMinusSrcAlpha 
        
        Pass
        {
            ZTest Off Cull Off ZWrite Off
            Fog { Mode off }           
           
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma fragmentoption ARB_precision_hint_fastest
            //#pragma Lambert alpha
           
            #include "UnityCG.cginc"
                       
            #define s2(a, b)                            temp = a; a = min(a, b); b = max(temp, b);
            #define t2(a, b)                            s2(v[a], v[b]);
            #define t24(a, b, c, d, e, f, g, h)         t2(a, b); t2(c, d); t2(e, f); t2(g, h);
            #define t25(a, b, c, d, e, f, g, h, i, j)   t24(a, b, c, d, e, f, g, h); t2(i, j);
   
            uniform sampler2D   _MainTex;
            uniform float4      _MainTex_ST;
            uniform float      _Amount;
                           
            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };
   
           
            v2f vert (appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord.xy, _MainTex); //MultiplyUV (UNITY_MATRIX_TEXTURE0, uv);
                return o;
            }
       
       
            float4 frag (v2f i) : COLOR
            {
                float2 Tinvsize = float2 (_Amount / _ScreenParams.x, _Amount / _ScreenParams.y);               
                float4 v[25];
               
                // Add the pixels which make up our window to the pixel array.
                for(int dX = -2; dX <= 2; ++dX)
                {
                    for(int dY = -2; dY <= 2; ++dY)
                    {
                        float2 offset = float2(float(dX), float(dY));
                       
                        // If a pixel in the window is located at (x+dX, y+dY), put it at index (dX + R)(2R + 1) + (dY + R) of the pixel array.
                        // This will fill the pixel array, with the top left pixel of the window at pixel[0] and the bottom right pixel of the window at pixel[N-1].
                        v[(dX + 2) + (dY + 2) * 5] = tex2D(_MainTex, i.uv + offset * Tinvsize);
                    }
                }
               
                float4 temp;
               
                t25(0, 1,       3, 4,       2, 4,       2, 3,       6, 7);
                t25(5, 7,       5, 6,       9, 7,       1, 7,       1, 4);
                t25(12, 13,     11, 13,     11, 12,     15, 16,     14, 16);
                t25(14, 15,     18, 19,     17, 19,     17, 18,     21, 22);
                t25(20, 22,     20, 21,     23, 24,     2, 5,       3, 6);
                t25(0, 6,       0, 3,       4, 7,       1, 7,       1, 4);
                t25(11, 14,     8, 14,      8, 11,      12, 15,     9, 15);
                t25(9, 12,      13, 16,     10, 16,     10, 13,     20, 23);
                t25(17, 23,     17, 20,     21, 24,     18, 24,     18, 21);
                t25(19, 22,     8, 17,      9, 18,      0, 18,      0, 9);
                t25(10, 19,     1, 19,      1, 10,      11, 20,     2, 20);
                t25(2, 11,      12, 21,     3, 21,      3, 12,      13, 22);
                t25(4, 22,      4, 13,      14, 23,     5, 23,      5, 14);
                t25(15, 24,     6, 24,      6, 15,      7, 16,      7, 19);
                t25(3, 11,      5, 17,      11, 17,     9, 17,      4, 10);
                t25(6, 12,      7, 14,      4, 6,       4, 7,       12, 14);
                t25(10, 14,     6, 7,       10, 12,     6, 10,      6, 17);
                t25(12, 17,     7, 17,      7, 10,      12, 18,     7, 12);
                t24(10, 18,     12, 20,     10, 20,     10, 12);
               
                return v[12];
            }  
            ENDCG
        }
    }
   
    FallBack "Diffuse"
}