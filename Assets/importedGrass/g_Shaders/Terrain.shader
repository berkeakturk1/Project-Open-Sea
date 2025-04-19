Shader "Custom/TerrainURP"
{
    Properties
    {
        _Albedo ("Albedo", Color) = (1, 1, 1)
        _TerrainTex ("Terrain Texture", 2D) = "white" {}
        [NoScaleOffset] _NormalMap ("Normal Map", 2D) = "bump" {}
        _TessellationEdgeLength ("Tessellation Edge Length", Range(1, 100)) = 50
        [NoScaleOffset] _HeightMap ("Height Map", 2D) = "white" {}
        _DisplacementStrength ("Displacement Strength", Range(0.1, 200)) = 5
        _NormalStrength ("Normals Strength", Range(0.0, 10)) = 1
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
        
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo;
                float4 _TerrainTex_ST;
                float _TessellationEdgeLength;
                float _DisplacementStrength;
                float _NormalStrength;
                float _Smoothness;
                float _Metallic;
                float4 _HeightMap_TexelSize;
            CBUFFER_END
            
            TEXTURE2D(_TerrainTex);
            SAMPLER(sampler_TerrainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);
            
            struct TessellationFactors
            {
                float edge[3] : SV_TessFactor;
                float inside : SV_InsideTessFactor;
            };
            
            float TessellationHeuristic(float3 cp0, float3 cp1)
            {
                float edgeLength = distance(cp0, cp1);
                float3 edgeCenter = (cp0 + cp1) * 0.5;
                float viewDistance = distance(edgeCenter, _WorldSpaceCameraPos);
                
                return edgeLength * _ScreenParams.y / (_TessellationEdgeLength * (viewDistance * 0.5));
            }
            
            bool TriangleIsBelowClipPlane(float3 p0, float3 p1, float3 p2, int planeIndex, float bias)
            {
                // Get clip plane in world space
                float4 plane = unity_CameraWorldClipPlanes[planeIndex];
                
                return dot(float4(p0, 1), plane) < bias &&
                       dot(float4(p1, 1), plane) < bias &&
                       dot(float4(p2, 1), plane) < bias;
            }
            
            bool cullTriangle(float3 p0, float3 p1, float3 p2, float bias)
            {
                return TriangleIsBelowClipPlane(p0, p1, p2, 0, bias) ||
                       TriangleIsBelowClipPlane(p0, p1, p2, 1, bias) ||
                       TriangleIsBelowClipPlane(p0, p1, p2, 2, bias) ||
                       TriangleIsBelowClipPlane(p0, p1, p2, 3, -_DisplacementStrength);
            }
            
            // Define interpolation macro outside of specific passes
            #define DP_INTERPOLATE(fieldName) data.fieldName = \
                patch[0].fieldName * barycentricCoordinates.x + \
                patch[1].fieldName * barycentricCoordinates.y + \
                patch[2].fieldName * barycentricCoordinates.z;
        ENDHLSL
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma target 5.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #pragma vertex dummyvp
            #pragma hull hp
            #pragma domain dp
            #pragma geometry gp
            #pragma fragment fp
            
            struct TessellationControlPoint
            {
                float4 vertex : INTERNALTESSPOS;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangent : TANGENT;
            };
            
            struct VertexData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct v2g
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : NORMAL;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD0;
            };
            
            struct g2f
            {
                v2g data;
                float2 barycentricCoordinates : TEXCOORD9;
            };
            
            TessellationControlPoint dummyvp(VertexData v)
            {
                TessellationControlPoint p;
                p.vertex = v.vertex;
                p.normal = v.normal;
                p.uv = v.uv;
                p.tangent = v.tangent;
                
                return p;
            }
            
            v2g vp(VertexData v)
            {
                v2g o = (v2g)0;
                
                // Sample heightmap and displace vertex
                float displacement = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, v.uv, 0).r;
                float height = displacement * _DisplacementStrength;
                v.vertex.y = height;
                
                // Transform positions
                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.vertex.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(v.normal, v.tangent);
                
                o.positionCS = positionInputs.positionCS;
                o.positionWS = positionInputs.positionWS;
                o.normalWS = normalInputs.normalWS;
                o.tangentWS = float4(normalInputs.tangentWS, v.tangent.w);
                o.uv = TRANSFORM_TEX(v.uv, _TerrainTex);
                
                return o;
            }
            
            TessellationFactors PatchFunction(InputPatch<TessellationControlPoint, 3> patch)
            {
                float3 p0 = TransformObjectToWorld(patch[0].vertex.xyz);
                float3 p1 = TransformObjectToWorld(patch[1].vertex.xyz);
                float3 p2 = TransformObjectToWorld(patch[2].vertex.xyz);
                
                TessellationFactors f;
                float bias = -0.5 * _DisplacementStrength;
                
                if (cullTriangle(p0, p1, p2, bias))
                {
                    f.edge[0] = f.edge[1] = f.edge[2] = f.inside = 0;
                }
                else
                {
                    f.edge[0] = TessellationHeuristic(p1, p2);
                    f.edge[1] = TessellationHeuristic(p2, p0);
                    f.edge[2] = TessellationHeuristic(p0, p1);
                    f.inside = (TessellationHeuristic(p1, p2) +
                                TessellationHeuristic(p2, p0) +
                                TessellationHeuristic(p0, p1)) * (1 / 3.0);
                }
                
                return f;
            }
            
            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("integer")]
            [patchconstantfunc("PatchFunction")]
            TessellationControlPoint hp(InputPatch<TessellationControlPoint, 3> patch, uint id : SV_OUTPUTCONTROLPOINTID)
            {
                return patch[id];
            }
            
            [maxvertexcount(3)]
            void gp(triangle v2g g[3], inout TriangleStream<g2f> stream)
            {
                g2f g0, g1, g2;
                g0.data = g[0];
                g1.data = g[1];
                g2.data = g[2];
                
                g0.barycentricCoordinates = float2(1, 0);
                g1.barycentricCoordinates = float2(0, 1);
                g2.barycentricCoordinates = float2(0, 0);
                
                stream.Append(g0);
                stream.Append(g1);
                stream.Append(g2);
            }
            
            [domain("tri")]
            v2g dp(TessellationFactors factors, OutputPatch<TessellationControlPoint, 3> patch, float3 barycentricCoordinates : SV_DOMAINLOCATION)
            {
                VertexData data;
                
                // Manual interpolation
                data.vertex = 
                    patch[0].vertex * barycentricCoordinates.x +
                    patch[1].vertex * barycentricCoordinates.y +
                    patch[2].vertex * barycentricCoordinates.z;
                    
                data.normal = 
                    patch[0].normal * barycentricCoordinates.x +
                    patch[1].normal * barycentricCoordinates.y +
                    patch[2].normal * barycentricCoordinates.z;
                    
                data.tangent = 
                    patch[0].tangent * barycentricCoordinates.x +
                    patch[1].tangent * barycentricCoordinates.y +
                    patch[2].tangent * barycentricCoordinates.z;
                    
                data.uv = 
                    patch[0].uv * barycentricCoordinates.x +
                    patch[1].uv * barycentricCoordinates.y +
                    patch[2].uv * barycentricCoordinates.z;
                
                return vp(data);
            }
            
            half4 fp(g2f i) : SV_TARGET
            {
                // Sample albedo texture
                float3 albedo = SAMPLE_TEXTURE2D(_TerrainTex, sampler_TerrainTex, i.data.uv).rgb;
                albedo = pow(albedo, 1.5);
                albedo *= _Albedo.rgb;
                
                // Calculate normal from normal map and height map
                float3 normalTS;
                normalTS.xy = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.data.uv).wy * 2 - 1;
                normalTS.z = sqrt(1 - saturate(dot(normalTS.xy, normalTS.xy)));
                normalTS = normalTS.xzy;
                
                // Calculate normal from heightmap using central difference
                float2 du = float2(_HeightMap_TexelSize.x * 0.5, 0);
                float u1 = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, i.data.uv - du).r;
                float u2 = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, i.data.uv + du).r;
                
                float2 dv = float2(0, _HeightMap_TexelSize.y * 0.5);
                float v1 = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, i.data.uv - dv).r;
                float v2 = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, i.data.uv + dv).r;
                
                float3 centralDifference = float3(u1 - u2, 1, v1 - v2);
                centralDifference = normalize(centralDifference);
                
                normalTS += centralDifference;
                normalTS.xz *= _NormalStrength;
                normalTS = normalize(float3(normalTS.x, 1, normalTS.z));
                
                // Convert tangent space normal to world space
                float3 normalWS = TransformTangentToWorld(normalTS, 
                                                         float3x3(i.data.tangentWS.xyz, 
                                                                 cross(i.data.normalWS, i.data.tangentWS.xyz) * i.data.tangentWS.w,
                                                                 i.data.normalWS));
                normalWS = normalize(normalWS);
                
                // Setup SurfaceData
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = 1.0;
                
                // Setup InputData
                InputData inputData = (InputData)0;
                inputData.positionWS = i.data.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.data.positionWS);
                
                // Shadow coordinates
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 positionCS = TransformWorldToHClip(i.data.positionWS);
                    inputData.shadowCoord = TransformWorldToShadowCoord(i.data.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif
                
                inputData.fogCoord = 0; // Not handling fog in this shader
                
                // Additional settings for screen space effects
                #if defined(_NORMALMAP)
                    inputData.normalWS = normalWS;
                #endif
                
                // Apply lighting
                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                
                return color;
            }
            ENDHLSL
        }
        
        // Shadow casting pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma target 5.0
            
            // This is used during shadow map generation to differentiate between directional and punctual light shadows
            #pragma multi_compile_shadowcaster
            
            #pragma vertex dummyvp
            #pragma hull hp
            #pragma domain dp
            #pragma fragment ShadowPassFragment
            
            // Shadow Casting specific input
            struct ShadowTessControlPoint
            {
                float4 vertex : INTERNALTESSPOS;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct VertexData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct ShadowOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            float3 _LightDirection;
            
            ShadowTessControlPoint dummyvp(VertexData v)
            {
                ShadowTessControlPoint p;
                p.vertex = v.vertex;
                p.normal = v.normal;
                p.uv = v.uv;
                
                return p;
            }
            
            float4 GetShadowPositionHClip(VertexData v)
            {
                // Sample heightmap and displace vertex
                float displacement = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, v.uv, 0).r;
                displacement *= _DisplacementStrength;
                v.vertex.y = displacement;
                
                // Get positions in object space
                float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normal);
                
                // Apply shadowing logic
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                
                return positionCS;
            }
            
            ShadowOutput vp(VertexData v)
            {
                ShadowOutput o;
                o.positionCS = GetShadowPositionHClip(v);
                o.uv = v.uv;
                
                return o;
            }
            
            TessellationFactors PatchFunction(InputPatch<ShadowTessControlPoint, 3> patch)
            {
                float3 p0 = TransformObjectToWorld(patch[0].vertex.xyz);
                float3 p1 = TransformObjectToWorld(patch[1].vertex.xyz);
                float3 p2 = TransformObjectToWorld(patch[2].vertex.xyz);
                
                TessellationFactors f;
                float bias = -0.5 * _DisplacementStrength;
                
                if (cullTriangle(p0, p1, p2, bias))
                {
                    f.edge[0] = f.edge[1] = f.edge[2] = f.inside = 0;
                }
                else
                {
                    f.edge[0] = TessellationHeuristic(p1, p2);
                    f.edge[1] = TessellationHeuristic(p2, p0);
                    f.edge[2] = TessellationHeuristic(p0, p1);
                    f.inside = (TessellationHeuristic(p1, p2) +
                                TessellationHeuristic(p2, p0) +
                                TessellationHeuristic(p0, p1)) * (1 / 3.0);
                }
                
                return f;
            }
            
            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("integer")]
            [patchconstantfunc("PatchFunction")]
            ShadowTessControlPoint hp(InputPatch<ShadowTessControlPoint, 3> patch, uint id : SV_OUTPUTCONTROLPOINTID)
            {
                return patch[id];
            }
            
            [domain("tri")]
            ShadowOutput dp(TessellationFactors factors, OutputPatch<ShadowTessControlPoint, 3> patch, float3 barycentricCoordinates : SV_DOMAINLOCATION)
            {
                VertexData data;
                
                // Manual interpolation
                data.vertex = 
                    patch[0].vertex * barycentricCoordinates.x +
                    patch[1].vertex * barycentricCoordinates.y +
                    patch[2].vertex * barycentricCoordinates.z;
                    
                data.normal = 
                    patch[0].normal * barycentricCoordinates.x +
                    patch[1].normal * barycentricCoordinates.y +
                    patch[2].normal * barycentricCoordinates.z;
                    
                data.uv = 
                    patch[0].uv * barycentricCoordinates.x +
                    patch[1].uv * barycentricCoordinates.y +
                    patch[2].uv * barycentricCoordinates.z;
                
                return vp(data);
            }
            
            half4 ShadowPassFragment(ShadowOutput IN) : SV_TARGET
            {
                // Simply return 0 for shadows
                return 0;
            }
            ENDHLSL
        }
        
        // Depth prepass
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            Cull Back
            
            HLSLPROGRAM
            #pragma target 5.0
            
            #pragma vertex dummyvp
            #pragma hull hp
            #pragma domain dp
            #pragma fragment DepthOnlyFragment
            
            // Same structures as shadow caster for tessellation
            struct ShadowTessControlPoint
            {
                float4 vertex : INTERNALTESSPOS;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct VertexData
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct DepthOnlyOutput
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            ShadowTessControlPoint dummyvp(VertexData v)
            {
                ShadowTessControlPoint p;
                p.vertex = v.vertex;
                p.normal = v.normal;
                p.uv = v.uv;
                
                return p;
            }
            
            DepthOnlyOutput vp(VertexData v)
            {
                DepthOnlyOutput o;
                
                // Sample heightmap and displace vertex
                float displacement = SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, v.uv, 0).r;
                displacement *= _DisplacementStrength;
                v.vertex.y = displacement;
                
                o.positionCS = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                
                return o;
            }
            
            TessellationFactors PatchFunction(InputPatch<ShadowTessControlPoint, 3> patch)
            {
                float3 p0 = TransformObjectToWorld(patch[0].vertex.xyz);
                float3 p1 = TransformObjectToWorld(patch[1].vertex.xyz);
                float3 p2 = TransformObjectToWorld(patch[2].vertex.xyz);
                
                TessellationFactors f;
                float bias = -0.5 * _DisplacementStrength;
                
                if (cullTriangle(p0, p1, p2, bias))
                {
                    f.edge[0] = f.edge[1] = f.edge[2] = f.inside = 0;
                }
                else
                {
                    f.edge[0] = TessellationHeuristic(p1, p2);
                    f.edge[1] = TessellationHeuristic(p2, p0);
                    f.edge[2] = TessellationHeuristic(p0, p1);
                    f.inside = (TessellationHeuristic(p1, p2) +
                                TessellationHeuristic(p2, p0) +
                                TessellationHeuristic(p0, p1)) * (1 / 3.0);
                }
                
                return f;
            }
            
            [domain("tri")]
            [outputcontrolpoints(3)]
            [outputtopology("triangle_cw")]
            [partitioning("integer")]
            [patchconstantfunc("PatchFunction")]
            ShadowTessControlPoint hp(InputPatch<ShadowTessControlPoint, 3> patch, uint id : SV_OUTPUTCONTROLPOINTID)
            {
                return patch[id];
            }
            
            [domain("tri")]
            DepthOnlyOutput dp(TessellationFactors factors, OutputPatch<ShadowTessControlPoint, 3> patch, float3 barycentricCoordinates : SV_DOMAINLOCATION)
            {
                VertexData data;
                
                // Manual interpolation
                data.vertex = 
                    patch[0].vertex * barycentricCoordinates.x +
                    patch[1].vertex * barycentricCoordinates.y +
                    patch[2].vertex * barycentricCoordinates.z;
                    
                data.normal = 
                    patch[0].normal * barycentricCoordinates.x +
                    patch[1].normal * barycentricCoordinates.y +
                    patch[2].normal * barycentricCoordinates.z;
                    
                data.uv = 
                    patch[0].uv * barycentricCoordinates.x +
                    patch[1].uv * barycentricCoordinates.y +
                    patch[2].uv * barycentricCoordinates.z;
                
                return vp(data);
            }
            
            half4 DepthOnlyFragment(DepthOnlyOutput IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
        
        // For deferred rendering and optionally Meta pass for Global Illumination
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            
            Cull Off
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex MetaVertex
            #pragma fragment MetaFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float2 uv2          : TEXCOORD2;
            };
            
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };
            
            Varyings MetaVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
                output.uv = TRANSFORM_TEX(input.uv0, _TerrainTex);
                
                return output;
            }
            
            half4 MetaFragment(Varyings input) : SV_TARGET
            {
                // Sample albedo texture
                float3 albedo = SAMPLE_TEXTURE2D(_TerrainTex, sampler_TerrainTex, input.uv).rgb;
                albedo = pow(albedo, 1.5);
                albedo *= _Albedo.rgb;
                
                // Setup MetaInput structure
                MetaInput metaInput;
                metaInput.Albedo = albedo;
                metaInput.Emission = half3(0.0h, 0.0h, 0.0h);
                
                return MetaFragmentLighting(metaInput, input.uv);
            }
            ENDHLSL
        }
    }
    
    // Fallback to a simpler shader when tessellation is not available
    FallBack "Universal Render Pipeline/Lit"
}