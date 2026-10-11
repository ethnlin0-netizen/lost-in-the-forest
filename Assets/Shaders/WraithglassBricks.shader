Shader "LostInTheForest/WraithglassBricks"
{
    Properties
    {
        [PerRendererData] _MainTex ("Brick texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CrackColor ("Ice streaks", Color) = (.4,.8,1,1)
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
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CrackColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // Isolate red cracks and their glow, preserving the violet stone pixels.
                half redExcess = texel.r - max(texel.g, texel.b);
                half streak = smoothstep(.025, .12, redExcess);
                half3 frost = _CrackColor.rgb * (.25 + .75 * texel.r);
                texel.rgb = lerp(texel.rgb, frost, streak);
                return texel * input.color;
            }
            ENDHLSL
        }
    }
}
