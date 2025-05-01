using UnityEngine;
using System.Collections.Generic;

public class TreeSpawner : MonoBehaviour 
{
    [Header("Tree Settings")]
    public GameObject treePrefab;
    [Range(0.01f, 1f)]
    public float treeDensity = 0.1f;
    [Range(0f, 1f)]
    public float minHeightThreshold = 0.2f; // Minimum height to spawn trees
    [Range(0f, 1f)]
    public float maxHeightThreshold = 0.8f; // Maximum height to spawn trees
    [Range(0f, 85f)]
    public float maxSlopeAngle = 30f; // Maximum slope angle for tree placement
    
    [Header("Tree Variation")]
    public Vector2 scaleRange = new Vector2(0.01f, 1f);
    [Range(0f, 1f)]
    public float randomRotation = 1f;
    
    [Header("Performance")]
    public int maxTreesPerChunk = 100;
    public Transform treeParent;
    
    // Method to spawn trees based on the heightMap data
    public void SpawnTrees(MapData mapData, Vector2 chunkPosition, float meshHeightMultiplier, AnimationCurve heightCurve, bool useFlatShading)
    {
        int width = mapData.heightMap.GetLength(0);
        int height = mapData.heightMap.GetLength(1);
        float[,] heightMap = mapData.heightMap;
        
        // Create parent if not assigned
        if (treeParent == null)
        {
            GameObject treeContainer = new GameObject("Trees_" + chunkPosition.x + "_" + chunkPosition.y);
            treeParent = treeContainer.transform;
            treeParent.SetParent(transform);
        }
        
        int treeCount = 0;
        List<Vector3> treePositions = new List<Vector3>();
        
        // Calculate a reasonable step to avoid checking every vertex
        int step = Mathf.Max(1, Mathf.FloorToInt(1f / treeDensity / 10f));
        
        for (int y = 1; y < height - 1; y += step)
        {
            for (int x = 1; x < width - 1; x += step)
            {
                // Skip if we've reached the maximum tree count
                if (treeCount >= maxTreesPerChunk)
                    break;
                
                // Only place trees with a probability based on density
                if (Random.value > treeDensity)
                    continue;
                
                float heightValue = heightMap[x, y];
                
                // Skip if the height is outside the valid range for trees
                if (heightValue < minHeightThreshold || heightValue > maxHeightThreshold)
                    continue;
                
                // Calculate world position
                float worldX = (x - width/2f) + chunkPosition.x;
                float worldZ = (y - height/2f) + chunkPosition.y;
                float worldY = heightCurve.Evaluate(heightValue) * meshHeightMultiplier;
                
                // Calculate slope based on neighboring vertices
                Vector3 normal = CalculateNormal(x, y, heightMap, meshHeightMultiplier, heightCurve);
                float slope = Vector3.Angle(normal, Vector3.up);
                
                // Skip if the slope is too steep
                if (slope > maxSlopeAngle)
                    continue;
                
                // Check for minimum distance between trees
                Vector3 treePos = new Vector3(worldX, worldY, worldZ);
                bool tooClose = false;
                
                foreach (Vector3 existingTree in treePositions)
                {
                    if (Vector3.Distance(existingTree, treePos) < 2.0f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                
                if (tooClose)
                    continue;
                
                // Add to our position list
                treePositions.Add(treePos);
                
                // Instantiate tree
                GameObject tree = Instantiate(treePrefab, treePos, Quaternion.identity, treeParent);
                
                // Random variation
                float scale = Random.Range(scaleRange.x, scaleRange.y);
                tree.transform.localScale = new Vector3(scale, scale, scale);
                
                // Apply random rotation around vertical axis
                if (randomRotation > 0)
                {
                    float rotationAmount = Random.Range(0f, 360f) * randomRotation;
                    tree.transform.rotation = Quaternion.Euler(0, rotationAmount, 0);
                }
                
                treeCount++;
            }
        }
        
        //Debug.Log($"Spawned {treeCount} trees in chunk at {chunkPosition}");
    }
    
    // Calculate the normal at a vertex for slope detection
    private Vector3 CalculateNormal(int x, int y, float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve)
    {
        float heightL = heightCurve.Evaluate(heightMap[x-1, y]) * heightMultiplier;
        float heightR = heightCurve.Evaluate(heightMap[x+1, y]) * heightMultiplier;
        float heightD = heightCurve.Evaluate(heightMap[x, y-1]) * heightMultiplier;
        float heightU = heightCurve.Evaluate(heightMap[x, y+1]) * heightMultiplier;
        
        Vector3 normal = new Vector3(heightL - heightR, 2f, heightD - heightU).normalized;
        return normal;
    }
    
    // Method to clear all trees
    public void ClearTrees()
    {
        if (treeParent != null)
        {
            DestroyImmediate(treeParent.gameObject);
            treeParent = null;
        }
    }
}