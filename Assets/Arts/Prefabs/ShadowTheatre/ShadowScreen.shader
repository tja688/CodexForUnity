Shader "Custom/ShadowScreen"
{
    Properties
    {
        _MainTex ("Curtain Texture", 2D) = "white" {}
        _NormalMap ("Curtain Normal", 2D) = "bump" {}
        _ShadowTex ("Shadow RT", 2D) = "white" {}
        _BlurAmount ("Blur Amount", Float) = 0
        _EmissionIntensity ("Emission Intensity", Float) = 1
        _Saturation ("Shadow Saturation", Range(0.5, 2)) = 1
        _WobbleStrength ("Wobble Strength", Float) = 0.005
        _WobbleSpeed ("Wobble Speed", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NormalMap_ST;
                float _BlurAmount;
                float _EmissionIntensity;
                float _Saturation;
                float _WobbleStrength;
                float _WobbleSpeed;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_ShadowTex); SAMPLER(sampler_ShadowTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uvNormal : TEXCOORD1;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float wobble = sin(_Time.y * _WobbleSpeed + IN.positionOS.x * 2 + IN.positionOS.y * 2) * _WobbleStrength;
                float3 positionOS = IN.positionOS.xyz + float3(0, 0, wobble);
                OUT.positionHCS = TransformObjectToHClip(positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.uvNormal = TRANSFORM_TEX(IN.uv, _NormalMap);
                return OUT;
            }

            float3 ApplySaturation(float3 color, float saturation)
            {
                float luma = dot(color, float3(0.299, 0.587, 0.114));
                return lerp(luma.xxx, color, saturation);
            }

            float3 SampleShadowBlur(float2 uv)
            {
                float2 texel = _ShadowTex_TexelSize.xy;
                float2 blur = texel * _BlurAmount;
                float3 sum = 0;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv + blur * float2(-1, -1)).rgb;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv + blur * float2(0, -1)).rgb;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv + blur * float2(1, -1)).rgb;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv + blur * float2(-1, 0)).rgb;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv).rgb;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv + blur * float2(1, 0)).rgb;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv + blur * float2(-1, 1)).rgb;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv + blur * float2(0, 1)).rgb;
                sum += SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, uv + blur * float2(1, 1)).rgb;
                return sum / 9.0;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 curtain = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).rgb;
                float3 shadow = SampleShadowBlur(IN.uv);
                shadow = ApplySaturation(shadow, _Saturation);

                float3 normal = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, IN.uvNormal));
                float ndotl = saturate(dot(normal, normalize(float3(0.25, 0.2, 1))));
                float3 clothLighting = lerp(0.85, 1.15, ndotl);

                float3 multiplied = curtain * shadow * clothLighting;
                float3 emission = multiplied * _EmissionIntensity;
                return half4(emission, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
