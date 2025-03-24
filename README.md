# CubeTerrain
CubeTerrain
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

### Create Terrain
- CreateTerrain(int width, int height, string cellMap, List<TileInfo> tileMaterials = null, string tileMap = null)
- ClearTerrain();
- ResetTerrain();


### Edit Terrain



### Create Mesh
