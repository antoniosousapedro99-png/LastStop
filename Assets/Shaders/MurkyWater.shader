// The sewers' water: murky but see-through. Through a thin layer you see what's under it (the floor, steps,
// rubbish), tinted green; the more water there is in the way, the more it fades to opaque green-brown murk
// ('_Clarity' metres of water hide about two thirds). How much water is in the way comes from the camera's
// depth picture (URP's depth texture). The rippling surface pictures (WaterFrames sets _BaseMap) brighten it
// a little and tilt its surface, so lamps and the torch catch small moving glints instead of one flat shine.
// The picture is laid in world space twice, at two sizes and angles, each drifting slowly its own way, and
// the two are blended: no square grid shows, and neighbouring surfaces join without a seam.
Shader "LastStop/MurkyWater"
{
    Properties
    {
        _BaseMap ("Ripple frame", 2D) = "grey" {}
        _ShallowColor ("Shallow colour (alpha = how see-through when thin)", Color) = (0.32, 0.38, 0.22, 0.35)
        _DeepColor ("Deep colour", Color) = (0.07, 0.09, 0.05, 1)
        _Clarity ("Clarity (metres)", Float) = 1.2
        _RippleStrength ("Ripple strength", Range(0, 1)) = 0.35
        _Bumpiness ("Ripple tilt", Range(0, 3)) = 1
        _SpecColor ("Glint colour", Color) = (0.35, 0.35, 0.3, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.78
        _RippleSize ("Ripple picture size (metres)", Float) = 4.7
        _Drift ("Drift (metres per second)", Float) = 0.035
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

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #define _SPECULAR_COLOR   // Blinn-Phong glints

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _ShallowColor, _DeepColor, _SpecColor;
                half _Clarity, _RippleStrength, _Bumpiness, _Smoothness;
                float _RippleSize, _Drift;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float eyeDepth : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.eyeDepth = -p.positionVS.z;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half Picture(float2 uv) { return dot(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb, half3(0.3, 0.59, 0.11)); }

            // The ripple height at a world spot (x, z): the picture at two sizes and angles, drifting apart, blended.
            half Ripple(float2 xz)
            {
                float t = _Time.y * _Drift;
                float2 a = xz / _RippleSize + t * float2(0.8, 0.45);
                float2 r = float2(xz.x * 0.799 - xz.y * 0.602, xz.x * 0.602 + xz.y * 0.799);   // turned 37 degrees
                float2 b = r / (_RippleSize * 1.68) - t * float2(0.5, -0.7);
                return (Picture(a) + Picture(b + 0.37)) * 0.5;
            }

            half4 frag(Varyings i, bool front : SV_IsFrontFace) : SV_Target
            {
                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);

                // How much water lies between the surface and whatever is behind it.
                float behind = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float thickness = max(0, behind - i.eyeDepth);
                half murk = 1 - exp(-thickness / max(_Clarity, 0.01));
                if (!front) murk = 1;   // seen from under the water: just the murk overhead

                // The ripple picture: a little brightness, and a tilt from its slope for the glints.
                float2 d = float2(0.02, 0);   // a small step across the water (metres)
                float2 xz = i.positionWS.xz;
                half h = Ripple(xz), hx = Ripple(xz + d.xy), hz = Ripple(xz + d.yx);
                float3 normal = normalize(float3((h - hx) * _Bumpiness * 6, 1, (h - hz) * _Bumpiness * 6));
                if (!front) normal = -normal;

                half3 albedo = lerp(_ShallowColor.rgb, _DeepColor.rgb, murk) * lerp(1, 0.6 + h * 0.8, _RippleStrength);
                half alpha = lerp(_ShallowColor.a, 1, murk);

                InputData input = (InputData)0;
                input.positionWS = i.positionWS;
                input.normalWS = normal;
                input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                input.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                input.fogCoord = i.fogFactor;
                input.normalizedScreenSpaceUV = screenUV;
                input.shadowMask = half4(1, 1, 1, 1);
                input.bakedGI = SampleSH(normal);

                SurfaceData water = (SurfaceData)0;
                water.albedo = albedo;
                water.alpha = alpha;
                water.occlusion = 1;
                water.specular = _SpecColor.rgb;
                water.smoothness = _Smoothness;

                half4 colour = UniversalFragmentBlinnPhong(input, water);
                colour.rgb = MixFog(colour.rgb, i.fogFactor);
                colour.a = alpha;
                return colour;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
