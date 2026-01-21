Shader "Custom/RainbowGrassDefault" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Vector) = (1,1,1,1)
		[Toggle] PixelSnap ("Pixel snap", Float) = 0
		[HideInInspector] _RendererColor ("RendererColor", Vector) = (1,1,1,1)
		[HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
		[PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0

		_SwaySpeed ("SwaySpeed", Float) = 1
		_SwayAmount ("Sway Amount", Float) = 1
		_WorldOffset ("World Offset", Float) = 1
		_HeightOffset ("Height Offset", Float) = 0
		_ClampZ ("Clamp Z Position", Float) = 1
		[PerRendererData] _PushAmount ("Push Amount (Player)", Float) = 0
    	[PerRendererData] _SwayMultiplier ("Sway Multiplier (Extra)", Float) = 1

		[Toggle(PIN_TYPE)] _IsPinnedType ("Is Pinned Type", Float) = 0
		_MaxPinY ("Max Pin Y", Float) = 1
		_MinPinY ("Min Pin Y", Float) = -1
		_MaxPinX ("Max Pin X", Float) = 1
		_MinPinX ("Min Pin X", Float) = -1
		_PinMask ("Pin Mask", 2D) = "white"

		[Toggle(FRAMERATE_SNAPPING)] _EnableFramerateSnapping ("Enable Framerate Snapping", Float) = 0
		_SnappedFramerate ("Snapped Framerate", Float) = 12
		[Toggle(SWAP_XY)] _SwapXY ("Sway X & Y", Float) = 0 //sic

		_SaturationLerpEnabled ("Enable Saturation Lerp", Float) = 0
		_SaturationLerp ("Saturation Lerp", Float) = 1

		[HideInInspector] _MagnitudeMultA ("", Float) = 1
		[HideInInspector] _TimeMultA ("", Float) = 1
		[HideInInspector] _MagnitudeMultB ("", Float) = 1
		[HideInInspector] _TimeMultB ("", Float) = 1
		[HideInInspector] _MagnitudeMultC ("", Float) = 1
		[HideInInspector] _TimeMultC ("", Float) = 1

		_Phase ("Phase", Float) = 0
        _Frequency ("Frequency", Vector) = (0,0,0,0)
	}

	SubShader{
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "DisableBatching"="True"}
		Blend SrcAlpha OneMinusSrcAlpha
		Lighting Off
        Cull Off
		ZWrite Off

		Pass {
			CGPROGRAM

			#pragma vertex GrassVert
			#pragma fragment RainbowFrag
			#pragma target 2.0
			#pragma multi_compile_local _ PIXELSNAP_ON
			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			#pragma multi_compile _ FRAMERATE_SNAPPING
			#pragma multi_compile _ PIN_TYPE
			#pragma multi_compile _ SWAP_XY

			#include "Rainbow.cginc"
			#include "UnitySprites.cginc"
			#include "Grass.cginc"

			fixed4 RainbowFrag(fragData IN) : SV_Target
			{
				fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
				c.rgb = hueshift(IN.worldPos, c.rgb);
				return c;
			}
			ENDCG
		}
	}
	Fallback "Sprites/Diffuse"
}
