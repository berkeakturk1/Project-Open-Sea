Shader "Unlit/ModelGrass URP" {
    Properties {
        _Albedo1 ("Albedo 1", Color) = (1, 1, 1)
        _Albedo2 ("Albedo 2", Color) = (1, 1, 1)
        _AOColor ("Ambient Occlusion", Color) = (1, 1, 1)
        _TipColor ("Tip Color", Color) = (1, 1, 1)
        _Scale ("Scale", Range(0.0, 2.0)) = 0.0
        _Droop ("Droop", Range(0.0, 10.0)) = 0.0
        _FogColor ("Fog Color", Color) = (1, 1, 1)
        _FogDensity ("Fog Density", Range(0.0, 1.0)) = 0.0
        _FogOffset ("Fog Offset", Range(0.0, 10.0)) = 0.0
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
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // You'll need to include your custom Random.cginc file or copy its contents here
            // Replace this with the actual path to your Random.cginc
            #include "../Resources/Random.cginc"
            
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

            struct GrassData {
                float4 position;
                float2 uv;
                float displacement;
            };

            TEXTURE2D(_WindTex);
            SAMPLER(sampler_WindTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo1, _Albedo2, _AOColor, _TipColor, _FogColor;
                float _Scale, _Droop, _FogDensity, _FogOffset;
                float4 _WindTex_ST;
                int _ChunkNum;
            CBUFFER_END
            
            StructuredBuffer<GrassData> positionBuffer;

            float4 RotateAroundYInDegrees(float4 vertex, float degrees) {
                float alpha = degrees * PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float4(mul(m, vertex.xz), vertex.yw).xzyw;
            }
            
            float4 RotateAroundXInDegrees(float4 vertex, float degrees) {
                float alpha = degrees * PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                float2 rotated = mul(m, vertex.yz);
                return float4(vertex.x, rotated.x, rotated.y, vertex.w);
            }

            v2f vert(VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;
                float4 grassPosition = positionBuffer[instanceID].position;

                float idHash = randValue(abs(grassPosition.x * 10000 + grassPosition.y * 100 + grassPosition.z * 0.05f + 2));
                idHash = randValue(idHash * 100000);

                float4 animationDirection = float4(0.0f, 0.0f, 1.0f, 0.0f);
                animationDirection = normalize(RotateAroundYInDegrees(animationDirection, idHash * 180.0f));

                float4 localPosition = RotateAroundXInDegrees(v.vertex, 90.0f);
                localPosition = RotateAroundYInDegrees(localPosition, idHash * 180.0f);
                localPosition.y += _Scale * v.uv.y * v.uv.y * v.uv.y;
                localPosition.xz += _Droop * lerp(0.5f, 1.0f, idHash) * (v.uv.y * v.uv.y * _Scale) * animationDirection.xz;
                
                float4 worldUV = float4(positionBuffer[instanceID].uv, 0, 0);
                
                float swayVariance = lerp(0.8, 1.0, idHash);
                float movement = v.uv.y * v.uv.y * (SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, worldUV.xy, 0).r);
                movement *= swayVariance;
                
                localPosition.xz += movement;
                
                float4 worldPosition = float4(grassPosition.xyz + localPosition.xyz, 1.0f);

                worldPosition.y -= positionBuffer[instanceID].displacement;
                worldPosition.y *= 1.0f + positionBuffer[instanceID].position.w * lerp(0.8f, 1.0f, idHash);
                worldPosition.y += positionBuffer[instanceID].displacement;
                
                o.vertex = TransformObjectToHClip(worldPosition.xyz);
                o.uv = v.uv;
                o.noiseVal = SAMPLE_TEXTURE2D_LOD(_WindTex, sampler_WindTex, worldUV.xy, 0).r;
                o.worldPos = worldPosition;
                o.chunkNum = float3(randValue(_ChunkNum * 20 + 1024), randValue(randValue(_ChunkNum) * 10 + 2048), randValue(_ChunkNum * 4 + 4096));

                return o;
            }

            half4 frag(v2f i) : SV_Target {
                half4 col = lerp(_Albedo1, _Albedo2, i.uv.y);
                
                // Get main light direction
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float ndotl = saturate(dot(lightDir, normalize(float3(0, 1, 0))));

                half4 ao = lerp(_AOColor, 1.0f, i.uv.y);
                half4 tip = lerp(0.0f, _TipColor, i.uv.y * i.uv.y * (1.0f + _Scale));

                half4 grassColor = (col + tip) * ndotl * ao;

                /* Fog */
                float viewDistance = length(_WorldSpaceCameraPos - i.worldPos.xyz);
                float fogFactor = (_FogDensity / sqrt(log(2))) * (max(0.0f, viewDistance - _FogOffset));
                fogFactor = exp2(-fogFactor * fogFactor);

                return lerp(_FogColor, grassColor, fogFactor);
            }
            ENDHLSL
        }
    }
}