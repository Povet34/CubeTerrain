# CubeTerrain
CubeTerrain
![image](https://github.com/user-attachments/assets/c688f0d7-3595-4a96-826f-d361810fa8be)

-----

### Cell & Tile Infos
- Max Tile 갯수 50
- Cell은 최대 10층까지 쌓임
- ```c#
  public const byte CELL_NONE = (byte)0;   // A
  public const byte CELL_LAND = (byte)1;   // B
  public const byte CELL_WATER = (byte)2;  // C
  public const byte CELL_SMOOTH = (byte)3; // D
  public const byte TOTAL_CELL_BASE_TYPE = 4;
  ```
- ex) CellInfo의 저장은 8X8 크기일 경우 "BBBBBBBBCCCCCCCBBBBDDDDDDBBBBFFFFJJJBBBBBBBBBBBBBBBBBBBBBBBBBBBB" 이렇게 저장됨.

### Create Terrain
- CreateTerrain(int width, int height, string cellMap, List<TileInfo> tileMaterials = null, string tileMap = null)
- ClearTerrain();
- ResetTerrain();


### Edit Terrain


### Create Mesh


### Check NeighborInfo
