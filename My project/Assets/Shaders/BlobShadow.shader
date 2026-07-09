// Flat, always-transparent unlit shader used only for the projected ground
// shadows. It is unconditionally transparent (no runtime opaque/transparent
// toggling), double-sided, and never writes depth - so a projected shadow
// always reads as a flat translucent decal instead of a solid black shape.
//
// The stencil block makes overlapping shadows draw each pixel exactly ONCE:
// the first shadow fragment marks the stencil bit, and any later shadow
// fragment on the same pixel fails the NotEqual test. Overlapping shadows
// from different objects therefore never stack into a darker patch.
Shader "Custom/BlobShadow"
{
    Properties
    {
        [MainColor] _BaseColor ("Color", Color) = (0,0,0,0.33)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            Stencil
            {
                Ref 128
                ReadMask 128
                WriteMask 128
                Comp NotEqual
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
