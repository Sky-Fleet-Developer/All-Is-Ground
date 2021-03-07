using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassErosion : MonoBehaviourPlus
{

    public bool printTimers;

    [Header ("Mesh Settings")]
    public int mapSize = 255;

    [Header ("Erosion Settings")]
    public ComputeShader erosion;
    public int numErosionIterations = 30000;
    public int erosionBrushRadius = 10;
    public AnimationCurve erosionBrushRemap;

    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 6;
    public float minSedimentCapacity = .005f;
    public float depositSpeed = 0.08f;
    public float erodeSpeed = 1.5f;

    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    public float smooth = 0.2f;
    [Range (0, 1)]
    public float inertia = 0.3f;


    public float TerrainHeight = 600;

    // Internal
    [HideInInspector]
    public TerrainData terrain;
    public List<float[,]> Undo = new List<float[,]>();
    float[] map;
    Mesh mesh;
    int mapSizeWithBorder;
    float[] erosionWeights;

    public void GenerateHeightMap (TerrainData target) {
        mapSize = target.heightmapResolution - erosionBrushRadius * 2;
        mapSizeWithBorder = target.heightmapResolution;
        map = TowDToOneD(target.GetHeights(0, 0, target.heightmapResolution, target.heightmapResolution), mapSizeWithBorder);
       /* if (Undo.Count > 10)
            Undo.RemoveAt(0);*/
       // map = FindObjectOfType<HeightMapGenerator> ().GenerateHeightMap (mapSizeWithBorder);
    }

    public void Erode(TerrainData target)
    {
        terrain = target;
        int numThreads = numErosionIterations / 1024;

        // Create brush
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++)
        {
            for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erosionBrushRadius * erosionBrushRadius)
                {
                    brushIndexOffsets.Add(brushY * mapSize + brushX);
                    float brushWeight = erosionBrushRemap.Evaluate(1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius);
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        // Generate random indices for droplet placement
        int[] randomIndices = new int[numErosionIterations];
        for (int i = 0; i < numErosionIterations; i++)
        {
            int randomX = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
            int randomY = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
            randomIndices[i] = randomY * mapSize + randomX;
        }

        // Send random indices to compute shader
        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

        // Heightmap buffer
        ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
        mapBuffer.SetData(map);
        erosion.SetBuffer(0, "map", mapBuffer);

        // Rocks
        erosionWeights = new float[map.Length];
        ComputeBuffer erosionWeightsBuffer = new ComputeBuffer(map.Length, sizeof(float));
        erosionWeightsBuffer.SetData(erosionWeights);
        erosion.SetBuffer(0, "erosionWeights", erosionWeightsBuffer);


        // Settings
        erosion.SetInt("borderSize", erosionBrushRadius);
        erosion.SetInt("mapSize", mapSizeWithBorder);
        erosion.SetInt("brushLength", brushIndexOffsets.Count);
        erosion.SetInt("maxLifetime", maxLifetime);
        erosion.SetFloat("inertia", inertia);
        erosion.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
        erosion.SetFloat("minSedimentCapacity", minSedimentCapacity);
        erosion.SetFloat("depositSpeed", depositSpeed);
        erosion.SetFloat("erodeSpeed", erodeSpeed);
        erosion.SetFloat("evaporateSpeed", evaporateSpeed);
        erosion.SetFloat("gravity", gravity);
        erosion.SetFloat("startSpeed", startSpeed);
        erosion.SetFloat("startWater", startWater);
        erosion.SetFloat("smooth", smooth);

        // Run compute shader
        erosion.Dispatch(0, numThreads, 1, 1);
        mapBuffer.GetData(map);
        erosionWeightsBuffer.GetData(erosionWeights);
        // Release buffers
        mapBuffer.Release();
        randomIndexBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
        erosionWeightsBuffer.Release();

        terrain.SetHeights(0, 0, OneDToTowD(map));
    }

    public IEnumerator StartErode(int iterations, TerrainData target)
    {
        terrain = target;
        Undo.Add(terrain.GetHeights(0, 0, terrain.heightmapResolution, terrain.heightmapResolution));
        while ((iterations--) > 0)
        {
            yield return new WaitUntil(delegate { GenerateHeightMap(target); Erode(target); return true; });
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    public void UnDo()
    {
        if (Undo.Count > 0)
        {
            terrain.SetHeights(0, 0, Undo[Undo.Count - 1]);
            Undo.RemoveAt(Undo.Count - 1);
        }
        else
            Debug.Log("<color=yellow>Nothing to undo</color>");
    }
}