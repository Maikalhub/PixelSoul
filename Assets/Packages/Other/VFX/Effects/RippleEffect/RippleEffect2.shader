Shader "Custom/RippleEffect2"
{
    Properties
    {
        _MainTex("Base Texture", 2D) = "white" {}
        _GradTex("Wave Gradient", 2D) = "white" {}
        _Drop1("Drop 1", Vector) = (0,0,0,0)
        _Drop2("Drop 2", Vector) = (0,0,0,0)
        _Drop3("Drop 3", Vector) = (0,0,0,0)
        _Params1("Params1", Vector) = (1,1,1,0)
        _Params2("Params2", Vector) = (1,1,1,1)
        _Reflection("Reflection Color", Color) = (0.5,0.5,0.5,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Ripple"
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_GradTex);
            SAMPLER(sampler_GradTex);

            float4 _Drop1;
            float4 _Drop2;
            float4 _Drop3;
            float4 _Params1; // aspect,1,1/waveSpeed,0
            float4 _Params2; // 1,1/aspect,refractionStrength,reflectionStrength
            float4 _Reflection;

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

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float aspect = _Params1.x;
                float refraction = _Params2.z;
                float reflection = _Params2.w;

                float2 pos1 = _Drop1.xy; float time1 = _Drop1.z;
                float2 pos2 = _Drop2.xy; float time2 = _Drop2.z;
                float2 pos3 = _Drop3.xy; float time3 = _Drop3.z;

                float dropValue = 0;

                // Используем SAMPLE_TEXTURE2D вместо tex2D (URP)
                dropValue += SAMPLE_TEXTURE2D(_GradTex, sampler_GradTex, float2(length(uv - pos1) * _Params1.z - time1, 0)).r;
                dropValue += SAMPLE_TEXTURE2D(_GradTex, sampler_GradTex, float2(length(uv - pos2) * _Params1.z - time2, 0)).r;
                dropValue += SAMPLE_TEXTURE2D(_GradTex, sampler_GradTex, float2(length(uv - pos3) * _Params1.z - time3, 0)).r;

                // смещение для преломления
                float2 offset = normalize(uv - pos1) * dropValue * refraction;

                // основной цвет с преломлением
                float4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset);

                // добавляем отражение
                col.rgb = lerp(col.rgb, _Reflection.rgb, reflection * dropValue);

                return col;
            }
            ENDHLSL
        }
    }
}
