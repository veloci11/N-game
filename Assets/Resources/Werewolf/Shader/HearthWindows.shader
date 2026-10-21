Shader "NGame/Interior Hearth Glass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Separate building", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _Clock ("Hearth clock", Float) = 0
        _Night ("Night", Range(0,1)) = 1
        _Reduced ("Reduced motion", Float) = 0
        _Phase ("House phase", Float) = 0
        _WindowCount ("Openings", Float) = 0
        _Window0 ("Opening 0", Vector) = (0,0,0,0)
        _Window1 ("Opening 1", Vector) = (0,0,0,0)
        _Window2 ("Opening 2", Vector) = (0,0,0,0)
        _Window3 ("Opening 3", Vector) = (0,0,0,0)
        _Window4 ("Opening 4", Vector) = (0,0,0,0)
        _Window5 ("Opening 5", Vector) = (0,0,0,0)
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
            #pragma vertex HearthVertex
            #pragma fragment HearthFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS half4 color : COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Clock, _Night, _Reduced, _Phase, _WindowCount;
                float4 _Window0, _Window1, _Window2, _Window3, _Window4, _Window5;
            CBUFFER_END
            Varyings HearthVertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings result = CommonUnlitVertex(input);
                result.color = input.color * _Color * unity_SpriteColor;
                return result;
            }
            float Pane(float2 uv, float4 rect)
            {
                // Soft inset only. No sampling displacement, facade deformation or rectangle painted on top.
                float2 edge = smoothstep(rect.xy, rect.xy + .004, uv) * (1 - smoothstep(rect.zw - .004, rect.zw, uv));
                return edge.x * edge.y;
            }
            half4 HearthFragment(Varyings input) : SV_Target
            {
                half4 paper = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 result = paper * input.color;
                float opening = Pane(input.uv, _Window0) * step(.5, _WindowCount)
                    + Pane(input.uv, _Window1) * step(1.5, _WindowCount)
                    + Pane(input.uv, _Window2) * step(2.5, _WindowCount)
                    + Pane(input.uv, _Window3) * step(3.5, _WindowCount)
                    + Pane(input.uv, _Window4) * step(4.5, _WindowCount)
                    + Pane(input.uv, _Window5) * step(5.5, _WindowCount);
                // The original bright golden glass supplies the exact pane silhouette and paper grain.
                // Dark wooden mullions and stone remain under the untouched building tint.
                float glass = smoothstep(.62, .88, paper.r) * smoothstep(.30, .48, paper.g)
                    * (1 - smoothstep(.50, .67, paper.b)) * smoothstep(.16, .28, paper.r-paper.g);
                float t = _Clock * (1-_Reduced);
                float flicker = .91 + .035*sin(t*1.7+_Phase) + .018*sin(t*4.3+_Phase*1.7)
                    + .009*sin(t*8.1+input.uv.y*32+_Phase);
                half3 interior = paper.rgb * half3(1,.94,.82) * flicker;
                result.rgb = lerp(result.rgb, interior, saturate(opening*glass)*_Night*.88);
                return result;
            }
            ENDHLSL
        }
    }
}
