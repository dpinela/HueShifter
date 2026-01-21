float _SwaySpeed;
float _SwayAmount;
float _WorldOffset;
float _HeightOffset;
float _ClampZ;
float _PushAmount;
float _SwayMultiplier;
float _MaxPinY;
float _MinPinY;
float _MaxPinX;
float _MinPinX;
sampler2D _PinMask;
float _SnappedFramerate;
float _MagnitudeMultA;
float _TimeMultA;
float _MagnitudeMultB;
float _TimeMultB;
float _MagnitudeMultC;
float _TimeMultC;

struct fragData
{
    float4 vertex   : SV_POSITION;
    fixed4 color    : COLOR;
    float2 texcoord : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

fragData GrassVert(appdata_full IN)
{
    fragData OUT;

    float phase = (unity_ObjectToWorld[0][3] + unity_ObjectToWorld[1][3] + unity_ObjectToWorld[2][3]);
    phase *= _WorldOffset;

    float time = _Time.y;
    if (FRAMERATE_SNAPPING) {
        time = time * _SnappedFramerate + 0.5;
        time = floor(time);
        time = time / _SnappedFramerate;
    }
    phase += time * _SwaySpeed;

    float3 sways = sin(phase * float3(_TimeMultA, _TimeMultB, _TimeMultC));
    float sway = dot(sways, float3(_MagnitudeMultA, _MagnitudeMultB, _MagnitudeMultC));
    sway *= _SwayAmount;
    sway *= _SwayMultiplier;
    sway += _PushAmount;


    float z = unity_ObjectToWorld[2][3];
    z = max(z, -_ClampZ);
    z = min(z, _ClampZ);
    z = abs(z) + 1;
    sway *= z;

    OUT.vertex = IN.vertex;

    if (PIN_TYPE) {
        float u = (_MaxPinX - IN.vertex.x) / (_MaxPinX - _MinPinX);
        float v = (_MaxPinY - IN.vertex.y) / (_MaxPinY - _MinPinY);
        sway *= tex2Dlod(_PinMask, float4(u, v, 0.0, 0.0)).r;
        if (SWAP_XY) {
            OUT.vertex.y += sway;
        } else {
            OUT.vertex.x += sway;
        }
    } else {
        float height = unity_ObjectToWorld[1][0] + unity_ObjectToWorld[1][1] + unity_ObjectToWorld[1][2];
        height *= _HeightOffset;
        height += IN.vertex.y;
        sway *= height;
        OUT.vertex.x += sway;
    }

    OUT.vertex = UnityFlipSprite(OUT.vertex, _Flip);
    OUT.vertex = UnityObjectToClipPos(OUT.vertex);
    #if defined(PIXELSNAP_ON)
        OUT.vertex = UnityPixelSnap (OUT.vertex);
    #endif

    OUT.color = IN.color * _Color * _RendererColor;
    OUT.texcoord = IN.texcoord;
    OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex);

    return OUT;
}
