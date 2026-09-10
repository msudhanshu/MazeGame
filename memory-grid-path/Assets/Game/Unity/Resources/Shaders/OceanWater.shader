Shader "Nixin Studio/OceanWater"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.05, 0.32, 0.46, 1)
        _ShallowColor ("Shallow Color", Color) = (0.16, 0.52, 0.58, 1)
        _HighlightColor ("Highlight Color", Color) = (0.42, 0.78, 0.82, 1)
        _WaveScale ("Wave Scale", Float) = 0.28
        _WaveSpeed ("Wave Speed", Float) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite On
        Cull Off

        Pass
        {
            Name "OceanWater"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _HighlightColor;
                float _WaveScale;
                float _WaveSpeed;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float t = _TimeParameters.y * _WaveSpeed;
                float2 p = input.positionWS.xz * _WaveScale;

                float swell =
                    sin(dot(p, float2(1.00, 0.22)) * 2.4 + t) +
                    0.55 * sin(dot(p, float2(-0.65, 0.92)) * 3.6 - t * 1.05) +
                    0.28 * sin(dot(p, float2(0.40, -1.00)) * 6.2 + t * 0.62);
                swell *= 0.45;

                float ripples =
                    sin(dot(p, float2(0.90, 0.40)) * 9.0 + t * 1.4) *
                    sin(dot(p, float2(-0.35, 1.00)) * 8.2 - t * 1.15);
                float highlight = pow(saturate(0.52 + 0.48 * swell), 5.0);
                highlight += saturate(ripples) * 0.06;

                half3 colour = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(0.5 + swell * 0.35));
                colour = lerp(colour, _HighlightColor.rgb, highlight * 0.22);
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
