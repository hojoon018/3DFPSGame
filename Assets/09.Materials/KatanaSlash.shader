// 월아천충 느낌의 검기 궤적 셰이더 (URP, 언릿, 반투명)
// 검은 몸통 + HDR 붉은 테두리. 오래된 부분은 노이즈로 불타듯 갉아먹히며 사라진다.
// uv.x = 나이(0: 방금, 1: 사라짐), uv.y = 칼날 위치(0: 밑동, 1: 칼끝) — KatanaSlashTrail이 넣어 준다.
Shader "Custom/KatanaSlash"
{
    Properties
    {
        _CoreColor ("몸통 색", Color) = (0.04, 0, 0, 1)
        _CoreAlpha ("몸통 불투명도", Range(0, 1)) = 0.85
        [HDR] _RimColor ("테두리 색 (HDR, 블룸용)", Color) = (4, 0.12, 0.08, 1)
        _RimWidth ("칼끝 테두리 두께", Range(0, 0.5)) = 0.15
        _LeadWidth ("앞쪽 테두리 두께", Range(0, 0.3)) = 0.06
        _EdgeWidth ("타들어가는 경계 두께", Range(0.01, 0.5)) = 0.15
        _BaseFade ("밑동 쪽 흐려짐", Range(0.01, 1)) = 0.3
        _DissolveNoise ("갉아먹힘 정도", Range(0, 2)) = 0.8
        _NoiseScale ("노이즈 크기 (나이, 칼날)", Vector) = (6, 3, 0, 0)
        _NoiseSpeed ("노이즈 흐르는 속도", Float) = 3
        _InnerGlow ("몸통 속 붉은 결", Range(0, 1)) = 0.15
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
            Name "KatanaSlash"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float _CoreAlpha;
                float4 _RimColor;
                float _RimWidth;
                float _LeadWidth;
                float _EdgeWidth;
                float _BaseFade;
                float _DissolveNoise;
                float4 _NoiseScale;
                float _NoiseSpeed;
                float _InnerGlow;
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
                float age = input.uv.x;
                float across = input.uv.y;

                // 두 크기의 노이즈를 섞어 불꽃 같은 결을 만들고, 시간에 따라 흘러가게 한다.
                float2 noiseUV = float2(age * _NoiseScale.x - _Time.y * _NoiseSpeed, across * _NoiseScale.y);
                float noise = ValueNoise(noiseUV) * 0.6 + ValueNoise(noiseUV * 2.3) * 0.4;

                // 나이 0이면 항상 보이고(>0), 나이 1이면 항상 사라진다(<=0). 그 사이는 노이즈에 따라 갉아먹힌다.
                float dissolve = (1.0 - age) * (1.0 + _DissolveNoise) - noise * _DissolveNoise;
                float body = smoothstep(0.0, 0.02, dissolve);

                // 테두리: 타들어가는 경계 + 칼끝 쪽 가장자리 + 칼이 막 지나간 앞쪽 가장자리
                float burnEdge = 1.0 - smoothstep(0.0, _EdgeWidth, dissolve);
                float tipEdge = smoothstep(1.0 - _RimWidth, 1.0, across);
                float leadEdge = 1.0 - smoothstep(0.0, _LeadWidth, age);
                float rim = saturate(max(burnEdge, max(tipEdge, leadEdge)));

                float3 color = lerp(_CoreColor.rgb, _RimColor.rgb, rim);
                color += _RimColor.rgb * _InnerGlow * noise * (1.0 - age);

                // 손잡이 쪽은 궤적이 얇게 흐려지도록 한다.
                float baseFade = smoothstep(0.0, _BaseFade, across);
                float alpha = body * baseFade * lerp(_CoreAlpha, 1.0, rim);

                return float4(color, alpha);
            }
            ENDHLSL
        }
    }
}
