using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/*
 *  Terrain Tools
 *  Basic auto texturing, detail spawning based on paramaters
 *  Author: Matt Antoszkiw (RFL)
 * 
 */

[CustomEditor(typeof(TerrainTools))]
public class TerrainToolsEditor : Editor
{
    int randomSeed = 42;
    
    void GenerateTrees(TerrainTools terrainTools)
    {
        Terrain terrain = terrainTools.Terrain;
        
        if (terrain == null)
        {
            Debug.LogError("Terrain is not assigned!");
            return;
        }

        // Set the random seed for consistent results
        UnityEngine.Random.InitState(randomSeed);

        TerrainData terrainData = terrain.terrainData;
        terrainData.treeInstances = new TreeInstance[0]; // Clear existing trees

        float[,,] splatMapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        for (int y = 0; y < terrainData.heightmapResolution; y++)
        {
            for (int x = 0; x < terrainData.heightmapResolution; x++)
            {
                float normalizedX = (float)x / (float)terrainData.heightmapResolution;
                float normalizedY = (float)y / (float)terrainData.heightmapResolution;

                float height = terrainData.GetHeight(x, y) / terrainData.size.y;
                float slope = terrainData.GetSteepness(normalizedX, normalizedY) / 90.0f;

                // Convert heightmap resolution to splatmap resolution coordinates
                int splatX = Mathf.FloorToInt(normalizedX * terrainData.alphamapWidth);
                int splatY = Mathf.FloorToInt(normalizedY * terrainData.alphamapHeight);

                for (int t = 0; t < terrainTools.TerrainTreePlacementData.Count; t++)
                {
                    if (height >= terrainTools.TerrainTreePlacementData[t].minHeight && height <= terrainTools.TerrainTreePlacementData[t].maxHeight &&
                        slope >= terrainTools.TerrainTreePlacementData[t].minSlope && slope <= terrainTools.TerrainTreePlacementData[t].maxSlope &&
                        IsAllowedTexture(splatX, splatY, terrainTools.TerrainTreePlacementData[t].spawnOnTextures, splatMapData))
                    {
                        float chance = terrainTools.TerrainTreePlacementData[t].density / terrainData.heightmapResolution;
                        if (UnityEngine.Random.value < chance) // Randomly place trees based on density
                        {
                            float treeSize = Random.Range(terrainTools.TerrainTreePlacementData[t].minSize,
                                terrainTools.TerrainTreePlacementData[t].maxSize);
                            PlaceTree(terrainTools.TerrainTreePlacementData[t].treePrefab, normalizedX, normalizedY, treeSize, terrain);
                        }
                    }
                }
            }
        }

        terrain.Flush(); // Refresh terrain to show placed trees
    }

    void GenerateDetailsPerLayer(TerrainTools terrainTools, int layer)
    {
          Terrain terrain = terrainTools.Terrain;
        if (terrain == null)
        {
            Debug.LogError("Terrain is not assigned!");
            return;
        }

        Debug.Log("Gen details layer " + layer);
        
        TerrainData terrainData = terrain.terrainData;
        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        float[,,] splatMapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        int[,] detailLayer = terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, layer);
        
        for (int y = 0; y < detailHeight; y++)
        {
            for (int x = 0; x < detailWidth; x++)
            {
                float normalizedX = (float)x / (float)detailWidth;
                float normalizedY = (float)y / (float)detailHeight;

                float height = terrainData.GetHeight(
                    Mathf.FloorToInt(normalizedX * terrainData.heightmapResolution),
                    Mathf.FloorToInt(normalizedY * terrainData.heightmapResolution)) / terrainData.size.y;

                float slope = terrainData.GetSteepness(normalizedX, normalizedY) / 90.0f;

                // Convert detail resolution to splatmap resolution coordinates
                int splatX = Mathf.FloorToInt(normalizedX * terrainData.alphamapWidth);
                int splatY = Mathf.FloorToInt(normalizedY * terrainData.alphamapHeight);


                if (height >= terrainTools.TerrainDetailPlacementData[layer].minHeight && height <= terrainData.detailPrototypes[layer].maxHeight &&
                        slope >= terrainTools.TerrainDetailPlacementData[layer].minSlope && slope <= terrainTools.TerrainDetailPlacementData[layer].maxSlope &&
                        IsAllowedTexture(splatX, splatY, terrainTools.TerrainDetailPlacementData[layer].spawnOnTextures, splatMapData))
                    {
                        float chance = terrainTools.TerrainDetailPlacementData[layer].density / detailWidth;
                        if (Random.value < chance) // Randomly place details based on density
                        {
                            //Debug.Log("Spawn detail.. " + d);
                            detailLayer[x, y] = (int)terrainTools.TerrainDetailPlacementData[layer].density; // Set detail density at this position
                        }
                    }
            }
        }
        
        terrainData.SetDetailLayer(0, 0, layer, detailLayer);

        terrain.Flush(); // Refresh terrain to show placed details
    }
    
    void GenerateDetails(TerrainTools terrainTools)
    {
        Terrain terrain = terrainTools.Terrain;
        if (terrain == null)
        {
            Debug.LogError("Terrain is not assigned!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        int detailWidth = terrainData.detailWidth;
        int detailHeight = terrainData.detailHeight;

        // Clear existing details
        for (int layer = 0; layer < terrainData.detailPrototypes.Length; layer++)
        {
            terrainData.SetDetailLayer(0, 0, layer, new int[detailWidth, detailHeight]);
        }

        float[,,] splatMapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        for (int y = 0; y < detailHeight; y++)
        {
            for (int x = 0; x < detailWidth; x++)
            {
                float normalizedX = (float)x / (float)detailWidth;
                float normalizedY = (float)y / (float)detailHeight;

                float height = terrainData.GetHeight(
                    Mathf.FloorToInt(normalizedX * terrainData.heightmapResolution),
                    Mathf.FloorToInt(normalizedY * terrainData.heightmapResolution)) / terrainData.size.y;

                float slope = terrainData.GetSteepness(normalizedX, normalizedY) / 90.0f;

                // Convert detail resolution to splatmap resolution coordinates
                int splatX = Mathf.FloorToInt(normalizedX * terrainData.alphamapWidth);
                int splatY = Mathf.FloorToInt(normalizedY * terrainData.alphamapHeight);

                for (int d = 0; d < terrainTools.TerrainDetailPlacementData.Count; d++)
                {
                    if (height >= terrainTools.TerrainDetailPlacementData[d].minHeight && height <= terrainData.detailPrototypes[d].maxHeight &&
                        slope >= terrainTools.TerrainDetailPlacementData[d].minSlope && slope <= terrainTools.TerrainDetailPlacementData[d].maxSlope &&
                        IsAllowedTexture(splatX, splatY, terrainTools.TerrainDetailPlacementData[d].spawnOnTextures, splatMapData))
                    {
                        float chance = terrainTools.TerrainDetailPlacementData[d].density / detailWidth;
                        if (Random.value < chance) // Randomly place details based on density
                        {
                            //Debug.Log("Spawn detail.. " + d);
                            int[,] detailLayer = terrainData.GetDetailLayer(0, 0, detailWidth, detailHeight, d);
                            detailLayer[x, y] = (int)terrainTools.TerrainDetailPlacementData[d].density; // Set detail density at this position
                            terrainData.SetDetailLayer(0, 0, d, detailLayer);
                        }
                    }
                }
            }
        }

        terrain.Flush(); // Refresh terrain to show placed details
    }
    
    void PlaceTree(GameObject treePrefab, float normalizedX, float normalizedY, float height, Terrain terrain)
    {
        TreeInstance tree = new TreeInstance();
        tree.position = new Vector3(normalizedX, height, normalizedY);
        tree.prototypeIndex = GetTreePrototypeIndex(treePrefab, terrain);
        tree.widthScale = height;
        tree.heightScale = height;
        tree.color = Color.white;
        tree.lightmapColor = Color.white;

        terrain.AddTreeInstance(tree);
    }
    
    
    bool IsAllowedTexture(int splatX, int splatY, int[] allowedTextures, float[,,] splatMapData)
    {
        for (int i = 0; i < allowedTextures.Length; i++)
        {
            int textureIndex = allowedTextures[i];
            if (textureIndex >= 0 && textureIndex < splatMapData.GetLength(2))
            {
                if (splatMapData[splatX, splatY, textureIndex] > 0.8f) // Check if the texture weight is dominant
                {
                    return true;
                }
            }
        }
        return false;
    }

    int GetTreePrototypeIndex(GameObject treePrefab, Terrain terrain)
    {
        TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
        for (int i = 0; i < prototypes.Length; i++)
        {
            if (prototypes[i].prefab == treePrefab)
            {
                return i;
            }
        }
        Debug.LogError("Tree prefab not found in terrain prototypes!");
        return -1;
    }
    
    void GetTerrainTextureLayers(TerrainTools terrainTools)
    {
        Debug.Log("GO " + terrainTools.gameObject.name);
        TerrainData terrainData = terrainTools.Terrain.terrainData;

        List<TerrainTools.TerrainLayerData> terrainLayersData = new List<TerrainTools.TerrainLayerData>();
        for (int i = 0; i < terrainData.terrainLayers.Length; i++)
        {
            TerrainTools.TerrainLayerData terrainLayerData = new TerrainTools.TerrainLayerData();
            terrainLayerData.index = i;
            terrainLayerData.name = terrainData.terrainLayers[i].name;
            terrainLayerData.tex = terrainData.terrainLayers[i].diffuseTexture;
            terrainLayersData.Add(terrainLayerData);
            TerrainTools.TerrainLayerData match =
                terrainTools.TerrainTexturingData.Find(a => a.name == terrainLayerData.name);
            if (match != null)
            {
                terrainLayerData.minHeight = match.minHeight;
                terrainLayerData.maxHeight = match.maxHeight;
                terrainLayerData.minSlope = match.minSlope;
                terrainLayerData.maxSlope = match.maxSlope;
            }
        }

        terrainTools.TerrainTexturingData = terrainLayersData;
    }

    void GetTreePrototypesData(TerrainTools terrainTools)
    {
        TerrainData terrainData = terrainTools.Terrain.terrainData;

        List<TerrainTools.TerrainTreeData> terrainTreeData = new List<TerrainTools.TerrainTreeData>();
        for (int i = 0; i < terrainData.treePrototypes.Length; i++)
        {
            TerrainTools.TerrainTreeData treeData = new TerrainTools.TerrainTreeData();
            treeData.name = terrainData.treePrototypes[i].prefab.name;
            treeData.treePrefab = terrainData.treePrototypes[i].prefab;
            TerrainTools.TerrainTreeData match =
                terrainTools.TerrainTreePlacementData.Find(a => a.treePrefab == treeData.treePrefab);
            if (match != null)
            {
                treeData.minHeight = match.minHeight;
                treeData.maxHeight = match.maxHeight;
                treeData.minSlope = match.minSlope;
                treeData.maxSlope = match.maxSlope;
                treeData.density = match.density;
                treeData.spawnOnTextures = match.spawnOnTextures;
                treeData.minSize = match.minSize;
                treeData.maxSize = match.maxSize;
            }
            terrainTreeData.Add(treeData);
        }

        terrainTools.TerrainTreePlacementData = terrainTreeData;
    }

    void GetDetailsPrototypeData(TerrainTools terrainTools)
    {
        TerrainData terrainData = terrainTools.Terrain.terrainData;
        List<TerrainTools.TerrainDetailData> terrainDetailsData = new List<TerrainTools.TerrainDetailData>();
        for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
        {
            TerrainTools.TerrainDetailData detailData = new TerrainTools.TerrainDetailData();
            detailData.name = terrainData.detailPrototypes[i].prototype.name;
            detailData.detailPrefab = terrainData.detailPrototypes[i].prototype;
            TerrainTools.TerrainDetailData match =
                terrainTools.TerrainDetailPlacementData.Find(a => a.detailPrefab == detailData.detailPrefab);
            if (match != null)
            {
                detailData.minHeight = match.minHeight;
                detailData.maxHeight = match.maxHeight;
                detailData.minSlope = match.minSlope;
                detailData.maxSlope = match.maxSlope;
                detailData.density = match.density;
                detailData.spawnOnTextures = match.spawnOnTextures;
                detailData.minSize = match.minSize;
                detailData.maxSize = match.maxSize;
            }
            terrainDetailsData.Add(detailData);
        }

        terrainTools.TerrainDetailPlacementData = terrainDetailsData;
    }
    
    public void GenerateSplatMap(TerrainTools terrainTools)
    {
        Terrain terrain = terrainTools.Terrain;
        
        if (terrain == null)
        {
            Debug.LogError("Terrain is not assigned!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        int splatMapWidth = terrainData.alphamapWidth;
        int splatMapHeight = terrainData.alphamapHeight;
        int numTextures = terrainData.alphamapLayers;

        float[,,] splatmapData = new float[splatMapWidth, splatMapHeight, numTextures];

        for (int y = 0; y < splatMapHeight; y++)
        {
            for (int x = 0; x < splatMapWidth; x++)
            {
                // Calculate normalized terrain coordinates
                float normalizedX = (float)x / (float)splatMapWidth;
                float normalizedY = (float)y / (float)splatMapHeight;

                // Get height and slope at this point
                float height = terrainData.GetHeight(
                    Mathf.FloorToInt(normalizedX * terrainData.heightmapResolution),
                    Mathf.FloorToInt(normalizedY * terrainData.heightmapResolution)
                );
                float normalizedHeight = height / terrainData.size.y;
                float slope = terrainData.GetSteepness(normalizedX, normalizedY) / 90.0f;

                // Calculate the weights based on the rules
                float[] weights = CalculateWeights(normalizedHeight, slope, terrainTools.TerrainTexturingData);

                // Normalize the weights
                NormalizeWeights(weights);

                // Assign the weights to the splat map
                for (int i = 0; i < numTextures; i++)
                {
                    splatmapData[x, y, i] = weights[i];
                }
            }
        }

        // Assign the splat map to the terrain
        terrainData.SetAlphamaps(0, 0, splatmapData);
    }

    float[] CalculateWeights(float height, float slope, List<TerrainTools.TerrainLayerData> terrainLayerData)
    {
        float[] weights = new float[terrainLayerData.Count];

        for (int i = 0; i < terrainLayerData.Count; i++)
        {
            TerrainTools.TerrainLayerData rule = terrainLayerData[i];

            // Check if height and slope fall within the specified ranges
            if (height >= rule.minHeight && height <= rule.maxHeight &&
                slope >= rule.minSlope && slope <= rule.maxSlope)
            {
                // Calculate weight based on how close the height and slope are to the mid-point of the range
                float heightWeight = 1f - Mathf.Abs((height - ((rule.minHeight + rule.maxHeight) / 2f)) / ((rule.maxHeight - rule.minHeight) / 2f));
                float slopeWeight = 1f - Mathf.Abs((slope - ((rule.minSlope + rule.maxSlope) / 2f)) / ((rule.maxSlope - rule.minSlope) / 2f));

                // Combine height and slope weights with base weight
                weights[i] = rule.weight * heightWeight * slopeWeight;
            }
            else
            {
                // If out of range, set weight to zero
                weights[i] = 0f;
            }
        }

        return weights;
    }

    void NormalizeWeights(float[] weights)
    {
        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            total += weights[i];
        }

        if (total > 0)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] /= total;
            }
        }
    }

    void ClearTrees(TerrainTools terrainTools)
    {
        terrainTools.Terrain.terrainData.treeInstances = new TreeInstance[0];
        terrainTools.Terrain.Flush();
    }
    
    public void ClearAllDetails(Terrain terrain)
    {
        if (terrain == null)
        {
            Debug.LogError("Terrain is not assigned!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        // Loop through each detail layer and clear it
        for (int layer = 0; layer < terrainData.detailPrototypes.Length; layer++)
        {
            // Create an empty array of the appropriate size
            int[,] emptyDetails = new int[terrainData.detailWidth, terrainData.detailHeight];

            // Set the detail layer to the empty array
            terrainData.SetDetailLayer(0, 0, layer, emptyDetails);
        }

        terrain.Flush(); // Refresh the terrain to show changes
        Debug.Log("All details have been cleared from the terrain.");
    }
    
    
    void DebugDetailMapCutoff(Terrain t, float threshold, int layer)
    {
        // Get all of layer zero.
        var map = t.terrainData.GetDetailLayer(0, 0, t.terrainData.detailWidth, t.terrainData.detailHeight, layer);

        // For each pixel in the detail map...
        for (int y = 0; y < t.terrainData.detailHeight; y++)
        {
            for (int x = 0; x < t.terrainData.detailWidth; x++)
            {
                // If the pixel value is below the threshold then
                // set it to zero.
                //if (map[x, y] < threshold)
                //{
                    map[x, y] = 256;
                //}
            }
        }

        // Assign the modified map back.
        t.terrainData.SetDetailLayer(0, 0, layer, map);
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        TerrainTools terrainTools = (TerrainTools)target;
        
        GUILayout.Space(10f);
        
        GUILayout.Label("Texturing", EditorStyles.boldLabel);

        if (GUILayout.Button("Update Terrain Layer Data"))
        {
            GetTerrainTextureLayers(terrainTools);
        }
        
        if (GUILayout.Button("Generate Splat Maps"))
        {
            GenerateSplatMap(terrainTools);
        }
        
        GUILayout.Space(10f);

        GUILayout.Label("Trees", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Update Tree Data"))
        {
            GetTreePrototypesData(terrainTools);
        }

        if (GUILayout.Button("Generate Trees"))
        {
            GenerateTrees(terrainTools);
        }
        
        if (GUILayout.Button("Clear Trees"))
        {
            ClearTrees(terrainTools);
        }
        
        GUILayout.Space(10f);
        
        GUILayout.Label("Details", EditorStyles.boldLabel);

        if (GUILayout.Button("Update Details Data"))
        {
            GetDetailsPrototypeData(terrainTools);
        }

        if (GUILayout.Button("Generate Details"))
        {
            //GenerateDetails(terrainTools);
            for (int i = 0; i < terrainTools.TerrainDetailPlacementData.Count; i++)
            {
                GenerateDetailsPerLayer(terrainTools, i);
            }
           // GenerateDetailsPerLayer(terrainTools, 0);
        }

        if (GUILayout.Button("Clear Details"))
        {
            ClearAllDetails(terrainTools.Terrain);
        }

        /*if (GUILayout.Button("Debug Details"))
        {
            DebugDetailMapCutoff(terrainTools.Terrain, 1f, 0);
        }
        
        if (GUILayout.Button("Debug Details 1"))
        {
            DebugDetailMapCutoff(terrainTools.Terrain, 1f, 1);
        }*/
    }
}
