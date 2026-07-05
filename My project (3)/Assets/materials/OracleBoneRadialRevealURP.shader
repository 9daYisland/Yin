Shader "Custom/OracleBoneRadialRevealURP"
{
    Properties
    {
        _NormalTex("Normal Texture", 2D) = "white" {}
        _CrackedTex("Cracked Texture", 2D) = "white" {}

        _NormalColor("Normal Color", Color) = (1,1,1,1)
        _CrackedColor("Cracked Color", Color) = (1,1,1,1)

        _Reveal("Reveal", Range(0,1)) = 0
        _Center("Reveal Center UV", Vector) = (0.5, 0.5, 0, 0)
        _MaxDistance("Max Distance", Range(0.1, 1.5)) = 0.75
        _Softness("Softness", Range(0.001, 0.3)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "OracleBoneRadialReveal"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);

            TEXTURE2D(_CrackedTex);
            SAMPLER(sampler_CrackedTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _NormalTex_ST;
                float4 _CrackedTex_ST;

                float4 _NormalColor;
                float4 _CrackedColor;

                float _Reveal;
                float4 _Center;
                float _MaxDistance;
                float _Softness;
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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 normalUV = input.uv * _NormalTex_ST.xy + _NormalTex_ST.zw;
                float2 crackedUV = input.uv * _CrackedTex_ST.xy + _CrackedTex_ST.zw;

                half4 normalCol = SAMPLE_TEXTURE2D(_NormalTex, sampler_NormalTex, normalUV) * _NormalColor;
                half4 crackedCol = SAMPLE_TEXTURE2D(_CrackedTex, sampler_CrackedTex, crackedUV) * _CrackedColor;

                float distFromCenter = distance(input.uv, _Center.xy);
                float revealRadius = _Reveal * _MaxDistance;

                float mask = 1.0 - smoothstep(revealRadius, revealRadius + _Softness, distFromCenter);

                half4 finalCol = lerp(normalCol, crackedCol, mask);

                return half4(finalCol.rgb, 1);
            }

            ENDHLSL
        }
    }
}