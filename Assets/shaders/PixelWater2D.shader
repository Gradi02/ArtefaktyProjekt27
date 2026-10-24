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

        [Header(Zalamanie i ksztalt tafli)]
        _SurfaceWaveHeight ("Surface Wave Height (Wysokosc fal na brzegu)", Range(0, 0.15)) = 0.03
        _SurfaceFoamThickness ("Surface Foam (Grubosc piany brzegu)", Range(0, 0.05)) = 0.015

        [Header(Slad i piana statku)]
        _ShipFoamColor ("Ship Foam Color", Color) = (1, 1, 1, 1)
        _ShipUV ("Ship UV Position", Vector) = (0.5, 0.5, 0, 0)
        _WakeLength ("Wake Length (Dlugosc sladu w tyl)", Range(0.05, 0.8)) = 0.35
        _WakeWidth ("Wake Width (Szerokosc pionowa)", Range(0.005, 0.1)) = 0.025
        _ShipFoamIntensity ("Ship Foam Intensity", Range(0, 4)) = 2.0
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
                float _SurfaceWaveHeight;
                float _SurfaceFoamThickness;
                float _FoamAmount;
                float _FoamSharpness;
                
                float4 _ShipFoamColor;
                float4 _ShipUV;
                float _WakeLength;
                float _WakeWidth;
                float _ShipFoamIntensity;
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

                // Spikselizowanie koordynatów
                float2 pixelUv = floor(uv * _PixelGrid) / _PixelGrid;

                // Szum fal
                float broadNoise = Noise(pixelUv * _WaveScale + float2(time * 0.45, -time * 0.2));
                float fineNoise = Noise(pixelUv * (_WaveScale * 2.2) + float2(-time * 0.8, time * 0.35));
                float waves = broadNoise * 0.65 + fineNoise * 0.35;

                // -----------------------------------------------------------
                // ZAŁAMANIE KRAWĘDZI (TWORZENIE FALUJĄCEGO BRZEGU)
                // -----------------------------------------------------------
                // Generujemy nieregularną linię powierzchni tafli wody
                float surfaceLine = 1.0 - _SurfaceWaveHeight * 1.5;
                surfaceLine += (waves - 0.5) * _SurfaceWaveHeight * 2.0;

                // Jeśli piksel jest powyżej pofalowanej linii -> obcinamy go (przezroczystość)
                // Używamy step dla ostrego, pikselowego odcięcia
                float surfaceMask = step(pixelUv.y, surfaceLine);

                // Zniekształcenie UV do próbkowania tekstury
                float2 distortedUv = pixelUv;
                distortedUv.x += (waves - 0.5) * _WaveStrength;
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, distortedUv);

                // Bazowy gradient głębi wody
                float depth = saturate(pixelUv.y * 0.85 + waves * 0.25);
                half3 waterBase = lerp(_DeepColor.rgb, _ShallowColor.rgb, depth) * source.rgb * input.color.rgb;

                // 1. Zwykła, ogólna piana fal
                float waveLine = frac(pixelUv.y * (_WaveScale * 0.7) + waves * 0.35 - time * 0.45);
                float crest = 1.0 - smoothstep(0.08, 0.22, abs(waveLine - 0.5));
                float foamNoise = Noise(pixelUv * (_WaveScale * 1.6) + float2(time, -time * 0.65));
                float ambientFoam = saturate((crest * foamNoise - (1.0 - _FoamAmount)) * _FoamSharpness);

                // Piana na samej krawędzi tafli wody (Waterline)
                float edgeFoam = step(surfaceLine - _SurfaceFoamThickness, pixelUv.y);
                ambientFoam = max(ambientFoam, edgeFoam);

                // 2. Kilwater i piana wokół statku (Ship Foam)
                float2 relPos = pixelUv - _ShipUV.xy;
                float behindMask = smoothstep(-_WakeLength, 0.0, relPos.x) * (1.0 - step(0.04, relPos.x));
                
                float wakeTaper = saturate(-relPos.x / max(_WakeLength, 0.001));
                float currentWidth = _WakeWidth * (1.0 + wakeTaper * 1.8);
                
                float vertMask = 1.0 - smoothstep(0.0, currentWidth, abs(relPos.y));
                float hullTrailShape = behindMask * vertMask;

                float wakeNoise = Noise(pixelUv * (_PixelGrid * 0.25) + float2(time * 2.5, 0.0));
                float shipFoam = saturate((hullTrailShape * wakeNoise * _ShipFoamIntensity) - 0.15) * _FoamSharpness;

                // 3. Łączenie kolorów
                half3 finalColor = waterBase;
                finalColor = lerp(finalColor, _FoamColor.rgb, ambientFoam);
                finalColor = lerp(finalColor, _ShipFoamColor.rgb, shipFoam);

                // Końcowa przezroczystość z uwzględnieniem pofalowanego brzegu!
                half finalAlpha = source.a * input.color.a * surfaceMask;

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}