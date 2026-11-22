// 착시 퍼즐 블록용: 빛 없이 정점 색만 씀 (면마다 밝기를 미리 칠해 둠 -> 2D 프로젝트에서도 입체로 보임)
// URP 2D 렌더러에서도 그려짐 (LightMode 태그 없는 패스 = SRPDefaultUnlit)
// _SrcBlend/_DstBlend/_ZWrite: 블록은 불투명(1,0,1), 강조 표시는 반투명(5,10,0)
// [추가] _MainTex: 블록 무늬 그림 (정점 색 x 그림). 비워 두면 흰색 = 예전과 같음
// [추가] _Scroll: 그림을 시간에 따라 흘림 (물결용, xy = 초당 이동량)
// [추가] _Cull: 배경 판은 양면 (0), 블록은 뒷면 안 그림 (2)
Shader "Sobek/IllusionUnlit"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Scroll ("Scroll (xy)", Vector) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_Cull]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Scroll;

            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + _Scroll.xy * _Time.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target { return tex2D(_MainTex, i.uv) * i.color; }
            ENDCG
        }
    }
}
