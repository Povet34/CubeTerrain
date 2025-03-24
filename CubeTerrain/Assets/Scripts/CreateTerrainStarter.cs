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
        cubeNewTerrain.CreateTerrain(8, 8, "BBBBBBBBCCCCCCCBBBBDDDDDDBBBBFFFFJJJBBBBBBBBBBBBBBBBBBBBBBBBBBBB", tileInfos);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // ���콺 ���� ��ư Ŭ�� ����
        {
            HandleMouseClick(true);
        }
        else if (Input.GetMouseButtonDown(1)) // ���콺 ������ ��ư Ŭ�� ����
        {
            HandleMouseClick(false);
        }
    }

    private void HandleMouseClick(bool isLeftClick)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 clickPosition = hit.point;
            byte floor, x, y;
            if (cubeNewTerrain.PickCell(clickPosition, ray.direction, out floor, out x, out y))
            {
                // ���� �� Ÿ���� �����ͼ� �� �� �� �װų� ������
                byte currentCellType = cubeNewTerrain.GetCellType(x, y);
                byte newCellType;

                newCellType = CELL_LAND;

                // �� Ÿ���� ��ȿ���� Ȯ��
                if (cubeNewTerrain.ModifyCell(newCellType, x, y, x, y, ++floor))
                {
                    Debug.Log($"�� ���� ����: {x}, {y}, {newCellType}");
                }
                else
                {
                    Debug.Log($"�� ���� ����: {x}, {y}, {newCellType}");
                }
            }
        }
    }
}