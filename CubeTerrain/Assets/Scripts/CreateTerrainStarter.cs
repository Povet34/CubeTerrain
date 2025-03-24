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
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 clickPosition = hit.point;
            byte floor, x, y;
            if (cubeNewTerrain.PickCell(clickPosition, ray.direction, out floor, out x, out y))
            {
                // 현재 셀 타입을 가져와서 한 층 더 쌓거나 내리기
                byte currentCellType = cubeNewTerrain.GetCellType(x, y);
                byte newCellType;

                newCellType = CELL_LAND;

                // 셀 타입이 유효한지 확인
                if (cubeNewTerrain.ModifyCell(newCellType, x, y, x, y, ++floor))
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
}