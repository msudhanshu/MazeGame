Shader "Nixin Studio/MosaicGlass"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _TintStrength ("Tint Strength", Range(0, 1)) = 0.45
        _GlassColor ("Glass Color", Color) = (0.78, 0.90, 1, 1)
        _GlassAlpha ("Glass Alpha", Range(0, 1)) = 0.4
        _Frost ("Frost", Range(0, 1)) = 0.55
        _SeamColor ("Seam Color", Color) = (0.82, 0.94, 1, 1)
        _SeamWidth ("Seam Width", Range(0.004, 0.14)) = 0.04
        _Rim ("Rim", Range(0.2, 6)) = 2.4
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

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "MosaicGlass"
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
                half4 _Tint;
                half _TintStrength;
                half4 _GlassColor;
                half _GlassAlpha;
                half _Frost;
                half4 _SeamColor;
                half _SeamWidth;
                half _Rim;
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
                float edge = max(centred.x, centred.y);
                float inner = 1.0 - edge;

                float seamWidth = max(_SeamWidth, 0.004);
                float seam = saturate((edge - (1.0 - seamWidth)) / seamWidth);
                float rim = pow(saturate(edge), _Rim);

                float2 fromCentre = input.uv - 0.5;
                float streak = pow(saturate(1.0 - abs(fromCentre.x * 0.35 + fromCentre.y) * 3.4), 10.0);

                half3 glass = _GlassColor.rgb;
                half3 stained = lerp(glass, _Tint.rgb, _TintStrength);
                half3 frosted = lerp(stained, lerp(stained, half3(1.0, 1.0, 1.0), 0.45), _Frost * saturate(inner + 0.15));
                half3 colour = frosted + streak * (0.22 + 0.18 * (1.0 - _Frost)) * stained;
                colour = lerp(colour, _SeamColor.rgb, seam);
                colour += rim * stained * 0.18;

                half alpha = lerp(_GlassAlpha * 0.35, _GlassAlpha, _Frost);
                alpha = max(alpha, rim * 0.28);
                alpha = max(alpha, seam * 0.92);
                alpha = saturate(alpha);

                return half4(colour, alpha);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
