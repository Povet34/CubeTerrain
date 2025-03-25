//#define USE_SMOOTH
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 기본적인 cell들은 사각형 폴리곤 하나씩 가지고 있다.
// 하지만, 지형변경시에는 2*2 cell 씩 변경하게끔 했다.
public partial class CubeTerrain : WorldTerrain
{
    // todo : test
    public Material[] materialList;
    public List<TileInfo> testTileInfos;

    public GameObject selectionMarkPrefab;
    public GameObject touchMarkPrefab;
    public GameObject brushMarkPrefab;
    public bool canRemoveFirstFloor = false;

    protected List<TileInfo> baseMaterialList;

    public enum TilePropType
    {
        GRASS = 0,
        WATERFALL = 1,
    }
    [System.Serializable]
    public class TilePropInfo
    {
        public int index;
        public GameObject propPrefab;
        public TilePropType type;
        public int randomMin;
        public int randomMax;
        public int probPercentage; // [0,100] 배치할 확률. 100이면, 100% 배치한다.

        public void CopyFrom(TilePropInfo source)
        {
            index = source.index;
            propPrefab = source.propPrefab;
            type = source.type;
            randomMin = source.randomMin;
            randomMax = source.randomMax;
            probPercentage = source.probPercentage;
        }

        List<GameObject> pool;
        static System.Random random = null;
        static int[] fixedRandomTable = null;
        static int startSeed = 0;
        // min, max가 100이하의 수이기 때문에 이런 방식으로 한다.
        // 만일 그보다 커지면, 다시 고려해야 된다.
        public static void SetRandomeSeed(byte x, byte y)
        {
            startSeed = x + y * 511;
        }
        static int GetRandom(int min, int max)
        {
            const int MAX_VALUE = 100;
            if (random == null)
            {
                random = new System.Random();
                fixedRandomTable = new int[111]; // 소수로 한다. 그래야 반복되지 않음.
                for (int i = 0; i < fixedRandomTable.Length; i++)
                    fixedRandomTable[i] = random.Next(0, MAX_VALUE);
            }

            int value = fixedRandomTable[startSeed++ % fixedRandomTable.Length];
            float ratio = ((float)value) / ((float)MAX_VALUE);
            float v = min * (1 - ratio) + max * ratio;
            return Mathf.RoundToInt(v);
        }
        public GameObject GetProp()
        {
            if (pool != null && pool.Count > 0)
            {
                GameObject obj = pool[0];
                pool.RemoveAt(0);
                obj.SetActive(true);
                return obj;
            }
            else
            {
                GameObject obj = GameObject.Instantiate(propPrefab);
                return obj;
            }
        }
        const int TOT_DIV = 4; // 4*4 로 영역을 나누어서 배치한다. 대신에 5까지만 된다. 그래야 32bit map으로 체크 가능함
        public void PlaceRandomOnRectTile(Vector3 startPos, CellGroup group, int cellIndex)
        {
            int prob = GetRandom(0, 100);
            if (probPercentage >= prob)
            {
                int map = 0;
                float fullWidth = GetCellFullWidth() * TOT_DIV / (TOT_DIV + 1);
                float fullHeight = GetCellFullHeight() * TOT_DIV / (TOT_DIV + 1);
                float offsetX = fullWidth / TOT_DIV;
                float offsetZ = fullHeight / TOT_DIV;
                int num = GetRandom(randomMin, randomMax);
                for (int i = 0; i < num; i++)
                {
                    int randomNum = GetRandom(0, TOT_DIV * TOT_DIV - 1);
                    int bit = (1 << randomNum);
                    if ((map & bit) != 0)
                        continue;
                    map |= bit;
                    int ix = (randomNum % TOT_DIV);
                    int iz = Mathf.FloorToInt(randomNum / TOT_DIV);

                    int randomNum2 = GetRandom(0, 8);
                    float offset = ((float)(randomNum2 - 4)) / 4f * linkX * 1.5f;
                    float x = ix * fullWidth / TOT_DIV + offsetX;
                    float z = iz * fullHeight / TOT_DIV + offsetZ;
                    group.AddProp(index, this, startPos + new Vector3(x + offset, 0, z + offset), cellIndex);
                }
            }
        }
        public void PlaceRandomOnHalfTile(Vector3 startPos, CellGroup group, byte normalType, int cellIndex)
        {
            int prob = GetRandom(0, 100);
            if (probPercentage >= prob)
            {
                int map = 0;
                float fullWidth = GetCellFullWidth() * TOT_DIV / (TOT_DIV + 1);
                float fullHeight = GetCellFullHeight() * TOT_DIV / (TOT_DIV + 1);
                float offsetX = fullWidth / TOT_DIV;
                float offsetZ = fullHeight / TOT_DIV;
                int num = GetRandom(randomMin, randomMax);
                for (int i = 0; i < num; i++)
                {
                    int randomNum = GetRandom(0, TOT_DIV * TOT_DIV - 1);
                    int bit = (1 << randomNum);
                    if ((map & bit) != 0)
                        continue;
                    map |= bit;
                    int ix = (randomNum % TOT_DIV);
                    int iz = Mathf.FloorToInt(randomNum / TOT_DIV);

                    int randomNum2 = GetRandom(0, 8);
                    float offset = ((float)(randomNum2 - 4)) / 4f * linkX;
                    float x = ix * fullWidth / TOT_DIV + offsetX;
                    float z = iz * fullHeight / TOT_DIV + offsetZ;
                    if (normalType == NORM_SW && ix + iz <= TOT_DIV - 1)
                        continue;
                    else if (normalType == NORM_SE && iz - ix <= 0)
                        continue;
                    else if (normalType == NORM_NW && iz - ix >= 0)
                        continue;
                    else if (normalType == NORM_NE && ix + iz >= TOT_DIV - 1)
                        continue;
                    group.AddProp(index, this, startPos + new Vector3(x + offset, 0, z + offset), cellIndex);
                }
            }
        }
        public void PlaceWaterFall(Vector3 startPos, CellGroup group, byte normalType, int cellIndex, int diffFloor = 1)
        {
            float fullWidth = GetCellFullWidth();
            float fullHeight = GetCellFullHeight();
            Vector3 pos, forward;
            if (normalType == NORM_N)
            {
                pos = startPos + new Vector3(fullWidth * 0.5f, 0, fullHeight);
                forward = new Vector3(0, 0, 1);
            }
            else if (normalType == NORM_E)
            {
                pos = startPos + new Vector3(fullWidth, 0, fullHeight * 0.5f);
                forward = new Vector3(1, 0, 0);
            }
            else if (normalType == NORM_S)
            {
                pos = startPos + new Vector3(fullWidth * 0.5f, 0, 0);
                forward = new Vector3(0, 0, -1);
            }
            else if (normalType == NORM_W)
            {
                pos = startPos + new Vector3(0, 0, fullHeight * 0.5f);
                forward = new Vector3(-1, 0, 0);
            }
            else if (normalType == NORM_NE)
            {
                pos = startPos + new Vector3(fullWidth * 0.6f, 0, fullHeight * 0.6f);
                forward = new Vector3(1, 0, 1);
            }
            else if (normalType == NORM_SE)
            {
                pos = startPos + new Vector3(fullWidth * 0.6f, 0, fullHeight * 0.4f);
                forward = new Vector3(1, 0, -1);
            }
            else if (normalType == NORM_SW)
            {
                pos = startPos + new Vector3(fullWidth * 0.4f, 0, fullHeight * 0.4f);
                forward = new Vector3(-1, 0, -1);
            }
            else // NORM_NW
            {
                pos = startPos + new Vector3(fullWidth * 0.4f, 0, fullHeight * 0.6f);
                forward = new Vector3(-1, 0, 1);
            }
            GameObject obj = group.AddProp(index, this, pos, cellIndex, diffFloor);
            obj.transform.forward = forward;
        }
        public void Return(List<CellGroup.PropData> objList)
        {
            if (pool == null)
                pool = new List<GameObject>();
            for (int i = 0; i < objList.Count; i++)
                objList[i].Return(pool);
        }
        public void ReturnAt(List<CellGroup.PropData> objList, int cellIndex)
        {
            if (pool == null)
                pool = new List<GameObject>();
            for (int i = 0; i < objList.Count; i++)
            {
                if (objList[i].cellIndex == cellIndex)
                    objList[i].Return(pool);
            }
        }
    }
    [SerializeField]
    public TilePropInfo[] propInfo;

    // 최적화를 위해서 따로 pointer를 가진다.
    protected int grassPropInfo1 = -1;
    protected int grassPropInfo2 = -1;
    protected int waterfallPropInfo = -1;

    // 아스키코드로 'A'(65)부터 시작하기 때문에 안전하게 50개만 된다고 하자. (smooth의 경우 +100을 해야 되므로)
    // '0' 은 none이라는 뜻이다.
    public const int MAX_TILE = 50;

    protected const int PLANE_LAND = 0;
    protected const int PLANE_WALL = 1;
    protected const int PLANE_WATER = 2;
    protected const int PLANE_WATER_SIDE = 3;
    protected const int PLANE_WATER_WALL = 4;
    protected const int PLANE_LAND_SIDE = 5;
    protected const int PLANE_WATER_SIDE2 = 6;
    protected const int TOTAL_PLANE = 7;

    public const byte CELL_NONE = (byte)0; // A
    public const byte CELL_LAND = (byte)1; // B
    public const byte CELL_WATER = (byte)2;// C
    public const byte CELL_SMOOTH = (byte)3;//D
    public const byte CELL_WATER_SMOOTH = (byte)4;//E
    public const byte TOTAL_CELL_BASE_TYPE = 4;
    protected const byte CELL_LAND2 = (byte)5;//F
    protected const byte CELL_WATER2 = (byte)6;//G
    protected const byte CELL_SMOOTH2 = (byte)7;//H
    protected const byte CELL_WATER_SMOOTH2 = (byte)8;//I
    protected const byte CELL_LAND3 = (byte)9;//J
    protected const byte CELL_WATER3 = (byte)10;//K
    protected const byte CELL_SMOOTH3 = (byte)11;//L
    protected const byte CELL_WATER_SMOOTH3 = (byte)12;//M
    protected const byte CELL_LAND4 = (byte)13;//N
    protected const byte CELL_WATER4 = (byte)14;//O
    protected const byte CELL_SMOOTH4 = (byte)15;//P
    protected const byte CELL_WATER_SMOOTH4 = (byte)16;//Q
    protected const byte CELL_LAND5 = (byte)17;//R
    protected const byte CELL_WATER5 = (byte)18;//S
    protected const byte CELL_SMOOTH5 = (byte)19;//T
    protected const byte CELL_WATER_SMOOTH5 = (byte)20;//U
    protected const byte CELL_LAND6 = (byte)21;//V
    protected const byte CELL_WATER6 = (byte)22;//W
    protected const byte CELL_SMOOTH6 = (byte)23;//X
    protected const byte CELL_WATER_SMOOTH6 = (byte)24;//Y
    protected const byte CELL_LAND7 = (byte)25;//Z
    protected const byte CELL_WATER7 = (byte)26;//[
    protected const byte CELL_SMOOTH7 = (byte)27;//\
    protected const byte CELL_WATER_SMOOTH7 = (byte)28;//]
    protected const byte CELL_LAND8 = (byte)29;//^
    protected const byte CELL_WATER8 = (byte)30;//_
    protected const byte CELL_SMOOTH8 = (byte)31;//`
    protected const byte CELL_WATER_SMOOTH8 = (byte)32;//a
    protected const byte CELL_LAND9 = (byte)33;//b
    protected const byte CELL_WATER9 = (byte)34;//c
    protected const byte CELL_SMOOTH9 = (byte)35;//d
    protected const byte CELL_WATER_SMOOTH9 = (byte)36;//e
    protected const byte CELL_LAND10 = (byte)37;//f
    protected const byte CELL_WATER10 = (byte)38;//g
    protected const byte CELL_SMOOTH10 = (byte)39;//h
    protected const byte CELL_WATER_SMOOTH10 = (byte)40;//i
    // 여기까지는 인덱스
    // 층수를 늘리기 위해서
    protected const byte CELL_HALF_WATER = (byte)100;
    //const byte CELL_TYPE_INDEX = (byte)0x0f;
    //const byte CELL_FLAGS = (byte)0xf0;

    // floor(층) : 0~2
    protected const byte MAX_FLOOR = 10;

    public static int CellIndex(byte floor, byte x, byte y)
    {
        return (int)(floor << 16) + (int)(y << 8) + x;
    }
    protected static byte GetOnlyCellIndex(byte cell)
    {
        return cell >= CELL_HALF_WATER ? (byte)(cell - CELL_HALF_WATER) : cell;
    }
    protected static bool IsOnFloor(byte floor, byte cell)
    {
        byte _cell = GetOnlyCellIndex(cell);
        return TOTAL_CELL_BASE_TYPE * floor + CELL_LAND <= _cell && _cell <= TOTAL_CELL_BASE_TYPE * floor + CELL_WATER_SMOOTH;
    }
    // 각 층에 해당하는 값이 있으면, 1층의 1~4 값으로 바꿔서 return한다. (없으면 none)
    protected byte GetBaseCellOnFloor(byte floor, byte cell, byte x, byte y)
    {
        byte _cell = GetOnlyCellIndex(cell);
        byte _flag = cell >= CELL_HALF_WATER ? CELL_HALF_WATER : (byte)0;
        if (IsOnFloor(floor, cell))
            return (byte)(_cell - TOTAL_CELL_BASE_TYPE * floor + _flag);
        else
        {
            byte cellFloor = GetFloor(_cell);
            byte baseCell = (byte)(_cell - TOTAL_CELL_BASE_TYPE * cellFloor);
            // 만일 바로 윗층이 smooth계열이면 land로 return한다.
            if (floor == 0 && baseCell == CELL_WATER_SMOOTH)
                return CELL_WATER;
            else if (floor == 0 && baseCell == CELL_SMOOTH)
                return CELL_LAND;
            // 3층이 다듬기가 되어있는데, 2층 주변에 land가 있다면, 이 땅은 land가 된다.
            else if (floor > 0 && cellFloor == floor + 1 && baseCell == CELL_SMOOTH)
            {
                byte CHECK_CELL = GetCellOfFloor(floor, CELL_LAND);
                // recursive하게 호출되지 않도록 그냥 바로 체크한다.
                byte cellW = (x - 1 >= 0) ? cellArr[(x - 1) + y * terrainWidth] : CELL_NONE;
                if (cellW == CHECK_CELL)
                    return CELL_LAND;
                byte cellE = (x + 1 < terrainWidth) ? cellArr[x + 1 + y * terrainWidth] : CELL_NONE;
                if (cellE == CHECK_CELL)
                    return CELL_LAND;
                byte cellS = (y - 1 >= 0) ? cellArr[x + (y - 1) * terrainWidth] : CELL_NONE;
                if (cellS == CHECK_CELL)
                    return CELL_LAND;
                byte cellN = (y + 1 < terrainHeight) ? cellArr[x + (y + 1) * terrainWidth] : CELL_NONE;
                if (cellN == CHECK_CELL)
                    return CELL_LAND;
                return CELL_NONE;
            }
            else if (floor > 0 && cellFloor == floor + 1 && baseCell == CELL_WATER_SMOOTH)
            {
                byte CHECK_CELL = GetCellOfFloor(floor, CELL_WATER);
                // recursive하게 호출되지 않도록 그냥 바로 체크한다.
                byte cellW = (x - 1 >= 0) ? cellArr[(x - 1) + y * terrainWidth] : CELL_NONE;
                if (cellW == CHECK_CELL)
                    return CELL_WATER;
                byte cellE = (x + 1 < terrainWidth) ? cellArr[x + 1 + y * terrainWidth] : CELL_NONE;
                if (cellE == CHECK_CELL)
                    return CELL_WATER;
                byte cellS = (y - 1 >= 0) ? cellArr[x + (y - 1) * terrainWidth] : CELL_NONE;
                if (cellS == CHECK_CELL)
                    return CELL_WATER;
                byte cellN = (y + 1 < terrainHeight) ? cellArr[x + (y + 1) * terrainWidth] : CELL_NONE;
                if (cellN == CHECK_CELL)
                    return CELL_WATER;
                return CELL_NONE;
            }
            else
                return CELL_NONE;
        }
    }
    protected static byte GetCellOfFloor(byte floor, byte baseCell)
    {
        return (byte)(baseCell + floor * TOTAL_CELL_BASE_TYPE);
    }
    protected static byte GetFloor(byte cell)
    {
        byte _cell = GetOnlyCellIndex(cell);
        if (CELL_NONE <= _cell && _cell <= CELL_WATER_SMOOTH)
            return 0;
        else
            return (byte)Mathf.FloorToInt((_cell - CELL_LAND) / TOTAL_CELL_BASE_TYPE);
    }
    protected int GetDiffFloor(byte floor, byte[] cellArr, int width, int height, byte x, byte y, byte normalType)
    {
        byte neighborCell = CELL_NONE;
        if (normalType == NORM_S || normalType == NORM_SE || normalType == NORM_SW)
            neighborCell = (y > 0) ? cellArr[x + (y - 1) * width] : CELL_NONE;
        else if (normalType == NORM_N || normalType == NORM_NE || normalType == NORM_NW)
            neighborCell = (y < height - 1) ? cellArr[x + (y + 1) * width] : CELL_NONE;
        else if (normalType == NORM_E)
            neighborCell = (x < width - 1) ? cellArr[x + 1 + y * width] : CELL_NONE;
        else if (normalType == NORM_W)
            neighborCell = (x > 0) ? cellArr[x - 1 + y * width] : CELL_NONE;
        int neighborFloor = neighborCell == CELL_NONE ? -1 : GetFloor(neighborCell);
        return floor - neighborFloor;
    }
    public byte GetFloor(byte tx, byte ty)
    {
        return GetFloor(cellArr[tx + ty * terrainWidth]);
    }

    public const byte TILE_NONE = (byte)255;
    public const byte TILE_SMOOTH = (byte)100;

    protected const byte NORM_NIL = (byte)255;
    protected const byte NORM_N = (byte)0;
    protected const byte NORM_NE = (byte)1;
    protected const byte NORM_E = (byte)2;
    protected const byte NORM_SE = (byte)3;
    protected const byte NORM_S = (byte)4;
    protected const byte NORM_SW = (byte)5;
    protected const byte NORM_W = (byte)6;
    protected const byte NORM_NW = (byte)7;
    protected static readonly Vector3[] NORMALS = new Vector3[8]
    {
        new Vector3(0,0,1),
        new Vector3(1,0,1).normalized,
        new Vector3(1,0,0),
        new Vector3(1,0,-1).normalized,
        new Vector3(0,0,-1),
        new Vector3(-1,0,-1).normalized,
        new Vector3(-1,0,0),
        new Vector3(-1,0,1).normalized,
    };
    protected static readonly byte[] INV_NORMALS = new byte[8]
    {
        NORM_S,
        NORM_SW,
        NORM_W,
        NORM_NW,
        NORM_N,
        NORM_NE,
        NORM_E,
        NORM_SE,
    };

    // group unit 단위로 cell들을 묶어서 vertex 관리를 한다. 따라서 이 단위로 render call이 생성된다.
    // 왜냐면, cell하나를 바꿀때, 전체를 다시 계산하는 일이 없도록 구획을 나눈 것이다.
    public const int GROUP_UNIT_X = 8;
    public const int GROUP_UNIT_Y = 8;
    public const int DEFAULT_WIDTH = GROUP_UNIT_X * 2;
    public const int DEFAULT_HEIGHT = GROUP_UNIT_Y * 2;
    protected const int DIV = 4;
    protected const float POLYGON_SCALE = 4;
    protected const float TERRAIN_SCALE = 4 * 0.25f;
    protected const float cellX = 1f * POLYGON_SCALE * TERRAIN_SCALE * ADJUST_CELL_SIZE;
    protected const float cellY = 1f * TERRAIN_SCALE * ADJUST_CELL_SIZE * 0.5f;
    protected const float cellZ = 1f * POLYGON_SCALE * TERRAIN_SCALE * ADJUST_CELL_SIZE;
    protected const float waterDepth = 0.33f * TERRAIN_SCALE * ADJUST_CELL_SIZE * 0.5f;
    protected const float waterPlaneDepth = 0.87f * TERRAIN_SCALE * ADJUST_CELL_SIZE * 0.5f;
    private const float ADJUST_CELL_SIZE = 2f;

    const int TOTAL_LINK_POINT = 4;
    const byte PT_TOP = 0;
    const byte PT_LINK_START = 1;    // 1개뿐일때 혹은 y up 방향 normal인 wwww
    const byte PT_REV_LINK_START = PT_LINK_START + TOTAL_LINK_POINT; // y down 방향 normal인 vertex

    // 부채꼴을 만들기 위한 삽입되는 vertex수
    const int TOTAL_TRI = 5;

    const float linkX = cellX * 0.05f / POLYGON_SCALE;
    const float linkY = cellY * 0.05f;

    const float waterSide2Offset = linkX;

    protected const float uScale = 0.25f * POLYGON_SCALE;
    protected const float vScale = 0.25f * POLYGON_SCALE;
    const float waterSideScale = 0.25f;

    protected static float GetCellFullWidth()
    {
        return cellX / (float)DIV;
    }
    protected static float GetCellFullHeight()
    {
        return cellZ / (float)DIV;
    }

    //터레인 확장 변수
    public const float CHANGE_TERRAIN_X = GROUP_UNIT_X * (cellX / DIV);
    public const float CHANGE_TERRAIN_Y = MAX_FLOOR * (cellY * POLYGON_SCALE / DIV);
    public const float CHANGE_TERRAIN_Z = GROUP_UNIT_Y * (cellZ / DIV);
    public interface CellGroup
    {
        public class PropData
        {
            public GameObject obj;
            public int cellIndex;
            public PropData(GameObject obj, int cellIndex)
            {
                this.obj = obj;
                this.cellIndex = cellIndex;
            }
            public void Return(List<GameObject> pool)
            {
                obj.transform.SetParent(null);
                obj.SetActive(false);
                pool.Add(obj);
            }
        }
        bool IsReset();
        void Reset(List<TileInfo> tileMaterials, TilePropInfo[] propInfo);
        GameObject AddProp(int propIndex, TilePropInfo info, Vector3 localPosition, int cellIndex, int diffFloor = -1);
        void RemoveTileProps(TilePropInfo[] propInfo, int cellIndex);
        void CreateGroup(Transform parent, List<TileInfo> tileMaterials, bool isDefaultTerrain);
    }

    public static int GetCellGroupIndex(byte floor, byte x, byte y)
    {
        int gx = x / GROUP_UNIT_X;
        int gy = y / GROUP_UNIT_Y;
        return gx + (int)(gy << 8) + (int)(floor << 16);
    }
    public static void GetFloorXYFromIndex(int index, out byte floor, out byte x, out byte y)
    {
        floor = (byte)(index >> 16);
        y = (byte)((index & 0xffff) >> 8);
        x = (byte)(index & 0xff);
    }

    protected Dictionary<int, CellGroup> groupMap = new Dictionary<int, CellGroup>();
    protected Dictionary<int, CellGroup> tileGroupMap = new Dictionary<int, CellGroup>();

    protected int GetNeighborLandIndexAtPoint(byte floor, byte[] cellArr, int width, int height, byte x, byte y)
    {
        int cell1 = GetBaseCellOnFloor(floor, (x - 1 >= 0 && y < height) ? cellArr[(x - 1) + y * width] : CELL_NONE, (byte)(x - 1), y);
        int cell2 = GetBaseCellOnFloor(floor, (x < width && y < height) ? cellArr[x + y * width] : CELL_NONE, x, y);
        int cell4 = GetBaseCellOnFloor(floor, (x - 1 >= 0 && y - 1 >= 0) ? cellArr[(x - 1) + (y - 1) * width] : CELL_NONE, (byte)(x - 1), (byte)(y - 1));
        int cell8 = GetBaseCellOnFloor(floor, (x < width && y - 1 >= 0) ? cellArr[x + (y - 1) * width] : CELL_NONE, x, (byte)(y - 1));
        int neighborIndex =
            ((cell1 == CELL_LAND || cell1 == CELL_SMOOTH || cell1 == CELL_SMOOTH + CELL_HALF_WATER) ? 1 : 0) +
            ((cell2 == CELL_LAND || cell2 == CELL_SMOOTH || cell2 == CELL_SMOOTH + CELL_HALF_WATER) ? 2 : 0) +
            ((cell4 == CELL_LAND || cell4 == CELL_SMOOTH || cell4 == CELL_SMOOTH + CELL_HALF_WATER) ? 4 : 0) +
            ((cell8 == CELL_LAND || cell8 == CELL_SMOOTH || cell8 == CELL_SMOOTH + CELL_HALF_WATER) ? 8 : 0);

        return neighborIndex;
    }
    protected int GetNeighborWaterIndexAtPoint(byte floor, byte[] cellArr, int width, int height, byte x, byte y)
    {
        int cell1 = GetBaseCellOnFloor(floor, (x - 1 >= 0 && y < height) ? cellArr[(x - 1) + y * width] : CELL_NONE, (byte)(x - 1), y);
        int cell2 = GetBaseCellOnFloor(floor, (x < width && y < height) ? cellArr[x + y * width] : CELL_NONE, x, y);
        int cell4 = GetBaseCellOnFloor(floor, (x - 1 >= 0 && y - 1 >= 0) ? cellArr[(x - 1) + (y - 1) * width] : CELL_NONE, (byte)(x - 1), (byte)(y - 1));
        int cell8 = GetBaseCellOnFloor(floor, (x < width && y - 1 >= 0) ? cellArr[x + (y - 1) * width] : CELL_NONE, x, (byte)(y - 1));
        int waterIndex =
            ((cell1 == CELL_WATER || cell1 == CELL_WATER_SMOOTH || cell1 == CELL_SMOOTH + CELL_HALF_WATER) ? 1 : 0) +
            ((cell2 == CELL_WATER || cell2 == CELL_WATER_SMOOTH || cell2 == CELL_SMOOTH + CELL_HALF_WATER) ? 2 : 0) +
            ((cell4 == CELL_WATER || cell4 == CELL_WATER_SMOOTH || cell4 == CELL_SMOOTH + CELL_HALF_WATER) ? 4 : 0) +
            ((cell8 == CELL_WATER || cell8 == CELL_WATER_SMOOTH || cell8 == CELL_SMOOTH + CELL_HALF_WATER) ? 8 : 0);

        return waterIndex;
    }

    protected int GetNeighborCellIndexAtPoint(byte cellType, byte floor, byte[] cellArr, int width, int height, byte x, byte y)
    {
        return cellType == CELL_LAND ? GetNeighborLandIndexAtPoint(floor, cellArr, width, height, x, y) : GetNeighborWaterIndexAtPoint(floor, cellArr, width, height, x, y);
    }
    int GetNeighborIndexAtPoint(byte floor, byte[] cellArr, int width, int height, byte x, byte y, out int waterIndex)
    {
        int cell1 = GetBaseCellOnFloor(floor, (x - 1 >= 0 && y < height) ? cellArr[(x - 1) + y * width] : CELL_NONE, (byte)(x - 1), y);
        int cell2 = GetBaseCellOnFloor(floor, (x < width && y < height) ? cellArr[x + y * width] : CELL_NONE, x, y);
        int cell4 = GetBaseCellOnFloor(floor, (x - 1 >= 0 && y - 1 >= 0) ? cellArr[(x - 1) + (y - 1) * width] : CELL_NONE, (byte)(x - 1), (byte)(y - 1));
        int cell8 = GetBaseCellOnFloor(floor, (x < width && y - 1 >= 0) ? cellArr[x + (y - 1) * width] : CELL_NONE, x, (byte)(y - 1));
        int neighborIndex =
            ((cell1 == CELL_LAND || cell1 == CELL_SMOOTH || cell1 == CELL_SMOOTH + CELL_HALF_WATER) ? 1 : 0) +
            ((cell2 == CELL_LAND || cell2 == CELL_SMOOTH || cell2 == CELL_SMOOTH + CELL_HALF_WATER) ? 2 : 0) +
            ((cell4 == CELL_LAND || cell4 == CELL_SMOOTH || cell4 == CELL_SMOOTH + CELL_HALF_WATER) ? 4 : 0) +
            ((cell8 == CELL_LAND || cell8 == CELL_SMOOTH || cell8 == CELL_SMOOTH + CELL_HALF_WATER) ? 8 : 0);

        waterIndex =
            ((cell1 == CELL_WATER || cell1 == CELL_WATER_SMOOTH || cell1 == CELL_SMOOTH + CELL_HALF_WATER) ? 1 : 0) +
            ((cell2 == CELL_WATER || cell2 == CELL_WATER_SMOOTH || cell2 == CELL_SMOOTH + CELL_HALF_WATER) ? 2 : 0) +
            ((cell4 == CELL_WATER || cell4 == CELL_WATER_SMOOTH || cell4 == CELL_SMOOTH + CELL_HALF_WATER) ? 4 : 0) +
            ((cell8 == CELL_WATER || cell8 == CELL_WATER_SMOOTH || cell8 == CELL_SMOOTH + CELL_HALF_WATER) ? 8 : 0);

        return neighborIndex;
    }
    static int GetNeighborTileIndexAtPoint(byte[] tileArr, int width, int height, byte x, byte y)
    {
        int cell1 = (x - 1 >= 0 && y < height) ? tileArr[(x - 1) + y * width] : TILE_NONE;
        int cell2 = (x < width && y < height) ? tileArr[x + y * width] : TILE_NONE;
        int cell4 = (x - 1 >= 0 && y - 1 >= 0) ? tileArr[(x - 1) + (y - 1) * width] : TILE_NONE;
        int cell8 = (x < width && y - 1 >= 0) ? tileArr[x + (y - 1) * width] : TILE_NONE;
        int neighborIndex =
            (cell1 != TILE_NONE ? 1 : 0) +
            (cell2 != TILE_NONE ? 2 : 0) +
            (cell4 != TILE_NONE ? 4 : 0) +
            (cell8 != TILE_NONE ? 8 : 0);

        return neighborIndex;
    }
    protected int GetNeighborTileIndexAtPoint(byte tileIndex, byte floor, byte x, byte y)
    {
        int cell1 = (x - 1 >= 0 && y < terrainHeight) && CanPaintTile(floor, (byte)(x - 1), y) ? tileArr[(x - 1) + y * terrainWidth] : TILE_NONE;
        int cell2 = (x < terrainWidth && y < terrainHeight) && CanPaintTile(floor, x, y) ? tileArr[x + y * terrainWidth] : TILE_NONE;
        int cell4 = (x - 1 >= 0 && y - 1 >= 0) && CanPaintTile(floor, (byte)(x - 1), (byte)(y - 1)) ? tileArr[(x - 1) + (y - 1) * terrainWidth] : TILE_NONE;
        int cell8 = (x < terrainWidth && y - 1 >= 0) && CanPaintTile(floor, x, (byte)(y - 1)) ? tileArr[x + (y - 1) * terrainWidth] : TILE_NONE;
        int neighborIndex =
            (cell1 == tileIndex ? 1 : 0) +
            (cell2 == tileIndex ? 2 : 0) +
            (cell4 == tileIndex ? 4 : 0) +
            (cell8 == tileIndex ? 8 : 0);

        return neighborIndex;
    }
    bool IsLand(byte floor, byte cellType, byte x, byte y, bool onlyThisFloor = false)
    {
        if (onlyThisFloor)
        {
            byte baseCell = GetOnlyCellIndex(GetBaseCellOnFloor(floor, cellType, x, y));
            if (baseCell == CELL_LAND || baseCell == CELL_SMOOTH)
                return true;
        }
        else
        {
            for (int f = floor; f < MAX_FLOOR; f++)
            {
                byte baseCell = GetOnlyCellIndex(GetBaseCellOnFloor((byte)f, cellType, x, y));
                if (baseCell == CELL_LAND || baseCell == CELL_SMOOTH)
                    return true;
            }
        }
        return false;
    }
    bool IsWater(byte floor, byte cellType, byte x, byte y, bool onlyThisFloor = false)
    {
        if (onlyThisFloor)
        {
            byte baseCell = GetOnlyCellIndex(GetBaseCellOnFloor(floor, cellType, x, y));
            if (baseCell == CELL_WATER || baseCell == CELL_WATER_SMOOTH || baseCell == CELL_SMOOTH + CELL_HALF_WATER)
                return true;
        }
        else
        {
            for (int f = floor; f < MAX_FLOOR; f++)
            {
                byte baseCell = GetOnlyCellIndex(GetBaseCellOnFloor((byte)f, cellType, x, y));
                if (baseCell == CELL_WATER || baseCell == CELL_WATER_SMOOTH || baseCell == CELL_SMOOTH + CELL_HALF_WATER)
                    return true;
            }
        }
        return false;
    }
    // land일때만 다듬기가 되고, 인접한 면이 2개 붙어있어야 가능하다.
    // 주변에 높은층의 land가 있어도 된다.
    //       +---+
    //       | 1 |
    //   +---+---+---+
    //   | 8 |   | 2 |
    //   +---+---+---+
    //       | 4 |
    //       +---+
    protected int GetNeighborCellIndex(byte floor, byte[] cellArr, int width, int height, byte x, byte y, bool onlyThisFloor = false)
    {
        bool cellW = IsLand(floor, (x - 1 >= 0 && y < height) ? cellArr[(x - 1) + y * width] : CELL_NONE, (byte)(x - 1), y, onlyThisFloor);
        bool cellE = IsLand(floor, (x + 1 < width && y < height) ? cellArr[x + 1 + y * width] : CELL_NONE, (byte)(x + 1), y, onlyThisFloor);
        bool cellS = IsLand(floor, (x < width && y - 1 >= 0) ? cellArr[x + (y - 1) * width] : CELL_NONE, x, (byte)(y - 1), onlyThisFloor);
        bool cellN = IsLand(floor, (x < width && y + 1 < height) ? cellArr[x + (y + 1) * width] : CELL_NONE, x, (byte)(y + 1), onlyThisFloor);
        int neighborIndex =
            (cellN ? 1 : 0) +
            (cellE ? 2 : 0) +
            (cellS ? 4 : 0) +
            (cellW ? 8 : 0);
        return neighborIndex;
    }
    protected byte GetCellData(byte[] cellArr, int width, int height, int x, int y)
    {
        return (0 <= x && x < width && 0 <= y && y < height) ? cellArr[x + y * width] : CELL_NONE;
    }
    protected int GetNeighborCellIndexWithType(byte cellType, byte floor, byte[] cellArr, int width, int height, byte x, byte y, bool onlyThisFloor = false)
    {
        return cellType == CELL_LAND ? GetNeighborCellIndex(floor, cellArr, width, height, x, y, onlyThisFloor) : GetNeighborWaterCellIndex(floor, cellArr, width, height, x, y, onlyThisFloor);
    }
    protected int GetNeighborTileIndex(byte[] tileArr, int width, int height, byte x, byte y)
    {
        int cellW = (x - 1 >= 0 && y < height) ? tileArr[(x - 1) + y * width] : TILE_NONE;
        int cellE = (x + 1 < width && y < height) ? tileArr[x + 1 + y * width] : TILE_NONE;
        int cellS = (x < width && y - 1 >= 0) ? tileArr[x + (y - 1) * width] : TILE_NONE;
        int cellN = (x < width && y + 1 < height) ? tileArr[x + (y + 1) * width] : TILE_NONE;
        int neighborIndex =
            ((cellN != TILE_NONE) ? 1 : 0) +
            ((cellE != TILE_NONE) ? 2 : 0) +
            ((cellS != TILE_NONE) ? 4 : 0) +
            ((cellW != TILE_NONE) ? 8 : 0);

        return neighborIndex;
    }

    protected bool CanPaintTile(byte floor, byte x, byte y)
    {
        int index = x + y * terrainWidth;
        byte originalCellType = cellArr[index];
        byte cellType = GetBaseCellOnFloor(floor, originalCellType, x, y);
        return (cellType == CELL_LAND || cellType == CELL_SMOOTH);
    }
    protected int GetNeighborTileIndex(byte tileIndex, byte floor, byte x, byte y)
    {
        int cellW = (x - 1 >= 0 && y < terrainHeight) && CanPaintTile(floor, (byte)(x - 1), y) ? tileArr[(x - 1) + y * terrainWidth] : TILE_NONE;
        int cellE = (x + 1 < terrainWidth && y < terrainHeight) && CanPaintTile(floor, (byte)(x + 1), y) ? tileArr[x + 1 + y * terrainWidth] : TILE_NONE;
        int cellS = (x < terrainWidth && y - 1 >= 0) && CanPaintTile(floor, x, (byte)(y - 1)) ? tileArr[x + (y - 1) * terrainWidth] : TILE_NONE;
        int cellN = (x < terrainWidth && y + 1 < terrainHeight) && CanPaintTile(floor, x, (byte)(y + 1)) ? tileArr[x + (y + 1) * terrainWidth] : TILE_NONE;
        int neighborIndex =
            ((cellN == tileIndex) ? 1 : 0) +
            ((cellE == tileIndex) ? 2 : 0) +
            ((cellS == tileIndex) ? 4 : 0) +
            ((cellW == tileIndex) ? 8 : 0);

        return neighborIndex;
    }
    bool CanTileSmooth(byte[] tileArr, int width, int height, byte x, byte y)
    {
        int neighborIndex = GetNeighborTileIndex(tileArr, width, height, x, y);
        return neighborIndex == 3 || neighborIndex == 6 || neighborIndex == 12 || neighborIndex == 9;
    }
    // 주변이 땅으로 인접해야 다듬기를 할 수 있다. 땅 + 물 이 인접한 경우에는 뾰족한 형태의 땅이 되어서 가장자리 폴리곤이 이상해진다.
    bool CanLandSmooth(byte floor, byte[] cellArr, int width, int height, byte x, byte y)
    {
        int neighborIndex = GetNeighborCellIndex(floor, cellArr, width, height, x, y);
        return neighborIndex == 3 || neighborIndex == 6 || neighborIndex == 12 || neighborIndex == 9;
    }
    bool CanWaterSmooth(byte floor, byte[] cellArr, int width, int height, byte x, byte y)
    {
        int neighborIndex = GetNeighborWaterCellIndex(floor, cellArr, width, height, x, y);
        if (neighborIndex == 3 || neighborIndex == 6 || neighborIndex == 12 || neighborIndex == 9)
        {
            // 주변에 땅이 없을때만 water smooth가능하다.
            int landNeighborIndex = GetNeighborCellIndex(floor, cellArr, width, height, x, y);
            return landNeighborIndex == 0;
        }
        return false;
    }
    protected int GetNeighborWaterCellIndex(byte floor, byte[] cellArr, int width, int height, byte x, byte y, bool onlyThisFloor = false)
    {
        bool cellW = IsWater(floor, (x - 1 >= 0 && y < height) ? cellArr[(x - 1) + y * width] : CELL_NONE, (byte)(x - 1), y, onlyThisFloor);
        bool cellE = IsWater(floor, (x + 1 < width && y < height) ? cellArr[x + 1 + y * width] : CELL_NONE, (byte)(x + 1), y, onlyThisFloor);
        bool cellS = IsWater(floor, (x < width && y - 1 >= 0) ? cellArr[x + (y - 1) * width] : CELL_NONE, x, (byte)(y - 1), onlyThisFloor);
        bool cellN = IsWater(floor, (x < width && y + 1 < height) ? cellArr[x + (y + 1) * width] : CELL_NONE, x, (byte)(y + 1), onlyThisFloor);
        int neighborIndex =
            (cellN ? 1 : 0) +
            (cellE ? 2 : 0) +
            (cellS ? 4 : 0) +
            (cellW ? 8 : 0);
        return neighborIndex;
    }

    // 땅을 다듬었는데, 주변에 물이 있을 경우, CELL_HALF_WATER flag를 붙여준다.
    // 그런데, 버그가 있을수도 있다. cellArr을 바꾸면서 for문을 돌고 있다. 정확하게 하려면, 별도의 arr를 만들어서 그것을 바꾸게끔 해야 된다.
    // 현재까지는 문제가 없어서 그냥 이대로 쓴다.
    protected void CheckCellArr(byte[] cellArr, int width, int height, byte[] tileArr)
    {
        for (byte floor = 0; floor < MAX_FLOOR; floor++)
        {
            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    byte cellType = GetOnlyCellIndex(cellArr[index]);
                    if (GetFloor(cellType) != floor)
                        continue;
                    // flag값을 제거함
                    cellArr[index] = cellType;
                    if (GetBaseCellOnFloor(floor, cellType, x, y) == CELL_SMOOTH)
                    {
                        int neighborCell = GetNeighborCellIndex(floor, cellArr, width, height, x, y);
                        int neighborWater = GetNeighborWaterCellIndex(floor, cellArr, width, height, x, y);
                        bool makeWaterCell = false;
                        // norm_sw 방향
                        if ((neighborCell & 4) == 0 && (neighborCell & 8) == 0)
                        {
                            if ((neighborWater & 4) != 0 || (neighborWater & 8) != 0)
                                makeWaterCell = true;
                        }
                        // norm_se 방향
                        else if ((neighborCell & 2) == 0 && (neighborCell & 4) == 0)
                        {
                            if ((neighborWater & 2) != 0 || (neighborWater & 4) != 0)
                                makeWaterCell = true;
                        }
                        // norm_nw 방향
                        else if ((neighborCell & 1) == 0 && (neighborCell & 8) == 0)
                        {
                            if ((neighborWater & 1) != 0 || (neighborWater & 8) != 0)
                                makeWaterCell = true;
                        }
                        // norm_ne 방향
                        else if ((neighborCell & 1) == 0 && (neighborCell & 2) == 0)
                        {
                            if ((neighborWater & 1) != 0 || (neighborWater & 2) != 0)
                                makeWaterCell = true;
                        }

                        if (makeWaterCell)
                            cellArr[index] = (byte)(cellType + CELL_HALF_WATER);
                    }
                }
            }
        }
    }

    protected GameObject newBlock;
    //VertexInfo basePlaneInfo;
    protected int pivotX;
    protected int pivotY;
    protected byte[] cellArr;
    protected byte[] tileArr;
    protected List<TileInfo> tileMaterials;

    public const byte BASE_FLOOR = (byte)0;
    public const byte MAX_TERRAIN_SIZE = (byte)255;
    public const int DEFAULT_PIVOT_POS = 0;

    public GameObject GetTerrain()
    {
        return newBlock;
    }

    public int GetWidth()
    {
        return terrainWidth;
    }

    public int GetHeight()
    {
        return terrainHeight;
    }

    public int GetPivotX()
    {
        return pivotX;
    }

    public int GetPivotY()
    {
        return pivotY;
    }

    public Bounds GetTerrainBounds_WithY()
    {
        Vector2 terrainSize = GetTerrainSize();
        float height = cellY * MAX_FLOOR;
        Vector3 size = new Vector3(terrainSize.x, height, terrainSize.y);

        Vector3 pivotPos = new Vector3(0f - pivotX * GROUP_UNIT_X * GetCellFullWidth(), 0f, 0f - pivotY * GROUP_UNIT_Y * GetCellFullHeight());
        Vector3 center = new Vector3(pivotPos.x + (size.x * 0.5f), height * 0.5f, pivotPos.z + (size.z * 0.5f));

        Bounds bounds = new Bounds(center, size);
        return bounds;
    }


    //높이 값은 설정X
    public override Bounds GetTerrainBounds()
    {
        Vector2 terrainSize = GetTerrainSize();
        Vector3 size = new Vector3(terrainSize.x, 0f, terrainSize.y);

        Vector3 pivotPos = new Vector3(0f - pivotX * GROUP_UNIT_X * GetCellFullWidth(), 0f, 0f - pivotY * GROUP_UNIT_Y * GetCellFullHeight());
        Vector3 center = new Vector3(pivotPos.x + (size.x * 0.5f), 0f, pivotPos.z + (size.z * 0.5f));

        Bounds bounds = new Bounds(center, size);
        //CLogger.Log($"bounds.center : {bounds.center}, bounds.size : {bounds.size}, bounds.min : {bounds.min}, bounds.max : {bounds.max}");
        return bounds;
    }

    public Vector2 GetTerrainSize()
    {
        return new Vector2(terrainWidth * cellX / DIV, terrainHeight * cellZ / DIV);
    }

    // return index
    int _AddTileMaterial(TileInfo tile)
    {
        for (int i = 0; i < tileMaterials.Count; i++)
        {
            if (tileMaterials[i] != null && tileMaterials[i].tileItemID == tile.tileItemID)
                return i;
        }
        if (tileMaterials.Count >= MAX_TILE)
        {
            return 0;
        }
        tileMaterials.Add(tile);
        return tileMaterials.Count - 1;
    }

    public void ResetTerrain()
    {
        isTerrainChanged = true;
        foreach (var d in groupMap)
            d.Value.Reset(baseMaterialList, propInfo);
        foreach (var d in tileGroupMap)
            d.Value.Reset(tileMaterials, propInfo);

        int width = terrainWidth;// WorldTerrain.DEFAULT_WIDTH;
        int height = terrainHeight;// WorldTerrain.DEFAULT_HEIGHT;
        int pX = pivotX;
        int pZ = pivotY;
        string initCellMap = GetDefaultCellMap(width, height);
        CreateTerrain(width, height, initCellMap, tileMaterials);
        _AdjustTerrainPosition(pX, pZ);
    }
    public static string GetDefaultCellMap(int width, int height)
    {
        System.Text.StringBuilder map = new System.Text.StringBuilder(width * height);
        // 원형으로 만든다.
        float radius = ((float)width) * 0.5f;
        float radius2 = radius * radius;
        Vector2 center = new Vector2((float)(width) * 0.5f, (float)(height) * 0.5f);
        for (int i = 0; i < width * height; i++)
        {
            //int x = i % width;
            //int y = i / width;
            //if ((center - new Vector2(y + 0.5f, x + 0.5f)).sqrMagnitude <= radius2)
            map.Append('B');
            //else
            //    map.Append('A');
        }
        return map.ToString();
    }

    // 개별적으로 오는 값을 처리한다.
    class CubeTerrainParam : TerrainParam
    {
        public int pivotX;
        public int pivotY;
        public string cellMap = null;
        public List<string> tileItems = null;
        public List<TileInfo> tileMaterials = null;
        public string tileMap = null;

        public bool isTileLoaded { get { return null == tileMaterials || tileMaterials.Count == 0 || tileLoaded; } }

        public void LoadTiles(List<string> tileItems, CubeTerrain owner)
        {
            Reset();
            this.tileItems = tileItems;
            tileMaterials = null;
            if (tileItems != null && tileItems.Count > 0)
            {
                tileMaterials = new List<TileInfo>();
                // 우선 모든 값을 null로 채워둔다.
                for (int i = 0; i < tileItems.Count; i++)
                    tileMaterials.Add(null);

                for (int i = 0; i < tileItems.Count; i++)
                {
                    string tileItemId = tileItems[i];
                    if (string.IsNullOrEmpty(tileItemId) || tileItemId == "null")
                    {
                        Material defaultMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        tileMaterials[i] = new TileInfo(tileItemId, defaultMat);
                    }
                    else
                    {
                        // owner의 함수로 callback하는 이유는, 이 terrain이 destroy되면 callback이 오지 않도록 하기 위함.
                        //owner._GetHelper(tileItemId).LoadMaterialItem(tileItemId, owner._Callback_Loaded_Material, i);
                    }
                }
            }
        }
        public void SetTileMaterial(int index, TileInfo info)
        {
            if (null != tileMaterials && 0 <= index && index < tileMaterials.Count)
            {
                tileMaterials[index] = info;
                tileLoaded = true;
                for (int i = 0; i < tileMaterials.Count; i++)
                {
                    if (null == tileMaterials[i])
                    {
                        tileLoaded = false;
                        break;
                    }
                }
            }
        }
    }

    //void _Callback_Loaded_Material(AssetRef assetRef, AssetLoadParam param, eAssetState state)
    //{
    //    Debug.Assert(null != param);
    //    Debug.Assert(assetRef.isLoaded);
    //    Debug.Assert(assetRef is AssetRef_Material);
    //    int index = param.index1;
    //    if (null != _param)
    //        _param.SetTileMaterial(index, new TileInfo(assetRef.itemId, assetRef.getAsset_Material));
    //}

    CubeTerrainParam _param;
    Coroutine _reservedCreationCoroutine = null;

    void _DefaultParamSetting()
    {
        if (null == _param)
        {
            _param = new CubeTerrainParam();
            _param.width = terrainWidth;
            _param.height = terrainHeight;
        }
        if (null == _reservedCreationCoroutine && gameObject.activeInHierarchy)
            _reservedCreationCoroutine = StartCoroutine(_CreateTerrain());
    }

    public void CreateTerrainImmediately()
    {
        if (null != _reservedCreationCoroutine)
        {
            StopCoroutine(_reservedCreationCoroutine);
            _reservedCreationCoroutine = null;
        }
        _reservedCreationCoroutine = StartCoroutine(_CreateTerrain(false));
    }

    public void SetTerrainCellMap(string cellMap)
    {
        //CLogger.Log("SetTerrainCellMap : " + cellMap);
        _DefaultParamSetting();

        if (string.IsNullOrEmpty(cellMap) && _param.width > 0 && _param.height > 0)
            _param.cellMap = CubeTerrain.GetDefaultCellMap(_param.width, _param.height);
        else
            _param.cellMap = cellMap;
    }

    public void SetTerrainPivot(int pivotX, int pivotY)
    {
        _DefaultParamSetting();

        _param.pivotX = pivotX;
        _param.pivotY = pivotY;
    }

    public void SetTerrainSize(int width, int height)
    {
        _DefaultParamSetting();

        _param.width = width;
        _param.height = height;

        GameObject.Destroy(newBlock);
        newBlock = null;

        if (string.IsNullOrEmpty(_param.cellMap))
            _param.cellMap = CubeTerrain.GetDefaultCellMap(width, height);
    }

    public void SetTerrainTileList(List<string> tileItems)
    {
        _DefaultParamSetting();

        _param.LoadTiles(tileItems, this);
    }

    public void SetTerrainTileMap(string tileMap)
    {
        //CLogger.Log("SetTerrainTileMap : " + tileMap);
        _DefaultParamSetting();

        _param.tileMap = tileMap;
    }

    IEnumerator _CreateTerrain(bool delayCall = true)
    {
        if (delayCall)
            yield return null;
        // 먼저 cellmap부터 그린다.

        if (null != _param)
        {
            if (_param.isTileLoaded)
            {
                CreateTerrain(_param.width, _param.height, _param.cellMap, testTileInfos, _param.tileMap);
                _AdjustTerrainPosition(_param.pivotX, _param.pivotY);
                _param = null;
                _reservedCreationCoroutine = null;
                yield break;
            }
            else
            {
                CreateTerrain(_param.width, _param.height, _param.cellMap);
                _AdjustTerrainPosition(_param.pivotX, _param.pivotY);
            }
        }

        // 그 다음에 tilemap을 그린다.
        do
        {
            yield return null;
        } while (null != _param && !_param.isTileLoaded);

        if (null != _param)
        {
            CreateTerrain(_param.width, _param.height, _param.cellMap, _param.tileMaterials, _param.tileMap);
            _AdjustTerrainPosition(_param.pivotX, _param.pivotY);
            _param = null;
        }
        _reservedCreationCoroutine = null;
    }

    public string GetTerrainCellMap()
    {
        int totalLength = terrainWidth * terrainHeight;
        System.Text.StringBuilder map = new System.Text.StringBuilder(totalLength);
        for (int i = 0; i < totalLength; i++)
        {
            char str = (char)(GetOnlyCellIndex(cellArr[i]) + 'A');
            map.Append(str);
        }
        return map.ToString();
    }
    public string GetTerrainTileMap()
    {
        int totalLength = terrainWidth * terrainHeight;
        System.Text.StringBuilder map2 = new System.Text.StringBuilder(totalLength);
        for (int i = 0; i < totalLength; i++)
        {
            char str = tileArr[i] == TILE_NONE ? '0' : (char)(tileArr[i] + 'A');
            map2.Append(str);
        }
        return map2.ToString();
    }
    public List<string> GetTerrainTileList()
    {
        List<string> list = new List<string>();
        if (null != tileMaterials)
        {
            for (int i = 0; i < tileMaterials.Count; i++)
                list.Add(tileMaterials[i].tileItemID);
        }
        return list;
    }
    //bool temp = false;
    // width , height는 254 이하여야 한다. (그래야 마지막 점이 255가 됨)
    public void CreateTerrain(int width, int height, string cellMap, List<TileInfo> tileMaterials = null, string tileMap = null)
    {
        testTileInfos = tileMaterials;

        Debug.Log("CreateTerrain - " + width + "," + height);
        foreach (var d in groupMap)
            d.Value.Reset(baseMaterialList, propInfo);
        foreach (var d in tileGroupMap)
            d.Value.Reset(testTileInfos, propInfo);
        groupMap.Clear();
        tileGroupMap.Clear();

        //isTerrainChanged = false;
        baseMaterialList = new List<TileInfo>();
        for (int i = 0; i < materialList.Length; i++)
            baseMaterialList.Add(new TileInfo("0", materialList[i]));
        cellArr = new byte[width * height];
        tileArr = new byte[width * height];
        //for (int i = 0; i < width*height; i++)
        //  cellArr[i] = CELL_LAND; 

        byte[] convertMap = new byte[CELL_WATER_SMOOTH10 + 1];
        for (int i = 0; i <= CELL_WATER_SMOOTH10; i++)
        {
            int ind = ((i - 1) % 4) + 1;
            if (ind == 3)
                convertMap[i] = CELL_LAND;
            else if (ind == 4)
                convertMap[i] = CELL_WATER;
            else
                convertMap[i] = (byte)i;
        }

        char[] arr2 = cellMap.ToCharArray();
        for (int i = 0; i < arr2.Length && i < cellArr.Length; i++)
        {
            // todo : smooth를 우선 제거하자. 나중에 다시 넣자.
            cellArr[i] = convertMap[(byte)((int)arr2[i] - (int)'A')];
        }

        bool tileOk = false;
        if (!string.IsNullOrEmpty(tileMap))
        {
            char[] arr = tileMap.ToCharArray();
            if (arr.Length == cellMap.Length)
            {
                tileOk = true;
                for (int i = 0; i < arr.Length && i < tileArr.Length; i++)
                    tileArr[i] = arr[i] == '0' ? TILE_NONE : (byte)((int)arr[i] - (int)'A');
            }
        }
        if (!tileOk)
        {
            for (int i = 0; i < width * height; i++)
                tileArr[i] = TILE_NONE;
        }

        this.tileMaterials = testTileInfos;
        if (this.tileMaterials == null)
            this.tileMaterials = new List<TileInfo>();

        //int realWidth, realHeight;
        //cellArr = ConvertToRealCell(cellArr, width, height, out realWidth, out realHeight);
        //width = realWidth;
        //height = realHeight;

        //CheckCellArr(cellArr, width, height);
        terrainWidth = width;
        terrainHeight = height;


        //if (!temp)
        //{
        //    temp = true;
        //    ResetTerrain();
        //    return;
        //}

#if UNITY_EDITOR
        if (width >= 255 || height >= 255)
        {
            Debug.LogError("CreateTerrain - width or height must be under 255.");
            return;
        }
#endif
        if (newBlock == null)
        {
            newBlock = new GameObject("terrainFloorBlock");
            newBlock.transform.SetParent(transform);
            newBlock.transform.localPosition = Vector3.zero;
            newBlock.transform.localRotation = Quaternion.identity;
            newBlock.transform.localScale = Vector3.one;

            CreateBasePlane();
        }

        RefreshTerrain();
    }

    private void _AdjustTerrainPosition(int pX, int pZ)
    {
        newBlock.transform.position = new Vector3(-(pX * GROUP_UNIT_X * GetCellFullWidth()), 0f, -(pZ * GROUP_UNIT_Y * GetCellFullHeight()));
        pivotX = pX;
        pivotY = pZ;
    }

    public void InitializeTerrain()
    {
        int width = terrainWidth;
        int height = terrainHeight;
        cellArr = new byte[width * height];
        tileArr = new byte[width * height];

        int cellCount = width * height;
        for (int index = 0; index < cellCount; index++)
        {
            cellArr[index] = CELL_LAND;
            tileArr[index] = TILE_NONE;
        }

        isTerrainChanged = true;
    }

    protected virtual void CreateBasePlane()
    {

    }

    public void ClearTerrain()
    {
        foreach (var d in groupMap)
            d.Value.Reset(baseMaterialList, propInfo);
        foreach (var d in tileGroupMap)
            d.Value.Reset(tileMaterials, propInfo);

        if (newBlock != null)
        {
            GameObject.Destroy(newBlock);
            newBlock = null;
        }
    }
    protected virtual void RefreshTerrain()
    {
    }

    protected bool editMode = false;
    public virtual void SetEditMode(bool on)
    {
        editMode = on;
    }
    public bool ModifyTile(byte x, byte y, byte tileType, bool refreshTerrain = true)
    {
        int index = x + y * terrainWidth;
        byte _cell = GetOnlyCellIndex(cellArr[index]);
        byte floor = GetFloor(_cell);
        byte cellType = GetBaseCellOnFloor(floor, _cell, x, y);
        // cellType == CELL_SMOOTH 이면 바로 tile을 그리고 smooth를 처리할 것이다.
        if (!(cellType == CELL_LAND || cellType == CELL_SMOOTH) && tileType != TILE_NONE)
            return false;

        byte curType = tileArr[index];
        if (tileType == TILE_SMOOTH && (curType == TILE_NONE || !CanTileSmooth(tileArr, terrainWidth, terrainHeight, x, y)))
            return false;
        // cellType == CELL_SMOOTH 이면 바로 tile을 그리고 smooth를 처리할 것인데, 만일 smooth를 할 수 없는 상황이면 실패시킨다.
        if (cellType == CELL_SMOOTH && tileType != TILE_NONE && !CanTileSmooth(tileArr, terrainWidth, terrainHeight, x, y))
            return false;

        if (curType != tileType)
        {
            if (tileType == TILE_SMOOTH)
            {
                byte tileIndex = (curType != TILE_NONE && curType >= TILE_SMOOTH) ? (byte)(curType - TILE_SMOOTH) : curType;
                tileArr[index] = (byte)(tileIndex + tileType);
            }
            else
            {
                if (cellType == CELL_SMOOTH && tileType != TILE_NONE)
                    tileArr[index] = (byte)(tileType + TILE_SMOOTH);
                else
                    tileArr[index] = tileType;
            }

            //CLogger.Log("tile : " + curType + "=>" + tileArr[index]);
            // 주변의 8개 cell에 대한 smooth check도 다시 한다.
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int tx = x + dx;
                    int ty = y + dy;
                    if (0 <= tx && tx < terrainWidth
                        && 0 <= ty && ty < terrainHeight)
                    {
                        int ind = tx + ty * terrainWidth;
                        byte thisType = tileArr[ind];
                        if (thisType != TILE_NONE)
                        {
                            bool changed = false;
                            if (thisType >= TILE_SMOOTH && !CanTileSmooth(tileArr, terrainWidth, terrainHeight, (byte)tx, (byte)ty))
                            {
                                byte thisCell = GetOnlyCellIndex(cellArr[ind]);
                                byte thisFloor = GetFloor(thisCell);
                                byte thisCellType = GetBaseCellOnFloor(thisFloor, thisCell, (byte)tx, (byte)ty);
                                if (thisCellType == CELL_SMOOTH)
                                    tileArr[ind] = TILE_NONE;
                                else
                                    tileArr[ind] = (byte)(thisType - TILE_SMOOTH);
                                changed = true;
                            }

                            if (changed)
                                ResetCellGroup(floor, (byte)tx, (byte)ty, true);
                        }
                    }
                }
            }

            ResetCellGroup(floor, x, y, true);
            if (refreshTerrain)
                RefreshTerrain();
            isTerrainChanged = true;
            return true;
        }
        return false;
    }
    bool CanChangeCell(byte current, byte targetBaseCell, out byte result, int targetFloor = -1)
    {
        result = targetBaseCell;
        byte floor = GetFloor(current);

        // cube쌓기를 하는데, 특정 층으로 모두 올릴 경우에만 따로 처리
        if ((targetBaseCell == CELL_LAND || targetBaseCell == CELL_NONE) && targetFloor >= 0)
        {
            int newFloor = Mathf.Max(0, Mathf.Min(MAX_FLOOR - 1, targetFloor));
            result = (byte)(CELL_LAND + TOTAL_CELL_BASE_TYPE * newFloor);
            return true;
        }

        switch (current)
        {
            case CELL_NONE:
                return (targetBaseCell == CELL_LAND || targetBaseCell == CELL_WATER);
            case CELL_LAND:
                {
                    if (targetBaseCell == CELL_LAND)
                    {
                        result = CELL_LAND2;
                        return true;
                    }
                    if (!canRemoveFirstFloor && targetBaseCell == CELL_NONE)
                        return false;
                    return (targetBaseCell == CELL_SMOOTH || targetBaseCell == CELL_WATER || targetBaseCell == CELL_NONE);
                }
            case CELL_WATER:
                {
                    if (targetBaseCell == CELL_SMOOTH)
                    {
                        result = CELL_WATER_SMOOTH;
                        return true;
                    }
                    if (!canRemoveFirstFloor && targetBaseCell == CELL_NONE)
                        return false;
                    return (targetBaseCell == CELL_LAND || targetBaseCell == CELL_NONE);
                }
            case CELL_SMOOTH:
            case CELL_WATER_SMOOTH:
                {
                    if (!canRemoveFirstFloor && targetBaseCell == CELL_NONE)
                        return false;
                    return (targetBaseCell == CELL_LAND || targetBaseCell == CELL_WATER || targetBaseCell == CELL_NONE);
                }
            default:
                {
                    byte baseCell = (byte)(current - TOTAL_CELL_BASE_TYPE * floor);
                    if (baseCell == CELL_LAND)
                    {
                        if (floor < MAX_FLOOR - 1 && targetBaseCell == CELL_LAND)
                        {
                            // 한층 올림
                            result = (byte)(current + TOTAL_CELL_BASE_TYPE);
                            return true;
                        }
                        else if (targetBaseCell == CELL_SMOOTH || targetBaseCell == CELL_WATER)
                        {
                            result = GetCellOfFloor(floor, targetBaseCell);
                            return true;
                        }
                        else if (targetBaseCell == CELL_NONE)
                        {
                            // 한층 내림
                            result = (byte)(current - TOTAL_CELL_BASE_TYPE);
                            return true;
                        }
                    }
                    else if (baseCell == CELL_WATER)
                    {
                        if (targetBaseCell == CELL_SMOOTH)
                        {
                            result = GetCellOfFloor(floor, CELL_WATER_SMOOTH);
                            return true;
                        }
                        else if (targetBaseCell == CELL_LAND)
                        {
                            result = GetCellOfFloor(floor, CELL_LAND);
                            return true;
                        }
                        else if (targetBaseCell == CELL_NONE)
                        {
                            result = GetCellOfFloor((byte)(floor - 1), CELL_LAND);
                            return true;
                        }
                    }
                    else
                    {
                        if (targetBaseCell == CELL_LAND || targetBaseCell == CELL_WATER)
                        {
                            result = GetCellOfFloor(floor, targetBaseCell);
                            return true;
                        }
                        else if (targetBaseCell == CELL_NONE)
                        {
                            result = GetCellOfFloor((byte)(floor - 1), CELL_LAND);
                            return true;
                        }
                    }
                }
                break;
        }

        return false;
    }
    List<int> tempGroup = new List<int>(4);

    public byte GetCellType(byte x, byte y)
    {
        int index = x + y * terrainWidth;
        if (index >= 0 && index <= cellArr.Length)
            return GetOnlyCellIndex(cellArr[index]);

        return 0;
    }

    bool IsInTerrain(Vector3 localPos, float EPSILON = 0.05f)
    {
        return -EPSILON <= localPos.x && localPos.x <= GetCellFullWidth() * terrainWidth + EPSILON
            && -EPSILON <= localPos.z && localPos.z <= GetCellFullHeight() * terrainHeight + EPSILON
            && -EPSILON - cellY <= localPos.y && localPos.y <= cellY * MAX_FLOOR + EPSILON;
    }

    public bool PickCell(Vector3 startWorld, Vector3 directionWorld, out byte floor, out byte x, out byte y)
    {
        floor = 0;
        x = 0;
        y = 0;
        // 전체영역과 만나는 점을 찾는다. 0~2개가 가능하다.
        Vector3 localStart = newBlock.transform.InverseTransformPoint(startWorld);
        Vector3 localDir = newBlock.transform.InverseTransformDirection(directionWorld);
        Ray ray = new Ray(localStart, localDir);

        // 디버그용으로 Ray를 그립니다.
        DrawRayDebug(localStart, localDir, 100f, Color.red, 5f);

        // 우선 6면과 만나는 점들을 찾아서 그중에 제일 가까운 점을 찾는다.
        Plane[] planes = new Plane[6];
        planes[0] = new Plane(new Vector3(1, 0, 0), 0);
        planes[1] = new Plane(new Vector3(1, 0, 0), -terrainWidth * GetCellFullWidth());
        planes[2] = new Plane(new Vector3(0, 1, 0), cellY);
        planes[3] = new Plane(new Vector3(0, 1, 0), -cellY * MAX_FLOOR);
        planes[4] = new Plane(new Vector3(0, 0, 1), 0);
        planes[5] = new Plane(new Vector3(0, 0, 1), -terrainHeight * GetCellFullHeight());

        Bounds terrainBounds = GetTerrainBounds_WithY();
        DrawTerrainBoundsDebug(terrainBounds, Color.blue, 5f);

        bool hasPoint = false;
        float minRatio = 10000000000;
        float maxRatio = -10000000000;
        for (int i = 0; i < planes.Length; i++)
        {
            float ratio;
            if (planes[i].Raycast(ray, out ratio))
            {
                Vector3 hitPoint = ray.GetPoint(ratio);
                Vector3 pt = ray.origin + ray.direction * ratio;

                // 디버그용으로 Plane과의 교차점을 그립니다.
                Debug.DrawLine(ray.origin, hitPoint, Color.green, 5f);

                if (IsInTerrain(pt))
                {
                    hasPoint = true;
                    minRatio = Mathf.Min(minRatio, ratio);
                    maxRatio = Mathf.Max(maxRatio, ratio);

                    // 디버그용으로 hitPoint를 저장합니다.
                    debugHitPoint = hitPoint;
                }
            }
        }

        pickPoses.Clear();

        if (hasPoint && maxRatio > 0)
        {
            Vector3 startPt = ray.origin + ray.direction * Mathf.Max(0, minRatio);
            Vector3 endPt = ray.origin + ray.direction * maxRatio;
            float distance = (endPt - startPt).magnitude;
            // 큐브 1개 거리의 0.3 만큼씩만 이동하는 것으로 하자. 좀더 detail하게 체크해야 정확도가 올라간다.
            float oneCellUnitDist = new Vector3(GetCellFullWidth(), cellY, GetCellFullHeight()).magnitude * 0.5f;
            int maxStep = Mathf.CeilToInt(distance / oneCellUnitDist);
            Dictionary<int, bool> checkMap = new Dictionary<int, bool>();

            Vector3 basePt = startPt;
            basePt.y = 0;
            int alterFloor = -1;
            int alterX = -1;
            int alterZ = -1;
            Vector3 alterClosetPos = Vector3.zero;
            for (int i = 0; i <= maxStep; i++)
            {
                Vector3 curPt = startPt + ray.direction * i * oneCellUnitDist;
                int _floor, _x, _z;

                pickPoses.Add(curPt);

                if (CheckPoint(curPt, checkMap, out _floor, out _x, out _z))
                {
                    Vector3 pickPos = GetCenterPos(_x, _z);
                    determinedPos = pickPos;

                    // 저장해둔 정보중에 현재것보다 가까운게 있다면, 그것을 return한다.
                    // (비교시에는 y = 0으로 비교한다.즉 높이는 무시한다.)
                    if (alterFloor >= 0 && (basePt - pickPos).sqrMagnitude > (basePt - alterClosetPos).sqrMagnitude)
                    {
                        x = (byte)alterX;
                        floor = (byte)alterFloor;
                        y = (byte)alterZ;
                    }
                    else
                    {
                        x = (byte)_x;
                        floor = (byte)_floor;
                        y = (byte)_z;
                    }
                    return true;
                }
                // 현재것과 다음것 사이에 y up 평면을 지나치는지 확인하자.
                if (i < maxStep)
                {
                    float offsetY = ray.direction.y > 0 ? 1 : -1;
                    Plane tempPlane = new Plane(Vector3.up, -(_floor + offsetY) * cellY);
                    float hitRatio;
                    if (tempPlane.Raycast(ray, out hitRatio))
                    {
                        // 살짝 offset을 줘서 알맞은 cell이 나오게 한다.
                        if (CheckPoint(ray.origin + ray.direction * hitRatio + Vector3.up * offsetY * linkY, checkMap, out _floor, out _x, out _z))
                        {
                            // 가장 가까운 것을 keep해둔다. (비교시에는 y = 0으로 비교한다.즉 높이는 무시한다.)
                            Vector3 pickPos = GetCenterPos(_x, _z);
                            lastedPos = pickPos;

                            if (alterFloor < 0 || (basePt - pickPos).sqrMagnitude < (basePt - alterClosetPos).sqrMagnitude)
                            {
                                alterFloor = _floor;
                                alterX = _x;
                                alterZ = _z;
                                alterClosetPos = pickPos;
                            }
                        }
                    }
                }
            }

            if (alterFloor >= 0)
            {
                x = (byte)alterX;
                floor = (byte)alterFloor;
                y = (byte)alterZ;
                return true;
            }
        }
        return false;
    }

    Vector3 GetCenterPos(int _x, int _z)
    {
        return new Vector3(((float)_x + 0.5f) * GetCellFullWidth(), 0, ((float)_z + 0.5f) * GetCellFullHeight());
    }
    bool CheckPoint(Vector3 curPt, Dictionary<int, bool> checkMap, out int _floor, out int _x, out int _z)
    {
        _x = Mathf.FloorToInt(curPt.x / GetCellFullWidth());
        _floor = Mathf.FloorToInt(curPt.y / cellY) + 1; // y는 -cellY부터 시작되므로.
        _z = Mathf.FloorToInt(curPt.z / GetCellFullHeight());
        if (0 <= _x && _x < terrainWidth
            && 0 <= _z && _z < terrainHeight
            && 0 <= _floor && _floor < MAX_FLOOR)
        {
            int ii = _z + (_floor << 8) + (_z << 16);
            if (!checkMap.ContainsKey(ii))
            {
                checkMap[ii] = true;

                int index = _x + _z * terrainWidth;
                byte curType = GetOnlyCellIndex(cellArr[index]);
                byte curFloor = GetFloor(curType);
                // 현재 있는 cell인지 체크해서, 맞으면 그것을 return한다.
                if (curFloor >= _floor)
                    return true;
            }
        }
        return false;
    }

    public const int BLOCK = 1;
    // 2*2 cell씩 바꾼다.(이거 이제는 안함)
    public bool ModifyCell(byte baseCellType, byte x, byte y)
    {
        if (baseCellType == CELL_SMOOTH || baseCellType == CELL_WATER_SMOOTH)
        {
            Debug.LogWarning("ModifyCell - CELL_SMOOTH/CELL_WATER_SMOOTH is not implemented!!");
            return false;
        }
        bool isModifyCell = false;
        byte sx = (byte)((x / BLOCK) * BLOCK);
        byte sy = (byte)((y / BLOCK) * BLOCK);
        //for (byte _y = sy; _y < sy + BLOCK; _y++)
        {
            //for (byte _x = sx; _x < sx + BLOCK; _x++)
            {
                if (_ModifyCellBody(baseCellType, sx, sy))
                    isModifyCell = true;
            }
        }

        if (isModifyCell)
            RefreshTerrain();
        return isModifyCell;
    }

    public bool ModifyCell(byte baseCellType, byte fromTx, byte fromTy, byte toTx, byte toTy, byte targetFloor)
    {
        if (CELL_SMOOTH == baseCellType || CELL_WATER_SMOOTH == baseCellType)
        {
            Debug.LogWarning("ModifyCell - CELL_SMOOTH/CELL_WATER_SMOOTH is not implemented!!");
            return false;
        }
        bool isModifyCell = false;
        byte floor;
        for (byte _y = fromTy; _y < toTy + BLOCK; _y++)
        {
            for (byte _x = fromTx; _x < toTx + BLOCK; _x++)
            {
                floor = GetFloor(_x, _y);
                if (CELL_LAND == baseCellType)
                {
                    if (floor >= targetFloor)
                    {
                        continue;
                    }

                    if (_ModifyCellBody(baseCellType, _x, _y, targetFloor))
                    {
                        isModifyCell = true;
                    }
                }
                else if (CELL_NONE == baseCellType)
                {
                    if (floor <= targetFloor)
                    {
                        continue;
                    }

                    if (_ModifyCellBody(baseCellType, _x, _y, targetFloor))
                    {
                        isModifyCell = true;
                    }
                }
                else if (CELL_WATER == baseCellType)
                {
                    if (floor >= targetFloor)
                    {
                        continue;
                    }

                    if (_ModifyCellBody(baseCellType, _x, _y, targetFloor))
                    {
                        isModifyCell = true;
                    }
                }
            }
        }

        if (isModifyCell)
        {
            RefreshTerrain();
        }
        return isModifyCell;
    }

    public void RevertCell(string prevTerrainCellMap)
    {
        if (string.IsNullOrEmpty(prevTerrainCellMap))
        {
            return;
        }

        bool isModifyCell = false;
        byte prevFloor = 0;
        byte currFloor = 0;
        char cha;
        for (byte _y = 0; _y < terrainHeight; ++_y)
        {
            for (byte _x = 0; _x < terrainWidth; ++_x)
            {
                cha = prevTerrainCellMap[_x + _y * terrainWidth];
                prevFloor = GetFloor((byte)(cha - 'A'));
                currFloor = GetFloor(_x, _y);

                if (prevFloor < currFloor)
                {
                    if (_ModifyCellBody(CELL_NONE, _x, _y, prevFloor))
                    {
                        isModifyCell = true;
                    }
                }
                else if (prevFloor > currFloor)
                {
                    if (_ModifyCellBody(CELL_LAND, _x, _y, prevFloor))
                    {
                        isModifyCell = true;
                    }
                }
            }

        }

        if (isModifyCell)
        {
            RefreshTerrain();
        }
    }

    bool _ModifyCellBody(byte baseCellType, byte x, byte y, int targetFloor = -1)
    {
        int index = x + y * terrainWidth;
        byte curType = GetOnlyCellIndex(cellArr[index]);
        byte originalFloor = GetFloor(curType);
        byte change;
        if (!CanChangeCell(curType, baseCellType, out change, targetFloor))
            return false;
        byte targetCell = change;
        //CLogger.Log("change : " + curType + "->" + change);
        byte floor = GetFloor(targetCell);

        byte standardCellType = GetBaseCellOnFloor(floor, targetCell, x, y);
#if USE_SMOOTH
        if (standardCellType == CELL_SMOOTH && !CanLandSmooth(floor, cellArr, terrainWidth, terrainHeight, x, y))
            return false;

        if (standardCellType == CELL_WATER_SMOOTH && !CanWaterSmooth(floor, cellArr, terrainWidth, terrainHeight, x, y))
            return false;
#endif

        if (curType != targetCell)
        {
            // 땅을 바꾸면, 위에 있던 tile도 지운다.
            if (tileArr[index] != TILE_NONE)
            {
                if (baseCellType == CELL_SMOOTH)
                    ModifyTile(x, y, TILE_SMOOTH, false);
                else
                    ModifyTile(x, y, TILE_NONE, false);
            }

            cellArr[index] = targetCell;

#if USE_SMOOTH
            // 주변의 8개 cell에 대한 smooth check도 다시 한다.
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int tx = x + dx;
                    int ty = y + dy;
                    if (0 <= tx && tx < terrainWidth
                        && 0 <= ty && ty < terrainHeight)
                    {
                        int ind = tx + ty * terrainWidth;
                        byte thisType = GetOnlyCellIndex(cellArr[ind]);
                        byte thisStandardCellType = GetBaseCellOnFloor(floor, thisType, (byte)tx, (byte)ty);
                        if (thisStandardCellType != CELL_NONE)
                        {
                            bool changed = false;
                            if (thisStandardCellType == CELL_SMOOTH && !CanLandSmooth(floor, cellArr, terrainWidth, terrainHeight, (byte)tx, (byte)ty))
                            {
                                //Debug.LogWarning("CELL_SMOOTH - > CELL_LAND " + tx + "," + ty);
                                cellArr[ind] = GetCellOfFloor(floor, CELL_LAND);
                                changed = true;
                            }
                            else if (thisStandardCellType == CELL_WATER_SMOOTH && !CanWaterSmooth(floor, cellArr, terrainWidth, terrainHeight, (byte)tx, (byte)ty))
                            {
                                //Debug.LogWarning("CELL_WATER_SMOOTH - > CELL_WATER " + tx + "," + ty);
                                cellArr[ind] = GetCellOfFloor(floor, CELL_WATER);
                                changed = true;
                            }

                            if (changed)
                            {
                                ResetCellGroup(floor, (byte)tx, (byte)ty, false);
                                if (floor > 0)
                                {
                                    for (int f = 0; f <= floor - 1; f++)
                                        ResetCellGroup((byte)f, (byte)tx, (byte)ty, false);
                                }
                            }
                        }
                    }
                }
            }
#endif

            ResetCellGroup(floor, x, y, false);
            if (originalFloor > floor)
                ResetCellGroup(originalFloor, x, y, false);
            if (floor > 0)
            {
                for (int f = 0; f <= floor - 1; f++)
                    ResetCellGroup((byte)f, x, y, false);
            }
            isTerrainChanged = true;

            return true;
        }
        return false;
    }

    void ResetCellGroup(byte floor, byte x, byte y, bool tile = false)
    {
        // 4 귀퉁이들의 group을 reset하고 terrain을 refresh한다.
        tempGroup.Clear();
        tempGroup.Add(GetCellGroupIndex(floor, (byte)(x + 1), (byte)(y + 1)));
        if (x > 0)
            tempGroup.Add(GetCellGroupIndex(floor, (byte)(x - 1), (byte)(y + 1)));
        if (y > 0)
            tempGroup.Add(GetCellGroupIndex(floor, (byte)(x + 1), (byte)(y - 1)));
        if (x > 0 && y > 0)
            tempGroup.Add(GetCellGroupIndex(floor, (byte)(x - 1), (byte)(y - 1)));

        Dictionary<int, CellGroup> thisGroupMap = tile ? tileGroupMap : groupMap;
        List<TileInfo> matList = tile ? tileMaterials : baseMaterialList;
        for (int i = 0; i < tempGroup.Count; i++)
        {
            CellGroup curGroup;
            if (thisGroupMap.TryGetValue(tempGroup[i], out curGroup))
                curGroup.Reset(matList, propInfo);
        }

        // tile의 경우, 위에 생성된 grass prop같은 것을 없앤다.
        if (tile)
        {
            int curIndex = GetCellGroupIndex(floor, x, y);
            CellGroup group;
            if (groupMap.TryGetValue(curIndex, out group))
                group.RemoveTileProps(propInfo, CellIndex(0, x, y));
        }
    }

    protected virtual Vector3 GetPoint(byte floor, byte x, byte y)
    {
        return new Vector3((float)x * GetCellFullWidth(), floor * cellY, (float)y * GetCellFullHeight());
    }
    protected virtual Vector3 GetPoint(byte floor, float x, float y)
    {
        return new Vector3(x * GetCellFullWidth(), floor * cellY, y * GetCellFullHeight());
    }
    public Vector3 GetCellCenterPos(Vector3 posWorld)
    {
        byte x, y;
        if (GetCellIndex(posWorld, out x, out y))
        {
            Vector3 stPos = GetPoint(0, x, y);
            stPos += new Vector3(GetCellFullWidth(), 0, GetCellFullHeight()) * 0.5f;
            return newBlock.transform.TransformPoint(stPos);
        }
        return posWorld;
    }
    public Bounds GetCellBounds(byte x, byte y)
    {
        Vector3 stPos = GetPoint(0, x, y);
        byte floor = GetFloor(cellArr[x + y * terrainWidth]);
        if (floor < 0)
            floor = 0;
        stPos += new Vector3(GetCellFullWidth(), floor * cellY, GetCellFullHeight()) * 0.5f;
        Vector3 center = newBlock.transform.TransformPoint(stPos);
        return new Bounds() { center = center, size = new Vector3(GetCellFullWidth(), floor * cellY, GetCellFullHeight()) };
    }
    public Bounds GetCellTopBounds(byte x, byte y)
    {
        Vector3 stPos = GetPoint(0, x, y);
        byte floor = GetFloor(cellArr[x + y * terrainWidth]);
        if (floor < 0)
            floor = 0;
        stPos += new Vector3(GetCellFullWidth() * 0.5f, floor * cellY, GetCellFullHeight() * 0.5f);
        Vector3 center = newBlock.transform.TransformPoint(stPos);
        return new Bounds() { center = center, size = new Vector3(GetCellFullWidth(), 0, GetCellFullHeight()) };
    }
    public Bounds GetCellBounds(byte firstX, byte firstY, byte lastX, byte lastY)
    {
        byte fromTx = (byte)Mathf.Min(firstX, lastX);
        byte fromTy = (byte)Mathf.Min(firstY, lastY);
        byte toTx = (byte)Mathf.Max(firstX, lastX);
        byte toTy = (byte)Mathf.Max(firstY, lastY);

        float x = (fromTx + toTx) * 0.5f;
        float y = (fromTy + toTy) * 0.5f;
        Vector3 stPos = GetPoint(0, x, y);
        byte floor = GetFloor(firstX, firstY);
        if (floor < 0)
        {
            floor = 0;
        }
        stPos += new Vector3(GetCellFullWidth(), floor * cellY, GetCellFullHeight()) * 0.5f;
        Vector3 center = newBlock.transform.TransformPoint(stPos);
        x = toTx - fromTx + 1;
        y = toTy - fromTy + 1;
        return new Bounds() { center = center, size = new Vector3(GetCellFullWidth() * x, floor * cellY, GetCellFullHeight() * y) };
    }
    public Vector3 GetSelectedCellCenterPos(byte x, byte y)
    {
        int index = x + y * terrainWidth;
        byte _cell = GetOnlyCellIndex(cellArr[index]);
        byte floor = GetFloor(_cell);
        Vector3 stPos = GetPoint(floor, x, y);
        stPos += new Vector3(GetCellFullWidth(), _cell == 0 ? (cellY * -1) : 0, GetCellFullHeight());
        return newBlock.transform.TransformPoint(stPos);
    }
    bool GetCellIndex(Vector3 posWorld, out byte x, out byte y, bool snap = false, bool limitMinMax = false)
    {
        x = 0;
        y = 0;
        if (newBlock == null)
            return false;
        Vector3 localPos = newBlock.transform.InverseTransformPoint(posWorld);
        int ix = Mathf.FloorToInt(localPos.x / (GetCellFullWidth()));
        int iy = Mathf.FloorToInt(localPos.z / (GetCellFullHeight()));
        if (limitMinMax)
        {
            ix = Mathf.Max(0, Mathf.Min(terrainWidth - 1, ix));
            iy = Mathf.Max(0, Mathf.Min(terrainHeight - 1, iy));
        }
        if (0 <= ix && ix < terrainWidth && 0 <= iy && iy < terrainHeight)
        {
            if (snap)
            {
                ix = (ix / BLOCK) * BLOCK;
                iy = (iy / BLOCK) * BLOCK;
            }
            x = (byte)ix;
            y = (byte)iy;
            return true;
        }
        return false;
    }

    GameObject selectionMark;
    GameObject brushMark;

    GameObject touchMark;
    Coroutine hideRoutine;
    public GameObject ShowTouchMark(bool show, Vector3 posWorld, float hideDelay = 1)
    {
        if (touchMark == null)
        {
            touchMark = GameObject.Instantiate(touchMarkPrefab);
            touchMark.transform.SetParent(newBlock.transform);
            touchMark.transform.localRotation = Quaternion.identity;
            touchMark.transform.localScale = Vector3.one;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        touchMark.ShowObject(show);
        if (show)
        {
            touchMark.transform.position = posWorld;
            if (hideDelay > 0)
                hideRoutine = StartCoroutine(HideObject(touchMark, hideDelay));
        }
        return touchMark;
    }
    IEnumerator HideObject(GameObject obj, float hideDelay)
    {
        yield return new WaitForSeconds(hideDelay);
        obj.ShowObject(false);
    }
    private void OnDisable()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }
    private void OnDestroy()
    {
        if (null != _param)
        {
            _param.Reset();
            _param = null;
        }
        if (null != _reservedCreationCoroutine)
        {
            StopCoroutine(_reservedCreationCoroutine);
            _reservedCreationCoroutine = null;
        }
        //if (null != _helper)
        //    AssetLoadHelper.Destroy(ref _helper);
    }
    protected bool isTerrainChanged = false;
    public bool IsTerrainChanged()
    {
        return isTerrainChanged;
    }
    public void ResetChanged()
    {
        isTerrainChanged = false;
    }

    public float GetAreaHeight(Vector3 minWorld, Vector3 maxWorld)
    {
        if (newBlock == null)
            return transform.position.y;
        byte x1, y1, x2, y2;
        GetCellIndex(minWorld, out x1, out y1, false, true);
        GetCellIndex(maxWorld, out x2, out y2, false, true);

        byte minX = (byte)Mathf.Min(x1, x2);
        byte minY = (byte)Mathf.Min(y1, y2);
        byte maxX = (byte)Mathf.Max(x1, x2);
        byte maxY = (byte)Mathf.Max(y1, y2);
        int maxFloor = 0;
        for (byte y = minY; y <= maxY; y++)
        {
            for (byte x = minX; x <= maxX; x++)
            {
                maxFloor = Mathf.Max(maxFloor, GetFloor(cellArr[x + y * terrainWidth]));
            }
        }
        return maxFloor * cellY + newBlock.transform.position.y;
    }
    protected virtual void Awake()
    {
        _InitProp();
    }

    protected void _InitProp()
    {
        grassPropInfo1 = -1;
        grassPropInfo2 = -1;
        waterfallPropInfo = -1;
        if (propInfo != null)
        {
            for (int i = 0; i < propInfo.Length; i++)
            {
                if (propInfo[i] != null)
                {
                    propInfo[i].index = i;
                    if (propInfo[i].type == TilePropType.GRASS)
                    {
                        if (grassPropInfo1 >= 0)
                            grassPropInfo2 = i;
                        else
                            grassPropInfo1 = i;
                    }
                    else if (propInfo[i].type == TilePropType.WATERFALL)
                        waterfallPropInfo = i;
                }
            }
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearTerrain();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTerrain();
        }
    }

    private Vector3? debugHitPoint = null;
    private Vector3? determinedPos = null;
    private Vector3? lastedPos = null;
    private List<Vector3> pickPoses = new List<Vector3>();

    private void OnDrawGizmos()
    {
        if (debugHitPoint.HasValue)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(debugHitPoint.Value, 0.1f);
        }

        if (pickPoses.Count > 0)
        {
            Gizmos.color = Color.green;
            foreach (var pos in pickPoses)
            {
                Gizmos.DrawSphere(pos, 0.2f);
            }
        }

        if(determinedPos.HasValue && lastedPos.HasValue && determinedPos.Value == lastedPos.Value)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lastedPos.Value + (Vector3.left * 0.5f), 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(determinedPos.Value - (Vector3.left * 0.5f), 0.5f);
        }
        else
        {
            if (determinedPos.HasValue)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(determinedPos.Value, 0.5f);
            }

            if (lastedPos.HasValue)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(lastedPos.Value, 0.5f);
            }
        }
    }

    private void DrawRayDebug(Vector3 start, Vector3 direction, float length, Color color, float duration)
    {
        Debug.DrawRay(start, direction * length, color, duration);
    }

    private void DrawTerrainBoundsDebug(Bounds bounds, Color color, float duration)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        // 8개의 꼭짓점을 계산합니다.
        Vector3[] corners = new Vector3[8];
        corners[0] = center + new Vector3(-extents.x, -extents.y, -extents.z);
        corners[1] = center + new Vector3(extents.x, -extents.y, -extents.z);
        corners[2] = center + new Vector3(extents.x, -extents.y, extents.z);
        corners[3] = center + new Vector3(-extents.x, -extents.y, extents.z);
        corners[4] = center + new Vector3(-extents.x, extents.y, -extents.z);
        corners[5] = center + new Vector3(extents.x, extents.y, -extents.z);
        corners[6] = center + new Vector3(extents.x, extents.y, extents.z);
        corners[7] = center + new Vector3(-extents.x, extents.y, extents.z);

        // 아래 면
        Debug.DrawLine(corners[0], corners[1], color, duration);
        Debug.DrawLine(corners[1], corners[2], color, duration);
        Debug.DrawLine(corners[2], corners[3], color, duration);
        Debug.DrawLine(corners[3], corners[0], color, duration);

        // 위 면
        Debug.DrawLine(corners[4], corners[5], color, duration);
        Debug.DrawLine(corners[5], corners[6], color, duration);
        Debug.DrawLine(corners[6], corners[7], color, duration);
        Debug.DrawLine(corners[7], corners[4], color, duration);

        // 수직선
        Debug.DrawLine(corners[0], corners[4], color, duration);
        Debug.DrawLine(corners[1], corners[5], color, duration);
        Debug.DrawLine(corners[2], corners[6], color, duration);
        Debug.DrawLine(corners[3], corners[7], color, duration);
    }


#endif
}