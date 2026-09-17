Shader "Nixin Studio/ScoutFogPadding"
{
    Properties
    {
        _MainTex ("Clouds", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _ArenaOrigin ("Arena Origin", Vector) = (0, 0, 0, 0)
        _ArenaHalf ("Arena Half", Vector) = (1, 1, 0, 0)
        _EdgeOverlap ("Edge Overlap", Float) = 0
        _Feather ("Feather", Float) = 0.55
        _CloudTiling ("Cloud Tiling", Float) = 0.07
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
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "ScoutFogPadding"
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
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _ArenaOrigin;
                float4 _ArenaHalf;
                float _EdgeOverlap;
                float _Feather;
                float _CloudTiling;
            CBUFFER_END

            float SdBox(float2 p, float2 b)
            {
                float2 d = abs(p) - b;
                float2 outside = max(d, float2(0.0, 0.0));
                return length(outside) + min(max(d.x, d.y), 0.0);
            }

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz - _ArenaOrigin.xz;
                float2 holeHalf = max(_ArenaHalf.xy - float2(_EdgeOverlap, _EdgeOverlap), float2(0.05, 0.05));
                float sd = SdBox(p, holeHalf);
                float hole = smoothstep(0.0, max(0.01, _Feather), sd);
                float2 uv = input.positionWS.xz * _CloudTiling;
                uv = uv * _MainTex_ST.xy + _MainTex_ST.zw;
                half4 clouds = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                return half4(clouds.rgb * _BaseColor.rgb, hole * _BaseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
