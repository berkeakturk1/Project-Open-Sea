Shader "Custom/URP/Terrain"
{
    Properties
    {
        testTexture("Texture", 2D) = "white" {}
        testScale("Scale", Float) = 1
        
        // URP specific properties
        [HideInInspector] _BaseMap("Base Map", 2D) = "white" {}
        [HideInInspector] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Roughness("Global Roughness", Range(0.0, 1.0)) = 0.8
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        
        // Advanced texture settings
        [Header(Height and Normal Mapping)]
        _HeightmapEnabled("Enable Heightmap", Float) = 0
        _HeightScale("Height Scale", Range(0.0, 1.0)) = 0.1
        _NormalMapEnabled("Enable Normal Mapping", Float) = 0
        _RoughnessMapEnabled("Enable Roughness Mapping", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Roughness;
            float _Metallic;
            float testScale;
            
            // New advanced texture variables
            float _HeightmapEnabled;
            float _HeightScale;
            float _NormalMapEnabled;
            float _RoughnessMapEnabled;
        CBUFFER_END
        
        TEXTURE2D(testTexture);
        SAMPLER(sampler_testTexture);
        
        TEXTURE2D_ARRAY(baseTextures);
        SAMPLER(sampler_baseTextures);
        
        // New texture samplers for heightmaps, normal maps, and roughness maps
        TEXTURE2D_ARRAY(baseHeightmaps);
        SAMPLER(sampler_baseHeightmaps);
        
        TEXTURE2D_ARRAY(baseNormalMaps);
        SAMPLER(sampler_baseNormalMaps);
        
        TEXTURE2D_ARRAY(baseRoughnessMaps);
        SAMPLER(sampler_baseRoughnessMaps);
        
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
            #pragma multi_compile_fog // Add fog support

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;
            };

            static const int maxLayerCount = 8;
            static const float epsilon = 1E-4;
            
            // These will be set via script using material.SetFloatArray(), etc.
            int layerCount;
            float3 baseColours[maxLayerCount];
            float baseStartHeights[maxLayerCount];
            float baseBlends[maxLayerCount];
            float baseColourStrength[maxLayerCount];
            float baseTextureScales[maxLayerCount];
            float baseRoughnessValues[maxLayerCount];
            
            float minHeight;
            float maxHeight;

            float inverseLerp(float a, float b, float value)
            {
                return saturate((value - a) / (b - a));
            }

            // Enhanced triplanar sampling with optional heightmap, normal map, and roughness map support
            float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex, 
                out float height, out float3 blendedNormal, out float roughness)
            {
                float3 scaledWorldPos = worldPos / scale;
                
                float2 uvYZ = float2(scaledWorldPos.y, scaledWorldPos.z);
                float2 uvXZ = float2(scaledWorldPos.x, scaledWorldPos.z);
                float2 uvXY = float2(scaledWorldPos.x, scaledWorldPos.y);
                
                // Base texture sampling
                float3 xProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, uvYZ, textureIndex).rgb * blendAxes.x;
                float3 yProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, uvXZ, textureIndex).rgb * blendAxes.y;
                float3 zProjection = SAMPLE_TEXTURE2D_ARRAY(baseTextures, sampler_baseTextures, uvXY, textureIndex).rgb * blendAxes.z;
                
                // Heightmap sampling (if enabled)
                height = 0;
                if (_HeightmapEnabled > 0)
                {
                    float hX = SAMPLE_TEXTURE2D_ARRAY(baseHeightmaps, sampler_baseHeightmaps, uvYZ, textureIndex).r * blendAxes.x;
                    float hY = SAMPLE_TEXTURE2D_ARRAY(baseHeightmaps, sampler_baseHeightmaps, uvXZ, textureIndex).r * blendAxes.y;
                    float hZ = SAMPLE_TEXTURE2D_ARRAY(baseHeightmaps, sampler_baseHeightmaps, uvXY, textureIndex).r * blendAxes.z;
                    height = (hX + hY + hZ) * _HeightScale;
                }
                
                // Normal map sampling (if enabled)
                blendedNormal = float3(0, 0, 1);
                if (_NormalMapEnabled > 0)
                {
                    float3 nX = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(baseNormalMaps, sampler_baseNormalMaps, uvYZ, textureIndex)) * blendAxes.x;
                    float3 nY = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(baseNormalMaps, sampler_baseNormalMaps, uvXZ, textureIndex)) * blendAxes.y;
                    float3 nZ = UnpackNormal(SAMPLE_TEXTURE2D_ARRAY(baseNormalMaps, sampler_baseNormalMaps, uvXY, textureIndex)) * blendAxes.z;
                    blendedNormal = normalize(nX + nY + nZ);
                }
                
                // Roughness map sampling (if enabled)
                roughness = _Roughness;
                if (_RoughnessMapEnabled > 0)
                {
                    float rX = SAMPLE_TEXTURE2D_ARRAY(baseRoughnessMaps, sampler_baseRoughnessMaps, uvYZ, textureIndex).r * blendAxes.x;
                    float rY = SAMPLE_TEXTURE2D_ARRAY(baseRoughnessMaps, sampler_baseRoughnessMaps, uvXZ, textureIndex).r * blendAxes.y;
                    float rZ = SAMPLE_TEXTURE2D_ARRAY(baseRoughnessMaps, sampler_baseRoughnessMaps, uvXY, textureIndex).r * blendAxes.z;
                    roughness = rX + rY + rZ;
                }
                
                return xProjection + yProjection + zProjection;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                
                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.tangentWS = normalInputs.tangentWS;
                OUT.bitangentWS = normalInputs.bitangentWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                
                return OUT;
            }
            
            float4 frag(Varyings IN) : SV_Target
            {
                float heightPercent = inverseLerp(minHeight, maxHeight, IN.positionWS.y);
                float3 blendAxes = abs(IN.normalWS);
                blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;
                
                float3 albedo = float3(0, 0, 0);
                float3 finalNormal = IN.normalWS;
                float finalRoughness = _Roughness;
                
                for (int i = 0; i < layerCount; i++)
                {
                    float drawStrength = inverseLerp(-baseBlends[i] / 2 - epsilon, baseBlends[i] / 2, heightPercent - baseStartHeights[i]);
                    
                    float layerHeight;
                    float3 layerNormal;
                    float layerRoughness;
                    float3 baseColour = baseColours[i] * baseColourStrength[i];
                    float3 textureColour = triplanar(IN.positionWS, baseTextureScales[i], blendAxes, i, layerHeight, layerNormal, layerRoughness) * (1 - baseColourStrength[i]);
                    
                    // Blend height and color
                    IN.positionWS.y += layerHeight;
                    
                    // Blend normals
                    if (_NormalMapEnabled > 0)
                    {
                        // Transform normal from tangent space to world space
                        float3 T = normalize(IN.tangentWS);
                        float3 B = normalize(IN.bitangentWS);
                        float3 N = normalize(IN.normalWS);
                        float3x3 TBN = float3x3(T, B, N);
                        
                        layerNormal = mul(TBN, layerNormal);
                        finalNormal = normalize(lerp(finalNormal, layerNormal, drawStrength));
                    }
                    
                    // Blend roughness
                    if (_RoughnessMapEnabled > 0)
                    {
                        // Use layer-specific roughness or base roughness
                        float roughnessValue = baseRoughnessValues[i] > 0 ? 
                            layerRoughness * baseRoughnessValues[i] : 
                            layerRoughness;
                        finalRoughness = lerp(finalRoughness, roughnessValue, drawStrength);
                    }
                    
                    albedo = albedo * (1 - drawStrength) + (baseColour + textureColour) * drawStrength;
                }
                
                // URP lighting
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = IN.positionWS;
                lightingInput.normalWS = normalize(finalNormal);
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                lightingInput.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                lightingInput.fogCoord = IN.fogFactor;
                
                // Enhanced environmental lighting
                lightingInput.bakedGI = SampleSH(finalNormal);
                lightingInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                lightingInput.shadowMask = half4(1, 1, 1, 1);
                
                SurfaceData surfaceInput = (SurfaceData)0;
                surfaceInput.albedo = albedo;
                surfaceInput.metallic = _Metallic;
                surfaceInput.smoothness = 1.0 - finalRoughness;
                surfaceInput.normalTS = float3(0, 0, 1);
                surfaceInput.occlusion = 1.0;
                surfaceInput.emission = 0;
                surfaceInput.alpha = 1.0;
                
                // Calculate final color with PBR lighting
                float4 finalColor = UniversalFragmentPBR(lightingInput, surfaceInput);
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, IN.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Shadow and Depth passes remain the same as in previous versions
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            float3 _LightDirection;
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return output;
            }
            
            float4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            float4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}