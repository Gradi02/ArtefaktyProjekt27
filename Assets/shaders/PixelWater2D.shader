Shader "Artefakty/Pixel Water 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1, 1, 1, 1)
        _ShallowColor ("Shallow Color", Color) = (0.12, 0.55, 0.72, 1)
        _DeepColor ("Deep Color", Color) = (0.02, 0.12, 0.30, 1)
        _FoamColor ("Foam Color", Color) = (0.65, 0.92, 0.86, 1)
        _PixelGrid ("Pixel Grid", Range(16, 256)) = 96
        _WaveScale ("Wave Scale", Range(1, 30)) = 9
        _WaveSpeed ("Wave Speed", Range(-3, 3)) = 0.7
        _WaveStrength ("Wave Strength", Range(0, 0.25)) = 0.06
        _FoamAmount ("Foam Amount", Range(0, 1)) = 0.42
        _FoamSharpness ("Foam Sharpness", Range(1, 20)) = 8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

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
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _PixelGrid;
                float _WaveScale;
                float _WaveSpeed;
                float _WaveStrength;
                float _FoamAmount;
                float _FoamSharpness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            float Hash21(float2 position)
            {
                position = frac(position * float2(123.34, 456.21));
                position += dot(position, position + 45.32);
                return frac(position.x * position.y);
            }

            float Noise(float2 position)
            {
                float2 cell = floor(position);
                float2 local = frac(position);
                local = local * local * (3.0 - 2.0 * local);

                float bottomLeft = Hash21(cell);
                float bottomRight = Hash21(cell + float2(1, 0));
                float topLeft = Hash21(cell + float2(0, 1));
                float topRight = Hash21(cell + float2(1, 1));

                return lerp(
                    lerp(bottomLeft, bottomRight, local.x),
                    lerp(topLeft, topRight, local.x),
                    local.y
                );
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y * _WaveSpeed;

                float2 pixelUv = floor(uv * _PixelGrid) / _PixelGrid;
                float broadNoise = Noise(pixelUv * _WaveScale + float2(time * 0.45, -time * 0.2));
                float fineNoise = Noise(pixelUv * (_WaveScale * 2.2) + float2(-time * 0.8, time * 0.35));
                float waves = broadNoise * 0.65 + fineNoise * 0.35;

                float2 distortedUv = pixelUv;
                distortedUv.x += (waves - 0.5) * _WaveStrength;
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUv);

                float depth = saturate(pixelUv.y * 0.85 + waves * 0.25);
                half3 water = lerp(_DeepColor.rgb, _ShallowColor.rgb, depth);

                float waveLine = frac(pixelUv.y * (_WaveScale * 0.7) + waves * 0.35 - time * 0.45);
                float crest = 1.0 - smoothstep(0.08, 0.22, abs(waveLine - 0.5));
                float foamNoise = Noise(pixelUv * (_WaveScale * 1.6) + float2(time, -time * 0.65));
                float foam = saturate((crest * foamNoise - (1.0 - _FoamAmount)) * _FoamSharpness);

                water = lerp(water, _FoamColor.rgb, foam);
                half alpha = source.a * input.color.a;
                return half4(water * source.rgb * input.color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
