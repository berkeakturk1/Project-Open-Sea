using UnityEngine;
using System; // For Math.Tanh

public class GerstnerWaveManager : MonoBehaviour
{
    [System.Serializable]
    public struct GerstnerWave
    {
        public float amplitude;   // Height of the wave
        public Vector3 direction; // Wave direction (not normalized)
        public float depth;       // Water depth
        public float gravity;     // Gravity constant
        public float timescale;   // Speed of wave propagation
    }

    [Header("Wave Settings")]
    public GerstnerWave[] waves = new GerstnerWave[4]; // Four waves

    [Header("Global Settings")]
    public float timeOffset = 0f; // Time offset for animation

    [Header("Grid Settings")]
    public int gridSize = 10;       // Number of points along one axis
    public float gridSpacing = 1f; // Spacing between points
    public GameObject dotPrefab;   // Prefab for visualizing points
    public Vector3 gridOrigin = Vector3.zero; // Origin of the grid in local space
    
    [Header("Toggle Settings")]
    public bool renderDots = true; // Toggle to render or destroy the dots
    
    private GameObject[,] dotGrid; // Grid of dots
    private bool dotsActive = false; // Internal state to track if dots are active

    private void Start()
    {
        dotGrid = new GameObject[gridSize, gridSize];
    }

    private void Update()
    {
        // Check if the renderDots state has changed
        if (renderDots && !dotsActive)
        {
            CreateDots();
        }
        else if (!renderDots && dotsActive)
        {
            DestroyDots();
        }

        // Update the dots' heights if they are active
        if (dotsActive)
        {
            UpdateDots();
        }
    }

    /// <summary>
    /// Creates the dots grid and instantiates the dot prefabs.
    /// </summary>
    private void CreateDots()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Calculate position relative to the grid origin
                Vector3 localPosition = gridOrigin + new Vector3(x * gridSpacing, 0, z * gridSpacing);
                Vector3 worldPosition = transform.TransformPoint(localPosition);

                // Instantiate the dot at the correct position
                dotGrid[x, z] = Instantiate(dotPrefab, worldPosition, Quaternion.identity, transform);
            }
        }
        dotsActive = true; // Mark dots as active
    }

    /// <summary>
    /// Destroys the dots grid and clears the dotGrid array.
    /// </summary>
    private void DestroyDots()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                if (dotGrid[x, z] != null)
                {
                    Destroy(dotGrid[x, z]);
                    dotGrid[x, z] = null;
                }
            }
        }
        dotsActive = false; // Mark dots as inactive
    }

    /// <summary>
    /// Updates the height of each dot in the grid.
    /// </summary>
    private void UpdateDots()
    {
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Calculate position relative to the grid origin
                Vector3 localPosition = gridOrigin + new Vector3(x * gridSpacing, 0, z * gridSpacing);
                Vector3 worldPosition = transform.TransformPoint(localPosition);

                // Get the wave height at this position
                float height = GetWaveHeight(worldPosition);

                // Update the dot's position
                Vector3 dotPosition = dotGrid[x, z].transform.position;
                dotGrid[x, z].transform.position = new Vector3(dotPosition.x, height, dotPosition.z);
            }
        }
    }

    /// <summary>
    /// Calculate the frequency of a wave based on your Shader Graph logic.
    /// </summary>
    private float CalculateFrequency(GerstnerWave wave)
    {
        float directionLength = wave.direction.magnitude; // Length of the direction vector
        float kh = directionLength * wave.depth; // Direction Length * Depth
        float tanhKh = (float)Math.Tanh(kh); // Hyperbolic tangent of kh
        return Mathf.Sqrt(wave.gravity * directionLength * tanhKh); // Frequency
    }

    /// <summary>
    /// Calculate the phase for a wave.
    /// </summary>
    private float CalculatePhase(Vector3 position, GerstnerWave wave, float frequency)
    {
        Vector2 direction = new Vector2(wave.direction.x, wave.direction.z).normalized;
        return Vector2.Dot(direction, new Vector2(position.x, position.z)) - frequency * (Time.time + timeOffset) * wave.timescale;
    }

    /// <summary>
    /// Calculate only the height of the waves at a given position.
    /// </summary>
    public float GetWaveHeight(Vector3 position)
    {
        float height = 0f;
        foreach (var wave in waves)
        {
            float frequency = CalculateFrequency(wave); // Get the wave frequency
            float phase = CalculatePhase(position, wave, frequency); // Get the phase
            height += wave.amplitude * Mathf.Sin(phase); // Vertical displacement
        }
        return height;
    }
}
