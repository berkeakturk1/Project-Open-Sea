Shader "Hidden/URPSSFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FogColor("Fog Color", Color) = (0.5, 0.5, 0.5, 1)
        _FogDensity("Fog Density", Float) = 1.0
        _FogOffset("Fog Offset", Float) = 0.0
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "SSFog"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv         : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _FogColor;
                float  _FogDensity;
                float  _FogOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample the color from the source texture
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Get the scene depth at this pixel
                #if UNITY_REVERSED_Z
                    float depth = SampleSceneDepth(input.uv);
                #else
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(input.uv));
                #endif

                // Convert depth to linear eye space distance
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float viewDistance = linearDepth;
                
                // Calculate fog factor using exponential squared fog
                float fogFactor = (_FogDensity / sqrt(log(2))) * max(0.0, viewDistance - _FogOffset);
                fogFactor = exp2(-fogFactor * fogFactor);

                // Blend fog color and scene color
                half4 fogOutput = lerp(_FogColor, col, saturate(fogFactor));
                return fogOutput;
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/Blit"
}
