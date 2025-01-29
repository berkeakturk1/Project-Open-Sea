using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;
using UnityEditor;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour {

	public float spawnRadius = 1000f;
	public float minDistance = 300f;
	private bool hasInitialized = false;
	
	public GameObject oakTreePrefab; // Spawnlanacak prefab
	public int maxTreesPerChunk = 5; // Her Chunk'ta spawnlanacak maksimum ağaç sayısı

	public enum DrawMode {NoiseMap, ColourMap, Mesh, FalloffMap};
	public DrawMode drawMode;

	public Noise.NormalizeMode normalizeMode;

	public const int mapChunkSize = 239;
	[Range(0,6)]
	public int editorPreviewLOD;
	public float noiseScale;

	public int octaves;
	[Range(0,1)]
	public float persistance;
	public float lacunarity;

	public int seed;
	public Vector2 offset;

	public bool useFalloff;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;

	public bool autoUpdate;

	public TerrainType[] regions;
	
	[SerializeField] GameObject prefab;

	[Header("Raycast Settings")]
	[SerializeField] int density;
	[Space]
	[SerializeField] float minHeight;
	[SerializeField] float maxHeight;

	[SerializeField] Vector2 xRange;
	[SerializeField] Vector2 zRange;

	[Header("Prefab Variation Settings")]
	[SerializeField, Range(0, 1)] float rotateTowardsNormal;
	[SerializeField] Vector2 rotationRange;
	[SerializeField] Vector3 minScale;
	[SerializeField] Vector3 maxScale;
	
	[Header("Debug Settings")]
	[SerializeField] private bool showSpawnRays = false;
	[SerializeField] private float debugRayDuration = 2f;
	[SerializeField] private Color successRayColor = Color.green;
	[SerializeField] private Color failedRayColor = Color.red;


	float[,] falloffMap;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void Awake() {
		falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize);
	}

	public void DrawMapInEditor() {
		MapData mapData = GenerateMapData (Vector2.zero);

		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap (mapData.heightMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap (mapData.colourMap, mapChunkSize, mapChunkSize));
		} else if (drawMode == DrawMode.Mesh) {
			display.DrawMesh (MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap (mapData.colourMap, mapChunkSize, mapChunkSize));
		} else if (drawMode == DrawMode.FalloffMap) {
			display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize)));
		}
	}

	public void RequestMapData(Vector2 centre, Action<MapData> callback) {
		ThreadStart threadStart = delegate {
			MapDataThread (centre, callback);
		};

		new Thread (threadStart).Start ();
	}

	void MapDataThread(Vector2 centre, Action<MapData> callback) {
		MapData mapData = GenerateMapData (centre);
		lock (mapDataThreadInfoQueue) {
			mapDataThreadInfoQueue.Enqueue (new MapThreadInfo<MapData> (callback, mapData));
		}
	}

	public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
		ThreadStart threadStart = delegate {
			MeshDataThread (mapData, lod, callback);
		};

		new Thread (threadStart).Start ();
	}

	void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}

	void Update() {
		if (mapDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

		if (meshDataThreadInfoQueue.Count > 0) {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue ();
				threadInfo.callback (threadInfo.parameter);
			}
		}

		if (!hasInitialized && transform.childCount > 0) {
    		StartCoroutine(DelayedRandomize());
    		hasInitialized = true;
		}
	}

	MapData GenerateMapData(Vector2 centre) {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapChunkSize + 2, mapChunkSize + 2, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);

		Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
		for (int y = 0; y < mapChunkSize; y++) {
			for (int x = 0; x < mapChunkSize; x++) {
				if (useFalloff) {
					noiseMap [x, y] = Mathf.Clamp01(noiseMap [x, y] - falloffMap [x, y]);
				}
				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight >= regions [i].height) {
						colourMap [y * mapChunkSize + x] = regions [i].colour;
					} else {
						break;
					}
				}
			}
		}


		return new MapData (noiseMap, colourMap);
	}

	void OnValidate() {
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		if (octaves < 0) {
			octaves = 0;
		}

		falloffMap = FalloffGenerator.GenerateFalloffMap (mapChunkSize);
	}

	struct MapThreadInfo<T> {
		public readonly Action<T> callback;
		public readonly T parameter;

		public MapThreadInfo (Action<T> callback, T parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}

	}

	IEnumerator DelayedRandomize()
	{
		yield return new WaitForSeconds(0.5f);

		System.Random seededRandom = new System.Random(seed);

		List<Transform> terrainChunks = new List<Transform>();
		foreach (Transform child in transform)
		{
			if (child.name.Contains("Terrain Chunk"))
			{
				terrainChunks.Add(child);
			}
		}

		List<Vector2> usedPositions = new List<Vector2>();
		float chunkSize = mapChunkSize;

		foreach (Transform chunk in terrainChunks)
		{
			Vector2 randomPos;
			bool validPosition = false;
			int attempts = 0;

			float[,] heightMap = GenerateMapData(Vector2.zero).heightMap;

			SpawnObjectsInChunk(chunk, seededRandom);

			do {
			    float randomX = (float)((seededRandom.NextDouble() * 2 - 1) * spawnRadius);
			    float randomY = (float)((seededRandom.NextDouble() * 2 - 1) * spawnRadius);
			    randomPos = new Vector2(randomX, randomY);

			    validPosition = true;
			    foreach (Vector2 usedPos in usedPositions) {
			        if (Vector2.Distance(randomPos, usedPos) < (minDistance + chunkSize)) {
			            validPosition = false;
			            break;
			        }
			    }

			    attempts++;
			    if (attempts > 100) {
			        spawnRadius += chunkSize;
			        attempts = 0;
			    }
			} while (!validPosition);

			usedPositions.Add(randomPos);
			chunk.position = new Vector3(randomPos.x, 0, randomPos.y);
			
		}
    }
	

	void SpawnObjectsInChunk(Transform chunk, System.Random seededRandom)
    {
        for (int i = 0; i < density; i++)
        {
            float sampleX = (float)(seededRandom.NextDouble() * mapChunkSize - mapChunkSize / 2f) + chunk.position.x;
            float sampleZ = (float)(seededRandom.NextDouble() * mapChunkSize - mapChunkSize / 2f) + chunk.position.z;

            Vector3 rayStart = new Vector3(sampleX, maxHeight, sampleZ);
            Vector3 rayDirection = Vector3.down;

            bool hit = Physics.Raycast(rayStart, rayDirection, out RaycastHit hitInfo, Mathf.Infinity);

            if (showSpawnRays)
            {
                if (hit)
                {
                    // Draw ray from start to hit point
                    Debug.DrawLine(rayStart, hitInfo.point, 
                        hitInfo.point.y >= minHeight && hitInfo.point.y <= maxHeight ? successRayColor : failedRayColor, 
                        debugRayDuration);
                }
                else
                {
                    // Draw ray for maximum distance if no hit
                    Debug.DrawLine(rayStart, rayStart + rayDirection * 1000f, failedRayColor, debugRayDuration);
                }
            }

            if (!hit || hitInfo.point.y < minHeight || hitInfo.point.y > maxHeight)
                continue;

            GameObject instantiatedPrefab = Instantiate(prefab, hitInfo.point, Quaternion.identity, chunk);

            float rotation = (float)(seededRandom.NextDouble() * (rotationRange.y - rotationRange.x) + rotationRange.x);
            instantiatedPrefab.transform.Rotate(Vector3.up, rotation, Space.Self);

            instantiatedPrefab.transform.localScale = new Vector3(
                (float)(seededRandom.NextDouble() * (maxScale.x - minScale.x) + minScale.x),
                (float)(seededRandom.NextDouble() * (maxScale.y - minScale.y) + minScale.y),
                (float)(seededRandom.NextDouble() * (maxScale.z - minScale.z) + minScale.z)
            );

            if (rotateTowardsNormal > 0)
            {
                instantiatedPrefab.transform.rotation = Quaternion.Lerp(
                    instantiatedPrefab.transform.rotation,
                    Quaternion.FromToRotation(Vector3.up, hitInfo.normal),
                    rotateTowardsNormal
                );
            }
        }
    }





	

}

[System.Serializable]
public struct TerrainType {
	public string name;
	public float height;
	public Color colour;
}

public struct MapData {
	public readonly float[,] heightMap;
	public readonly Color[] colourMap;

	public MapData (float[,] heightMap, Color[] colourMap)
	{
		this.heightMap = heightMap;
		this.colourMap = colourMap;
	}
}
