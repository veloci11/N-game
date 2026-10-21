Shader "NGame/Living Catalogue Paper UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Paper world", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ArtRect ("Atlas rectangle", Vector) = (0,0,1,1)
        _Kind ("Game illustration", Float) = 1
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
            float _Clock, _Motion, _Kind;
            float4 _ArtRect;
            float SoftSpot(float2 location, float2 center, float2 extent)
            { float2 d=(location-center)/extent; return exp(-dot(d,d)*2.5); }
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
                float2 local=(input.uv-_ArtRect.xy)/max(_ArtRect.zw,float2(.00001,.00001));
                float2 displacement=float2(0,0);
                float glow=0;
                if(_Kind<1.5)
                {
                    float head=SoftSpot(local,float2(.42,.57),float2(.38,.34));
                    float2 pivot=local-float2(.34,.22);
                    displacement=float2(-pivot.y,pivot.x)*head*sin(_Clock*.85)*.035;
                    displacement.x+=SoftSpot(local,float2(.32,.83),float2(.18,.14))*sin(_Clock*1.6)*.006;
                    glow=SoftSpot(local,float2(.50,.63),float2(.06,.035))*(.5+.5*sin(_Clock*1.2));
                }
                else if(_Kind<2.5)
                {
                    float crown=SoftSpot(local,float2(.5,.57),float2(.34,.33));
                    displacement.y=crown*sin(_Clock*1.1)*.004;
                    float sweep=frac(_Clock*.13);
                    glow=pow(saturate(1-abs(local.x-sweep)*14),3)*crown*.3;
                }
                else
                {
                    float hat=smoothstep(.60,.78,local.y)*(1-smoothstep(.9,1,local.y));
                    float2 pivot=local-float2(.50,.60);
                    displacement=float2(-pivot.y,pivot.x)*hat*sin(_Clock*1.4)*.065;
                    displacement.y+=SoftSpot(local,float2(.5,.35),float2(.25,.3))*sin(_Clock*.9)*.002;
                }
                float2 uv=_ArtRect.xy+clamp(local+displacement*_Motion,.001,.999)*_ArtRect.zw;
                fixed4 color=(tex2D(_MainTex,uv)+_TextureSampleAdd)*input.color;
                color.rgb+=float3(.16,.085,.005)*glow*_Motion;
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
