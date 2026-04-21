using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainTools : MonoBehaviour
{

    public Terrain Terrain;

    [System.Serializable]
    public class TerrainLayerData
    {
        public int index;
        public string name;
        public Texture2D tex;
        public float minHeight;
        public float maxHeight;
        public float minSlope;
        public float maxSlope;
        public float weight = 1f;
    }

    public List<TerrainLayerData> TerrainTexturingData = new List<TerrainLayerData>();

    [System.Serializable]
    public class TerrainTreeData
    {
        public string name;
        public GameObject treePrefab;
        public float minHeight = 0f;
        public float maxHeight = 1;
        public float minSlope = 0f;
        public float maxSlope = 1f;
        public float density = 1f;
        public int[] spawnOnTextures;
        public float minSize = 0.6f;
        public float maxSize = 1.2f;
    }

    public List<TerrainTreeData> TerrainTreePlacementData = new List<TerrainTreeData>();
    
    [System.Serializable]
    public class TerrainDetailData
    {
        public string name;
        public GameObject detailPrefab;
        public float minHeight = 0f;
        public float maxHeight = 1;
        public float minSlope = 0f;
        public float maxSlope = 1f;
        public float density = 1f;
        public int[] spawnOnTextures;
        public float minSize = 0.6f;
        public float maxSize = 1.2f;
    }

    public List<TerrainDetailData> TerrainDetailPlacementData = new List<TerrainDetailData>();
    
    
}
