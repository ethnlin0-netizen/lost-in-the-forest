Shader "LostInTheWoods/EnemyAnimatedArtwork"
{
    Properties
    {
        _MainTex ("Artwork", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Blink ("Stone eye blink", Range(0,1)) = 0
        _ShapeMode ("Particle shape", Float) = 0
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
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Blink;
                half _ShapeMode;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                // Ivory eyes within the stone face close without covering the mouth or helmet.
                half eyes = step(.78h,color.r)*step(.72h,color.g)*step(.62h,color.b)
                    *step(.28h,input.uv.x)*step(input.uv.x,.72h)
                    *step(.32h,input.uv.y)*step(input.uv.y,.72h);
                color.rgb = lerp(color.rgb, half3(.16h,.13h,.22h), eyes*_Blink);
                float2 p = input.uv*2-1;
                if (_ShapeMode > .5h && _ShapeMode < 1.5h)
                    color.a *= saturate(1-dot(p,p));
                else if (_ShapeMode > 1.5h && _ShapeMode < 2.5h)
                    color.a *= 1-smoothstep(.70,1,abs(p.x)*1.5+abs(p.y)*.7);
                else if (_ShapeMode > 2.5h)
                    color.a *= 1-smoothstep(.7,1,max(abs(p.x),abs(p.y))+.25*abs(p.x+p.y));
                return color*input.color*_Tint;
            }
            ENDHLSL
        }
    }
}
