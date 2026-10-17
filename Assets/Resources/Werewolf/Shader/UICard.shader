Shader "NGame/Animated Storybook Card UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Card illustration", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ArtRect ("Atlas rectangle", Vector) = (0,0,1,1)
        _Clock ("Card clock", Float) = 0
        _Motion ("Animation strength", Float) = 1
        _Seed ("Individual phase", Float) = 0
        _RevealGlow ("Reveal light", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex CardVertex
            #pragma fragment CardFragment
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; float4 localPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _ArtRect;
            float _Clock;
            float _Motion;
            float _Seed;
            float _RevealGlow;
            v2f CardVertex(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }
            fixed4 CardFragment(v2f input) : SV_Target
            {
                float2 local = (input.texcoord - _ArtRect.xy) / max(_ArtRect.zw, float2(0.00001, 0.00001));
                float portrait = smoothstep(0.13, 0.28, local.x) * (1 - smoothstep(0.73, 0.88, local.x))
                    * smoothstep(0.18, 0.32, local.y) * (1 - smoothstep(0.72, 0.86, local.y));
                float breath = sin(_Clock * 1.2 + _Seed);
                float2 shift = float2(sin(_Clock * 0.75 + local.y * 5 + _Seed) * 0.011,
                    breath * 0.006) * portrait * _ArtRect.zw * _Motion;
                float2 uv = clamp(input.texcoord + shift, _ArtRect.xy + 0.0001, _ArtRect.xy + _ArtRect.zw - 0.0001);
                fixed4 paper = tex2D(_MainTex, uv) + _TextureSampleAdd;
                float gold = smoothstep(0.055, 0.22, paper.r - paper.b) * smoothstep(0.27, 0.66, dot(paper.rgb, float3(0.299, 0.587, 0.114)));
                float diagonal = local.x * 0.74 + local.y * 0.26;
                float sweep = frac(_Clock * 0.115 + _Seed * 0.147) * 1.5 - 0.25;
                float shine = exp(-pow((diagonal - sweep) * 19, 2)) * gold * _Motion;
                paper.rgb += float3(0.34, 0.23, 0.08) * (shine + _RevealGlow * 0.35) * paper.a;
                paper *= input.color;
                #ifdef UNITY_UI_CLIP_RECT
                    paper.a *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(paper.a - 0.001);
                #endif
                return paper;
            }
            ENDCG
        }
    }
}
