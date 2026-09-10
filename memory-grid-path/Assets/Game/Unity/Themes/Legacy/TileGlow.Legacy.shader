Shader "Nixin Studio/TileGlowLegacy"
{
    Properties
    {
        _EdgeColor ("Edge Color", Color) = (0.08, 0.09, 0.12, 1)
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Intensity ("Glow Intensity", Float) = 1
        _Falloff ("Edge Falloff", Float) = 2.2
        _Border ("Border Width", Range(0, 0.2)) = 0.028
        _Alpha ("Alpha", Range(0, 1)) = 1
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
            Name "TileGlowUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _EdgeColor;
                float4 _GlowColor;
                float _Intensity;
                float _Falloff;
                float _Border;
                float _Alpha;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 centred = abs(input.uv - 0.5) * 2.0;
                float edgeDistance = max(centred.x, centred.y);
                float border = max(_Border, 0.02);
                float fill = 1.0 - smoothstep(1.0 - border, 1.0 - border + 0.008, edgeDistance);

                float glow = pow(saturate(1.0 - edgeDistance), max(_Falloff, 1.2));
                half3 fillColour = _GlowColor.rgb * _Intensity;
                fillColour = lerp(fillColour, fillColour * 1.28, glow * 0.5);
                half3 colour = lerp(_EdgeColor.rgb, fillColour, fill);
                return half4(colour, _Alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
