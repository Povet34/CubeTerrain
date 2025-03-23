using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class CreateTerrainStarter : MonoBehaviour
{
    public CubeNewTerrain cubeNewTerrain;
    public Material[] materialList;

    private void Start()
    {
        List<WorldTerrain.TileInfo> tileInfos = new List<WorldTerrain.TileInfo>();

        foreach (Material material in materialList)
        {
            tileInfos.Add(new WorldTerrain.TileInfo(material.name, material));
        }

        cubeNewTerrain.CreateTerrain(8,8, "BBBBBBBBCCCCCCCBBBBDDDDDDBBBBFFFFJJJBBBBBBBBBBBBBBBBBBBBBBBBBBBB", tileInfos);
        //cubeNewTerrain.CreateTerrainImmediately(tileInfos);
    }
}
