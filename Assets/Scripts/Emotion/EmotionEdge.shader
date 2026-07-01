Shader "Whitespace/ScreenSpaceOutline"
{
    Properties
    {
        _OutlineColor  ("Outline Color",       Color)        = (0, 0, 0, 1)
        _OutlineWidth  ("Outline Width",        Range(1, 4))  = 1
        _DepthThresh   ("Depth Threshold",      Range(0, 1))  = 0.01
        _NormalThresh  ("Normal Threshold",     Range(0, 1))  = 0.3
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
                
                float2 uv0 = uv + float2( texel.x,  texel.y);
                float2 uv1 = uv + float2(-texel.x,  texel.y);
                float2 uv2 = uv + float2( texel.x, -texel.y);
                float2 uv3 = uv + float2(-texel.x, -texel.y);
                
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

                float edge = step(_DepthThresh,  depthEdge)
                           + step(_NormalThresh, normalEdge);
                edge = saturate(edge);

                half4 scene = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                return lerp(scene, _OutlineColor, edge * _OutlineColor.a);
            }
            ENDHLSL
        }
    }
}
