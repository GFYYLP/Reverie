Shader "Whitespace/DreamscapeLit"
{
    Properties
    {
        _FillColor    ("Fill Color",   Color)       = (1, 1, 1, 1)
        _AmbientMin   ("Ambient Min",  Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Opaque"
            "Queue"           = "Geometry"
            "RenderPipeline"  = "UniversalPipeline"
            "DecalCompatible" = "True"
        }

        //Pass 1: Lit Fill
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FillColor;
                float  _AmbientMin;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);

                // main light 
                Light mainLight  = GetMainLight();
                float diffuse    = saturate(dot(normalWS, mainLight.direction));

                // clamp diffuse to ambient floor so no surface goes fully black
                float lighting   = max(diffuse * mainLight.distanceAttenuation, _AmbientMin);

                // additional lights (additive diffuse only)
                #ifdef _ADDITIONAL_LIGHTS
                uint lightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < lightCount; i++)
                {
                    Light light   = GetAdditionalLight(i, IN.positionWS);
                    float contrib = saturate(dot(normalWS, light.direction)) * light.distanceAttenuation;
                    lighting      = saturate(lighting + contrib * 0.5);
                }
                #endif

                float3 color = _FillColor.rgb;

                // apply DBuffer decals onto albedo
                #ifdef _DBUFFER
                ApplyDecalToBaseColor(IN.positionCS, color);
                #endif

                return half4(color * lighting, 1);
            }
            ENDHLSL
        }

        //Pass 2: DepthOnly (required for _CameraDepthTexture) 
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull Back
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   DOVert
            #pragma fragment DOFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            Varyings DOVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 DOFrag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        //Pass 3: DepthNormals (required for decal projection) 
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex   DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings DNVert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 DNFrag(Varyings IN) : SV_Target
            {
                return half4(normalize(IN.normalWS) * 0.5 + 0.5, 1);
            }
            ENDHLSL
        }
    }
}
