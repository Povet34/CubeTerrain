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

        if (cubeNewTerrain.PickCell(Camera.main.transform.position, dir, out floor, out x, out y))
        {
            // 현재 셀 타입을 가져와서 한 층 더 쌓거나 내리기
            byte currentCellType = cubeNewTerrain.GetCellType(x, y);
            byte newCellType;

            if (isLeftClick)
            {
                newCellType = CELL_LAND;
            }
            else
            {
                newCellType = currentCellType; // 셀 타입 유지
            }

            // 셀 타입이 유효한지 확인
            if (cubeNewTerrain.ModifyCell(newCellType, x, y, x, y, isLeftClick ? ++floor : --floor))
            {
                Debug.Log($"셀 수정 성공: {x}, {y}, {newCellType}");
            }
            else
            {
                Debug.Log($"셀 수정 실패: {x}, {y}, {newCellType}");
            }
        }
    }
}