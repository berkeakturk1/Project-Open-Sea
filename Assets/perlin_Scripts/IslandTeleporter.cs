using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NumberedIslandTeleporter : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    
    [Header("Teleport Settings")]
    public float heightOffset = 5f;
    
    private List<Vector3> islandPositions = new List<Vector3>();


    private void Awake()
    {
        // Default to main camera if no player transform set
        if (playerTransform == null)
            playerTransform = Camera.main.transform;
        
        // Find MapGenerator in the scene
        MapGenerator mapGenerator = FindObjectOfType<MapGenerator>();
        
        if (mapGenerator == null)
        {
            Debug.LogError("No MapGenerator found in the scene!");
            return;
        }
        
        // Debug: Log MapGenerator hierarchy
        Debug.Log($"MapGenerator found: {mapGenerator.name}");
        Debug.Log($"MapGenerator children count: {mapGenerator.transform.childCount}");
        
        // Find all children of MapGenerator
        Transform[] allChildren = mapGenerator.GetComponentsInChildren<Transform>();
        Debug.Log($"Total children (including self): {allChildren.Length}");
        
        // Debug: List all child names
        foreach (Transform child in allChildren)
        {
            Debug.Log($"Child name: {child.name}");
        }
        
        // Find TerrainChunk children
        Transform[] terrainChunks = allChildren
            .Where(t => t.name == "Terrain Chunk")
            .ToArray();
        
        Debug.Log($"TerrainChunk objects found: {terrainChunks.Length}");
        
        // Store their world positions
        islandPositions = terrainChunks.Select(t => t.position).ToList();
        
        // Debug: Print island positions
        for (int i = 0; i < islandPositions.Count; i++)
        {
            Debug.Log($"Island {i} position: {islandPositions[i]}");
        }
        
        PrintInstructions();
    }

    private void Start()
    {
        
    }
    
    private void Update()
    {
        // Teleport to islands using number keys
        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                TeleportToIsland(i);
                break;
            }
        }
    }
    
    public void TeleportToIsland(int index)
    {
        // Check if index is valid
        if (index < 0 || index >= islandPositions.Count)
        {
            Debug.LogWarning($"No island defined for index {index}. Total islands: {islandPositions.Count}");
            return;
        }
        
        Vector3 worldPos = islandPositions[index];
        
        // Raycast down to find the terrain height at this position
        RaycastHit hit;
        if (Physics.Raycast(worldPos + Vector3.up * 1000f, Vector3.down, out hit, 2000f))
        {
            worldPos.y = hit.point.y + heightOffset;
        }
        else
        {
            // Fallback if raycast fails
            worldPos.y += heightOffset;
            Debug.LogWarning("Could not find terrain height at teleport location, using offset height.");
        }
        
        // Teleport player
        playerTransform.position = worldPos;
        
        Debug.Log($"Teleported to Island #{index + 1} at {worldPos}");
    }
    
    private void PrintInstructions()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("--- ISLAND TELEPORTER INSTRUCTIONS ---");
        sb.AppendLine($"Total Islands: {islandPositions.Count}");
        sb.AppendLine("Press a number key to teleport to the corresponding island:");
        
        for (int i = 0; i < islandPositions.Count && i < 10; i++)
        {
            int displayNum = (i == 9) ? 0 : i + 1; // 0 key maps to the 10th island
            Vector3 pos = islandPositions[i];
            sb.AppendLine($"  Key {displayNum}: Island at ({pos.x}, {pos.y}, {pos.z})");
        }
        
        Debug.Log(sb.ToString());
    }
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || islandPositions == null)
            return;
            
        Gizmos.color = Color.green;
        
        foreach (Vector3 pos in islandPositions)
        {
            Gizmos.DrawWireSphere(pos, 5f);
            
            // Try to find ground level with raycast
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 1000f, Vector3.down, out hit, 2000f))
            {
                Gizmos.DrawLine(pos, hit.point);
                Gizmos.DrawWireSphere(hit.point + Vector3.up * heightOffset, 2f);
            }
        }
    }
}