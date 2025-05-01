Shader "Custom/OptimizedGrassURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0, 2)) = 0.3
        _WindSpeed ("Wind Speed", Range(0.1, 5)) = 1.0
        _WindScale ("Wind Scale", Range(0.1, 10)) = 2.0
        
        [Header(Variation)]
        _ColorVariationStrength ("Color Variation", Range(0, 1)) = 0.5
        
        [Header(Optimization)]
        _LODDistanceFactor ("LOD Distance Factor", Range(0.01, 1)) = 0.3
        _LODFadeDistance ("LOD Fade Distance", Range(1, 300)) = 50
        _MaxDistance ("Max Render Distance", Range(1, 500)) = 150
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
        }
        LOD 100

        // -------------------------------------------------------------
        // Global HLSL include for all passes
        // -------------------------------------------------------------
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half   _Cutoff;
                half4  _Color;
                half   _WindStrength;
                half   _WindSpeed;
                half   _WindScale;
                half   _ColorVariationStrength;
                half   _LODDistanceFactor;
                half   _LODFadeDistance;
                half   _MaxDistance;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // Instance property buffer
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ColorVariation)
                UNITY_DEFINE_INSTANCED_PROP(float4, _WindParams)
                UNITY_DEFINE_INSTANCED_PROP(float,  _WindTime)
            UNITY_INSTANCING_BUFFER_END(Props)
        ENDHLSL

        // -------------------------------------------------------------
        // Forward Lit Pass
        // -------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Off

            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
                #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
                #pragma multi_compile _ _SHADOWS_SOFT
                #pragma multi_compile_instancing

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS   : NORMAL;
                    float4 tangentOS  : TANGENT;
                    float2 texcoord   : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float2 uv           : TEXCOORD0;
                    float3 positionWS   : TEXCOORD1;
                    float3 normalWS     : TEXCOORD2;
                    float4 positionCS   : SV_POSITION;
                    float4 vertexColor  : COLOR;
                    float  eyeDepth     : TEXCOORD3;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                
                Varyings vert(Attributes input)
                {
                    Varyings output = (Varyings)0;
                    
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                    // Get instance properties
                    float4 windParams = UNITY_ACCESS_INSTANCED_PROP(Props, _WindParams);
                    float  windTime   = UNITY_ACCESS_INSTANCED_PROP(Props, _WindTime);
                    
                    // If instance properties aren't set, use shader properties
                    if (windParams.x == 0)
                    {
                        windParams.x = _WindStrength;
                        windParams.y = _WindScale;
                        windTime     = _Time.y * _WindSpeed;
                    }
                    
                    // Apply wind
                    float4 vertexPosition = input.positionOS;
                    float  height         = vertexPosition.y;
                    
                    if (height > 0.01)
                    {
                        // Wind is stronger at the top
                        float windStrength = height * windParams.x;
                        
                        // Sample noise
                        float3 worldPos  = TransformObjectToWorld(vertexPosition.xyz);
                        float  windNoise = sin(windTime + worldPos.x * 0.1 * windParams.y + worldPos.z * 0.1 * windParams.y);
                        
                        // Apply displacement
                        vertexPosition.x += windNoise * windStrength;
                        vertexPosition.z += windNoise * windStrength * 0.5;
                    }
                    
                    // Transform positions
                    VertexPositionInputs vertexInput = GetVertexPositionInputs(vertexPosition.xyz);
                    VertexNormalInputs   normalInput = GetVertexNormalInputs(input.normalOS);
                    
                    // Output
                    output.uv         = TRANSFORM_TEX(input.texcoord, _MainTex);
                    output.positionWS = vertexInput.positionWS;
                    output.positionCS = vertexInput.positionCS;
                    output.normalWS   = normalInput.normalWS;
                    output.eyeDepth   = vertexInput.positionCS.w;
                    
                    // Store color variation
                    output.vertexColor = UNITY_ACCESS_INSTANCED_PROP(Props, _ColorVariation);
                    
                    return output;
                }
                
                half4 frag(Varyings input) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    
                    // Distance-based culling
                    if (input.eyeDepth > _MaxDistance)
                        discard;
                    
                    float2 uv = input.uv;
                    half4 color;
                    
                    // Distance-based LOD for texturing
                    if (input.eyeDepth > _LODFadeDistance)
                    {
                        // Round UV to nearest multiple of LODDistanceFactor
                        uv = floor(uv / _LODDistanceFactor) * _LODDistanceFactor;
                        color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
                        
                        // Fade out at max distance
                        float fadeRatio = 1.0 - saturate((input.eyeDepth - _LODFadeDistance) / (_MaxDistance - _LODFadeDistance));
                        color.a *= fadeRatio;
                    }
                    else
                    {
                        // Normal texture for closer grass
                        color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
                    }
                    
                    // Alpha test
                    clip(color.a - _Cutoff);
                    
                    // Apply color variation
                    color.rgb += input.vertexColor.rgb;
                    
                    // Main light
                    #ifdef _MAIN_LIGHT_SHADOWS
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    Light mainLight    = GetMainLight(shadowCoord);
                    #else
                    Light mainLight    = GetMainLight();
                    #endif
                    
                    // Basic lighting
                    half3 ambient = SampleSH(input.normalWS);
                    half3 diffuse = mainLight.color * mainLight.shadowAttenuation *
                                    saturate(dot(input.normalWS, mainLight.direction));
                    
                    color.rgb *= (ambient + diffuse);
                    
                    return color;
                }
            ENDHLSL
        }

        // -------------------------------------------------------------
        // ShadowCaster Pass
        // -------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            Cull Off
            
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS   : NORMAL;
                    float2 texcoord   : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float2 uv         : TEXCOORD0;
                    float4 positionCS : SV_POSITION;
                    float  eyeDepth   : TEXCOORD1;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                
                Varyings vert(Attributes input)
                {
                    Varyings output;
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);

                    // Get instance properties
                    float4 windParams = UNITY_ACCESS_INSTANCED_PROP(Props, _WindParams);
                    float  windTime   = UNITY_ACCESS_INSTANCED_PROP(Props, _WindTime);
                    
                    // Use shader properties if instance not set
                    if (windParams.x == 0)
                    {
                        windParams.x = _WindStrength;
                        windParams.y = _WindScale;
                        windTime     = _Time.y * _WindSpeed;
                    }
                    
                    // Wind
                    float4 vertexPosition = input.positionOS;
                    float  height         = vertexPosition.y;
                    
                    if (height > 0.01)
                    {
                        float windStrength = height * windParams.x;
                        float3 worldPos    = TransformObjectToWorld(vertexPosition.xyz);
                        float  windNoise   = sin(windTime + worldPos.x * 0.1 * windParams.y + worldPos.z * 0.1 * windParams.y);
                        
                        vertexPosition.x += windNoise * windStrength;
                        vertexPosition.z += windNoise * windStrength * 0.5;
                    }
                    
                    output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);

                    float4 clipPos = TransformObjectToHClip(vertexPosition.xyz);
                    output.eyeDepth = clipPos.w;

                    float3 positionWS = TransformObjectToWorld(vertexPosition.xyz);
                    float3 normalWS   = TransformObjectToWorldNormal(input.normalOS);

                    // Apply shadow bias
                    output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));

                    #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                    #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                    #endif
                    
                    return output;
                }
                
                half4 frag(Varyings input) : SV_TARGET
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                    // Distance-based culling for shadows
                    if (input.eyeDepth > _MaxDistance)
                        discard;
                    
                    half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    clip(texColor.a - _Cutoff);
                    
                    return 0;
                }
            ENDHLSL
        }

        // -------------------------------------------------------------
        // DepthOnly Pass
        // -------------------------------------------------------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            Cull Off
            
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float2 texcoord   : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct Varyings
                {
                    float2 uv         : TEXCOORD0;
                    float4 positionCS : SV_POSITION;
                    float  eyeDepth   : TEXCOORD1;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };
                
                Varyings vert(Attributes input)
                {
                    Varyings output = (Varyings)0;
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_TRANSFER_INSTANCE_ID(input, output);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                    
                    // Get instance properties
                    float4 windParams = UNITY_ACCESS_INSTANCED_PROP(Props, _WindParams);
                    float  windTime   = UNITY_ACCESS_INSTANCED_PROP(Props, _WindTime);
                    
                    if (windParams.x == 0)
                    {
                        windParams.x = _WindStrength;
                        windParams.y = _WindScale;
                        windTime     = _Time.y * _WindSpeed;
                    }
                    
                    // Apply wind
                    float4 vertexPosition = input.positionOS;
                    float  height         = vertexPosition.y;
                    
                    if (height > 0.01)
                    {
                        float windStrength = height * windParams.x;
                        float3 worldPos    = TransformObjectToWorld(vertexPosition.xyz);
                        float  windNoise   = sin(windTime + worldPos.x * 0.1 * windParams.y + worldPos.z * 0.1 * windParams.y);
                        vertexPosition.x  += windNoise * windStrength;
                        vertexPosition.z  += windNoise * windStrength * 0.5;
                    }
                    
                    output.uv         = TRANSFORM_TEX(input.texcoord, _MainTex);
                    output.positionCS = TransformObjectToHClip(vertexPosition.xyz);
                    output.eyeDepth   = output.positionCS.w;
                    
                    return output;
                }
                
                half4 frag(Varyings input) : SV_TARGET
                {
                    UNITY_SETUP_INSTANCE_ID(input);
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    
                    // Distance-based culling for depth
                    if (input.eyeDepth > _MaxDistance)
                        discard;
                    
                    half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                    clip(texColor.a - _Cutoff);
                    
                    return 0;
                }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
