// Words painted on a sign (TextMesh text): each letter is the font's shape in its own colour (rich text colours
// come through as vertex colours), lit by the lamps and the torch like the paint around it, so it's dark where
// the wall is dark. Unlike the built-in text shader it is hidden by anything in front of it (no reading through walls).
// The font's texture is handed over at run time by PaintedText.cs.
Shader "LastStop/PaintedText"
{
    Properties
    {
        _MainTex ("Font texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(float3(0, 0, -1));   // TextMesh letters face their -Z
                o.uv = v.uv;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a * i.color.a;
                clip(alpha - 0.02);

                InputData input = (InputData)0;
                input.positionWS = i.positionWS;
                input.normalWS = normalize(front ? i.normalWS : -i.normalWS);
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                input.fogCoord = i.fogFactor;
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                input.shadowMask = half4(1, 1, 1, 1);
                input.bakedGI = SampleSH(input.normalWS);

                SurfaceData paint = (SurfaceData)0;
                paint.albedo = i.color.rgb;
                paint.alpha = alpha;
                paint.occlusion = 1;
                paint.smoothness = 0.15;   // flat paint: no shine to speak of

                half4 colour = UniversalFragmentBlinnPhong(input, paint);
                colour.rgb = MixFog(colour.rgb, i.fogFactor);
                colour.a = alpha;
                return colour;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
