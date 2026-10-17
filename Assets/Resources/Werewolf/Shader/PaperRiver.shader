Shader "NGame/Storybook River"
{
    Properties
    {
        [PerRendererData] _MainTex ("Paper illustration", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _Clock ("Water clock", Float) = 0
        _Motion ("Motion strength", Range(0,1)) = 1
        _Reflection ("Warm reflections", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex PaperVertex
            #pragma fragment WaterFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS half4 color : COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Clock;
                float _Motion;
                float _Reflection;
            CBUFFER_END

            Varyings PaperVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings result = CommonUnlitVertex(input);
                result.color = input.color * _Color * unity_SpriteColor;
                return result;
            }

            half4 WaterFragment(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                // A narrow mask follows the river below the bridge. Houses and sky retain their original UVs.
                float riverLeft = lerp(0.45, 0.54, smoothstep(0.0, 0.35, uv.y));
                float riverRight = lerp(0.78, 0.69, smoothstep(0.0, 0.35, uv.y));
                float mask = smoothstep(riverLeft, riverLeft + 0.045, uv.x)
                    * (1.0 - smoothstep(riverRight - 0.045, riverRight, uv.x))
                    * (1.0 - smoothstep(0.31, 0.37, uv.y));
                float swell = sin(uv.y * 112.0 - _Clock * 1.7 + sin(uv.x * 21.0) * 1.8);
                float ripple = sin(uv.y * 193.0 + uv.x * 43.0 - _Clock * 2.9);
                float2 distortion = float2(swell * 0.0018 + ripple * 0.0005, ripple * 0.00045);
                half4 paper = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + distortion * mask * _Motion);
                float light = pow(saturate(0.5 + 0.5 * sin(uv.y * 157.0 - _Clock * 2.1 + uv.x * 27.0)), 9.0);
                paper.rgb += half3(0.9, 0.52, 0.18) * light * mask * 0.035 * _Motion * _Reflection;
                return paper * input.color;
            }
            ENDHLSL
        }
    }
}
