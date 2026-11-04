// 날아가는 초승달 검기 셰이더 (월아천충 느낌, URP, 언릿, 반투명)
// 바깥 앞날은 뜨겁게 빛나고, 몸통은 검게, 안쪽 가장자리는 불꽃처럼 들쭉날쭉하게 붉게 타오른다.
// 앞날 바깥으로는 붉은 기운이 번진다.
// uv.x = 호 위치(0~1, 양 끝), uv.y = 띠 위치(0: 바깥 기운 끝, 1: 띠 안쪽 끝)
// _AuraWidth, _BandWidth, _Fade는 SlashWave.cs가 MaterialPropertyBlock으로 넣어 준다.
Shader "Custom/SlashWave"
{
    Properties
    {
        [Header(Color)]
        [HDR] _EdgeColor ("앞날 색", Color) = (6, 1, 0.7, 1)
        [HDR] _RimColor ("테두리/기운 색", Color) = (4, 0.12, 0.06, 1)
        [HDR] _CrimsonColor ("진홍 색 (테두리와 몸통 사이)", Color) = (0.25, 0, 0, 1)
        [HDR] _BodyColor ("몸통 색", Color) = (0.01, 0, 0, 1)

        [Header(Color Bands)]
        _EdgeEnd ("앞날 → 테두리 끝", Range(0, 1)) = 0.03
        _CrimsonStart ("진홍 시작", Range(0, 1)) = 0.04
        _CrimsonEnd ("진홍 끝", Range(0, 1)) = 0.14
        _BodyStart ("몸통 색 시작", Range(0, 1)) = 0.12
        _BodyEnd ("몸통 색 끝", Range(0, 1)) = 0.3

        [Header(Shape)]
        _BodyThickness ("몸통 두께 (m)", Float) = 1.2
        _TipSharpness ("끝 뾰족함", Range(0.2, 4)) = 1.2
        _FlameReach ("안쪽 불꽃 길이 (m)", Float) = 1.0
        _FlameNoiseScale ("불꽃 노이즈 크기 (호, 깊이)", Vector) = (10, 2.2, 0, 0)
        _FlameSpeed ("불꽃 흐르는 속도", Float) = 2
        _InnerGlow ("안쪽 불꽃 가장자리 빛", Range(0, 1)) = 1
        _Streak ("속도선 결 세기", Range(0, 1)) = 0.35

        [Header(Aura)]
        _AuraAlpha ("바깥 기운 세기", Range(0, 1)) = 0.55
        _AuraFalloff ("바깥 기운 퍼짐 (m)", Float) = 0.18

        [Header(Set By Script)]
        _AuraWidth ("바깥 기운 폭 (스크립트)", Float) = 0.6
        _BandWidth ("띠 폭 (스크립트)", Float) = 2.4
        _Fade ("사라짐 진행도 (스크립트)", Range(0, 1)) = 0
        _FadeEdge ("타들어가는 경계 두께", Range(0.01, 0.3)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "SlashWave"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _EdgeColor;
                float4 _RimColor;
                float4 _CrimsonColor;
                float4 _BodyColor;
                float _EdgeEnd;
                float _CrimsonStart;
                float _CrimsonEnd;
                float _BodyStart;
                float _BodyEnd;
                float _BodyThickness;
                float _TipSharpness;
                float _FlameReach;
                float4 _FlameNoiseScale;
                float _FlameSpeed;
                float _InnerGlow;
                float _Streak;
                float _AuraAlpha;
                float _AuraFalloff;
                float _AuraWidth;
                float _BandWidth;
                float _Fade;
                float _FadeEdge;
            CBUFFER_END

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

            // 좌표 하나를 0~1 사이의 의사 난수로 바꾼다.
            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 격자 꼭짓점의 난수를 부드럽게 보간한 값 노이즈
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float u = input.uv.x;
                float centered = abs(u * 2.0 - 1.0); // 0: 가운데, 1: 양 끝

                // 가운데가 두툼하고 양 끝이 뾰족한 초승달 두께 곡선
                float profile = pow(saturate(1.0 - centered * centered), _TipSharpness);

                // 바깥 앞날에서 안쪽으로 들어간 거리 (m). 음수는 앞날 바깥의 기운 영역
                float depth = lerp(-_AuraWidth, _BandWidth, input.uv.y);

                // 안쪽 경계: 불꽃처럼 들쭉날쭉하고, 시간에 따라 안쪽으로 흐른다
                float2 flameUV = float2(u * _FlameNoiseScale.x, depth * _FlameNoiseScale.y - _Time.y * _FlameSpeed);
                float flame = ValueNoise(flameUV) * 0.55
                    + ValueNoise(flameUV * 2.3 + 7.1) * 0.3
                    + ValueNoise(flameUV * 5.1 + 3.3) * 0.15;
                float flicker = (ValueNoise(float2(u * 30.0, _Time.y * 14.0)) - 0.5) * 0.04;
                float thickness = max((_BodyThickness + flicker) * profile, 0.0);
                float inner = thickness + _FlameReach * profile * flame;

                // 몸통 영역 (앞날부터 들쭉날쭉한 안쪽 경계까지)
                float mask = smoothstep(-0.015, 0.01, depth) * (1.0 - smoothstep(inner - 0.08, inner, depth));
                float k = saturate(depth / max(inner, 0.001)); // 0: 앞날, 1: 불꽃 끝

                // 색: 앞날(뜨거움) → 테두리(붉음) → 진홍 → 몸통(검정)
                float3 color = lerp(_EdgeColor.rgb, _RimColor.rgb, smoothstep(0.0, _EdgeEnd, k));
                color = lerp(color, _CrimsonColor.rgb, smoothstep(_CrimsonStart, _CrimsonEnd, k));
                color = lerp(color, _BodyColor.rgb, smoothstep(_BodyStart, _BodyEnd, k));

                // 호를 따라 흐르는 속도선 느낌의 결
                float streak = ValueNoise(float2(u * 70.0, depth * 1.5));
                color *= 1.0 - _Streak * streak * smoothstep(0.05, 0.3, k);

                // 들쭉날쭉한 안쪽 불꽃 가장자리가 붉게 타오른다
                float innerEdge = smoothstep(inner - 0.18, inner - 0.03, depth) * step(0.12, k);
                color = lerp(color, _RimColor.rgb * 0.8, _InnerGlow * innerEdge);
                float bodyAlpha = mask * lerp(1.0, 0.8, k);

                // 앞날 바깥으로 번지는 붉은 기운
                float outside = max(-depth, 0.0);
                float aura = exp(-outside / max(_AuraFalloff, 0.001)) * step(depth, 0.0) * profile * _AuraAlpha;
                aura *= 0.75 + 0.5 * ValueNoise(float2(u * 18.0, outside * 6.0 - _Time.y * 3.0));

                color = lerp(_RimColor.rgb * 0.6, color, mask);
                float alpha = max(bodyAlpha, aura);

                // 사라질 때: 양 끝부터 노이즈를 따라 타들어가고, 타는 경계는 붉게 빛난다
                float burnKey = (1.0 - centered) * 0.85 + flame * 0.15; // 가운데일수록 늦게 탄다
                float threshold = _Fade * (1.0 + _FadeEdge);
                float visible = smoothstep(threshold - _FadeEdge, threshold, burnKey);
                float burnGlow = (1.0 - smoothstep(threshold, threshold + _FadeEdge * 2.0, burnKey)) * step(0.001, _Fade);
                color = lerp(color, _RimColor.rgb, burnGlow);
                alpha *= visible;

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
