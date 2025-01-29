using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WFC3D_Model : MonoBehaviour
{
    private readonly Dictionary<Vector3Int, int> directionToIndex = new Dictionary<Vector3Int, int>
    {
        { Vector3Int.left, 2 },  // -X
        { Vector3Int.right, 0 }, // +X
        { Vector3Int.forward, 1 }, // +Z (posY in Godot, Z-forward in Unity)
        { Vector3Int.back, 3 },   // -Z (negY)
        { Vector3Int.up, 4 },     // +Y (posZ)
        { Vector3Int.down, 5 }    // -Y (negZ)
    };

    public Dictionary<string, Prototype>[,,] waveFunction;
    public string[,,] _waveFunction;// 3D array to store prototypes
    private Vector3Int size;  // Size of the grid
    public int collapseCounter = 0;
// TODO 31: only give the string instead of the whole dict.
    public void Initialize(Vector3Int newSize, Dictionary<string, Prototype> allPrototypes)
    {
        size = newSize;

        // Initialize the 3D array with the size (x, y, z)
        waveFunction = new Dictionary<string, Prototype>[size.x, size.y, size.z];

        // Populate the 3D array with duplicated prototypes
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    // Assign a duplicated copy of the prototypes to each cell
                    waveFunction[x, y, z] = DuplicatePrototypes(allPrototypes);
                }
            }
        }
    }

    
    // This method returns a new dictionary with deep copies of all prototypes
    private Dictionary<string, Prototype> DuplicatePrototypes(Dictionary<string, Prototype> originalPrototypes)
    {
        var duplicatedPrototypes = new Dictionary<string, Prototype>();

        foreach (var kvp in originalPrototypes)
        {
            // Create a deep copy of the prototype
            var prototypeCopy = new Prototype
            {
                mesh_name = kvp.Value.mesh_name,
                mesh_rotation = kvp.Value.mesh_rotation,
                posX = kvp.Value.posX,
                negX = kvp.Value.negX,
                posY = kvp.Value.posY,
                negY = kvp.Value.negY,
                posZ = kvp.Value.posZ,
                negZ = kvp.Value.negZ,
                constrain_to = kvp.Value.constrain_to,
                constrain_from = kvp.Value.constrain_from,
                weight = kvp.Value.weight,
                // Deep copy the valid_neighbours list
                valid_neighbours = kvp.Value.valid_neighbours.Select(list => new List<string>(list)).ToList()
            };

            // Add the copied prototype to the new dictionary
            duplicatedPrototypes.Add(kvp.Key, prototypeCopy);
        }

        return duplicatedPrototypes;
    }
    //TODO: optimize triple loop 
    public bool IsCollapsed()
    {
        // Iterate through the 3D array
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    // Check if the cell contains more than one prototype
                    if (waveFunction[x, y, z].Count > 1)
                    {
                        return false;  // Grid is not fully collapsed
                    }
                }
            }
        }
        return true;  // All cells contain only one prototype
    }
    
    public Dictionary<string, Prototype> GetPossibilities(Vector3Int coords)
    {
        // Return the dictionary of prototypes at the given coordinates
        return waveFunction[coords.x, coords.y, coords.z];
    }
    
    public List<string> GetPossibleNeighbours(Vector3Int coords, Vector3Int direction)
    {
        // Get the list of prototypes at the given coordinates
        var prototypes = GetPossibilities(coords);
        var validNeighbours = new List<string>();

        // Get the index corresponding to the given direction
        if (!directionToIndex.TryGetValue(direction, out int dirIdx))
        {
            Debug.LogError($"Invalid direction: {direction}");
            return validNeighbours;  // Return an empty list if the direction is invalid
        }

        // Iterate through the prototypes at the given coordinates
        foreach (var prototype in prototypes.Values)
        {
            // Retrieve the neighbors for this prototype in the given direction
            var neighboursInDirection = prototype.valid_neighbours[dirIdx];

            // Add unique neighbors to the validNeighbours list
            foreach (var neighbour in neighboursInDirection)
            {
                if (!validNeighbours.Contains(neighbour))
                {
                    validNeighbours.Add(neighbour);
                }
            }
        }

        return validNeighbours;
    }
    
    public void CollapseCoordsTo(Vector3Int coords, string prototypeName)
    {
        // Get the dictionary of prototypes at the given coordinates
        var cell = waveFunction[coords.x, coords.y, coords.z];

        // Check if the prototype exists in the dictionary
        if (cell.TryGetValue(prototypeName, out Prototype selectedPrototype))
        {
            // Collapse the cell to only contain the selected prototype
            waveFunction[coords.x, coords.y, coords.z] = new Dictionary<string, Prototype>
            {
                { prototypeName, selectedPrototype }
            };
        }
        else
        {
            Debug.LogError($"Prototype '{prototypeName}' not found at ({coords.x}, {coords.y}, {coords.z})");
        }
    }
    
    public void CollapseAt(Vector3Int coords)
    {
        // Get the possible prototypes at the given coordinates
        var possiblePrototypes = waveFunction[coords.x, coords.y, coords.z];

        // Select a prototype using weighted choice
        string selectedPrototypeName = WeightedChoice(possiblePrototypes);
    
        // Collapse the cell to contain only the selected prototype
        if (possiblePrototypes.TryGetValue(selectedPrototypeName, out Prototype selectedPrototype))
        {
            waveFunction[coords.x, coords.y, coords.z] = new Dictionary<string, Prototype>
            {
                { selectedPrototypeName, selectedPrototype }
            };
        }
        else
        {
            Debug.LogError($"Failed to collapse at ({coords.x}, {coords.y}, {coords.z}). Prototype '{selectedPrototypeName}' not found.");
        }
    }
    
    private string WeightedChoice(Dictionary<string, Prototype> prototypes)
    {
        // Create a dictionary to store weights with the corresponding prototype names
        var protoWeights = new Dictionary<float, string>();

        // Assign each prototype a random adjusted weight
        foreach (var kvp in prototypes)
        {
            float weight = kvp.Value.weight + UnityEngine.Random.Range(-1.0f, 1.0f);
            protoWeights[weight] = kvp.Key;
        }

        // Get the sorted list of weights
        var weightList = protoWeights.Keys.ToList();
        weightList.Sort();

        // Return the prototype with the highest adjusted weight
        return protoWeights[weightList[weightList.Count - 1]];
    }

    public void collapse()
    {
        Vector3Int coords = GetMinEntropyCoords();
        CollapseAt(coords);
        Debug.Log($"Collapsed cell at {coords}");
    }
    
    public void Constrain(Vector3Int coords, string prototypeName)
    {
        // Get the dictionary of prototypes at the given coordinates
        var cell = waveFunction[coords.x, coords.y, coords.z];

        // Attempt to remove the specified prototype
        if (cell.Remove(prototypeName))
        {
            Debug.Log($"Prototype '{prototypeName}' removed from cell at {coords}");
        }
        else
        {
            Debug.LogWarning($"Prototype '{prototypeName}' not found in cell at {coords}");
        }
    }

    
    public Vector3Int GetMinEntropyCoords()
    {
        float? minEntropy = null;
        Vector3Int minCoords = Vector3Int.zero;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    int entropy = GetEntropy(new Vector3Int(x, y, z));
                    if (entropy > 1)
                    {
                        float adjustedEntropy = entropy + UnityEngine.Random.Range(-0.1f, 0.1f);
                        if (minEntropy == null || adjustedEntropy < minEntropy)
                        {
                            minEntropy = adjustedEntropy;
                            minCoords = new Vector3Int(x, y, z);
                        }
                    }
                }
            }
        }

        if (minEntropy == null) Debug.LogError("No valid entropy found!");
        return minCoords;
    }

    public int GetEntropy(Vector3Int coords)
    {
        // Get the list of possible prototypes at the given coordinates
        var possibilities = waveFunction[coords.x, coords.y, coords.z];
        return possibilities.Count;  // Return the number of possibilities
    }
    
    public void Iterate()
    {
        // Get the coordinates with the minimum entropy
        Vector3Int coords = GetMinEntropyCoords();

        // Collapse the selected cell
        CollapseAt(coords);
        
        // Propagate constraints from the collapsed cell
        Propagate(coords);

        Debug.Log($"Iterated and collapsed cell at {coords}");
        collapseCounter++;
    }
    
    private Stack<Vector3Int> stack = new Stack<Vector3Int>();

    public void Propagate(Vector3Int coords, bool singleIteration = false)
    {
        if (coords != null)
        {
            stack.Push(coords);
        }

        while (stack.Count > 0)
        {
            Vector3Int currentCoords = stack.Pop();

            foreach (var direction in GetValidDirections(currentCoords))
            {
                Vector3Int neighborCoords = currentCoords + direction;

                List<string> validNeighbors = GetPossibleNeighbours(currentCoords, direction);

                var neighborPrototypes = GetPossibilities(neighborCoords);

                if (neighborPrototypes.Count == 0)
                {
                    Debug.LogWarning($"Neighbor at {neighborCoords} is already empty!");
                    continue;
                }

                foreach (var prototypeName in new List<string>(neighborPrototypes.Keys))
                {
                    if (!validNeighbors.Contains(prototypeName))
                    {
                        Constrain(neighborCoords, prototypeName);

                        if (neighborPrototypes.Count == 0)
                        {
                            Debug.LogError($"Cell at {neighborCoords} has no valid prototypes after constraint!");
                        }

                        if (!stack.Contains(neighborCoords))
                        {
                            stack.Push(neighborCoords);
                        }
                    }
                }
            }

            if (singleIteration)
                break;
        }
    }

    
    public List<Vector3Int> GetValidDirections(Vector3Int coords)
    {
        List<Vector3Int> directions = new List<Vector3Int>();

        // Check the boundaries along each axis
        if (coords.x > 0) directions.Add(Vector3Int.left);     // -X (left)
        if (coords.x < size.x - 1) directions.Add(Vector3Int.right);    // +X (right)

        if (coords.y > 0) directions.Add(Vector3Int.down);     // -Y (down)
        if (coords.y < size.y - 1) directions.Add(Vector3Int.up);       // +Y (up)

        if (coords.z > 0) directions.Add(Vector3Int.back);     // -Z (back)
        if (coords.z < size.z - 1) directions.Add(Vector3Int.forward);  // +Z (forward)

        return directions;
    }

    
}
