Shader "Universal Render Pipeline/Unlit/ModelGrassURP" {
    Properties {
        _Albedo1 ("Albedo 1", Color) = (1, 1, 1, 1)
        _Albedo2 ("Albedo 2", Color) = (1, 1, 1, 1)
        _AOColor ("Ambient Occlusion", Color) = (1, 1, 1, 1)
        _TipColor ("Tip Color", Color) = (1, 1, 1, 1)
        _Scale ("Scale", Range(0.0, 2.0)) = 0.0
        _Droop ("Droop", Range(0.0, 10.0)) = 0.0
        _FogColor ("Fog Color", Color) = (1, 1, 1, 1)
        _FogDensity ("Fog Density", Range(0.0, 1.0)) = 0.0
        _FogOffset ("Fog Offset", Range(0.0, 10.0)) = 0.0
        _MainLightDir ("Main Light Direction", Vector) = (0, 1, 0, 0)
        _WindTex ("Wind Texture", 2D) = "white" {}
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
            #pragma multi_compile_fog

            // These includes must match your URP version & folder structure:
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // --------------------------
            // Inline random function
            // --------------------------
            float randValue(float seed) {
                // Simple hash-based pseudo-random
                return frac(sin(seed * 12.9898) * 43758.5453);
            }

            // --------------------------
            // Define your GrassData struct
            // --------------------------
            struct GrassData {
                float4 position;
                float2 uv;
                float displacement;
            };

            // --------------------------------------------------
            // Shader variables (must match the Properties block)
            // --------------------------------------------------
            Texture2D _WindTex;
            SamplerState sampler_WindTex;

            float4 _Albedo1, _Albedo2, _AOColor, _TipColor, _FogColor;
            float _Scale, _Droop, _FogDensity, _FogOffset;
            float3 _MainLightDir;
            float3 _CameraPosition;
            int _ChunkNum;

            // ------------------------------------
            // Vertex Input / Output (v2f) structs
            // ------------------------------------
            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                float noiseVal : TEXCOORD2;
                float3 chunkNum : TEXCOORD3;
            };

            // --------------------------------------------------
            // Utility rotation functions
            // --------------------------------------------------
            float4 RotateAroundYInDegrees(float4 vertex, float degrees) {
                float alpha = radians(degrees);
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                float2 xz = mul(m, vertex.xz);
                return float4(xz.x, vertex.y, xz.y, vertex.w);
            }
            
            float4 RotateAroundXInDegrees(float4 vertex, float degrees) {
                float alpha = radians(degrees);
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2 yz = mul(float2x2(cosa, -sina, sina, cosa), vertex.yz);
                return float4(vertex.x, yz.x, yz.y, vertex.w);
            }

            float DotClamped(float3 a, float3 b) {
                return max(0.0, dot(a, b));
            }

            // --------------------------------------------------
            // Vertex Shader
            // --------------------------------------------------
            StructuredBuffer<GrassData> positionBuffer;

            v2f vert (VertexData v, uint instanceID : SV_InstanceID) {
                v2f o;

                // Retrieve grass instance data
                float4 grassPosition = positionBuffer[instanceID].position;

                // Generate a pseudo-random number for this instance
                float idHash = randValue(abs(grassPosition.x * 10000 +
                                             grassPosition.y * 100 +
                                             grassPosition.z * 0.05 + 2));
                idHash = randValue(idHash * 100000);

                // Animation direction
                float4 animationDirection = float4(0, 0, 1, 0);
                animationDirection = normalize(RotateAroundYInDegrees(animationDirection, idHash * 180.0));

                // Start with a billboard: rotate plane 90 degrees, then random Y
                float4 localPosition = RotateAroundXInDegrees(v.vertex, 90.0);
                localPosition = RotateAroundYInDegrees(localPosition, idHash * 180.0);

                // Vertical growth (bending up)
                localPosition.y += _Scale * v.uv.y * v.uv.y * v.uv.y;

                // Droop offset
                localPosition.xz += _Droop * lerp(0.5, 1.0, idHash) 
                                    * (v.uv.y * v.uv.y * _Scale)
                                    * animationDirection.xz;
                
                // Wind sway
                float4 worldUV = float4(positionBuffer[instanceID].uv, 0, 0);
                float swayVariance = lerp(0.8, 1.0, idHash);
                float movement = v.uv.y * v.uv.y * SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, worldUV.xy, 0).r;
                movement *= swayVariance;
                localPosition.xz += movement;

                // Build final world position
                float4 worldPosition = float4(grassPosition.xyz + localPosition.xyz, 1.0);

                // Vertical displacement
                worldPosition.y -= positionBuffer[instanceID].displacement;
                worldPosition.y *= 1.0 + positionBuffer[instanceID].position.w * lerp(0.8, 1.0, idHash);
                worldPosition.y += positionBuffer[instanceID].displacement;
                
                // Convert to clip space
                o.vertex = TransformWorldToHClip(worldPosition);
                o.uv = v.uv;
                o.noiseVal = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, worldUV.xy, 0).r;
                o.worldPos = worldPosition;

                // Just random chunk data for debug or variation
                o.chunkNum = float3(
                    randValue(_ChunkNum * 20 + 1024),
                    randValue(randValue(_ChunkNum) * 10 + 2048),
                    randValue(_ChunkNum * 4 + 4096)
                );

                return o;
            }

            // --------------------------------------------------
            // Fragment Shader
            // --------------------------------------------------
            half4 frag (v2f i) : SV_Target {
                float4 col = lerp(_Albedo1, _Albedo2, i.uv.y);
                float3 lightDir = normalize(_MainLightDir);
                float ndotl = DotClamped(lightDir, float3(0, 1, 0));

                float4 ao = lerp(_AOColor, 1.0, i.uv.y);
                float4 tip = lerp(0.0, _TipColor, i.uv.y * i.uv.y * (1.0 + _Scale));
                float4 grassColor = (col + tip) * ndotl * ao;

                // Fog calculation
                float viewDistance = length(_CameraPosition - i.worldPos.xyz);
                float fogFactor = (_FogDensity / sqrt(log(2))) * max(0.0, viewDistance - _FogOffset);
                fogFactor = exp2(-fogFactor * fogFactor);

                return lerp(_FogColor, grassColor, fogFactor);
            }

            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Error"
}
