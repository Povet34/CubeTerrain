using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static CubeTerrain;

public class CreateTerrainStarter : MonoBehaviour
{
    public CubeNewTerrain cubeNewTerrain;
    public Material[] materialList;
    public GameObject[] props;

    private void Start()
    {
        List<WorldTerrain.TileInfo> tileInfos = new List<WorldTerrain.TileInfo>();
        foreach (Material material in materialList)
        {
            tileInfos.Add(new WorldTerrain.TileInfo(material.name, material));
        }

        int propIndex = 0;
        List<TilePropInfo> tilePropInfos = new List<TilePropInfo>();
        foreach (GameObject prop in props)
        {
            var propInfo = new TilePropInfo();
            propInfo.index = propIndex++;
            propInfo.propPrefab = prop;
            propInfo.type = TilePropType.GRASS;
            propInfo.randomMin = 10;
            propInfo.randomMax = 100;
            propInfo.probPercentage = 50;

            tilePropInfos.Add(propInfo);
        }

        cubeNewTerrain.propInfo = tilePropInfos.ToArray();
        cubeNewTerrain.CreateTerrain(8,8, "BBBBBBBBCCCCCCCBBBBDDDDDDBBBBFFFFJJJBBBBBBBBBBBBBBBBBBBBBBBBBBBB", tileInfos);
    }
}
