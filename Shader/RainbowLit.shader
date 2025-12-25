// Based on unity's Sprites-Diffuse.shader

/*
Copyright (c) 2016 Unity Technologies

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

Shader "Custom/RainbowLit"
{
    Properties
    {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor ("RendererColor", Vector) = (1,1,1,1)
		[HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        
		_Phase ("Phase", Float) = 0
        _Frequency ("Frequency", Vector) = (0,0,0,0) 
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		Lighting Off
        Cull Off
		ZWrite Off

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert nofog nolightmap nodynlightmap keepalpha noinstancing
        #pragma target 2.0
        #pragma multi_compile_local _ PIXELSNAP_ON
        #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
        
        #include "Rainbow.cginc"
        #include "UnitySprites.cginc"

        struct Input
        {
            float2 uv_MainTex;
            fixed4 color;
        	fixed3 worldPos;
        };

        void vert (inout appdata_full v, out Input o)
        {
            v.vertex = UnityFlipSprite(v.vertex, _Flip);
        	// o.worldPos = mul(unity_ObjectToWorld, v.vertex);

            #if defined(PIXELSNAP_ON)
            v.vertex = UnityPixelSnap (v.vertex);
            #endif

            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.color = v.color * _Color * _RendererColor;
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 c = SampleSpriteTexture (IN.uv_MainTex) * IN.color;
			// Unity's built in shader seems to multiply rgb by alpha here.
			// HK's shaders don't I think?
            o.Albedo = hueshift(IN.worldPos, c.rgb);
            o.Alpha = c.a;
        }

        ENDCG
    }
}
