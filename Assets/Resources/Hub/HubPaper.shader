Shader "NGame/Living Universal Storybook UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Paper world", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Clock ("Paper clock", Float) = 0
        _Motion ("Motion amount", Float) = 1
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex PaperVertex
            #pragma fragment PaperFragment
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 localPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect;
            float _Clock, _Motion;
            v2f PaperVertex(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv; output.color = input.color * _Color;
                return output;
            }
            fixed4 PaperFragment(v2f input) : SV_Target
            {
                float2 uv = input.uv;
                // The central reading field remains still. Only the book's two page fans and border foliage move.
                float pageArea = smoothstep(.37,.39,uv.y) * (1-smoothstep(.44,.46,uv.y))
                    * smoothstep(.06,.15,uv.x) * (1-smoothstep(.86,.94,uv.x));
                float distanceToSpine = abs(uv.x-.5);
                float pageLift = sin(_Clock*1.45-distanceToSpine*20) * distanceToSpine * .021 * pageArea;
                float foliage = (1-smoothstep(.06,.24,uv.x) + smoothstep(.76,.94,uv.x))
                    * smoothstep(.40,.46,uv.y) * (1-smoothstep(.69,.78,uv.y));
                float sway = sin(_Clock*.82+uv.y*13) * foliage;
                float wolf = smoothstep(.53,.68,uv.x) * (1-smoothstep(.93,1,uv.x))
                    * smoothstep(.54,.62,uv.y) * (1-smoothstep(.77,.81,uv.y));
                float headTurn = sin(_Clock*.74) * wolf * .012;
                uv.x += (sway*.006 + pageLift*.24 - (uv.y-.55)*headTurn) * _Motion;
                uv.y += (pageLift + cos(_Clock*.66+uv.x*8)*foliage*.002 + (uv.x-.68)*headTurn) * _Motion;
                fixed4 color = (tex2D(_MainTex,clamp(uv,.001,.999)) + _TextureSampleAdd) * input.color;
                float pageShine = pow(saturate(1-abs(distanceToSpine-frac(_Clock*.09)) * 15),3) * pageArea;
                color.rgb += float3(.035,.022,.004) * pageShine * _Motion;
                float2 paperBand = float2(input.uv.x, (input.uv.y-.365)/.425);
                color.a *= smoothstep(0,.025,paperBand.x) * (1-smoothstep(.975,1,paperBand.x))
                    * smoothstep(0,.035,paperBand.y) * (1-smoothstep(.985,1,paperBand.y));
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.localPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
