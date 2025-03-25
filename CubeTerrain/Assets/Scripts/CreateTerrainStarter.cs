using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static CubeTerrain;
using UnityEngine.UI;


public class CreateTerrainStarter : MonoBehaviour
{
    public CubeNewTerrain _terrain;
    public Material[] materialList;
    public GameObject[] props;

    byte currentCellType = CELL_LAND;

    public Button cellLandButton; // CELL_LAND ��ư
    public Button cellWaterButton; // CELL_WATER ��ư

    private void Start()
    {
        List<WorldTerrain.TileInfo> tileInfos = new List<WorldTerrain.TileInfo>();
        foreach (Material material in materialList)
        {
            tileInfos.Add(new WorldTerrain.TileInfo(material.name, material));
        }

        _terrain.CreateTerrain(8, 8, "BBBBBBBBCCCCCCCBBBBDDDDDDBBBBFFFFJJJBBBBBBBBBBBBBBBBBBBBBBBBBBBB", tileInfos);

        cellLandButton.onClick.AddListener(() => SetCellType(CELL_LAND));
        cellWaterButton.onClick.AddListener(() => SetCellType(CELL_WATER));
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

        if (_terrain.PickCell(startPos, dir, out floor, out x, out y))
        {
            // ���� �� Ÿ���� �����ͼ� �� �� �� �װų� ������
            byte cellType = _terrain.GetCellType(x, y);

            byte upFloor = (byte)(floor < 10 - 1 ? floor + 1 : floor);
            byte downFloor = (byte)(floor >= 0 ? floor - 1 : floor);

            byte type = isLeftClick ? currentCellType : CELL_NONE;
            byte dofloor = isLeftClick ? upFloor : downFloor;

            if (_terrain.ModifyCell(type, x, y, x, y, dofloor))
            {
                Debug.Log($"�� ���� ����: ({x}, {y}), {floor}��");
            }
            else
            {
                Debug.Log($"�� ���� ����: ({x}, {y}), {floor}��");
            }
        }
    }

    private void SetCellType(byte cellType)
    {
        currentCellType = cellType;
        Debug.Log($"���� �� Ÿ�� ����: {cellType}");
    }
}