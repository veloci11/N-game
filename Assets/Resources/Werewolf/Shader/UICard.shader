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
        _RoleKind ("Illustrated role, zero for the common back", Float) = 0
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
            float _RoleKind;
            float SoftSpot(float2 location, float2 center, float2 extent)
            {
                float2 d = (location - center) / extent;
                return exp(-dot(d, d) * 2.5);
            }
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
                float2 shift = float2(sin(_Clock * 0.75 + local.y * 5 + _Seed) * 0.014,
                    breath * 0.009) * portrait * _ArtRect.zw * _Motion * step(0.5, _RoleKind);
                // Object-specific motion stays inside the portrait. Borders, title and shared back are untouched.
                float2 ornament = float2(0, 0);
                float3 livingLight = float3(0, 0, 0);
                float flicker = 0.72 + 0.14 * sin(_Clock * 5.1 + _Seed)
                    + 0.08 * sin(_Clock * 11.3 + _Seed * 2);
                if (_RoleKind > 0.5 && _RoleKind < 1.5)
                {
                    float eyes = SoftSpot(local, float2(0.50, 0.59), float2(0.035, 0.026))
                        + SoftSpot(local, float2(0.70, 0.59), float2(0.032, 0.027));
                    livingLight = float3(0.36, 0.17, 0.015) * eyes * (0.6 + 0.4 * sin(_Clock * 1.1));
                    float ears = SoftSpot(local, float2(0.35, 0.77), float2(0.16, 0.14));
                    float head = SoftSpot(local, float2(0.53, 0.59), float2(0.28, 0.25));
                    float2 headDelta = local - float2(0.48, 0.40);
                    ornament = float2(-headDelta.y, headDelta.x) * head * sin(_Clock * 0.82) * 0.060;
                    ornament.x += ears * sin(_Clock * 1.7) * 0.012;
                }
                else if ((_RoleKind > 1.5 && _RoleKind < 2.5) || (_RoleKind > 6.5 && _RoleKind < 7.5))
                {
                    float lantern = SoftSpot(local, float2(0.78, _RoleKind < 3 ? 0.44 : 0.35), float2(0.10, 0.14));
                    livingLight = float3(0.30, 0.15, 0.025) * lantern * flicker;
                }
                else if (_RoleKind > 2.5 && _RoleKind < 3.5)
                {
                    float2 center = float2(0.64, 0.43);
                    float globe = SoftSpot(local, center, float2(0.17, 0.16));
                    float2 d = local - center;
                    ornament = float2(-d.y, d.x) * globe * sin(_Clock * 0.7) * 0.14;
                    livingLight = float3(0.10, 0.17, 0.29) * globe * (0.6 + 0.3 * sin(_Clock * 1.4));
                }
                else if (_RoleKind > 3.5 && _RoleKind < 4.5)
                {
                    float green = SoftSpot(local, float2(0.79, 0.50), float2(0.09, 0.13));
                    float purple = SoftSpot(local, float2(0.34, 0.30), float2(0.11, 0.10));
                    ornament.x = green * sin(_Clock * 1.3) * 0.014 + purple * sin(_Clock * 1.1 + 2) * 0.012;
                    float greenBubble = SoftSpot(local, float2(0.79 + sin(_Clock * 2.1) * 0.018, 0.45 + frac(_Clock * 0.32) * 0.20), float2(0.016, 0.020));
                    float purpleBubble = SoftSpot(local, float2(0.34 + cos(_Clock * 1.7) * 0.018, 0.27 + frac(_Clock * 0.27 + 0.4) * 0.16), float2(0.015, 0.018));
                    livingLight = (float3(0.08, 0.38, 0.10) * green + float3(0.30, 0.06, 0.28) * purple) * flicker
                        + float3(0.35, 0.70, 0.20) * greenBubble + float3(0.55, 0.20, 0.65) * purpleBubble;
                }
                else if (_RoleKind > 4.5 && _RoleKind < 6.5)
                {
                    float bow = SoftSpot(local, float2(0.77, 0.48), float2(0.09, 0.30));
                    float wings = _RoleKind > 5.5 ? SoftSpot(local, float2(0.24, 0.66), float2(0.16, 0.19)) : 0;
                    ornament.x = bow * sin(_Clock * 0.9) * 0.0025 + wings * sin(_Clock * 1.25) * 0.024;
                    livingLight = float3(0.14, 0.10, 0.03) * bow * (0.5 + 0.5 * sin(_Clock));
                }
                else if (_RoleKind > 7.5 && _RoleKind < 8.5)
                {
                    float ribbon = SoftSpot(local, float2(0.29, 0.42), float2(0.14, 0.16));
                    ornament.x = ribbon * sin(_Clock * 1.05) * 0.016;
                }
                shift += ornament * _ArtRect.zw * _Motion;
                // Keep the illustration rigid. Character motion belongs to separate cutouts.
                float2 uv = clamp(input.texcoord, _ArtRect.xy + 0.0001, _ArtRect.xy + _ArtRect.zw - 0.0001);
                fixed4 paper = tex2D(_MainTex, uv) + _TextureSampleAdd;
                float gold = smoothstep(0.055, 0.22, paper.r - paper.b) * smoothstep(0.27, 0.66, dot(paper.rgb, float3(0.299, 0.587, 0.114)));
                float diagonal = local.x * 0.74 + local.y * 0.26;
                float sweep = frac(_Clock * 0.115 + _Seed * 0.147) * 1.5 - 0.25;
                float shine = exp(-pow((diagonal - sweep) * 19, 2)) * gold * _Motion;
                paper.rgb += float3(0.34, 0.23, 0.08) * (shine + _RevealGlow * 0.35) * paper.a;
                paper.rgb += livingLight * _Motion * paper.a;
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
