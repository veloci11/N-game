Shader "NGame/Articulated Paper Wolf"
{
    Properties
    {
        _MainTex ("Original alpha illustration", 2D) = "white" {}
        _Color ("Day and route opacity", Color) = (1,1,1,1)
        _SourceRect ("Texture UV rectangle", Vector) = (0,0,1,1)
        _ArtSize ("Local mesh dimensions", Vector) = (1,1,0,0)
        _HeadAngle ("Head observation in radians", Float) = 0
        _LeftEarAngle ("Left ear angle in radians", Float) = 0
        _RightEarAngle ("Right ear angle in radians", Float) = 0
        _Breath ("Chest breath", Float) = 0
        _EyeLight ("Amber eye light", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex WolfVertex
            #pragma fragment WolfFragment
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _SourceRect;
                float4 _ArtSize;
                float _HeadAngle;
                float _LeftEarAngle;
                float _RightEarAngle;
                float _Breath;
                float _EyeLight;
            CBUFFER_END

            float2 TurnAround(float2 coordinate, float2 pivot, float angleValue)
            {
                float sine;
                float cosine;
                sincos(angleValue, sine, cosine);
                float2 delta = coordinate - pivot;
                return pivot + float2(delta.x * cosine - delta.y * sine, delta.x * sine + delta.y * cosine);
            }

            Varyings WolfVertex(Attributes input)
            {
                float2 original = input.uv;
                float2 folded = original;
                // Local chest expansion keeps the bottom of the paper silhouette rooted.
                float chest = (1.0 - smoothstep(0.38, 0.60, original.y)) * smoothstep(0.0, 0.15, original.y);
                folded.x += (original.x - 0.54) * _Breath * chest * 0.032;
                folded.y += _Breath * chest * 0.006;

                // Each ear has its own hinge. Weights fade through the surrounding folds.
                float leftEar = smoothstep(0.72, 0.89, original.y)
                    * smoothstep(0.40, 0.49, original.x) * (1.0 - smoothstep(0.68, 0.76, original.x));
                float rightEar = smoothstep(0.72, 0.87, original.y) * smoothstep(0.73, 0.82, original.x);
                folded = TurnAround(folded, float2(0.565, 0.75), _LeftEarAngle * leftEar);
                folded = TurnAround(folded, float2(0.815, 0.735), _RightEarAngle * rightEar);

                // The head moves about the neck; the lower torso stays still.
                float head = smoothstep(0.38, 0.65, original.y);
                folded = TurnAround(folded, float2(0.70, 0.405), _HeadAngle * head);
                float3 position = input.positionOS;
                position.xy += (folded - original) * _ArtSize.xy;
                Varyings output;
                output.positionCS = TransformObjectToHClip(position);
                output.uv = original;
                return output;
            }

            half4 WolfFragment(Varyings input) : SV_Target
            {
                float2 sampleUv = _SourceRect.xy + input.uv * _SourceRect.zw;
                half4 paper = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUv);
                // These two centers match the supplied wolf drawing. They follow the deformed mesh.
                float2 leftEye = (input.uv - float2(0.674, 0.624)) / float2(0.024, 0.027);
                float2 rightEye = (input.uv - float2(0.837, 0.609)) / float2(0.017, 0.027);
                float amber = exp(-dot(leftEye, leftEye) * 2.4) + exp(-dot(rightEye, rightEye) * 2.4);
                paper.rgb += half3(1.0, 0.55, 0.12) * amber * _EyeLight * 0.43;
                return paper * _Color;
            }
            ENDHLSL
        }
    }
}
