Shader "Custom/SailWindAnimation"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base (RGB)", 2D) = "white" {}
        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1.0
        [Toggle(_DOUBLE_SIDED_NORMALS)] _DoubleSidedNormals("Double-Sided Normals", Float) = 1
        
        [HDR] _ColorTint ("Color Tint", Color) = (1,1,1,1)
        _AmbientLightBoost ("Ambient Light Boost", Range(0, 2)) = 0.5
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.6
        
        [Toggle(_WIND_ENABLED)] _WindEnabled("Enable Wind Animation", Float) = 1
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.0
        _WindIntensity ("Wind Intensity", Range(0, 1)) = 0.5
        _WindFrequency ("Wind Frequency", Range(0, 10)) = 1.0
        _WindTurbulence ("Wind Turbulence", Range(0, 1)) = 0.3
        
        _BillowAmount ("Billow Amount", Range(0, 2)) = 0.5
        _BillowSpeed ("Billow Speed", Range(0, 2)) = 0.2
        
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        
        Cull Off // Disable culling to render both sides
        
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _DOUBLE_SIDED_NORMALS
            #pragma shader_feature_local _WIND_ENABLED
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            
            // Declare textures before including SurfaceInput to avoid redefinition errors
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
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
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                float3 vertexColor : TEXCOORD6;
                half3 vertexSH : TEXCOORD7;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NormalMap_ST;
                float4 _ColorTint;
                float _NormalStrength;
                float _WindSpeed;
                float _WindIntensity;
                float _WindFrequency;
                float _WindTurbulence;
                float _BillowAmount;
                float _BillowSpeed;
                float _Smoothness;
                float _Metallic;
                float _AmbientLightBoost;
                float _ShadowStrength;
                float _WindEnabled;
            CBUFFER_END
            
            // Simplified Perlin noise function for wind simulation 
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float a = sin(dot(i, float2(127.1, 311.7)));
                float b = sin(dot(i + float2(1.0, 0.0), float2(127.1, 311.7)));
                float c = sin(dot(i + float2(0.0, 1.0), float2(127.1, 311.7)));
                float d = sin(dot(i + float2(1.0, 1.0), float2(127.1, 311.7)));
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.5 + 0.5;
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 displacedPosition = input.positionOS.xyz;
                
                #if defined(_WIND_ENABLED)
                    // Calculate wind effect
                    float time = _Time.y * _WindSpeed;
                    float2 windUV = input.uv * _WindFrequency;
                    
                    // Main wind displacement
                    float windNoise = noise(windUV + time);
                    
                    // Add some turbulence/variation
                    float turbulence = noise((windUV * 2.5) + (time * 0.5)) * _WindTurbulence;
                    
                    // Billow effect - stronger in the middle of the sail
                    float billowMask = sin(input.uv.y * 3.14159) * sin(input.uv.x * 3.14159);
                    float billow = sin(time * _BillowSpeed) * _BillowAmount * billowMask;
                    
                    // Apply displacement primarily along normal direction
                    float totalDisplacement = (windNoise + turbulence) * _WindIntensity + billow;
                    displacedPosition = input.positionOS.xyz + input.normalOS * totalDisplacement;
                    
                    // Store wind values as vertex color for fragment shader use
                    output.vertexColor = float3(windNoise, turbulence, billowMask);
                #else
                    // No wind animation, use original position
                    output.vertexColor = float3(0, 0, 0);
                #endif
                
                // Handle standard transformation
                VertexPositionInputs vertexInput = GetVertexPositionInputs(displacedPosition);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                // Shadow coordinates
                output.shadowCoord = GetShadowCoord(vertexInput);
                
                // Calculate SH lighting (ambient)
                output.vertexSH = SampleSHVertex(output.normalWS.xyz);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Sample textures
                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float3 normalMap = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), 
                    _NormalStrength
                );
                
                // Calculate normal in world space 
                float3 normalWS = TransformTangentToWorld(
                    normalMap,
                    half3x3(input.tangentWS.xyz, cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w, input.normalWS)
                );
                normalWS = normalize(normalWS);
                
                // Handle backface normals when rendering double-sided
                float faceSign = dot(input.viewDirWS, input.normalWS) < 0 ? -1 : 1;
                normalWS *= faceSign;
                
                // Lighting calculations
                InputData lightingInput = (InputData)0;
                lightingInput.positionWS = input.positionWS;
                lightingInput.normalWS = normalWS;
                lightingInput.viewDirectionWS = normalize(input.viewDirWS);
                lightingInput.shadowCoord = input.shadowCoord;
                lightingInput.fogCoord = 0;
                lightingInput.vertexLighting = half3(0, 0, 0);
                lightingInput.bakedGI = input.vertexSH;
                
                // Adjust shadow strength to reduce darkness
                // Use shadow mask differently since we're in URP
                lightingInput.shadowMask = half4(1, 1, 1, 1);
                
                // Surface data setup
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseMap.rgb * _ColorTint.rgb;
                surfaceData.alpha = baseMap.a;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                
                // Add subtle variation to smoothness based on wind for a more dynamic look
                #if defined(_WIND_ENABLED)
                    float windVariation = input.vertexColor.x * 0.2;
                    surfaceData.smoothness = saturate(surfaceData.smoothness + windVariation - 0.1);
                #endif
                
                // Apply Universal Lighting
                float4 finalColor = UniversalFragmentPBR(lightingInput, surfaceData);
                
                // Apply color tint (already applied to surfaceData.albedo above, but ensuring it's applied to the final color as well)
                finalColor.rgb *= _ColorTint.rgb;
                
                // Add ambient light boost to reduce darkness
                half3 ambientColor = input.vertexSH;
                finalColor.rgb += ambientColor * _AmbientLightBoost * baseMap.rgb;
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local _WIND_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _WindSpeed;
                float _WindIntensity;
                float _WindFrequency;
                float _WindTurbulence;
                float _BillowAmount;
                float _BillowSpeed;
                float _WindEnabled;
            CBUFFER_END

            // Same noise function as above
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float a = sin(dot(i, float2(127.1, 311.7)));
                float b = sin(dot(i + float2(1.0, 0.0), float2(127.1, 311.7)));
                float c = sin(dot(i + float2(0.0, 1.0), float2(127.1, 311.7)));
                float d = sin(dot(i + float2(1.0, 1.0), float2(127.1, 311.7)));
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.5 + 0.5;
            }

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 displacedPosition = input.positionOS.xyz;
                
                #if defined(_WIND_ENABLED)
                    // Calculate the same wind effect as in the main vertex shader
                    float time = _Time.y * _WindSpeed;
                    float2 windUV = input.texcoord * _WindFrequency;
                    
                    float windNoise = noise(windUV + time);
                    float turbulence = noise((windUV * 2.5) + (time * 0.5)) * _WindTurbulence;
                    
                    float billowMask = sin(input.texcoord.y * 3.14159) * sin(input.texcoord.x * 3.14159);
                    float billow = sin(time * _BillowSpeed) * _BillowAmount * billowMask;
                    
                    float totalDisplacement = (windNoise + turbulence) * _WindIntensity + billow;
                    displacedPosition = input.positionOS.xyz + input.normalOS * totalDisplacement;
                #endif
                
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                
                float3 positionWS = TransformObjectToWorld(displacedPosition);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                output.positionCS = positionCS;
                
                return output;
            }

            float4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        // DepthOnly pass
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma shader_feature_local _WIND_ENABLED

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _WindSpeed;
                float _WindIntensity;
                float _WindFrequency;
                float _WindTurbulence;
                float _BillowAmount;
                float _BillowSpeed;
                float _WindEnabled;
            CBUFFER_END

            // Same noise function as above
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float a = sin(dot(i, float2(127.1, 311.7)));
                float b = sin(dot(i + float2(1.0, 0.0), float2(127.1, 311.7)));
                float c = sin(dot(i + float2(0.0, 1.0), float2(127.1, 311.7)));
                float d = sin(dot(i + float2(1.0, 1.0), float2(127.1, 311.7)));
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y) * 0.5 + 0.5;
            }

            struct Attributes
            {
                float4 position     : POSITION;
                float2 texcoord     : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 displacedPosition = input.position.xyz;
                
                #if defined(_WIND_ENABLED)
                    // Apply the same wind effect
                    float time = _Time.y * _WindSpeed;
                    float2 windUV = input.texcoord * _WindFrequency;
                    
                    float windNoise = noise(windUV + time);
                    float turbulence = noise((windUV * 2.5) + (time * 0.5)) * _WindTurbulence;
                    
                    float billowMask = sin(input.texcoord.y * 3.14159) * sin(input.texcoord.x * 3.14159);
                    float billow = sin(time * _BillowSpeed) * _BillowAmount * billowMask;
                    
                    float totalDisplacement = (windNoise + turbulence) * _WindIntensity + billow;
                    displacedPosition = input.position.xyz + input.normalOS * totalDisplacement;
                #endif
                
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = TransformObjectToHClip(displacedPosition);
                return output;
            }

            float4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}