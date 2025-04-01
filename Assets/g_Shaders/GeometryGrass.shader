Shader "Unlit/GeometryGrass URP" {
    Properties {
        _Albedo1 ("Albedo 1", Color) = (1, 1, 1)
        _Albedo2 ("Albedo 2", Color) = (1, 1, 1)
        _AOColor ("Ambient Occlusion", Color) = (1, 1, 1)
        _TipColor ("Tip Color", Color) = (1, 1, 1)
        _Height("Grass Height", float) = 3
        _Width("Grass Width", range(0, 0.1)) = 0.05
        _FogColor ("Fog Color", Color) = (1, 1, 1)
        _FogDensity ("Fog Density", Range(0.0, 1.0)) = 0.0
        _FogOffset ("Fog Offset", Range(0.0, 10.0)) = 0.0
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
            #pragma vertex vp
            #pragma fragment fp
            #pragma geometry gp
            
            #pragma target 4.5
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // You'll need to include your custom Random.cginc file or copy its contents here
            // Replace this with the actual path to your Random.cginc
            #include "../Resources/Random.cginc"
            #include "../Resources/Simplex.hlsl"
            
            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2g {
                float4 vertex : SV_POSITION;
            };

            struct g2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Albedo1, _Albedo2, _AOColor, _TipColor, _FogColor;
                float _FogDensity, _FogOffset;
                float _Height, _Width;
            CBUFFER_END

            v2g vp(VertexData v) {
                v2g o;
                o.vertex = v.vertex;
                return o;
            }

            float4 RotateAroundYInDegrees(float4 vertex, float degrees) {
                float alpha = degrees * PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                float2 rotated = mul(m, vertex.xz);
                return float4(rotated.x, vertex.y, rotated.y, vertex.w);
            }

            [maxvertexcount(30)]
            void gp(point v2g points[1], inout TriangleStream<g2f> triStream) {
                int i;
                float4 root = points[0].vertex;

                float idHash = randValue(abs(root.x * 10000 + root.y * 100 + root.z * 0.05f + 2));
                idHash = randValue(idHash * 100000);

                const int vertexCount = 12;

                g2f v[vertexCount];

                for (i = 0; i < vertexCount; ++i) {
                    v[i].vertex = 0.0f;
                    v[i].uv = 0.0f;
                }

                float currentV = 0;
                float offsetV = 1.0f / ((vertexCount / 2) - 1);

                float currentHeightOffset = 0;
                float currentVertexHeight = 0;

                for (i = 0; i < vertexCount; ++i) {

                    float widthMod = 1.0f - float(i) / float(vertexCount);
                    widthMod = pow(widthMod * widthMod, 1.0f / 3.0f);
                    
                    if (i % 2 == 0) {
                        v[i].vertex = float4(root.x - (_Width * widthMod), root.y + currentVertexHeight, root.z, 1);
                        v[i].uv = float2(0, currentV);
                    } else {
                        v[i].vertex = float4(root.x + (_Width * widthMod), root.y + currentVertexHeight, root.z, 1);
                        v[i].uv = float2(1, currentV);

                        currentV += offsetV;
                        currentVertexHeight = currentV * _Height * lerp(0.9f, 1.35f, idHash);
                    }

                    float sway = snoise(v[i].vertex.xyz * 0.35f + _Time.y * 0.25f) * v[i].uv.y * 0.07f;
                    v[i].vertex.xz += sway;
                    v[i].vertex.xyz -= root.xyz;
                    v[i].vertex = RotateAroundYInDegrees(v[i].vertex, idHash * 180.0f);
                    v[i].vertex.xyz += root.xyz;

                    v[i].worldPos = v[i].vertex;
                    v[i].vertex = TransformObjectToHClip(v[i].vertex.xyz);
                }

                for (i = 0; i < vertexCount - 2; ++i) {
                    triStream.Append(v[i]);
                    triStream.Append(v[i + 2]);
                    triStream.Append(v[i + 1]);
                }
            }

            half4 fp(g2f i) : SV_Target {
                half4 col = lerp(_Albedo1, _Albedo2, i.uv.y);
                
                // Get main light direction
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float ndotl = saturate(dot(lightDir, normalize(float3(0, 1, 0))));

                half4 ao = lerp(_AOColor, 1.0f, i.uv.y);
                half4 tip = lerp(0.0f, _TipColor, i.uv.y * i.uv.y);
                
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