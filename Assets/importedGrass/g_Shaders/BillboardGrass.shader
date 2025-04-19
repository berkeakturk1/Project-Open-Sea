Shader "Unlit/BillboardGrass URP" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _WindStrength ("Wind Strength", Range(0.5, 50.0)) = 1
        _CullingBias ("Cull Bias", Range(0.1, 1.0)) = 0.5
        _LODCutoff ("LOD Cutoff", Range(10.0, 500.0)) = 100
        _DisplacementStrength ("Displacement Strength", Range(0.1, 10.0)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
    }

    SubShader {
        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Cull Off
        ZWrite On

        Pass {
            Name "ForwardLit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma target 4.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // Include your custom Random.cginc file
            #include "../Resources/Random.cginc"
            
            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float saturationLevel : TEXCOORD1;
            };

            struct GrassData {
                float4 position;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Rotation;
                float _WindStrength;
                float _CullingBias;
                float _DisplacementStrength;
                float _LODCutoff;
            CBUFFER_END
            
            StructuredBuffer<GrassData> positionBuffer;
            
            float4 RotateAroundYInDegrees(float4 vertex, float degrees) {
                float alpha = degrees * PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                float2 rotated = mul(m, vertex.xz);
                return float4(rotated.x, vertex.y, rotated.y, vertex.w);
            }

            // Define frustum plane calculations for URP
            float4 GetFrustumPlane(int idx) {
                // Recreate the camera frustum planes in URP
                // These match the order in the original shader (left, right, bottom, top, near, far)
                float4x4 vpMatrix = GetWorldToHClipMatrix();
                float4 planes[6];
                
                // Left plane
                planes[0] = float4(vpMatrix._14 + vpMatrix._11, 
                                  vpMatrix._24 + vpMatrix._21,
                                  vpMatrix._34 + vpMatrix._31,
                                  vpMatrix._44 + vpMatrix._41);
                
                // Right plane
                planes[1] = float4(vpMatrix._14 - vpMatrix._11, 
                                  vpMatrix._24 - vpMatrix._21,
                                  vpMatrix._34 - vpMatrix._31,
                                  vpMatrix._44 - vpMatrix._41);
                
                // Bottom plane
                planes[2] = float4(vpMatrix._14 + vpMatrix._12, 
                                  vpMatrix._24 + vpMatrix._22,
                                  vpMatrix._34 + vpMatrix._32,
                                  vpMatrix._44 + vpMatrix._42);
                
                // Top plane
                planes[3] = float4(vpMatrix._14 - vpMatrix._12, 
                                  vpMatrix._24 - vpMatrix._22,
                                  vpMatrix._34 - vpMatrix._32,
                                  vpMatrix._44 - vpMatrix._42);
                
                // Near plane
                planes[4] = float4(vpMatrix._13, 
                                  vpMatrix._23,
                                  vpMatrix._33,
                                  vpMatrix._43);
                
                // Far plane
                planes[5] = float4(vpMatrix._14 - vpMatrix._13, 
                                  vpMatrix._24 - vpMatrix._23,
                                  vpMatrix._34 - vpMatrix._33,
                                  vpMatrix._44 - vpMatrix._43);
                
                // Normalize planes
                for (int i = 0; i < 6; i++) {
                    planes[i] *= 1.0f / length(planes[i].xyz);
                }
                
                return planes[idx];
            }
            
            bool VertexIsBelowClipPlane(float3 p, int planeIndex, float bias) {
                float4 plane = GetFrustumPlane(planeIndex);
                return dot(float4(p, 1), plane) < bias;
            }

            bool cullVertex(float3 p, float bias) {
                return  distance(_WorldSpaceCameraPos, p) > _LODCutoff ||
                        VertexIsBelowClipPlane(p, 0, bias) ||
                        VertexIsBelowClipPlane(p, 1, bias) ||
                        VertexIsBelowClipPlane(p, 2, bias) ||
                        VertexIsBelowClipPlane(p, 3, -max(1.0f, _DisplacementStrength));
            }

            v2f vert(VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;
            
                float3 localPosition = RotateAroundYInDegrees(v.vertex, _Rotation).xyz;

                float localWindVariance = min(max(0.4f, randValue(instanceID)), 0.75f);

                float4 grassPosition = positionBuffer[instanceID].position;
                
                float cosTime;
                if (localWindVariance > 0.6f)
                    cosTime = cos(_Time.y * (_WindStrength - (grassPosition.w - 1.0f)));
                else
                    cosTime = cos(_Time.y * ((_WindStrength - (grassPosition.w - 1.0f)) + localWindVariance * 0.1f));
                    
    
                float trigValue = ((cosTime * cosTime) * 0.65f) - localWindVariance * 0.5f;
                
                localPosition.x += v.uv.y * trigValue * grassPosition.w * localWindVariance * 0.6f;
                localPosition.z += v.uv.y * trigValue * grassPosition.w * 0.4f;
                localPosition.y *= v.uv.y * (0.5f + grassPosition.w);
                
                float4 worldPosition = float4(grassPosition.xyz + localPosition, 1.0f);

                if (cullVertex(worldPosition.xyz, -_CullingBias * max(1.0f, _DisplacementStrength)))
                    o.vertex = 0.0f;
                else
                    o.vertex = TransformObjectToHClip(worldPosition.xyz);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.saturationLevel = 1.0 - ((positionBuffer[instanceID].position.w - 1.0f) / 1.5f);
                o.saturationLevel = max(o.saturationLevel, 0.5f);
                
                return o;
            }

            half4 frag(v2f i) : SV_Target {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                clip(-(0.5 - col.a));

                // Convert RGB to luminance for URP
                float luminance = dot(col.rgb, float3(0.2126, 0.7152, 0.0722));

                float saturation = lerp(1.0f, i.saturationLevel, i.uv.y * i.uv.y * i.uv.y);
                col.r /= saturation;
                
                // Get main light direction in URP
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float ndotl = saturate(dot(lightDir, normalize(float3(0, 1, 0))));
                
                return col * ndotl;
            }
            ENDHLSL
        }
    }
}