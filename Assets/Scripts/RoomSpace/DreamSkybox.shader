Shader "Whitespace/DreamSkybox"
{
    Properties
    {
        _ElevScale  ("Elevation Scale", Float) = 1.0
        _SliceCount ("Slice Count",     Int)   = 8
        _Swirl      ("Swirl (awe)",     Float) = 0.0
        _FadeTint   ("Horizon Fade Tint (content)", Color) = (1, 1, 1, 1)
        _FadeSpread ("Fade Spread (content excess)", Float) = 0.15
        [NoScaleOffset] _Slices ("Slice Array", 2DArray) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma require 2darray
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_SLICES 32
            #define PI 3.14159265

            TEXTURE2D_ARRAY(_Slices);
            SAMPLER(sampler_Slices);
            
            float _ScrollSpeeds[MAX_SLICES];
            float _ScrollOffsets[MAX_SLICES];
            float _Opacity[MAX_SLICES];
            int   _SliceCount;
            float _ElevScale;
            float _Swirl;
            half4 _FadeTint;
            float _FadeSpread;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 viewDir : TEXCOORD0; };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.viewDir    = IN.positionOS.xyz;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.viewDir);

                //converts direction to spherical coordinates
                float azimuth   = atan2(dir.x, dir.z) / (2.0 * PI) + 0.5;
                float elevation = dir.y * _ElevScale * 0.5 + 0.5;
                
                //frac() keeps azimuth in [0,1) so the slice index below never goes negative
                azimuth += _Swirl * sin(elevation * PI * 2.0 + _Time.y * 0.15) * 0.15;  //awe: slow spiral warp of the dome
                azimuth = frac(azimuth);

                int   sliceIdx = (int)floor(azimuth * _SliceCount) % _SliceCount;
                float u = azimuth;
                float v = elevation + _Time.y * _ScrollSpeeds[sliceIdx] + _ScrollOffsets[sliceIdx];

                half4 col   = SAMPLE_TEXTURE2D_ARRAY(_Slices, sampler_Slices, float2(u, v), sliceIdx);

                //fades out the top and bottom of the skybox
                //content warms this fade toward its tint.
                float heightFade = pow(1.0 - saturate(abs(dir.y)), _FadeSpread);
                col.rgb = lerp(_FadeTint.rgb, col.rgb, heightFade) * 6.0;
                return col;
            }
            ENDHLSL
        }
    }
}
