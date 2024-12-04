Shader "Custom/PlaneGerstnerWaves"
{
    Properties
    {
        // Gerstner wave properties
        _TestDirection1("Wave Direction 1", Vector) = (1, 0, 0, 0)
        _Speed1("Speed 1", Float) = 1.0
        _Steepness1("Steepness 1", Range(0, 1)) = 0.5
        _Amplitude1("Amplitude 1", Float) = 1.0
        _Wavelength1("Wavelength 1", Float) = 1.0

        _TestDirection2("Wave Direction 2", Vector) = (0, 1, 0, 0)
        _Speed2("Speed 2", Float) = 1.5
        _Steepness2("Steepness 2", Range(0, 1)) = 0.4
        _Amplitude2("Amplitude 2", Float) = 0.7
        _Wavelength2("Wavelength 2", Float) = 1.2

        _TestDirection3("Wave Direction 3", Vector) = (1, 1, 0, 0)
        _Speed3("Speed 3", Float) = 0.8
        _Steepness3("Steepness 3", Range(0, 1)) = 0.6
        _Amplitude3("Amplitude 3", Float) = 0.9
        _Wavelength3("Wavelength 3", Float) = 0.9
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Gerstner wave parameters
            float2 _TestDirection1, _TestDirection2, _TestDirection3;
            float _Speed1, _Speed2, _Speed3;
            float _Steepness1, _Steepness2, _Steepness3;
            float _Amplitude1, _Amplitude2, _Amplitude3;
            float _Wavelength1, _Wavelength2, _Wavelength3;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            // Gerstner wave function to calculate vertex displacement
            float3 GerstnerWave(float3 vertex, float2 direction, float time, float speed, float steepness, float amplitude, float wavelength)
            {
                float dotProduct = dot(direction, vertex.xz);
                float phase = wavelength * dotProduct + speed * time;
                float cosFactor = cos(phase);
                float sinFactor = sin(phase);

                float displacedX = vertex.x + (steepness / wavelength) * direction.x * cosFactor;
                float displacedZ = vertex.z + (steepness / wavelength) * direction.y * cosFactor;
                float displacedY = vertex.y + amplitude * sinFactor;

                return float3(displacedX, displacedY, displacedZ);
            }

            // Aggregate multiple Gerstner waves
            float3 AggregateGerstnerWaves(float3 vertex, float time)
            {
                float3 result = vertex;

                // Add contributions from each wave
                result += GerstnerWave(vertex, normalize(_TestDirection1), time, _Speed1, _Steepness1, _Amplitude1, _Wavelength1);
                result += GerstnerWave(vertex, normalize(_TestDirection2), time, _Speed2, _Steepness2, _Amplitude2, _Wavelength2);
                result += GerstnerWave(vertex, normalize(_TestDirection3), time, _Speed3, _Steepness3, _Amplitude3, _Wavelength3);

                return result;
            }

            v2f vert(appdata v)
            {
                v2f o;
                float time = _Time.y; // Use Unity's _Time.y for animation

                // Calculate the displaced vertex position using aggregated waves
                float3 displacedVertex = AggregateGerstnerWaves(v.vertex.xyz, time);

                // Apply the displacement to the vertex position
                o.pos = UnityObjectToClipPos(float4(displacedVertex, 1.0));
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Return a fixed color since appearance is not relevant
                return float4(0.2, 0.5, 0.8, 1.0); // Ocean blue color
            }
            ENDCG
        }
    }

    Fallback "Diffuse"
}
