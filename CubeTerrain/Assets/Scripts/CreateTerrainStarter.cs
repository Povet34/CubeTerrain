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

    public Button cellLandButton; // CELL_LAND 버튼
    public Button cellWaterButton; // CELL_WATER 버튼

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
        if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭 감지
        {
            HandleMouseClick(true);
        }
        else if (Input.GetMouseButtonDown(1)) // 마우스 오른쪽 버튼 클릭 감지
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
            // 현재 셀 타입을 가져와서 한 층 더 쌓거나 내리기
            byte cellType = _terrain.GetCellType(x, y);

            byte upFloor = (byte)(floor < 10 - 1 ? floor + 1 : floor);
            byte downFloor = (byte)(floor >= 0 ? floor - 1 : floor);

            byte type = isLeftClick ? currentCellType : CELL_NONE;
            byte dofloor = isLeftClick ? upFloor : downFloor;

            if (_terrain.ModifyCell(type, x, y, x, y, dofloor))
            {
                Debug.Log($"셀 수정 성공: ({x}, {y}), {floor}층");
            }
            else
            {
                Debug.Log($"셀 수정 실패: ({x}, {y}), {floor}층");
            }
        }
    }

    private void SetCellType(byte cellType)
    {
        currentCellType = cellType;
        Debug.Log($"현재 셀 타입 변경: {cellType}");
    }
}