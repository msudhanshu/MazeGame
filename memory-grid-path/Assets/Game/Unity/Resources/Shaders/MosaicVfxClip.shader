Shader "Nixin Studio/MosaicVfxClip"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ClipCenter ("Clip Center", Vector) = (0, 0, 0, 0)
        _ClipExtents ("Clip Extents", Vector) = (1, 1, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "MosaicVfxClip"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _ClipCenter;
                float4 _ClipExtents;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color * _BaseColor;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float3 extra = abs(input.positionWS - _ClipCenter.xyz) - _ClipExtents.xyz;
                clip(-max(extra.x, max(extra.y, extra.z)));

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                return tex * input.color;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
