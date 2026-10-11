Shader "LostInTheForest/VeinrootTerrain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Existing terrain texture", 2D) = "white" {}
        _ShadowColor ("Mortar / root shadow", Color) = (0.10,0.075,0.055,1)
        _LightColor ("Weathered stone", Color) = (0.43,0.32,0.22,1)
        _AccentColor ("Foliage", Color) = (0.48,0.22,0.16,1)
        _HighlightColor ("Amber highlights", Color) = (0.76,0.51,0.24,1)
        _SurfaceMode ("Stone (0) / foliage (1)", Float) = 0
        _WorldMode ("Sprite UV (0) / world XY (1) / world X (2)", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 worldXY : TEXCOORD1; half4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor, _LightColor, _AccentColor, _HighlightColor;
                float _SurfaceMode, _WorldMode;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                output.worldXY = world.xy;
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                if (_WorldMode > 0.5 && _WorldMode < 1.5) uv = input.worldXY;
                else if (_WorldMode > 1.5) uv.x = input.worldXY.x;
                half4 source = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                // Retain the original pixel pattern; only remap its palette in this material.
                half3 original = source.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                    original = LinearToSRGB(original);
                #endif
                half brightness = saturate((dot(original, half3(.299,.587,.114)) - .12h) / .36h);
                half3 color = lerp(_ShadowColor.rgb, _LightColor.rgb, brightness);
                if (_SurfaceMode > .5)
                {
                    half foliage = step(original.r, original.g);
                    half amber = step(original.b * .8h, original.r);
                    color = lerp(color, lerp(_AccentColor.rgb, _HighlightColor.rgb, brightness * .5h), foliage);
                    color = lerp(color, _HighlightColor.rgb, amber);
                }
                return half4(color * input.color.rgb, source.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
