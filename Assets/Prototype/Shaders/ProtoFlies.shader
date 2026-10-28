// ゴミ箱にたかる蠅。一匹は画面上で _PixelSize ピクセル四方の点。距離によらず同じ大きさ。
// 一匹ごとに四頂点を同じ位置に置き、uv（-0.5〜0.5 の角）の向きへ画面のピクセル単位で広げる。
// 点の図形（MeshTopology.Points）は 1 ピクセル固定で、SMAA で半分ほど薄まるので、大きさを決められる四角で描く。
// 色と透明度は頂点色で受け取る（ProtoFlies が一匹ごとの淡入・淡出を書き込む）。
Shader "KyoumoMushoku/ProtoFlies"
{
    Properties
    {
        _PixelSize ("Pixel Size", Float) = 1
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
                float _PixelSize;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 corner : TEXCOORD0;
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
                positionCS.xy += input.corner * _PixelSize * 2.0 * positionCS.w / _ScreenParams.xy;
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
