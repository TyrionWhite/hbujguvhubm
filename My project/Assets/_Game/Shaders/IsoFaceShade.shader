// Stylized "illusory 3D" face shading for the isometric demo.
// Face brightness is decided by the world-space normal:
//   - upward faces (tops)   -> brightened by _TopBoost
//   - downward faces        -> darkest (_BottomShade)
//   - vertical side faces   -> shaded by the GLOBAL sun azimuth: sides facing
//     the sun get _SideShadeLit, sides facing away get _SideShadeDark. The sun
//     direction is published as the shader global _IsoSunDirWS by
//     GlobalLightController, so every object in the scene shades the same
//     sides consistently (e.g. the pit's inner walls are white like the ground
//     but read lighter/darker on the correct sides).
//
// Blend state is driven by properties so URPTransparencyUtil can flip an object
// between opaque and translucent at runtime (used by the staircase fade).
Shader "Custom/IsoFaceShade"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        _TopBoost     ("Top Brightness",        Range(1,2)) = 1.18
        _SideShadeLit ("Sun-Side Brightness",   Range(0,1)) = 0.85
        _SideShadeDark("Far-Side Brightness",   Range(0,1)) = 0.58
        _BottomShade  ("Bottom Brightness",     Range(0,1)) = 0.45

        // Set at runtime by URPTransparencyUtil for opaque/transparent toggling.
        [HideInInspector] _Surface  ("__surface", Float) = 0
        [HideInInspector] _Blend    ("__blend",   Float) = 0
        [HideInInspector] _SrcBlend ("__src",     Float) = 1
        [HideInInspector] _DstBlend ("__dst",     Float) = 0
        [HideInInspector] _ZWrite   ("__zw",      Float) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _TopBoost;
                float  _SideShadeLit;
                float  _SideShadeDark;
                float  _BottomShade;
                float  _Surface;
                float  _Blend;
                float  _SrcBlend;
                float  _DstBlend;
                float  _ZWrite;
            CBUFFER_END

            // Global sun travel direction (world space), set by GlobalLightController.
            float4 _IsoSunDirWS;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 n = normalize(IN.normalWS);
                half3 col = _BaseColor.rgb;

                if (n.y > 0.5)
                {
                    col *= _TopBoost;       // top face -> lighter
                }
                else if (n.y < -0.5)
                {
                    col *= _BottomShade;    // underside -> darkest
                }
                else
                {
                    // Vertical side: lit when the face normal opposes the sun's
                    // horizontal travel direction (i.e. the face looks toward
                    // where the light comes from).
                    float2 sunXZ = _IsoSunDirWS.xz;
                    if (dot(sunXZ, sunXZ) < 1e-4) sunXZ = float2(0.707, 0.707);
                    sunXZ = normalize(sunXZ);

                    float2 nXZ = normalize(n.xz);
                    float lit = saturate(dot(nXZ, -sunXZ) * 0.5 + 0.5);
                    col *= lerp(_SideShadeDark, _SideShadeLit, lit);
                }

                return half4(saturate(col), _BaseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
