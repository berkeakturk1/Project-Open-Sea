Shader "Custom/URP/SimpleSandFloor"
{
    Properties
    {
        [Header(Sand Textures)]
        _SandColor("Sand Color", Color) = (0.8, 0.7, 0.5, 1)
        _SandTexture("Sand Diffuse", 2D) = "white" {}
        _SandNormal("Sand Normal", 2D) = "bump" {}
        _SandRoughness("Sand Roughness", 2D) = "gray" {}
        _SandDisplacement("Sand Displacement", 2D) = "gray" {}
        
        [Header(Mapping Settings)]
        _TextureScale("Texture Scale", Range(1, 100)) = 20
        _DisplacementStrength("Displacement Strength", Range(0, 2)) = 0.3
        _NormalStrength("Normal Strength", Range(0, 2)) = 1.0
        
        [Header(Physical Properties)]
        _Roughness("Roughness Multiplier", Range(0, 1)) = 0.8
        _Metallic("Metallic", Range(0, 1)) = 0.0
        
        [Header(Animation)]
        _WaveSpeed("Wave Speed", Range(0, 5)) = 0.5
        _WaveStrength("Wave Strength", Range(0, 1)) = 0.1
        _WaveScale("Wave Scale", Range(1, 50)) = 10
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
            float4 _SandColor;
            float4 _SandTexture_ST;  // Add texture transform
            float4 _SandNormal_ST;
            float4 _SandRoughness_ST;
            float4 _SandDisplacement_ST;
            float _TextureScale;
            float _DisplacementStrength;
            float _NormalStrength;
            float _Roughness;
            float _Metallic;
            float _WaveSpeed;
            float _WaveStrength;
            float _WaveScale;
        CBUFFER_END
        
        TEXTURE2D(_SandTexture);
        SAMPLER(sampler_SandTexture);
        
        TEXTURE2D(_SandNormal);
        SAMPLER(sampler_SandNormal);
        
        TEXTURE2D(_SandRoughness);
        SAMPLER(sampler_SandRoughness);
        
        TEXTURE2D(_SandDisplacement);
        SAMPLER(sampler_SandDisplacement);
        
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 fogFactorAndVertexLight : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
                float3 viewDirWS : TEXCOORD6;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                // Apply procedural animation to vertices
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                
                // Animate ocean floor with subtle waves
                float time = _Time.y * _WaveSpeed;
                float2 waveUV = worldPos.xz / _WaveScale;
                
                // Simple wave height
                float waveHeight = sin(waveUV.x + time) * cos(waveUV.y + time * 0.8) * _WaveStrength;
                worldPos.y += waveHeight;
                
                // Convert back to object space
                IN.positionOS.xyz = TransformWorldToObject(worldPos);
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.tangentWS = normalInputs.tangentWS;
                OUT.bitangentWS = normalInputs.bitangentWS;
                
                // Use the original UV for texture sampling
                OUT.uv = TRANSFORM_TEX(IN.uv, _SandTexture);
                
                // Calculate fog factor
                half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                
                // Calculate vertex lighting
                half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
                
                OUT.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                
                return OUT;
            }
            
            float4 frag(Varyings IN) : SV_Target
            {
                // Use the interpolated UV for texture sampling
                float2 uv = IN.uv;
                
                // Sample sand texture
                float4 sandColor = SAMPLE_TEXTURE2D(_SandTexture, sampler_SandTexture, uv);
                
                // Sample sand roughness
                float roughnessMap = SAMPLE_TEXTURE2D(_SandRoughness, sampler_SandRoughness, uv).r;
                
                // Sample and apply normal map
                float3 normalMap = UnpackNormal(SAMPLE_TEXTURE2D(_SandNormal, sampler_SandNormal, uv));
                normalMap.xy *= _NormalStrength;
                normalMap.z = sqrt(1.0 - saturate(dot(normalMap.xy, normalMap.xy)));
                
                // Transform normal to world space
                float3 T = normalize(IN.tangentWS);
                float3 B = normalize(IN.bitangentWS);
                float3 N = normalize(IN.normalWS);
                float3x3 TBN = float3x3(T, B, N);
                float3 worldNormal = mul(normalMap, TBN);
                
                // Calculate the final sand color
                float3 albedo = _SandColor.rgb * sandColor.rgb;
                
                // Calculate final roughness
                float roughness = roughnessMap * _Roughness;
                
                // Setup URP lighting input data
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = IN.positionWS;
                lightingInput.normalWS = normalize(worldNormal);
                lightingInput.viewDirectionWS = normalize(IN.viewDirWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                lightingInput.fogCoord = IN.fogFactorAndVertexLight.x;
                lightingInput.vertexLighting = IN.fogFactorAndVertexLight.yzw;
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                
                // Setup surface data
                SurfaceData surfaceInput = (SurfaceData)0;
                surfaceInput.albedo = albedo;
                surfaceInput.metallic = _Metallic;
                surfaceInput.smoothness = 1.0 - roughness;
                surfaceInput.normalTS = normalMap;
                surfaceInput.occlusion = 1.0;
                surfaceInput.emission = 0;
                surfaceInput.alpha = 1.0;
                
                // Compute final lighting
                return UniversalFragmentPBR(lightingInput, surfaceInput);
            }
            ENDHLSL
        }
        
        // [Rest of the shader remains the same as in the original]
    }
    FallBack "Universal Render Pipeline/Lit"
}