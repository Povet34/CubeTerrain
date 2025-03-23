using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITerrainFunction
{
    Bounds GetTerrainBounds();
}

public class WorldTerrain : MonoBehaviour, ITerrainFunction
{
    public class TileInfo
    {
        public string tileItemID;
        public Material material;

        public TileInfo()
        {
        }

        public TileInfo(string tileItemID, Material material)
        {
            this.tileItemID = tileItemID;
            this.material = material;
        }
    }

    protected class TerrainParam
    {
        public int width;
        public int height;

        protected bool tileLoaded = false;

        public void Reset()
        {
            tileLoaded = false;
        }
    }

    public Material basePlaneMaterial;

    protected int terrainWidth;
    protected int terrainHeight;

    public virtual Bounds GetTerrainBounds() { return new Bounds(); }
}
