Shader "Rippies/PackFoil"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.035, 0.08, 0.12, 1)
        _AccentColor("Accent Color", Color) = (0.05, 1, 0.82, 1)
        _Metallic("Metallic", Range(0, 1)) = 0.82
        _Smoothness("Smoothness", Range(0, 1)) = 0.88
        _TearProgress("Tear Progress", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _AccentColor;
                half _Metallic;
                half _Smoothness;
                half _TearProgress;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirection)), 3.0h);
                half foilBand = 0.5h + 0.5h * sin((input.uv.x * 11.0h + input.uv.y * 4.0h + fresnel * 3.0h) * 3.14159h);
                half tearGlow = smoothstep(_TearProgress - 0.035h, _TearProgress, input.uv.x) *
                                (1.0h - smoothstep(_TearProgress, _TearProgress + 0.035h, input.uv.x));
                half3 baseColor = lerp(_BaseColor.rgb, _AccentColor.rgb, foilBand * (0.14h + fresnel * 0.32h));
                half3 lighting = baseColor * (0.2h + ndotl * mainLight.color);
                half specular = pow(saturate(dot(reflect(-mainLight.direction, normalWS), viewDirection)), lerp(8.0h, 128.0h, _Smoothness));
                half3 color = lighting + specular * lerp(0.2h, 1.0h, _Metallic) + _AccentColor.rgb * tearGlow * 1.8h;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
}