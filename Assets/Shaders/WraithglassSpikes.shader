Shader "LostInTheForest/WraithglassSpikes"
{
    Properties
    {
        _MainTex ("Corrupted wood", 2D) = "white" {}
        _TintColor ("Wood tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _TintColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS);
                // Derive an undistorted texture coordinate from the existing unit triangle.
                output.uv=input.positionOS.xy+.5;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // A cropped bark sample keeps the grain readable on small thorns.
                half height=floor(saturate(input.uv.y)*8)/8;
                half3 color=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv*.35+.3).rgb*_TintColor.rgb;
                color*=lerp(1.1,1.6,height);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
