// 触れる物の縁取り（裏返しの殻）。殻の網は法線を位置ごとに平均したもの（SliceOutlineBaker が焼く）。
// 表を捨てて裏だけを描くので、物の輪郭の外側にだけ縁が出る。幅は画面のピクセルで決める（遠くても近くても同じ太さ）。
// 色と幅は ProtoOutline が物ごとに MaterialPropertyBlock で渡す。
Shader "KyoumoMushoku/ProtoOutline"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.78, 0.35, 1)
        _Width ("Width (px)", Float) = 2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" "RenderType" = "Transparent" }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            // 部品の継ぎ目（ふたと本体の境など）で殻の裏と本体の表が同じ深さになり、縁が点線に化けるのを防ぐ。
            Offset 1, 1
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float2 normalCS = mul((float3x3)GetWorldToHClipMatrix(), normalWS).xy;
                float length2 = dot(normalCS, normalCS);
                if (length2 > 1e-10)
                {
                    // 正規化した法線の向きへ、_Width ピクセル分だけ押し出す（w を掛けて透視の割り算を打ち消す）。
                    positionCS.xy += normalCS * rsqrt(length2) * _Width * positionCS.w * 2.0 / _ScreenParams.xy;
                }

                output.positionCS = positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
