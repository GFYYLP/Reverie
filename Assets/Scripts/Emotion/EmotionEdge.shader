Shader "Whitespace/ScreenSpaceOutline"
{
    Properties
    {
        _OutlineColor  ("Outline Color",       Color)        = (0, 0, 0, 1)
        _OutlineWidth  ("Outline Width",        Range(1, 4))  = 1
        _DepthThresh   ("Depth Threshold",      Range(0, 1))  = 0.01
        _NormalThresh  ("Normal Threshold",     Range(0, 1))  = 0.3
        _EdgeSoftness  ("Edge Softness (content)",   Range(0, 1)) = 0
        _JitterAmount  ("Jitter Amount (unease)",    Range(0, 10)) = 0
        _WarpAmount    ("Warp Amount (awe)",         Range(0, 1)) = 0
        _WarpFrequency ("Warp Frequency (awe)",      Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "ScreenSpaceOutline"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D_X(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _DepthThresh;
                float  _NormalThresh;
                float  _EdgeSoftness;
                float  _JitterAmount;
                float  _WarpAmount;
                float  _WarpFrequency;
            CBUFFER_END
            
            float SampleDepth(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
            }

            float3 SampleNormal(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv).rgb * 2 - 1;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv     = IN.texcoord;
                float2 texel  = _OutlineWidth / _ScreenParams.xy;

                //awe: slow, large-wavelength warp
                float2 warp = float2(
                    sin(uv.y * _WarpFrequency * 6.283 + _Time.y * 0.6),
                    cos(uv.x * _WarpFrequency * 6.283 + _Time.y * 0.5)
                ) * _WarpAmount * 0.05;

                //unease: trembling edge
                float2 jitterSeed = uv * _ScreenParams.xy + _Time.y * 37.0;
                float2 jitter = (float2(
                    frac(sin(dot(jitterSeed, float2(12.9898, 78.233))) * 43758.5453),
                    frac(sin(dot(jitterSeed, float2(93.9898, 67.345))) * 12543.253)
                ) - 0.5) * _JitterAmount / _ScreenParams.xy;

                float2 offset = warp + jitter;

                float2 uv0 = uv + offset + float2( texel.x,  texel.y);
                float2 uv1 = uv + offset + float2(-texel.x,  texel.y);
                float2 uv2 = uv + offset + float2( texel.x, -texel.y);
                float2 uv3 = uv + offset + float2(-texel.x, -texel.y);

                float d0 = SampleDepth(uv0);
                float d1 = SampleDepth(uv1);
                float d2 = SampleDepth(uv2);
                float d3 = SampleDepth(uv3);
                float depthEdge = sqrt(pow(d0 - d3, 2) + pow(d1 - d2, 2));

                float3 n0 = SampleNormal(uv0);
                float3 n1 = SampleNormal(uv1);
                float3 n2 = SampleNormal(uv2);
                float3 n3 = SampleNormal(uv3);
                float normalEdge = sqrt(dot(n0 - n3, n0 - n3) + dot(n1 - n2, n1 - n2));

                //content: feathered glow
                float softness = max(_EdgeSoftness, 0.0001) * 0.05;
                float edge = smoothstep(_DepthThresh,  _DepthThresh  + softness, depthEdge)
                           + smoothstep(_NormalThresh, _NormalThresh + softness, normalEdge);
                edge = saturate(edge);

                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                return lerp(scene, _OutlineColor, edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
