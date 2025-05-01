using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using perlin_Scripts.Data;

public class EndlessTerrain : MonoBehaviour {

    // Ayarlanabilir parametreler
    public int desiredIslandCount = 20;
    public float minIslandDistance = 50f;
    /// cacheDistanceMultiplier: Bu çarpan ile maxViewDst'den hesaplanır. 
    /// Örneğin, 2f değeri; adalar maxViewDst * 2 mesafeye kadar önbellekte tutulur.
    public float cacheDistanceMultiplier = 2f;  
    
    // Grass management settings
    
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;
    const float colliderGenerationDistanceThreshold = 5f;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;
    public static float maxViewDst;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    
    [Header("Ocean Settings")]
    public bool enableOcean = true;
    private OceanFloorGenerator oceanFloorGenerator;
    
    [Header("Spawn Restriction")]
    public GameObject restrictedZoneObject;
    public float minDistanceFromRestrictedObject = 20f;
// Güvenli mesafe (çarpışma değilse bile yakınlık)


    
    // Yeni: Adaların hafızada tutulacağı mesafe (cache distance)
    float cacheDistance;

    // Key: ada konumu (dünya koordinatında), Value: TerrainChunk
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start() {
        mapGenerator = FindObjectOfType<MapGenerator>();

        // First calculate the maxViewDst
        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = mapGenerator.mapChunkSize - 1;
        // Önbellek mesafesini belirliyoruz:
        cacheDistance = maxViewDst * cacheDistanceMultiplier;
    
        // Now initialize the ocean after maxViewDst is available
        void Start() {
            mapGenerator = FindObjectOfType<MapGenerator>();

            // First calculate the maxViewDst
            maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
            chunkSize = mapGenerator.mapChunkSize - 1;
            // Önbellek mesafesini belirliyoruz:
            cacheDistance = maxViewDst * cacheDistanceMultiplier;
    
            // Now initialize the ocean after maxViewDst is available
            
    
            UpdateVisibleChunks();
        }
    
        UpdateVisibleChunks();
    }
    
    public Dictionary<Vector2, TerrainChunk> GetTerrainChunks() {
        return terrainChunkDictionary;
    }

    void Update() {
        // Oyuncu konumunu ölçeklenmiş dünya koordinatlarında alıyoruz.
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / mapGenerator.terrainData.uniformScale;

        // Görünür adaların çarpışma mesh'lerini güncelle
        if (viewerPosition != viewerPositionOld) {
            foreach (TerrainChunk chunk in visibleTerrainChunks) {
                chunk.UpdateCollisionMesh();
            }
        }
        
        if (mapGenerator.useDynamicTreePlacement) {
            foreach (TerrainChunk chunk in visibleTerrainChunks) {
                chunk.UpdateTreePlacementMethod();
            }
        }
        
        // Oyuncu belirli bir mesafe hareket ettiyse, görünür/chunk güncellemesini tetikliyoruz.
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
        
        // Update grass visibility based on viewer position
        
    }
    
    public float GetDistanceToClosestIsland(Vector2 position) {
        if (terrainChunkDictionary.Count == 0) return float.MaxValue;
    
        float minDistance = float.MaxValue;
        float scale = mapGenerator.terrainData.uniformScale;
    
        foreach (var chunk in terrainChunkDictionary) {
            Vector2 chunkPos = chunk.Key * scale;
            float distanceToIsland = Vector2.Distance(position, chunkPos);
        
            // Factor in the size of the island
            float islandRadius = mapGenerator.mapChunkSize * scale / 2f;
            distanceToIsland -= islandRadius;
        
            // Clamp to avoid negative values
            distanceToIsland = Mathf.Max(0, distanceToIsland);
        
            if (distanceToIsland < minDistance) {
                minDistance = distanceToIsland;
            }
        }
    
        return minDistance;
    }
    
    void UpdateVisibleChunks() {
        List<Vector2> keysToRemove = new List<Vector2>();

        // Mevcut adaların kontrolü:
        foreach (var kvp in terrainChunkDictionary) {
            TerrainChunk chunk = kvp.Value;
            float viewerDstFromNearestEdge = Mathf.Sqrt(chunk.bounds.SqrDistance(viewerPosition));
            
            // Eğer ada oyuncuya yakınsa (maxViewDst içerisindeyse) güncelle, böylece görünür hale gelir.
            if (viewerDstFromNearestEdge <= maxViewDst) {
                chunk.UpdateTerrainChunk();
                // Eğer chunk görünür hale geldiyse, visibleTerrainChunks listesine ekleyelim.
                if (!visibleTerrainChunks.Contains(chunk))
                    visibleTerrainChunks.Add(chunk);
            }
            // Eğer ada cacheDistance içerisinde fakat görünür mesafenin dışında ise, sadece görünürlüğünü kapat.
            else if (viewerDstFromNearestEdge <= cacheDistance) {
                chunk.SetVisible(false);
                visibleTerrainChunks.Remove(chunk);
            }
            // Eğer ada cacheDistance'in dışındaysa, artık hafızada tutmaya gerek yok; sil.
            else {
                keysToRemove.Add(kvp.Key);
            }
        }

        // Sözlükten (dictionary) çok uzaktaki adaları temizleyelim.
        foreach (var key in keysToRemove) {
            // Gerekirse GameObject'i sahneden de yok edelim.
            terrainChunkDictionary[key].Destroy();
            terrainChunkDictionary.Remove(key);
            visibleTerrainChunks.RemoveAll(t => t.coord == key);
        }

        // Eğer toplam ada sayısı istenenden azsa, yeni ada (chunk) oluştur.
        int attempts = 0;
        while (terrainChunkDictionary.Count < desiredIslandCount && attempts < 100) {
            attempts++; // Uygun konum bulunamazsa sonsuz döngüye girmemek için.
            float randomDistance = Random.Range(minIslandDistance, 100);
            float randomAngle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 randomOffset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomDistance;
            Vector2 islandPosition = viewerPosition + randomOffset;
            
            if (restrictedZoneObject != null)
            {
                // Referans objenin sınırlarını al
                Bounds restrictedBounds = new Bounds();
    
                // Öncelik sırası: MeshRenderer > Collider
                if (restrictedZoneObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
                {
                    restrictedBounds = meshRenderer.bounds;
                }
                else if (restrictedZoneObject.TryGetComponent<Collider>(out var collider))
                {
                    restrictedBounds = collider.bounds;
                }
                else
                {
                    Debug.LogWarning("Restricted object has no MeshRenderer or Collider.");
                    goto SkipRestriction;
                }

                restrictedBounds.Expand(minDistanceFromRestrictedObject * 2f);

                Vector2 scaledIslandPos = islandPosition * mapGenerator.terrainData.uniformScale;
                float chunkSize = mapGenerator.mapChunkSize * mapGenerator.terrainData.uniformScale;

                Bounds islandBounds = new Bounds(new Vector3(scaledIslandPos.x, 0, scaledIslandPos.y), new Vector3(chunkSize, 100f, chunkSize));

                if (restrictedBounds.Intersects(islandBounds))
                {
                    continue; // yasak alanda, spawnlama
                }

                SkipRestriction: ;
            }


            
            // Aynı veya çok yakın bir ada zaten var mı kontrol edelim.
            bool tooClose = false;
            foreach (var key in terrainChunkDictionary.Keys) {
                if (Vector2.Distance(key, islandPosition) < minIslandDistance) {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose)
                continue;
            
            

            
            // Yeni ada oluşturuluyor.
            TerrainChunk newChunk = new TerrainChunk(islandPosition, chunkSize, detailLevels, colliderLODIndex, transform, mapMaterial);
            terrainChunkDictionary.Add(islandPosition, newChunk);
            // Eğer yeni ada görünür mesafe içerisindeyse, onu visible listesine ekle.
            if (Vector2.Distance(viewerPosition, islandPosition) <= maxViewDst)
                visibleTerrainChunks.Add(newChunk);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (restrictedZoneObject != null)
        {
            Gizmos.color = Color.red;

            Bounds bounds = new Bounds();
            if (restrictedZoneObject.TryGetComponent<MeshRenderer>(out var meshRenderer))
                bounds = meshRenderer.bounds;
            else if (restrictedZoneObject.TryGetComponent<Collider>(out var collider))
                bounds = collider.bounds;

            bounds.Expand(minDistanceFromRestrictedObject * 2f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }


    


    public class TerrainChunk {

        public Vector2 coord; // Ada konumu (dünya koordinatı)
        
        private bool treesSpawned;
        private bool grassSpawned;

        GameObject meshObject;
        public Vector2 position;
        public Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int colliderLODIndex;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;
        bool hasSetCollider;
        
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Material material) {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLODIndex = colliderLODIndex;

            // Artık coord doğrudan dünya konumunu belirtiyor.
            position = coord;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = new Vector3(1, 1, 1) * mapGenerator.terrainData.uniformScale; // Apply negative X scale
            meshObject.transform.rotation = Quaternion.Euler(0, 180, 0); // Rotate 180 degrees around Y
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++) {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                if (i == colliderLODIndex) {
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateTerrainChunk();
        }
        
        public void UpdateTreePlacementMethod() {
            float distanceFromViewer = Vector2.Distance(position, viewerPosition);
    
            // If using dynamic tree placement, check distance to determine method
            if (mapGenerator.useDynamicTreePlacement) {
                bool shouldUseRaycast = distanceFromViewer <= mapGenerator.raycastThreshold;
        
                // If we already have trees spawned with the wrong method, we need to respawn them
                if (treesSpawned && mapGenerator.ShouldRespawnTrees(position, shouldUseRaycast)) {
                    // Clear existing trees
                    mapGenerator.ClearTrees(position);
                    treesSpawned = false;
            
                    // Request tree respawn using new method
                    if (lodMeshes[previousLODIndex].hasMesh) {
                        mapGenerator.RequestTreeData(mapData, position, previousLODIndex, shouldUseRaycast);
                        treesSpawned = true;
                    }
                }
            }
        }
        
        public void UpdateTerrainChunk() {
            if (mapDataReceived) {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                bool wasVisible = IsVisible();
                bool visible = viewerDstFromNearestEdge <= maxViewDst;

                if (visible) {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length - 1; i++) {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold) {
                            lodIndex = i + 1;
                        } else {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh) {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                            
                            // Spawn trees if needed, with appropriate method based on distance
                            if (mapGenerator.spawnTrees && !treesSpawned) {
                                float distanceFromViewer = Vector2.Distance(position, viewerPosition);
                                bool useRaycast = distanceFromViewer <= mapGenerator.raycastThreshold;
                                mapGenerator.RequestTreeData(mapData, position, lodIndex, useRaycast);
                                treesSpawned = true;
                            }
                            else if (mapGenerator.spawnVegetation && mapGenerator.vegetationManager != null)
                            {
                                float distanceFromViewer = Vector2.Distance(position, viewerPosition);
                                if (distanceFromViewer <= maxViewDst)
                                {
                                    mapGenerator.RequestVegetationData(mapData, position, lodIndex);
                                }
                            }
                            
                        } else if (!lodMesh.hasRequestedMesh) {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                }

                if (wasVisible != visible) {
                    SetVisible(visible);
                }
            }
        }

        public void UpdateCollisionMesh() {
            if (!hasSetCollider) {
                float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);

                if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDstThreshold) {
                    if (!lodMeshes[colliderLODIndex].hasRequestedMesh) {
                        lodMeshes[colliderLODIndex].RequestMesh(mapData);
                    }
                }

                if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold) {
                    if (lodMeshes[colliderLODIndex].hasMesh) {
                        meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                        hasSetCollider = true;
                    }
                }
            }
        }

        public void SetVisible(bool visible) {
            meshObject.SetActive(visible);
            
            if (!visible && treesSpawned) {
                mapGenerator.ClearTrees(position);
                treesSpawned = false;
            }
            
            
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }

        // Artık chunk'u bellekten temizlemek için kullanılacak metod.
        public void Destroy() {
            // Clean up grass when chunk is destroyed
            
            
            GameObject.Destroy(meshObject);
        }
    }

    class LODMesh {

        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        public event System.Action updateCallback;

        public LODMesh(int lod) {
            this.lod = lod;
        }

        void OnMeshDataReceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData) {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo {
        [Range(0, MeshGenerator.numSupportedLODs - 1)]
        public int lod;
        public float visibleDstThreshold;

        public float sqrVisibleDstThreshold {
            get {
                return visibleDstThreshold * visibleDstThreshold;
            }
        }
    }
}