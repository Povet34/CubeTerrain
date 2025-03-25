using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static CubeTerrain;

public class CreateTerrainStarter : MonoBehaviour
{
    public CubeTerrain cubeNewTerrain;
    public Material[] materialList;
    public GameObject[] props;

    private void Start()
    {
        List<WorldTerrain.TileInfo> tileInfos = new List<WorldTerrain.TileInfo>();
        foreach (Material material in materialList)
        {
            tileInfos.Add(new WorldTerrain.TileInfo(material.name, material));
        }

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
        byte floor, x, y;

        Vector3 startPos = Camera.main.transform.position;
        Vector3 dir = Camera.main.ScreenPointToRay(Input.mousePosition).direction;

        if (cubeNewTerrain.PickCell(Camera.main.transform.position, dir, out floor, out x, out y))
        {
            // ���� �� Ÿ���� �����ͼ� �� �� �� �װų� ������
            byte currentCellType = cubeNewTerrain.GetCellType(x, y);
            byte newCellType;

            if (isLeftClick)
            {
                newCellType = CELL_LAND;
            }
            else
            {
                newCellType = currentCellType; // �� Ÿ�� ����
            }

            // �� Ÿ���� ��ȿ���� Ȯ��
            if (cubeNewTerrain.ModifyCell(newCellType, x, y, x, y, isLeftClick ? ++floor : --floor))
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