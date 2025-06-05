    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;

    public class WFC3D_Model : MonoBehaviour
    {
        // --- Original Fields ---
        private readonly Dictionary<Vector3Int, int> directionToIndex = new Dictionary<Vector3Int, int>
        {
            { Vector3Int.left, 2 },  // -X
            { Vector3Int.right, 0 }, // +X
            { Vector3Int.forward, 1 }, // +Z
            { Vector3Int.back, 3 },   // -Z
            { Vector3Int.up, 4 },     // +Y
            { Vector3Int.down, 5 }    // -Y
        };

        public Dictionary<string, Prototype>[,,] waveFunction;
        // public string[,,] _waveFunction; // This field was unused and has been removed.
        private Vector3Int size;
        public int collapseCounter = 0;
        
        
        // --- Optimizations: State Management ---
        private HashSet<Vector3Int> uncollapsedCells; // Tracks cells with entropy > 1
        private Stack<Vector3Int> propagationStack = new Stack<Vector3Int>(); // Reusable stack for propagation

        public void Initialize(Vector3Int newSize, Dictionary<string, Prototype> allPrototypes)
        {
            size = newSize;
            waveFunction = new Dictionary<string, Prototype>[size.x, size.y, size.z];
            uncollapsedCells = new HashSet<Vector3Int>();

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        waveFunction[x, y, z] = DuplicatePrototypes(allPrototypes);
                        if (waveFunction[x, y, z].Count > 1) // Should always be true initially if allPrototypes > 1
                        {
                            uncollapsedCells.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }
            collapseCounter = 0; // Reset counter on initialize
        }

        private Dictionary<string, Prototype> DuplicatePrototypes(Dictionary<string, Prototype> originalPrototypes)
        {
            var duplicatedPrototypes = new Dictionary<string, Prototype>(originalPrototypes.Count);
            foreach (var kvp in originalPrototypes)
            {
                var originalProto = kvp.Value;
                var prototypeCopy = new Prototype
                {
                    mesh_name = originalProto.mesh_name,
                    mesh_rotation = originalProto.mesh_rotation,
                    posX = originalProto.posX,
                    negX = originalProto.negX,
                    posY = originalProto.posY,
                    negY = originalProto.negY,
                    posZ = originalProto.posZ,
                    negZ = originalProto.negZ,
                    constrain_to = originalProto.constrain_to, // Assuming shallow copy is fine for these
                    constrain_from = originalProto.constrain_from,
                    weight = originalProto.weight,
                    valid_neighbours = originalProto.valid_neighbours.Select(list => new List<string>(list)).ToList() // Deep copy list of lists
                };
                duplicatedPrototypes.Add(kvp.Key, prototypeCopy);
            }
            return duplicatedPrototypes;
        }

        /// <summary>
        /// Checks if the entire grid is collapsed (all cells have 1 prototype).
        /// Optimized to check the count of uncollapsed cells.
        /// </summary>
        public bool IsCollapsed()
        {
            return uncollapsedCells.Count == 0;
        }

        public Dictionary<string, Prototype> GetPossibilities(Vector3Int coords)
        {
            return waveFunction[coords.x, coords.y, coords.z];
        }

        /// <summary>
        /// Gets all possible prototype names that can be neighbors in a given direction.
        /// Optimized to use HashSet for collecting unique neighbors.
        /// </summary>
        public HashSet<string> GetPossibleNeighbours(Vector3Int coords, Vector3Int direction)
        {
            var prototypesInCurrentCell = GetPossibilities(coords);
            var validNeighbourPrototypes = new HashSet<string>();

            if (!directionToIndex.TryGetValue(direction, out int dirIdx))
            {
                Debug.LogError($"Invalid direction: {direction}");
                return validNeighbourPrototypes; // Return empty set
            }

            foreach (var prototype in prototypesInCurrentCell.Values)
            {
                // Ensure the prototype has valid_neighbours initialized and the index is valid
                if (prototype.valid_neighbours != null && dirIdx < prototype.valid_neighbours.Count)
                {
                    var neighboursInDirection = prototype.valid_neighbours[dirIdx];
                    if (neighboursInDirection != null)
                    {
                        foreach (var neighbourName in neighboursInDirection)
                        {
                            validNeighbourPrototypes.Add(neighbourName);
                        }
                    }
                }
            }
            return validNeighbourPrototypes;
        }
        
        public void CollapseCoordsTo(Vector3Int coords, string prototypeName)
        {
            var cell = waveFunction[coords.x, coords.y, coords.z];
            if (cell.TryGetValue(prototypeName, out Prototype selectedPrototype))
            {
                waveFunction[coords.x, coords.y, coords.z] = new Dictionary<string, Prototype>
                {
                    { prototypeName, selectedPrototype }
                };
                uncollapsedCells.Remove(coords); // Mark as collapsed
            }
            else
            {
                Debug.LogError($"Prototype '{prototypeName}' not found at {coords} during CollapseCoordsTo. Cell not changed.");
            }
        }

        public void CollapseAt(Vector3Int coords)
        {
            var possiblePrototypes = GetPossibilities(coords);
            if (possiblePrototypes.Count <= 1)
            {
                // Already collapsed or empty, ensure it's removed from uncollapsedCells
                uncollapsedCells.Remove(coords);
                return;
            }

            string selectedPrototypeName = WeightedChoice(possiblePrototypes);

            if (selectedPrototypeName != null && possiblePrototypes.TryGetValue(selectedPrototypeName, out Prototype selectedPrototype))
            {
                waveFunction[coords.x, coords.y, coords.z] = new Dictionary<string, Prototype>
                {
                    { selectedPrototypeName, selectedPrototype }
                };
                uncollapsedCells.Remove(coords); // Mark as collapsed
                // Debug.Log($"Cell at {coords} collapsed to '{selectedPrototypeName}'.");
            }
            else if (selectedPrototypeName == null && possiblePrototypes.Count > 0) {
                Debug.LogError($"WeightedChoice returned null for a non-empty set at {coords}. This might indicate all weights were extremely low or negative.");
            }
            else if (possiblePrototypes.Count > 0) // selectedPrototypeName was not null but not found, or other issue
            {
                Debug.LogError($"Failed to collapse at {coords}. Prototype '{selectedPrototypeName}' from WeightedChoice not found in cell or cell was empty. Cell possibility count: {possiblePrototypes.Count}");
            }
            // If possiblePrototypes.Count == 0, it's a contradiction handled elsewhere or prior.
        }
        
        /// <summary>
        /// Selects a prototype using weighted choice with random noise.
        /// Optimized to avoid creating intermediate collections and sorting.
        /// </summary>
        private string WeightedChoice(Dictionary<string, Prototype> prototypes)
        {
            if (prototypes.Count == 0)
            {
                Debug.LogWarning("WeightedChoice called with an empty dictionary of prototypes.");
                return null; // Cannot make a choice
            }

            string bestPrototypeName = null;
            float maxAdjustedWeight = float.MinValue; // Initialize with a very small number

            foreach (var kvp in prototypes)
            {
                // Using the original noise range. Ensure weights are scaled appropriately.
                float adjustedWeight = kvp.Value.weight + UnityEngine.Random.Range(-1.0f, 1.0f); 
                if (adjustedWeight > maxAdjustedWeight)
                {
                    maxAdjustedWeight = adjustedWeight;
                    bestPrototypeName = kvp.Key;
                }
            }
            
            // Fallback if no prototype was selected (e.g., if all weights + noise were <= float.MinValue)
            // This is highly unlikely with positive base weights.
            if (bestPrototypeName == null) {
                // This ensures that if there's at least one prototype, one is chosen.
                return prototypes.Keys.First();
            }

            return bestPrototypeName;
        }

        /// <summary>
        /// A simple collapse method. For full WFC, use Iterate().
        /// </summary>
        public void collapse() // Note: C# convention is PascalCase for public methods (Collapse)
        {
            if (IsCollapsed()) {
                Debug.Log("Model is already fully collapsed.");
                return;
            }
            Vector3Int coords = GetMinEntropyCoords(); // This might return sentinel if issue
            if (coords.x == -1 && coords.y == -1 && coords.z == -1) { // Check for sentinel
                Debug.LogWarning("collapse() could not find a valid cell to collapse via GetMinEntropyCoords.");
                return;
            }
            CollapseAt(coords);
            Debug.Log($"Collapsed cell at {coords} (simple collapse, no propagation)");
        }
        
        /// <summary>
        /// Constrains a cell by removing a specific prototype.
        /// Returns true if a change was made and the cell is not empty, false otherwise.
        /// Updates uncollapsedCells status.
        /// </summary>
        public bool Constrain(Vector3Int coords, string prototypeName)
        {
            var cell = GetPossibilities(coords);

            if (cell.Count <= 1 && cell.ContainsKey(prototypeName)) {
                 // Trying to remove the last prototype, or from an already empty cell.
                 // This would lead to a contradiction if removed.
                 // We only allow removal if there are other options, or if the specific one is not the only one.
            }
            
            if (cell.Count == 0) return false; // Already a contradiction

            if (cell.Remove(prototypeName))
            {
                // Debug.Log($"Prototype '{prototypeName}' removed from cell at {coords}. New count: {cell.Count}");
                if (cell.Count == 0)
                {
                    Debug.LogError($"CONTRADICTION: Cell at {coords} has no valid prototypes after constraint!");
                    uncollapsedCells.Remove(coords); // It's "resolved" into a contradiction state
                    return false; // Indicates contradiction / no valid state to propagate from
                }
                else if (cell.Count == 1)
                {
                    // Debug.Log($"Cell at {coords} collapsed to 1 due to constraint.");
                    uncollapsedCells.Remove(coords); // Mark as collapsed
                }
                // If cell.Count > 1, it remains in uncollapsedCells (if it was there).
                return true; // Change occurred, cell still valid (or just collapsed to 1)
            }
            return false; // Prototype not found, no change
        }
        
        /// <summary>
        /// Finds coordinates of an uncollapsed cell with the minimum entropy.
        /// Optimized to iterate only over uncollapsedCells.
        /// Returns a sentinel Vector3Int(-1,-1,-1) if no suitable cell found.
        /// </summary>
        public Vector3Int GetMinEntropyCoords()
        {
            if (uncollapsedCells.Count == 0)
            {
                // This should ideally be checked by the caller (e.g., Iterate or IsCollapsed())
                // Debug.LogWarning("GetMinEntropyCoords called when no uncollapsed cells exist.");
                return new Vector3Int(-1,-1,-1); // Sentinel value
            }

            float minAdjustedEntropy = float.MaxValue;
            Vector3Int minCoords = new Vector3Int(-1,-1,-1); // Sentinel
            bool foundCandidate = false;

            foreach (var coords in uncollapsedCells)
            {
                int entropy = GetEntropy(coords); // Entropy is the count of possibilities
                if (entropy > 1) // Only consider cells that can actually be collapsed further
                {
                    float adjustedEntropy = entropy + UnityEngine.Random.Range(-0.1f, 0.1f); // Small noise to break ties
                    if (adjustedEntropy < minAdjustedEntropy)
                    {
                        minAdjustedEntropy = adjustedEntropy;
                        minCoords = coords;
                        foundCandidate = true;
                    }
                }
                // If entropy is 1, it should have been removed from uncollapsedCells.
                // If entropy is 0, it's a contradiction, also should be handled/removed.
            }
            
            if (!foundCandidate && uncollapsedCells.Count > 0) {
                // This means all cells in uncollapsedCells have entropy <= 1 (or 0).
                // They should have been removed when their entropy dropped.
                // This indicates a potential logic error or a grid full of contradictions.
                Debug.LogWarning("GetMinEntropyCoords: No cell with entropy > 1 found in uncollapsedCells, " + 
                                 "yet uncollapsedCells is not empty. Possible issue or contradiction state. Count: " + uncollapsedCells.Count +
                                 ". First cell: " + (uncollapsedCells.Any() ? uncollapsedCells.First().ToString() : "N/A"));
                // Attempt to return the first if any, though it's likely an error state.
                if (uncollapsedCells.Any()) return uncollapsedCells.First(); 
            }
            else if (!foundCandidate) {
                 // This case (no candidate and uncollapsedCells is empty) is covered by the initial check.
                 // If reached, it means minCoords is still sentinel.
            }

            return minCoords;
        }

        public int GetEntropy(Vector3Int coords)
        {
            return GetPossibilities(coords).Count;
        }
        
        public void Iterate()
        {
            if (IsCollapsed())
            {
                Debug.Log("WFC Model is fully collapsed. No further iterations.");
                return;
            }

            Vector3Int coordsToCollapse = GetMinEntropyCoords();

            if (coordsToCollapse.x == -1 && coordsToCollapse.y == -1 && coordsToCollapse.z == -1) // Check sentinel
            {
                Debug.LogWarning("Iterate: GetMinEntropyCoords returned no valid cell to collapse. Model might be in a contradictory state or an issue with uncollapsedCells tracking.");
                // Check if there are still uncollapsed cells that are all contradictions
                bool allContradictions = true;
                if (uncollapsedCells.Count > 0) {
                    foreach(var c in uncollapsedCells) {
                        if (GetEntropy(c) > 0) {
                            allContradictions = false;
                            break;
                        }
                    }
                    if (allContradictions) Debug.LogError("Iterate: All remaining uncollapsed cells have 0 entropy (contradiction).");
                }
                return;
            }
            
            // Debug.Log($"Iterate: Attempting to collapse cell at {coordsToCollapse}");
            CollapseAt(coordsToCollapse); // This will also remove coordsToCollapse from uncollapsedCells

            // Check if the collapse led to a contradiction (cell became empty)
            // CollapseAt itself doesn't make a cell empty, but subsequent propagation might.
            // If CollapseAt selected a bad prototype (e.g. WeightedChoice error), the cell might be problematic.
            if (GetEntropy(coordsToCollapse) == 0) {
                Debug.LogError($"Iterate: Cell {coordsToCollapse} became a contradiction immediately after CollapseAt. This should not happen if CollapseAt works correctly.");
                // No point propagating from a contradiction.
                return;
            }
            
            Propagate(coordsToCollapse);

            // Debug.Log($"Iterated and collapsed cell at {coordsToCollapse}. Propagation complete.");
            collapseCounter++;
        }
        
        public void Propagate(Vector3Int initialCoords, bool singleStepPropagation = false)
        {
            // Ensure the stack is clear if this Propagate call is part of a new, distinct phase.
            // However, WFC usually uses one continuous propagation wave per collapse.
            // The class member `propagationStack` is reused.
            
            propagationStack.Clear(); // Clear for this new propagation wave
            propagationStack.Push(initialCoords);

            int safetyBreak = size.x * size.y * size.z * 100; // Max number of prototype checks, rough estimate
            int count = 0;

            while (propagationStack.Count > 0)
            {
                count++;
                if (count > safetyBreak) {
                    Debug.LogError("Propagation limit reached, breaking loop. Possible infinite loop or very complex state.");
                    break;
                }

                Vector3Int currentCoords = propagationStack.Pop();

                foreach (var direction in GetValidDirections(currentCoords))
                {
                    Vector3Int neighborCoords = currentCoords + direction;
                    var neighborPrototypes = GetPossibilities(neighborCoords);

                    if (neighborPrototypes.Count == 0) continue; // Neighbor is already a contradiction

                    // What prototypes in `currentCoords` allow for `neighborCoords`
                    HashSet<string> allowedNeighboringPrototypes = GetPossibleNeighbours(currentCoords, direction);
                    
                    // Iterate over a copy of keys if modifying the collection (which Constrain does)
                    List<string> currentNeighborPrototypeNames = new List<string>(neighborPrototypes.Keys);

                    foreach (var protoNameInNeighbor in currentNeighborPrototypeNames)
                    {
                        if (!allowedNeighboringPrototypes.Contains(protoNameInNeighbor))
                        {
                            // This prototype in the neighbor is not supported by any prototype in the current cell.
                            // So, constrain the neighbor by removing this prototype.
                            if (Constrain(neighborCoords, protoNameInNeighbor)) // Constrain returns true if a change occurred and cell not empty
                            {
                                // If constraint led to a change and neighbor is not a contradiction
                                if (GetEntropy(neighborCoords) > 0) { // Check again as Constrain might make it 0
                                    // Add to stack only if its possibilities changed and it's not already scheduled
                                    // (A more sophisticated check might be if it's in a "dirty set" rather than stack.Contains)
                                    if (!propagationStack.Contains(neighborCoords)) // .Contains on Stack is O(N)
                                    {
                                        propagationStack.Push(neighborCoords);
                                    }
                                }
                            }
                            // If Constrain returns false, it means either:
                            // 1. Prototype wasn't there (no change).
                            // 2. Removal led to a contradiction (cell empty). Error logged in Constrain.
                            // In case of contradiction, we don't add to stack.
                        }
                    }
                }
                if (singleStepPropagation) break;
            }
        }
        
        public List<Vector3Int> GetValidDirections(Vector3Int coords)
        {
            // Using a list with initial capacity can be a micro-optimization if number of directions is fixed.
            List<Vector3Int> directions = new List<Vector3Int>(6); 

            if (coords.x > 0) directions.Add(Vector3Int.left);
            if (coords.x < size.x - 1) directions.Add(Vector3Int.right);
            if (coords.y > 0) directions.Add(Vector3Int.down);
            if (coords.y < size.y - 1) directions.Add(Vector3Int.up);
            if (coords.z > 0) directions.Add(Vector3Int.back);
            if (coords.z < size.z - 1) directions.Add(Vector3Int.forward);

            return directions;
        }
    }

    // Assume Prototype class structure is defined elsewhere, e.g.:
    /*
    [System.Serializable]
    public class Prototype
    {
        public string mesh_name;
        public Vector3 mesh_rotation; // Or Quaternion
        public string posX, negX, posY, negY, posZ, negZ; // Example constraints, might be more complex
        public List<string> constrain_to;
        public List<string> constrain_from;
        public float weight;
        public List<List<string>> valid_neighbours; // Index corresponds to directionToIndex
    }
    */