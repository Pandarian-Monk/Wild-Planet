using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Collections.Generic;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

public class MapGenerator : MonoBehaviour {


	public Noise.NormalizeMode normalizeMode;

	public bool useFlatShading;

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
	[Range(0, 1)] public float falloffRadius;
	[Range(0, 1)] public float edgeFalloffRadius;

	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;


	public TerrainType[] regions;
	static MapGenerator instance;

	bool useMultepleMaps;
	public int xMapsCount;
	public int yMapsCount;
	public List<MapDisplay> maps;

    public Renderer noiseMapTextureRender;

    float[,] falloffMap;

	Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
	Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

	void Awake()
	{
		DrawMapInEditor();
    }
	
	public static int mapChunkSize {
		get {
			if (instance == null) {
				instance = FindObjectOfType<MapGenerator> ();
			}

			if (instance.useFlatShading) {
				return 95;
			} else {
				return 239;
			}
		}
	}


    public void DrawMapInEditor()
	{
		MapData mapData = GenerateMapData(Vector2.zero);

		int counter = 0;

        float[,] _heightMap = new float[mapData.heightMap.GetLength(0) / xMapsCount, mapData.heightMap.GetLength(1) / yMapsCount];
        int xSize = _heightMap.GetLength(0);
        int ySize = _heightMap.GetLength(1);
		Color[] _colourMap = new Color[xSize * ySize];

        for (int yPos = 0; yPos < xMapsCount; yPos++)
			for (int xPos = 0; xPos < xMapsCount; xPos++)
			{

				for (int y = 0; y < ySize; y++)
				{
					for (int x = 0; x < xSize; x++)
					{
                        _heightMap[x, y] = mapData.heightMap[xSize * xPos + x - xPos * 3, ySize * yPos + y - yPos * 3];
						

                        float currentHeight = mapData.heightMap[xSize * xPos + x - xPos * 2, ySize * yPos + y - yPos * 2];
						for (int j = 0; j < regions.Length; j++)
						{
							if (currentHeight >= regions[j].height)
							{
								_colourMap[y * ySize + x] = regions[j].colour;
							}
							else
							{
								break;
							}
						}
					}
				}

				maps[counter].DrawMesh(MeshGenerator.GenerateTerrainMesh(_heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, useFlatShading),
									   TextureGenerator.TextureFromColourMap(_colourMap, xSize, ySize));
				counter++;
			}


		//display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD, useFlatShading), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));

	}

	public void DrawFalloffMap()
	{
        int xSize = (mapChunkSize + 2) * xMapsCount;
        int ySize = (mapChunkSize + 2) * yMapsCount;

        falloffMap = FalloffGenerator.GenerateFalloffMap(xSize, falloffRadius, edgeFalloffRadius);

        Texture texture = TextureGenerator.TextureFromHeightMap(falloffMap);
        noiseMapTextureRender.sharedMaterial.mainTexture = texture;
        noiseMapTextureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

	public void DrawNoiseMap()
	{
        int xSize = (mapChunkSize + 2) * xMapsCount;
        int ySize = (mapChunkSize + 2) * yMapsCount;

        falloffMap = FalloffGenerator.GenerateFalloffMap(xSize, falloffRadius, edgeFalloffRadius);
		float[,] noiseMap = Noise.GenerateNoiseMap(xSize, ySize, seed, noiseScale, octaves, persistance, lacunarity, Vector2.zero + offset, normalizeMode);

        Color[] colourMap = new Color[xSize * ySize];
        for (int y = 0; y < ySize; y++)
        {
            for (int x = 0; x < xSize; x++)
            {
                if (useFalloff)
                {
                    Mathf.Clamp01(noiseMap[x, y]);
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colourMap[y * ySize + x] = regions[i].colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        Texture texture = TextureGenerator.TextureFromHeightMap(noiseMap);

        noiseMapTextureRender.sharedMaterial.mainTexture = texture;
        noiseMapTextureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
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
		MeshData meshData = MeshGenerator.GenerateTerrainMesh (mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod,useFlatShading);
		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue (new MapThreadInfo<MeshData> (callback, meshData));
		}
	}

	

	MapData GenerateMapData(Vector2 centre) {
		int xSize = (mapChunkSize + 2) * xMapsCount;
		int ySize = (mapChunkSize + 2) * yMapsCount;
		
        falloffMap = FalloffGenerator.GenerateFalloffMap(xSize, falloffRadius, edgeFalloffRadius);
        float[,] noiseMap = Noise.GenerateNoiseMap (xSize, ySize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizeMode);

		Color[] colourMap = new Color[xSize * ySize];
		for (int y = 0; y < ySize; y++) {
			for (int x = 0; x < xSize; x++) {
				if (useFalloff) 
				{
					Mathf.Clamp01(noiseMap[x, y]);
                    noiseMap [x, y] = Mathf.Clamp01(noiseMap [x, y] - falloffMap [x, y]);
				}
				float currentHeight = noiseMap [x, y];
				for (int i = 0; i < regions.Length; i++) {
					if (currentHeight >= regions [i].height) {
						colourMap [y * ySize + x] = regions [i].colour;
					} else {
						break;
					}
				}
			}
		}


		return new MapData (noiseMap, colourMap);
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
