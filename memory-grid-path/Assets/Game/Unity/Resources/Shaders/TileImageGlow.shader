Shader "Nixin Studio/TileImageGlow"
{
    Properties
    {
        _MainTex ("Arena Image", 2D) = "white" {}
        _EdgeColor ("Edge Color", Color) = (0.02, 0.02, 0.04, 1)
        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _Intensity ("Glow Intensity", Float) = 1
        _Falloff ("Edge Falloff", Float) = 2.2
        _Alpha ("Alpha", Range(0, 1)) = 0.9
        _ImageStrength ("Image Strength", Range(0, 1)) = 1
        _UVRect ("UV Rect", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "TileImageGlowUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

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
                float4 _UVRect;
                float _Intensity;
                float _Falloff;
                float _Alpha;
                float _ImageStrength;
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
                float2 boardUv = lerp(_UVRect.xy, _UVRect.zw, input.uv);
                half3 image = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, boardUv).rgb;

                float2 centred = abs(input.uv - 0.5) * 2.0;
                float edgeDistance = max(centred.x, centred.y);
                float glow = pow(saturate(1.0 - edgeDistance), _Falloff);

                half3 baseColour = lerp(_EdgeColor.rgb, image, _ImageStrength);
                half3 colour = lerp(baseColour, _GlowColor.rgb * _Intensity, glow);
                half alpha = _Alpha * saturate(0.35 + 0.65 * glow);

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
