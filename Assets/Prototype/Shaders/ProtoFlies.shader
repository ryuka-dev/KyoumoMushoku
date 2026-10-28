// ゴミ箱にたかる蠅。一匹は画素絵のような三つの四角：真ん中に黒い胴、左右に半透明の灰色の翅（胴より少し高い）。
// 一匹ごとに全頂点を同じ位置（蠅の中心）に置き、uv（胴の幅を 1 とした蠅の中での位置）の分だけ画面に向けて広げる。
// 大きさは世界の寸法（_BodySize m）で、近いほど大きく見える。遠くても胴が _MinBodyPixels ピクセルより小さくはならない。
// 色と透明度は頂点色で受け取る（ProtoFlies が淡入・淡出と、翅の羽ばたきを書き込む）。
Shader "KyoumoMushoku/ProtoFlies"
{
    Properties
    {
        _BodySize ("Body Size (m)", Float) = 0.01
        _MinBodyPixels ("Min Body Pixels", Float) = 1.5
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Name "Flies"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BodySize;
                float _MinBodyPixels;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 offset : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float4x4 projection = GetViewToHClipMatrix();
                float2 scale = abs(float2(projection[0][0], projection[1][1]));
                // 胴の幅（m）。遠くて画面上で小さくなりすぎるときは、最小のピクセル数に当たる幅まで広げる。
                float minimum = _MinBodyPixels * 2.0 * positionCS.w / (_ScreenParams.y * scale.y);
                float size = max(_BodySize, minimum);
                positionCS.xy += input.offset * size * scale;
                output.positionCS = positionCS;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return input.color;
            }
            ENDHLSL
        }
    }
}
