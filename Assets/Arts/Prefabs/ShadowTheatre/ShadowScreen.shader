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
        _LightColor ("Light Color", Color) = (1, 0.86, 0.7, 1)
        _LightCenter ("Light Center", Vector) = (0.5, 0.5, 0, 0)
        _LightFocus ("Light Focus", Float) = 3
        _LightJitter ("Light Jitter", Float) = 0.03
        _LightJitterSpeed ("Light Jitter Speed", Float) = 0.4
        _FlickerMin ("Flicker Min", Float) = 0.8
        _FlickerMax ("Flicker Max", Float) = 1.1
        _FlickerFastMin ("Flicker Fast Min", Float) = 0.95
        _FlickerFastMax ("Flicker Fast Max", Float) = 1.05
        _NoiseScaleSlow ("Noise Scale Slow", Float) = 5
        _NoiseScaleFast ("Noise Scale Fast", Float) = 50
        _DirtTex ("Dirt Texture", 2D) = "white" {}
        _DirtStrength ("Dirt Strength", Range(0, 0.5)) = 0.1
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
                float4 _ShadowTex_TexelSize;
                float4 _LightColor;
                float4 _LightCenter;
                float _LightFocus;
                float _LightJitter;
                float _LightJitterSpeed;
                float _FlickerMin;
                float _FlickerMax;
                float _FlickerFastMin;
                float _FlickerFastMax;
                float _NoiseScaleSlow;
                float _NoiseScaleFast;
                float _DirtStrength;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_ShadowTex); SAMPLER(sampler_ShadowTex);
            TEXTURE2D(_DirtTex); SAMPLER(sampler_DirtTex);

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

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float LightMask(float2 uv)
            {
                float2 jitter = float2(
                    Noise2D(float2(_Time.y * _LightJitterSpeed, 1.3)),
                    Noise2D(float2(_Time.y * _LightJitterSpeed, 2.7)));
                jitter = (jitter - 0.5) * 2.0 * _LightJitter;

                float2 center = _LightCenter.xy + jitter;
                float dist = distance(uv, center);
                float radial = saturate(1.0 - dist);
                radial = pow(radial, _LightFocus);
                return saturate(radial);
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

                float flickerSlow = lerp(_FlickerMin, _FlickerMax,
                    Noise2D(IN.uv * _NoiseScaleSlow + _Time.y * 0.5));
                float flickerFast = lerp(_FlickerFastMin, _FlickerFastMax,
                    Noise2D(IN.uv * _NoiseScaleFast + _Time.y * 2.5));
                float lightMask = LightMask(IN.uv) * flickerSlow * flickerFast;

                float dirt = SAMPLE_TEXTURE2D(_DirtTex, sampler_DirtTex, IN.uv * 1.2).r;
                float dirtFactor = lerp(1.0, dirt, _DirtStrength);

                float3 lightColor = _LightColor.rgb * lightMask * dirtFactor;
                float3 baseLit = curtain * lightColor * clothLighting;

                float3 multiplied = baseLit * shadow;
                float3 emission = multiplied * _EmissionIntensity;
                return half4(emission, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
