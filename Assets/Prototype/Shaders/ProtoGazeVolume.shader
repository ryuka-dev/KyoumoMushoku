// 警官の視線の扇（ProtoPoliceGaze）を、地面に沿って塗るための立体の材質。
// 扇の形を縦に伸ばした薄い立体を描き、型抜き（ステンシル）で「立体の中にある、画面に見えている表面」だけを塗る。
// 一つのシェーダーを、材質ごとに描き方（型抜きの読み書き・深度の比べ方・裏表・色の混ぜ方）を変えて何度か使う：
//   印をつける（裏面・深度が奥）→ 印を消す（表面・深度が手前）→ 印のある所だけ塗る。
// 塗りは「今の色に比例して明るくする」（Blend DstColor One）。暗い所は暗いまま明るくなるので、夜は夜の暗さになる。
// 明るくする割合は、頂点色の赤（立体の上の蓋で、警官の足もとが 1・届く端が 0）で近い所と遠い所の間をつなぐ。
Shader "KyoumoMushoku/ProtoGazeVolume"
{
    Properties
    {
        _GainNear ("Gain Near", Float) = 0.1
        _GainFar ("Gain Far", Float) = 0.1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
        _ColorMask ("Color Mask", Float) = 15
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 1
        _StencilRef ("Stencil Ref", Float) = 0
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp", Float) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass ("Stencil Pass", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "GazeVolume"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZTest [_ZTest]
            ZWrite Off
            ColorMask [_ColorMask]
            Blend [_SrcBlend] [_DstBlend]

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _GainNear;
                float _GainFar;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half near : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.near = input.color.r;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half gain = lerp(_GainFar, _GainNear, saturate(input.near));
                return half4(gain, gain, gain, 1);
            }
            ENDHLSL
        }
    }
}
