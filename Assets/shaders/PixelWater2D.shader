Shader "Artefakty/Pixel Water 2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1, 1, 1, 1)
        _ShallowColor ("Shallow Color", Color) = (0.12, 0.55, 0.72, 1)
        _DeepColor ("Deep Color", Color) = (0.02, 0.12, 0.30, 1)
        _FoamColor ("Wave Foam Color", Color) = (0.65, 0.92, 0.86, 1)
        _PixelGrid ("Pixel Grid", Range(16, 256)) = 96
        _WaveScale ("Wave Scale", Range(1, 30)) = 9
        _WaveSpeed ("Wave Speed", Range(-3, 3)) = 0.7
        _WaveStrength ("Wave Strength", Range(0, 0.25)) = 0.06
        _FoamAmount ("Foam Amount", Range(0, 1)) = 0.42
        _FoamSharpness ("Foam Sharpness", Range(1, 20)) = 8

        [Header(Surface Settings)]
        _SurfaceWaveHeight ("Surface Wave Height", Range(0, 0.15)) = 0.03
        _SurfaceFoamThickness ("Surface Foam", Range(0, 0.05)) = 0.015

        [Header(Spline Wake Settings)]
        _ShipFoamColor ("Ship Foam Color", Color) = (1, 1, 1, 1)
        _SplineFoamWidth ("Spline Foam Width", Range(0.005, 0.08)) = 0.022
        _ShipFoamIntensity ("Ship Foam Intensity", Range(0, 5)) = 2.5
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

            #define SPLINE_POINTS 12

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
                float _SurfaceWaveHeight;
                float _SurfaceFoamThickness;
                float _FoamAmount;
                float _FoamSharpness;
                
                float4 _ShipFoamColor;
                float _SplineFoamWidth;
                float _ShipFoamIntensity;
                float4 _SplinePoints[SPLINE_POINTS];
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

            float DistanceToSegment(float2 p, float2 a, float2 b)
            {
                float2 ba = b - a;
                float2 pa = p - a;
                float h = saturate(dot(pa, ba) / max(dot(ba, ba), 0.00001));
                return length(pa - ba * h);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y * _WaveSpeed;

                float2 pixelUv = floor(uv * _PixelGrid) / _PixelGrid;

                // Falowanie wody
                float broadNoise = Noise(pixelUv * _WaveScale + float2(time * 0.45, -time * 0.2));
                float fineNoise = Noise(pixelUv * (_WaveScale * 2.2) + float2(-time * 0.8, time * 0.35));
                float waves = broadNoise * 0.65 + fineNoise * 0.35;

                // Krawedz powierzchni
                float surfaceLine = 1.0 - _SurfaceWaveHeight * 1.5;
                surfaceLine += (waves - 0.5) * _SurfaceWaveHeight * 2.0;
                float surfaceMask = step(pixelUv.y, surfaceLine);

                // Znieksztalcenie UV
                float2 distortedUv = pixelUv;
                distortedUv.x += (waves - 0.5) * _WaveStrength;
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUv);

                float depth = saturate(pixelUv.y * 0.85 + waves * 0.25);
                half3 waterBase = lerp(_DeepColor.rgb, _ShallowColor.rgb, depth) * source.rgb * input.color.rgb;

                // Piana tla
                float waveLine = frac(pixelUv.y * (_WaveScale * 0.7) + waves * 0.35 - time * 0.45);
                float crest = 1.0 - smoothstep(0.08, 0.22, abs(waveLine - 0.5));
                float foamNoise = Noise(pixelUv * (_WaveScale * 1.6) + float2(time, -time * 0.65));
                float ambientFoam = saturate((crest * foamNoise - (1.0 - _FoamAmount)) * _FoamSharpness);

                float edgeFoam = step(surfaceLine - _SurfaceFoamThickness, pixelUv.y);
                ambientFoam = max(ambientFoam, edgeFoam);

                // Piana ze splajnu
                float splineFoamMask = 0.0;

                [unroll]
                for (int i = 0; i < SPLINE_POINTS - 1; i++)
                {
                    float weightA = _SplinePoints[i].z;
                    float weightB = _SplinePoints[i + 1].z;
                    float avgWeight = (weightA + weightB) * 0.5;

                    if (avgWeight > 0.02)
                    {
                        float2 ptA = _SplinePoints[i].xy;
                        float2 ptB = _SplinePoints[i + 1].xy;

                        float2 p = pixelUv;
                        p.y *= 1.6;
                        ptA.y *= 1.6;
                        ptB.y *= 1.6;

                        float dist = DistanceToSegment(p, ptA, ptB);
                        float currentRadius = _SplineFoamWidth * (1.0 + (1.0 - avgWeight) * 1.2);
                        float segmentFoam = saturate(1.0 - (dist / currentRadius)) * avgWeight;

                        splineFoamMask = max(splineFoamMask, segmentFoam);
                    }
                }

                float trailNoise = Noise(pixelUv * (_PixelGrid * 0.25) + float2(time * 2.0, 0));
                float shipFoam = saturate((splineFoamMask * trailNoise * _ShipFoamIntensity) - 0.12) * _FoamSharpness;

                // Laczenie kolorow
                half3 finalColor = waterBase;
                finalColor = lerp(finalColor, _FoamColor.rgb, ambientFoam);
                finalColor = lerp(finalColor, _ShipFoamColor.rgb, shipFoam);

                half finalAlpha = source.a * input.color.a * surfaceMask;
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}