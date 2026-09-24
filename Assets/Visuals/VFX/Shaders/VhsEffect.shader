Shader "Hidden/WarmBread/VhsEffect"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 1)) = 0.35
        _ScanlineCount ("Scanlines", Float) = 300
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.22
        _NoiseAmount ("Noise", Range(0, 1)) = 0.05
        _ChromaticShift ("Chromatic Shift", Range(0, 0.02)) = 0.0022
        _Curvature ("Curvature", Range(0, 0.5)) = 0.05
        _Desaturation ("Desaturation", Range(0, 1)) = 0.1
        _Vignette ("Vignette", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            Name "VhsEffect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Intensity;
            float _ScanlineCount;
            float _ScanlineStrength;
            float _NoiseAmount;
            float _ChromaticShift;
            float _Curvature;
            float _Desaturation;
            float _Vignette;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // --- искривление картинки, как у ЭЛТ-кинескопа ---
                float2 centered = uv * 2.0 - 1.0;
                float r2 = dot(centered, centered);
                centered *= 1.0 + _Curvature * r2;
                float2 warped = centered * 0.5 + 0.5;

                // за пределами кадра — чёрное
                if (warped.x < 0.0 || warped.x > 1.0 || warped.y < 0.0 || warped.y > 1.0)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                // --- расхождение каналов, как у аналогового сигнала ---
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warped + float2(_ChromaticShift, 0.0)).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warped).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warped - float2(_ChromaticShift, 0.0)).b;

                half3 color = half3(r, g, b);

                // --- строки развёртки ---
                float scan = 0.5 + 0.5 * sin(warped.y * _ScanlineCount * 3.14159265);
                color *= lerp(1.0, scan, _ScanlineStrength);

                // --- шум плёнки ---
                float noise = frac(sin(dot(warped * _Time.y, float2(12.9898, 78.233))) * 43758.5453);
                color += (noise - 0.5) * _NoiseAmount;

                // --- обесцвечивание и виньетка ---
                half luminance = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(color, luminance.xxx, _Desaturation);

                float vignette = 1.0 - _Vignette * r2 * 0.6;
                color *= vignette;

                // --- смешиваем с оригиналом по общей интенсивности ---
                half3 original = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                return half4(lerp(original, color, _Intensity), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
