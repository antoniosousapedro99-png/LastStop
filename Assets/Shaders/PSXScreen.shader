// Full-screen PS1 look, drawn after all other effects: the picture is made slightly chunky
// (pixelated), its colours are reduced to fewer steps, and a fine 4x4 checker pattern ("dither")
// hides the banding between steps, like the PlayStation did. The result is blended over the normal
// image by _Strength (0.15 = 15%), so it adds texture without hiding detail.
Shader "LastStop/PSXScreen"
{
    Properties
    {
        _Strength ("Strength", Range(0, 1)) = 0.15
        _PixelSize ("Pixel size (screen pixels per PS1 pixel)", Float) = 3
        _ColorSteps ("Colour steps per channel", Float) = 32
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Name "PSXScreen"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _Strength;
            float _PixelSize;
            float _ColorSteps;

            // 4x4 Bayer matrix: a fixed pattern of thresholds between 0 and 1.
            static const float Bayer[16] = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, uv);

                // Chunky pixels: snap the sample point to a coarser grid.
                float2 grid = _ScreenParams.xy / max(_PixelSize, 1.0);
                float2 cell = floor(uv * grid);
                half3 colour = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, (cell + 0.5) / grid).rgb;

                // Fewer colour steps, with the dither pattern deciding which way each pixel rounds.
                uint index = (uint)fmod(cell.y, 4.0) * 4 + (uint)fmod(cell.x, 4.0);
                float threshold = (Bayer[index] + 0.5) / 16.0;
                half3 psx = floor(colour * _ColorSteps + threshold) / _ColorSteps;

                return half4(lerp(original.rgb, psx, _Strength), original.a);
            }
            ENDHLSL
        }
    }
}
