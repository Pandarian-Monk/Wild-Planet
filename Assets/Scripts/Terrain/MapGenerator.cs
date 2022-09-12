using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    #region Variable
    public enum DrawMode { NoiseMap, ColorMap, Mesh, FalloffMap}
    public DrawMode drawMode;

    public bool useFalloff;

    public const int mapChunkSize = 241;
    [Range(0,6)] public int levelOfDetail;
    public float noiseScale;

    public float meshHeightMulitplier;
    public AnimationCurve meshHeightCurve;

    public int octaves;
    [Range(0, 1)] public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool autoUpdate;

    public TerrainType[] regions;

    float[,] falloffMap;
    #endregion


    void Awake()
    {
        falloffMap = MapFallof.GenerateFalloffMap(mapChunkSize);
    }


    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, offset);

        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];


        for(int y=0; y < mapChunkSize; y++)
            for(int x=0; x<mapChunkSize; x++)
            {
                if (useFalloff)
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }

            }


        MapDisplay display = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
                break;
            case DrawMode.ColorMap:
                display.DrawTexture(TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.Mesh:
                display.DrawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMulitplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(colorMap, mapChunkSize, mapChunkSize));
                break;
            case DrawMode.FalloffMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(MapFallof.GenerateFalloffMap(mapChunkSize)));
                break;
        }
    }

    void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
        if (octaves < 0)
            octaves = 0;

        falloffMap = MapFallof.GenerateFalloffMap(mapChunkSize);
    }
}

[System.Serializable] 
public struct TerrainType
{
    public string name;
    [Range(0,1)]
    public float height;
    public Color color;
}