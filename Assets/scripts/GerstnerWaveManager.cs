using System;
using System.Collections.Generic;
using UnityEngine;

public class GerstnerWaveManager : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public float amplitude;
        public Vector3 direction;
        public float depth;
        public float gravity;
        public float timescale;
        public float phase;
    }

    [Header("Wave Settings")]
    public List<Wave> waves = new List<Wave>();

    [Header("Grid Settings")]
    public int gridSize = 10;
    public float gridSpacing = 1f;
    public GameObject spherePrefab;

    [Header("Render Grid")]
    public bool renderGrid = false;

    private GameObject[,] grid;

    void Start()
    {
        CreateGrid();
    }

    void Update()
    {
        if (!renderGrid)
        {
            ResetGrid();
        }
        else
        {
            CreateGrid();
            UpdateGrid();    
        }

        
    }

    // Function to calculate Gerstner wave displacement at a given (x, z) position
    public Vector3 CalculateGerstnerWave(float x, float z, float time)
    {
        float newX = x;
        float newY = 0f;
        float newZ = z;

        foreach (Wave wave in waves)
        {
            float k = wave.direction.magnitude;
            float kmh = k * wave.depth;
            float omega = Mathf.Sqrt(wave.gravity * k * (float)Math.Tanh(kmh));
            float theta = wave.direction.x * x + wave.direction.z * z - omega * time * wave.timescale - wave.phase;

            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            newX -= (wave.direction.x / k) * (wave.amplitude / (float)Math.Tanh(kmh)) * sinTheta;
            newZ -= (wave.direction.z / k) * (wave.amplitude / (float)Math.Tanh(kmh)) * sinTheta;
            newY += wave.amplitude * cosTheta;
        }

        return new Vector3(newX, newY, newZ);
    }

    // Create a grid of spheres
    void CreateGrid()
    {
        grid = new GameObject[gridSize, gridSize];

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                Vector3 position = new Vector3(i * gridSpacing, 0, j * gridSpacing);
                grid[i, j] = Instantiate(spherePrefab, position, Quaternion.identity, transform);
            }
        }
    }

    // Update the grid positions to reflect wave heights
    void UpdateGrid()
    {
        float time = Time.time;

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                float x = i * gridSpacing;
                float z = j * gridSpacing;
                Vector3 wavePosition = CalculateGerstnerWave(x, z, time);
                grid[i, j].transform.position = wavePosition;
            }
        }
    }

    // Reset the grid of spheres
    void ResetGrid()
    {
        // Delete existing spheres
        if (grid != null)
        {
            for (int i = 0; i < gridSize; i++)
            {
                for (int j = 0; j < gridSize; j++)
                {
                    if (grid[i, j] != null)
                    {
                        Destroy(grid[i, j]);
                    }
                }
            }
        }

       
    }
}
