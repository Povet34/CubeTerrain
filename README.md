# CubeTerrain
CubeTerrain


https://github.com/user-attachments/assets/85ec54db-658d-42a1-8cc3-ca68f1348c75


-----

### Cell & Tile Infos
- Max Tile 갯수 50
- Cell은 최대 10층까지 쌓임
```c#
public const byte CELL_NONE = (byte)0;   // A
public const byte CELL_LAND = (byte)1;   // B
public const byte CELL_WATER = (byte)2;  // C
public const byte CELL_SMOOTH = (byte)3; // D
public const byte TOTAL_CELL_BASE_TYPE = 4;
```
- ex) CellInfo의 저장은 8X8 크기일 경우 "BBBBBBBBCCCCCCCBBBBDDDDDDBBBBFFFFJJJBBBBBBBBBBBBBBBBBBBBBBBBBBBB" 이렇게 저장됨.

### Create Terrain
```c#
void CreateTerrain(int width, int height, string cellMap, List<TileInfo> tileMaterials = null, string tileMap = null)
void ClearTerrain();
void ResetTerrain();
```


### Edit Terrain
```c#
bool ModifyTile(byte x, byte y, byte tileType, bool refreshTerrain = true)
bool ModifyCell(byte baseCellType, byte fromTx, byte fromTy, byte toTx, byte toTy, byte targetFloor)
bool PickCell(Vector3 startWorld, Vector3 directionWorld, out byte floor, out byte x, out byte y)
```

### Create Mesh


### Check NeighborInfo
