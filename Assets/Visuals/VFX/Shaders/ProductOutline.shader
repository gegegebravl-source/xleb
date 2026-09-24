Shader "WarmBread/ProductOutline"
{
    Properties
    {
        _BaseMap ("Item texture", 2D) = "white" {}
        _OutlineColor ("Outline color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline width", Range(0.001, 0.05)) = 0.012
        _Cutoff ("Alpha cutoff", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ProductOutline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _OutlineColor;
            float _OutlineWidth;
            float _Cutoff;
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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                return output;
            }

            /// Проба альфы текстуры предмета в точке со сдвигом.
            half SampleAlpha(float2 uv, float2 offset)
            {
                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + offset).a;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // Внутри самого предмета обводку не рисуем — только снаружи силуэта.
                if (SampleAlpha(uv, float2(0, 0)) >= _Cutoff)
                {
                    discard;
                }

                // Ищем непрозрачного соседа в 8 направлениях: получается тонкий контур по форме.
                float w = _OutlineWidth;
                float maxAlpha = 0;

                maxAlpha = max(maxAlpha, SampleAlpha(uv, float2(w, 0)));
                maxAlpha = max(maxAlpha, SampleAlpha(uv, float2(-w, 0)));
                maxAlpha = max(maxAlpha, SampleAlpha(uv, float2(0, w)));
                maxAlpha = max(maxAlpha, SampleAlpha(uv, float2(0, -w)));
                maxAlpha = max(maxAlpha, SampleAlpha(uv, float2(w, w) * 0.7));
                maxAlpha = max(maxAlpha, SampleAlpha(uv, float2(-w, w) * 0.7));
                maxAlpha = max(maxAlpha, SampleAlpha(uv, float2(w, -w) * 0.7));
                maxAlpha = max(maxAlpha, SampleAlpha(uv, float2(-w, -w) * 0.7));

                if (maxAlpha < _Cutoff)
                {
                    discard;
                }

                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
