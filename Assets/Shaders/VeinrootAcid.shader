Shader "LostInTheForest/VeinrootAcid"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite mask", 2D) = "white" {}
        _DeepColor ("Deep acid", Color) = (.11,.018,.19,1)
        _FlowColor ("Acid currents", Color) = (.43,.07,.52,1)
        _FoamColor ("Surface and bubbles", Color) = (.88,.34,.80,1)
        _SurfaceMode ("Pool (0) / surface (1)", Float) = 0
        _FlowSpeed ("Visual flow speed", Range(0,2)) = .35
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
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float2 worldXY : TEXCOORD1; half4 color : COLOR; };
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor, _FlowColor, _FoamColor;
                float _SurfaceMode, _FlowSpeed;
            CBUFFER_END
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                    lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
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
                float t = _Time.y * _FlowSpeed;
                float2 p = input.worldXY;
                float swirl = Noise(p * float2(.7,1.8) + float2(t, -t*.25));
                float ripple = .5 + .5 * sin(p.y * 13 + p.x * 1.4 + swirl * 6 - t * 3);
                half3 color = lerp(_DeepColor.rgb, _FlowColor.rgb, saturate(swirl*.85 + ripple*.25));
                // World coordinates keep the pattern density identical in both pools.
                float2 bubbleGrid = p * float2(1.6,2.4) + float2(t*.15,-t*.45);
                float2 cell = floor(bubbleGrid);
                float seed = Hash(cell);
                float2 center = float2(.25 + seed*.5, .25 + Hash(cell+17)*.5);
                float radius = .055 + seed*.09;
                float ring = 1 - smoothstep(.012,.035,abs(length(frac(bubbleGrid)-center)-radius));
                color = lerp(color, _FoamColor.rgb, ring * step(.7,seed)*.55);
                if (_SurfaceMode > .5)
                {
                    float crest = .45 + .18*sin(p.x*4 + t*3) + .1*sin(p.x*9-t*2);
                    float foam = smoothstep(crest-.2,crest+.06,input.uv.y);
                    color = lerp(_FlowColor.rgb, _FoamColor.rgb, foam*.8 + ripple*.15);
                }
                return half4(color * input.color.rgb, SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a * input.color.a);
            }
            ENDHLSL
        }
    }
}
