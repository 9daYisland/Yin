Shader "Custom/SkyboxGradient"
{
    Properties
    {
        _TopColor("Top Color", Color) = (1, 0.3, 0.3, 1)
        _MiddleColor("Middle Color", Color) = (1, 1, 1, 1)
        _BottomColor("Bottom Color", Color) = (0.3, 0.3, 1, 1)
        _Direction("Direction", Vector) = (0, 1, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _TopColor;
            float4 _MiddleColor;
            float4 _BottomColor;
            float4 _Direction;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.texcoord = input.texcoord;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 direction = normalize(_Direction.xyz);
                float3 viewDirection = normalize(input.texcoord);

                float range = dot(viewDirection, direction);

                half bottomRange = saturate(-range);
                half middleRange = saturate(1.0 - abs(range));
                half topRange = saturate(range);

                half3 finalColor =
                    _BottomColor.rgb * bottomRange +
                    _MiddleColor.rgb * middleRange +
                    _TopColor.rgb * topRange;

                return half4(finalColor, 1);
            }

            ENDHLSL
        }
    }

    FallBack Off
}