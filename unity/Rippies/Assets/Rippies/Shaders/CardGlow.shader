Shader "Rippies/CardGlow"
{
    Properties
    {
        _GlowColor("Glow Color", Color) = (0.25, 0.85, 1, 1)
        _Intensity("Intensity", Range(0, 8)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" }
        Pass
        {
            Name "Glow"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _Intensity;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv - 0.5;
                float radius = length(centered) * 2.0;
                float angle = atan2(centered.y, centered.x);
                half halo = pow(saturate(1.0 - radius), 1.65);
                half rays = 0.88h + 0.12h * sin(angle * 14.0h + radius * 24.0h);
                half energy = halo * rays * _Intensity;
                return half4(_GlowColor.rgb * energy, energy);
            }
            ENDHLSL
        }
    }
}