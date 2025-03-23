//#define USE_SMOOTH
using Colorverse;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 기본적인 cell들은 사각형 폴리곤 하나씩 가지고 있다.
// 하지만, 지형변경시에는 2*2 cell 씩 변경하게끔 했다.
public partial class CubeTerrain : WorldTerrain
{
    // todo : test
    public Material[] materialList;

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
    protected class TilePropInfo
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
    protected TilePropInfo[] propInfo;

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

    /*
    class Vertex
    {
        public Vector3[] points;
        public byte normalType;
        static Vector3[] tempNormals;
        static Vector2[] tempUVs;
        //
        //       +---+
        //      .|   |
        //   +---+---+
        //   |   | '
        //   +---+
        //   이렇게 block이 cross로 연결된 경우, link point는 2종류로 생긴다.
        // 기본 normal type이 아닌 경우에 대한 link point들. (대부분의 경우 null이다)

        public Vertex(VertexInfo info, byte floor, byte x, byte y, VertexInfo.NeighborInfo neighbor, int planeType = PLANE_LAND, int planeNormalType = -1)
        {
            Vector3 startPosition = VertexInfo.StartPoint(floor, x, y);
            if (planeType == PLANE_WATER || planeType == PLANE_WATER_SIDE || planeType == PLANE_WATER_SIDE2)
                startPosition.y -= waterDepth;
            else if (planeType == PLANE_WATER_WALL)
                startPosition.y -= waterPlaneDepth;
            int total = neighbor.onlyTop ? 1 : (neighbor.hasReverse ? PT_REV_LINK_START + TOTAL_LINK_POINT : PT_LINK_START + TOTAL_LINK_POINT);
            points = new Vector3[total];
            points[PT_TOP] = startPosition;
            this.normalType = neighbor.normalType;
            Vector3 normal = (NORMALS[normalType].z < 0 && neighbor.hasReverse) ? -NORMALS[normalType] : NORMALS[normalType];
            if (planeNormalType != -1)
                normal = (normal + NORMALS[planeNormalType]).normalized;

            if (tempNormals == null)
                tempNormals = new Vector3[PT_REV_LINK_START + TOTAL_LINK_POINT];
            if (tempUVs == null)
                tempUVs = new Vector2[PT_REV_LINK_START + TOTAL_LINK_POINT];

            tempNormals[PT_TOP] = Vector3.up;
            tempUVs[PT_TOP] = VertexInfo.UV(ref points[PT_TOP]);
            if (!neighbor.onlyTop)
            {
                //  * top
                //    *
                //      *
                //    *
                //  *
                //  
                //
                //
                //  * bottom
                const float XRATIO = 0.7f;
                const float UV_SCALE = 1f;
                points[PT_LINK_START] = points[PT_TOP] + normal * linkX * XRATIO;
                points[PT_LINK_START].y -= linkY * 0.5f;
                tempNormals[PT_LINK_START] = (normal + Vector3.up).normalized;
                float dist = (points[PT_TOP] - points[PT_LINK_START]).magnitude;
                tempUVs[PT_LINK_START] = tempUVs[PT_TOP] + new Vector2(normal.x, normal.z) * dist / cellY * UV_SCALE;

                points[PT_LINK_START + 1] = points[PT_TOP] + normal * linkX;
                points[PT_LINK_START + 1].y -= linkY;
                tempNormals[PT_LINK_START + 1] = normal;
                dist = (points[PT_TOP] - points[PT_LINK_START + 1]).magnitude;
                tempUVs[PT_LINK_START + 1] = tempUVs[PT_TOP] + new Vector2(normal.x, normal.z) * dist / cellY * UV_SCALE;

                points[PT_LINK_START + 2] = points[PT_TOP] + normal * linkX * XRATIO;
                points[PT_LINK_START + 2].y -= linkY * 1.5f;
                tempNormals[PT_LINK_START + 2] = (normal - Vector3.up).normalized;
                dist = (points[PT_TOP] - points[PT_LINK_START + 2]).magnitude;
                tempUVs[PT_LINK_START + 2] = tempUVs[PT_TOP] + new Vector2(normal.x, normal.z) * dist / cellY * UV_SCALE;

                points[PT_LINK_START + 3] = points[PT_TOP];
                points[PT_LINK_START + 3].y -= linkY * 2;
                tempNormals[PT_LINK_START + 3] = -Vector3.up;
                dist = (points[PT_TOP] - points[PT_LINK_START + 3]).magnitude;
                tempUVs[PT_LINK_START + 3] = tempUVs[PT_TOP] + new Vector2(normal.x, normal.z) * dist / cellY * UV_SCALE;


                if (neighbor.hasReverse)
                {
                    points[PT_REV_LINK_START] = points[PT_TOP] - normal * linkX * XRATIO;
                    points[PT_REV_LINK_START].y -= linkY * 0.5f;
                    tempNormals[PT_REV_LINK_START] = (-normal + Vector3.up).normalized;
                    dist = (points[PT_TOP] - points[PT_REV_LINK_START]).magnitude;
                    tempUVs[PT_REV_LINK_START] = tempUVs[PT_TOP] - new Vector2(normal.x, normal.z) * dist / cellY * UV_SCALE;

                    points[PT_REV_LINK_START + 1] = points[PT_TOP] - normal * linkX;
                    points[PT_REV_LINK_START + 1].y -= linkY;
                    tempNormals[PT_REV_LINK_START + 1] = -normal;
                    dist = (points[PT_TOP] - points[PT_REV_LINK_START + 1]).magnitude;
                    tempUVs[PT_REV_LINK_START + 1] = tempUVs[PT_TOP] - new Vector2(normal.x, normal.z) * dist / cellY * UV_SCALE;

                    points[PT_REV_LINK_START + 2] = points[PT_TOP] - normal * linkX * XRATIO;
                    points[PT_REV_LINK_START + 2].y -= linkY * 1.5f;
                    tempNormals[PT_REV_LINK_START + 2] = (-normal - Vector3.up).normalized;
                    dist = (points[PT_TOP] - points[PT_REV_LINK_START + 2]).magnitude;
                    tempUVs[PT_REV_LINK_START + 2] = tempUVs[PT_TOP] - new Vector2(normal.x, normal.z) * dist / cellY * UV_SCALE;

                    points[PT_REV_LINK_START + 3] = points[PT_TOP];
                    points[PT_REV_LINK_START + 3].y -= linkY * 2;
                    tempNormals[PT_REV_LINK_START + 3] = -Vector3.up;
                    dist = (points[PT_TOP] - points[PT_REV_LINK_START + 3]).magnitude;
                    tempUVs[PT_REV_LINK_START + 3] = tempUVs[PT_TOP] - new Vector2(normal.x, normal.z) * dist / cellY * UV_SCALE;
                }
            }

            for (byte i = 0; i < points.Length; i++)
            {
                //Vector2 uv = VertextInfo.UV(ref points[i]);
                //info.InsertVertex(ref points[i], ref tempNormals[i], ref uv, x, y, i);
                if (planeType == PLANE_LAND_SIDE)
                {
                    float h = points[0].y - points[points.Length - 1].y;
                    float cur = points[i].y - points[points.Length - 1].y;
                    Vector2 uv2 = new Vector2(0, h == 0 ? 0 : cur / h);
                    Vector2 uv = VertexInfo.UV(ref points[i]);
                    info.InsertVertex2(floor, ref points[i], ref tempNormals[i], ref uv, ref uv2, x, y, i);
                }
                else
                    info.InsertVertex(floor, ref points[i], ref tempNormals[i], ref tempUVs[i], x, y, i);
            }
        }
    }
    static readonly float[] POINT_OFFSET = new float[DIV]
    {
        0f,
        cellX * 0.015f * 1.5f,
        -cellX * 0.007f * 1.5f,
        cellX * 0.01f * 1.5f,
    };
    class VertexInfo
    {
        // key : terrain map index(y<<8+x)
        public Dictionary<int, Vertex> cellPointMap;
        public List<Vector3> vertexList;
        public List<Vector3> normalList;
        public List<Vector2> uvList;
        public List<Vector2> uv2List;
        // key : terrain map index + point index in Vertex class(y<<8+x + index<<16)
        // value : vertexList index
        public Dictionary<int, int> vertexIndexMap;
        public List<int> triangleList;

        public VertexInfo()
        {
            cellPointMap = new Dictionary<int, Vertex>();
            vertexList = new List<Vector3>();
            normalList = new List<Vector3>();
            uvList = new List<Vector2>();
            uv2List = new List<Vector2>();
            vertexIndexMap = new Dictionary<int, int>();
            triangleList = new List<int>();
        }
        public void Reset()
        {
            cellPointMap.Clear();
            vertexList.Clear();
            normalList.Clear();
            uvList.Clear();
            uv2List.Clear();
            vertexIndexMap.Clear();
            triangleList.Clear();
        }
        // 가정 : 1개 terrain 의 x,y값은 [0,255] 사이값.
        public static int MapIndex(byte floor, byte x, byte y)
        {
            return (int)(floor << 16) + (int)(y << 8) + x;
        }
        public static Vector3 StartPoint(byte floor, byte x, byte y, bool offset = true)
        {
            // todo: 이래야 대각선에서도 지그재그로 잘 표현된다.
            float sign = y % DIV == x % DIV ? -1f : 1f;
            return new Vector3((float)x * GetCellFullWidth() + (offset ? POINT_OFFSET[y % DIV] : 0), floor * cellY, (float)y * GetCellFullHeight() + (offset ? POINT_OFFSET[x % DIV] * sign : 0));
        }
        public static Vector2 UV(ref Vector3 pos)
        {
            return new Vector2(pos.x / cellX * uScale, pos.z / cellZ * vScale);
        }

        public static int VertexIndex(byte floor, byte x, byte y, byte pointIndex)
        {
            return (int)(floor << 24) + (int)(pointIndex << 16) + (int)(y << 8) + x;
        }
        public void AddVertex(byte floor, byte x, byte y, NeighborInfo info, int planeType = PLANE_LAND, int planeNormalType = -1)
        {
            Vertex vertex;
            int mapIndex = MapIndex(floor, x, y);
            if (!cellPointMap.TryGetValue(mapIndex, out vertex))
            {
                vertex = new Vertex(this, floor, x, y, info, planeType, planeNormalType);
                cellPointMap[mapIndex] = vertex;
            }
        }
        public void InsertVertex(byte floor, ref Vector3 vertex, ref Vector3 normal, ref Vector2 uv, byte x, byte y, byte pointIndex)
        {
            vertexList.Add(vertex);
            normalList.Add(normal);
            uvList.Add(uv);
            vertexIndexMap[VertexIndex(floor, x, y, pointIndex)] = vertexList.Count - 1;
        }
        public void InsertVertex2(byte floor, ref Vector3 vertex, ref Vector3 normal, ref Vector2 uv, ref Vector2 uv2, byte x, byte y, byte pointIndex)
        {
            vertexList.Add(vertex);
            normalList.Add(normal);
            uvList.Add(uv);
            uv2List.Add(uv2);
            vertexIndexMap[VertexIndex(floor, x, y, pointIndex)] = vertexList.Count - 1;
        }
        public int GetVertexInfo(byte floor, byte x, byte y, byte pointIndex)
        {
            int result = 0;
            if (vertexIndexMap.TryGetValue(VertexIndex(floor, x, y, pointIndex), out result))
                return result;
            CLogger.LogWarning("GetVertexInfo - invalid index." + x + "," + y + "," + pointIndex);
            return result;
        }
        public bool HasVertexInfo(byte floor, byte x, byte y, byte pointIndex)
        {
            return vertexIndexMap.ContainsKey(VertexIndex(floor, x, y, pointIndex));
        }
        //
        //   pt3 +------+ pt4
        //       |      |
        //       |      |
        //   pt2 +------+ pt1
        //
        public void AddPolygon(int pt1, int pt2, int pt3, int pt4)
        {
            triangleList.Add(pt1);
            triangleList.Add(pt2);
            triangleList.Add(pt3);

            triangleList.Add(pt4);
            triangleList.Add(pt1);
            triangleList.Add(pt3);
        }
        public void AddTopPlane(byte floor, byte x, byte y)
        {
            AddPolygon(GetVertexInfo(floor, x, y, PT_TOP), GetVertexInfo(floor, x, (byte)(y + 1), PT_TOP), GetVertexInfo(floor, (byte)(x + 1), (byte)(y + 1), PT_TOP), GetVertexInfo(floor, (byte)(x + 1), y, PT_TOP));
        }
        public void AddTopSidePlane(byte floor, byte sx, byte sy, byte ex, byte ey, bool isYUp = true)
        {
            byte startIndex = (!isYUp && HasVertexInfo(floor, sx, sy, PT_REV_LINK_START)) ? PT_REV_LINK_START : PT_LINK_START;
            byte endIndex = (!isYUp && HasVertexInfo(floor, ex, ey, PT_REV_LINK_START)) ? PT_REV_LINK_START : PT_LINK_START;
            AddPolygon(GetVertexInfo(floor, sx, sy, startIndex), GetVertexInfo(floor, ex, ey, endIndex), GetVertexInfo(floor, ex, ey, PT_TOP), GetVertexInfo(floor, sx, sy, PT_TOP));
            AddPolygon(GetVertexInfo(floor, sx, sy, (byte)(startIndex + 1)), GetVertexInfo(floor, ex, ey, (byte)(endIndex + 1)), GetVertexInfo(floor, ex, ey, endIndex), GetVertexInfo(floor, sx, sy, startIndex));
            AddPolygon(GetVertexInfo(floor, sx, sy, (byte)(startIndex + 2)), GetVertexInfo(floor, ex, ey, (byte)(endIndex + 2)), GetVertexInfo(floor, ex, ey, (byte)(endIndex + 1)), GetVertexInfo(floor, sx, sy, (byte)(startIndex + 1)));
            AddPolygon(GetVertexInfo(floor, sx, sy, (byte)(startIndex + 3)), GetVertexInfo(floor, ex, ey, (byte)(endIndex + 3)), GetVertexInfo(floor, ex, ey, (byte)(endIndex + 2)), GetVertexInfo(floor, sx, sy, (byte)(startIndex + 2)));
        }

        class VertexFan
        {
            public int index;
            public Vector3 point;
            public Vector3 center;
            public Vector3 dir;
            public float dirLen1;
            public float dirLen2;
            public VertexFan(int index, List<Vector3> vertexList, ref Vector3 center, int index2)
            {
                this.index = index;
                this.point = vertexList[index];
                this.center = center;
                this.center.y = point.y;
                dirLen1 = (this.point - this.center).magnitude;
                dirLen2 = (vertexList[index2] - this.center).magnitude;
                this.dir = (this.point - this.center).normalized;
            }
            public Vector3 Rotation(ref Quaternion rot, float ratio)
            {
                return rot * dir * (dirLen2 * ratio + dirLen1 * (1 - ratio)) * RotationScale(ratio) + center;
            }
            public static float RotationScale(float ratio)
            {
                // 좀더 볼록하게 만들기 위함.
                return 1 + (1 - Mathf.Abs((ratio - 0.5f) / 0.5f)) * 0.2f;
            }
            public Vector3 InverseFanRotation(ref Quaternion rot, float ratio)
            {
                return rot * dir * (dirLen2 * ratio + dirLen1 * (1 - ratio)) * InverseFanRotationScale(ratio) + center;
            }
            public static float InverseFanRotationScale(float ratio)
            {
                // 0.5 * ((ratio - 0.5)/0.5)^2 + 0.5 ==> [0.5,1] 인 포물선
                // 오목하게 만들기 위함.
                return (ratio - 0.5f) * (ratio - 0.5f) / 0.5f + 0.5f;
            }
        }
        VertexFan[] tempFanVertexList = new VertexFan[5];
        // (pt1)         (center)
        // ex,y *-------* cx,y
        //              |
        //              |
        //              | (pt2)
        //              * sx,y
        const int TOTAL_HALF_TRI = 3;
        public void AddTopHalfInverseFanTriangles(byte floor, byte cx, byte cy, byte sx, byte sy, byte ex, byte ey)
        {
            int indCenter = GetVertexInfo(floor, cx, cy, PT_TOP);
            int indPt1 = GetVertexInfo(floor, ex, ey, PT_TOP);
            int indPt2 = GetVertexInfo(floor, sx, sy, PT_TOP);

            Vector3 center = vertexList[indCenter];
            Vector3 pt1 = vertexList[indPt1];
            Vector3 pt2 = vertexList[indPt2];

            Vector3 normal = Vector3.up;
            // half
            pt1 = (pt1 - center) * 0.5f + center;
            indPt1 = InsertVertex3(ref pt1, ref normal, VertexInfo.UV(ref pt1));
            pt2 = (pt2 - center) * 0.5f + center;
            indPt2 = InsertVertex3(ref pt2, ref normal, VertexInfo.UV(ref pt2));

            tempFanVertexList[0] = new VertexFan(indPt1, vertexList, ref center, indPt2);
            for (int i = 0; i < TOTAL_HALF_TRI; i++)
            {
                // 윗면의 fan 삼각형 그림
                float ratio = (float)(i + 1) / (float)(TOTAL_HALF_TRI + 1);
                Quaternion rot = Quaternion.AngleAxis(-90f * ratio, Vector3.up);
                Vector3 newPt = tempFanVertexList[0].InverseFanRotation(ref rot, ratio);
                int indTop = InsertVertex3(ref newPt, ref normal, VertexInfo.UV(ref newPt));
                AddTriangle(indCenter, indTop, tempFanVertexList[0].index);

                tempFanVertexList[0].index = indTop;
            }
            AddTriangle(indCenter, indPt2, tempFanVertexList[0].index);
        }
        public void AddSideHalfInverseFanTriangles(byte floor, byte cx, byte cy, byte sx, byte sy, byte ex, byte ey, bool isYUp = true)
        {
            byte startIndex = (!isYUp && HasVertexInfo(floor, sx, sy, PT_REV_LINK_START)) ? PT_REV_LINK_START : PT_LINK_START;
            byte endIndex = (!isYUp && HasVertexInfo(floor, ex, ey, PT_REV_LINK_START)) ? PT_REV_LINK_START : PT_LINK_START;

            int indCenter = GetVertexInfo(floor, cx, cy, PT_TOP);
            int indPt1 = GetVertexInfo(floor, ex, ey, PT_TOP);
            int indPt2 = GetVertexInfo(floor, sx, sy, PT_TOP);

            Vector3 center = vertexList[indCenter];
            Vector3 pt1 = vertexList[indPt1];
            Vector3 pt2 = vertexList[indPt2];

            tempFanVertexList[0] = new VertexFan(indPt1, vertexList, ref center, indPt2);

            // half
            Vector3 normal = Vector3.up;
            Vector3 newPt = (pt1 - center) * 0.5f + center;
            Vector2 uv2 = uv2List[tempFanVertexList[0].index];
            tempFanVertexList[0].index = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
            tempFanVertexList[0].dirLen1 /= 2;
            tempFanVertexList[0].dirLen2 /= 2;

            for (int i = 1; i < tempFanVertexList.Length; i++)
            {
                tempFanVertexList[i] = new VertexFan(GetVertexInfo(floor, ex, ey, (byte)(endIndex + i - 1)), vertexList, ref center, GetVertexInfo(floor, sx, sy, (byte)(startIndex + i - 1)));
                tempFanVertexList[i].dirLen1 /= 2;
                tempFanVertexList[i].dirLen2 /= 2;

                newPt = (vertexList[tempFanVertexList[i].index] - tempFanVertexList[i].center) * 0.5f + tempFanVertexList[i].center;
                normal = normalList[tempFanVertexList[i].index];
                uv2 = uv2List[tempFanVertexList[i].index];
                tempFanVertexList[i].index = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
            }

            const float XRATIO = 0.7f;
            for (int i = 0; i < TOTAL_HALF_TRI; i++)
            {
                float ratio = (float)(i + 1) / (float)(TOTAL_HALF_TRI + 1);
                Quaternion rot = Quaternion.AngleAxis(-90f * ratio, Vector3.up);
                newPt = tempFanVertexList[0].InverseFanRotation(ref rot, ratio);
                normal = Vector3.up;
                uv2 = uv2List[tempFanVertexList[0].index];
                int indTop = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                //AddTriangle(indCenter, indTop, tempFanVertexList[0].index);

                // side면의 polygon을 그림
                newPt = tempFanVertexList[1].InverseFanRotation(ref rot, ratio);
                Vector3 dir = (newPt - tempFanVertexList[1].center).normalized;
                // 살짝 튀어나오게 함
                newPt += dir * linkX * XRATIO;
                normal = (Vector3.up + dir).normalized;
                uv2 = uv2List[tempFanVertexList[1].index];
                int indSt = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                AddPolygon(indSt, tempFanVertexList[1].index, tempFanVertexList[0].index, indTop);

                newPt = tempFanVertexList[2].InverseFanRotation(ref rot, ratio);
                dir = (newPt - tempFanVertexList[2].center).normalized;
                newPt += dir * linkX;
                normal = dir;
                uv2 = uv2List[tempFanVertexList[2].index];
                int indSt1 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                AddPolygon(indSt1, tempFanVertexList[2].index, tempFanVertexList[1].index, indSt);

                newPt = tempFanVertexList[3].InverseFanRotation(ref rot, ratio);
                dir = (newPt - tempFanVertexList[3].center).normalized;
                // 살짝 튀어나오게 함
                newPt += dir * linkX * XRATIO;
                normal = (-Vector3.up + dir).normalized;
                uv2 = uv2List[tempFanVertexList[3].index];
                int indSt2 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                AddPolygon(indSt2, tempFanVertexList[3].index, tempFanVertexList[2].index, indSt1);

                newPt = tempFanVertexList[4].InverseFanRotation(ref rot, ratio);
                normal = -Vector3.up;
                uv2 = uv2List[tempFanVertexList[4].index];
                int indSt3 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                AddPolygon(indSt3, tempFanVertexList[4].index, tempFanVertexList[3].index, indSt2);

                tempFanVertexList[0].index = indTop;
                tempFanVertexList[1].index = indSt;
                tempFanVertexList[2].index = indSt1;
                tempFanVertexList[3].index = indSt2;
                tempFanVertexList[4].index = indSt3;
            }

            // half
            normal = Vector3.up;
            newPt = (pt2 - center) * 0.5f + center;
            uv2 = uv2List[indPt2];
            indPt2 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);

            Vector3 _center = center;
            int index = GetVertexInfo(floor, sx, sy, startIndex);
            _center.y = vertexList[index].y;
            newPt = (vertexList[index] - _center) * 0.5f + _center;
            normal = normalList[index];
            uv2 = uv2List[index];
            int _indSt = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);

            index = GetVertexInfo(floor, sx, sy, (byte)(startIndex + 1));
            _center.y = vertexList[index].y;
            newPt = (vertexList[index] - _center) * 0.5f + _center;
            normal = normalList[index];
            uv2 = uv2List[index];
            int _indSt2 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);

            index = GetVertexInfo(floor, sx, sy, (byte)(startIndex + 2));
            _center.y = vertexList[index].y;
            newPt = (vertexList[index] - _center) * 0.5f + _center;
            normal = normalList[index];
            uv2 = uv2List[index];
            int _indSt3 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);

            index = GetVertexInfo(floor, sx, sy, (byte)(startIndex + 3));
            _center.y = vertexList[index].y;
            newPt = (vertexList[index] - _center) * 0.5f + _center;
            normal = normalList[index];
            uv2 = uv2List[index];
            int _indSt4 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);

            AddPolygon(_indSt, tempFanVertexList[1].index, tempFanVertexList[0].index, indPt2);
            AddPolygon(_indSt2, tempFanVertexList[2].index, tempFanVertexList[1].index, _indSt);
            AddPolygon(_indSt3, tempFanVertexList[3].index, tempFanVertexList[2].index, _indSt2);
            AddPolygon(_indSt4, tempFanVertexList[4].index, tempFanVertexList[3].index, _indSt3);
        }
        public void AddTopFanTriangles(byte floor, byte cx, byte cy, byte sx, byte sy, byte ex, byte ey)
        {
            int indCenter = GetVertexInfo(floor, cx, cy, PT_TOP);
            int indPt1 = GetVertexInfo(floor, ex, ey, PT_TOP);
            int indPt2 = GetVertexInfo(floor, sx, sy, PT_TOP);

            Vector3 center = vertexList[indCenter];

            tempFanVertexList[0] = new VertexFan(indPt1, vertexList, ref center, indPt2);
            for (int i = 0; i < TOTAL_TRI; i++)
            {
                // 윗면의 fan 삼각형 그림
                float ratio = (float)(i + 1) / (float)(TOTAL_TRI + 1);
                Quaternion rot = Quaternion.AngleAxis(-90f * ratio, Vector3.up);
                Vector3 newPt = tempFanVertexList[0].Rotation(ref rot, ratio);
                Vector3 normal = Vector3.up;
                int indTop = InsertVertex3(ref newPt, ref normal, VertexInfo.UV(ref newPt));
                AddTriangle(indCenter, indTop, tempFanVertexList[0].index);

                tempFanVertexList[0].index = indTop;
            }
            AddTriangle(indCenter, indPt2, tempFanVertexList[0].index);
        }
        public void AddSideFanTriangles(byte floor, byte cx, byte cy, byte sx, byte sy, byte ex, byte ey, bool isYUp = true)
        {
            byte startIndex = (!isYUp && HasVertexInfo(floor, sx, sy, PT_REV_LINK_START)) ? PT_REV_LINK_START : PT_LINK_START;
            byte endIndex = (!isYUp && HasVertexInfo(floor, ex, ey, PT_REV_LINK_START)) ? PT_REV_LINK_START : PT_LINK_START;

            int indCenter = GetVertexInfo(floor, cx, cy, PT_TOP);
            int indPt1 = GetVertexInfo(floor, ex, ey, PT_TOP);
            int indPt2 = GetVertexInfo(floor, sx, sy, PT_TOP);

            Vector3 center = vertexList[indCenter];
            Vector3 pt1 = vertexList[indPt1];
            Vector3 pt2 = vertexList[indPt2];

            tempFanVertexList[0] = new VertexFan(indPt1, vertexList, ref center, indPt2);
            for (int i = 1; i < tempFanVertexList.Length; i++)
                tempFanVertexList[i] = new VertexFan(GetVertexInfo(floor, ex, ey, (byte)(endIndex + i - 1)), vertexList, ref center, GetVertexInfo(floor, sx, sy, (byte)(startIndex + i - 1)));

            for (int i = 0; i < TOTAL_TRI; i++)
            {
                // 윗면의 fan 삼각형 그림
                float ratio = (float)(i + 1) / (float)(TOTAL_TRI + 1);
                Quaternion rot = Quaternion.AngleAxis(-90f * ratio, Vector3.up);
                Vector3 newPt = tempFanVertexList[0].Rotation(ref rot, ratio);
                Vector3 normal = Vector3.up;
                Vector2 uv2 = uv2List[tempFanVertexList[0].index];
                int indTop = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                //AddTriangle(indCenter, indTop, tempFanVertexList[0].index);

                // side면의 polygon을 그림
                newPt = tempFanVertexList[1].Rotation(ref rot, ratio);
                normal = (Vector3.up + (newPt - tempFanVertexList[1].center).normalized).normalized;
                uv2 = uv2List[tempFanVertexList[1].index];
                int indSt = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                AddPolygon(indSt, tempFanVertexList[1].index, tempFanVertexList[0].index, indTop);

                newPt = tempFanVertexList[2].Rotation(ref rot, ratio);
                normal = (newPt - tempFanVertexList[2].center).normalized;
                uv2 = uv2List[tempFanVertexList[2].index];
                int indSt1 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                AddPolygon(indSt1, tempFanVertexList[2].index, tempFanVertexList[1].index, indSt);

                newPt = tempFanVertexList[3].Rotation(ref rot, ratio);
                normal = (-Vector3.up + (newPt - tempFanVertexList[3].center).normalized).normalized;
                uv2 = uv2List[tempFanVertexList[3].index];
                int indSt2 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                AddPolygon(indSt2, tempFanVertexList[3].index, tempFanVertexList[2].index, indSt1);

                newPt = tempFanVertexList[4].Rotation(ref rot, ratio);
                normal = -Vector3.up;
                uv2 = uv2List[tempFanVertexList[4].index];
                int indSt3 = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), uv2);
                AddPolygon(indSt3, tempFanVertexList[4].index, tempFanVertexList[3].index, indSt2);

                tempFanVertexList[0].index = indTop;
                tempFanVertexList[1].index = indSt;
                tempFanVertexList[2].index = indSt1;
                tempFanVertexList[3].index = indSt2;
                tempFanVertexList[4].index = indSt3;
            }
            //AddTriangle(indCenter, indPt2, tempFanVertexList[0].index);
            AddPolygon(GetVertexInfo(floor, sx, sy, startIndex), tempFanVertexList[1].index, tempFanVertexList[0].index, indPt2);
            AddPolygon(GetVertexInfo(floor, sx, sy, (byte)(startIndex + 1)), tempFanVertexList[2].index, tempFanVertexList[1].index, GetVertexInfo(floor, sx, sy, startIndex));
            AddPolygon(GetVertexInfo(floor, sx, sy, (byte)(startIndex + 2)), tempFanVertexList[3].index, tempFanVertexList[2].index, GetVertexInfo(floor, sx, sy, (byte)(startIndex + 1)));
            AddPolygon(GetVertexInfo(floor, sx, sy, (byte)(startIndex + 3)), tempFanVertexList[4].index, tempFanVertexList[3].index, GetVertexInfo(floor, sx, sy, (byte)(startIndex + 2)));
        }
        public void AddTriangle(int pt1, int pt2, int pt3)
        {
            triangleList.Add(pt1);
            triangleList.Add(pt2);
            triangleList.Add(pt3);
        }
        public int InsertVertex3(ref Vector3 vertex, ref Vector3 normal, Vector2 uv)
        {
            vertexList.Add(vertex);
            normalList.Add(normal);
            uvList.Add(uv);
            return vertexList.Count - 1;
        }
        public int InsertVertex4(ref Vector3 vertex, ref Vector3 normal, ref Vector2 uv)
        {
            vertexList.Add(vertex);
            normalList.Add(normal);
            uvList.Add(uv);
            return vertexList.Count - 1;
        }
        public int InsertVertex5(ref Vector3 vertex, ref Vector3 normal, Vector2 uv, Vector2 uv2)
        {
            vertexList.Add(vertex);
            normalList.Add(normal);
            uvList.Add(uv);
            uv2List.Add(uv2);
            return vertexList.Count - 1;
        }
        //  ex(1) +----+ sx(2)
        //        |    |
        //  bt(3) +----+ bt(4)
        public void AddWallPlane(byte floor, byte sx, byte sy, byte ex, byte ey, byte normalType, float depth = 0f)
        {
            Vector3 startPt = VertexInfo.StartPoint(floor, sx, sy);
            startPt.y -= depth;
            Vector3 startBottomPt = startPt;
            startBottomPt.y -= cellY * (floor + 1) - depth;
            Vector3 endPt = VertexInfo.StartPoint(floor, ex, ey);
            endPt.y -= depth;
            Vector3 endBottomPt = endPt;
            endBottomPt.y -= cellY * (floor + 1) - depth;
            Vector3 normal = NORMALS[normalType];

            bool isVertical = normalType == NORM_N || normalType == NORM_S;
            float su = isVertical ? startPt.x / cellX * uScale : startPt.z / cellZ * vScale;
            float eu = isVertical ? endPt.x / cellX * uScale : endPt.z / cellZ * vScale;

            int ind1 = InsertVertex3(ref endPt, ref normal, new Vector2(eu, 1));
            int ind2 = InsertVertex3(ref startPt, ref normal, new Vector2(su, 1));
            int ind3 = InsertVertex3(ref endBottomPt, ref normal, new Vector2(eu, 0));
            int ind4 = InsertVertex3(ref startBottomPt, ref normal, new Vector2(su, 0));

            AddPolygon(ind3, ind1, ind2, ind4);
        }
        // (pt1)         (center)
        // ex,y *-------* cx,y
        //              |
        //              |
        //              | (pt2)
        //              * sx,y
        public void AddWallHalfInverseFanPlane(byte floor, byte cx, byte cy, byte sx, byte sy, byte ex, byte ey, byte normalType, float depth = 0f)
        {
            float bottomOffsetY = cellY * (floor + 1) - depth;
            Vector3 centerPt = VertexInfo.StartPoint(floor, cx, cy);
            Vector3 pt2 = (VertexInfo.StartPoint(floor, sx, sy) - centerPt) * 0.5f + centerPt;
            Vector3 pt1 = (VertexInfo.StartPoint(floor, ex, ey) - centerPt) * 0.5f + centerPt;

            centerPt.y -= depth;
            Vector3 centerBottomPt = centerPt;
            centerBottomPt.y -= bottomOffsetY;

            pt2.y -= depth;
            Vector3 pt2Bottom = pt2;
            pt2Bottom.y -= bottomOffsetY;
            Vector3 normal2;
            //Vector3 normal2 = (pt2 - centerPt).normalized;

            pt1.y -= depth;
            Vector3 pt1Bottom = pt1;
            pt1Bottom.y -= bottomOffsetY;
            Vector3 normal1;
            //Vector3 normal1 = (pt1 - centerPt).normalized;
            if (normalType == NORM_SW)
            {
                normal2 = NORMALS[NORM_S];
                normal1 = NORMALS[NORM_W];
            }
            else if (normalType == NORM_NW)
            {
                normal2 = NORMALS[NORM_W];
                normal1 = NORMALS[NORM_N];
            }
            else if (normalType == NORM_NE)
            {
                normal2 = NORMALS[NORM_N];
                normal1 = NORMALS[NORM_E];
            }
            else
            {
                normal2 = NORMALS[NORM_E];
                normal1 = NORMALS[NORM_S];
            }

            bool isVertical = normalType == NORM_N || normalType == NORM_S;
            float u2 = isVertical ? pt2.x / cellX * uScale : pt2.z / cellZ * vScale;
            float u1 = isVertical ? pt1.x / cellX * uScale : pt1.z / cellZ * vScale;

            int ind1 = InsertVertex3(ref pt1, ref normal1, new Vector2(u1, 1));
            int ind2 = InsertVertex3(ref pt2, ref normal2, new Vector2(u2, 1));
            int ind3 = InsertVertex3(ref pt1Bottom, ref normal1, new Vector2(u1, 0));
            int ind4 = InsertVertex3(ref pt2Bottom, ref normal2, new Vector2(u2, 0));

            Vector3 dir = pt1 - centerPt;
            float dirLen1 = dir.magnitude;
            float dirLen2 = (pt2 - centerPt).magnitude;
            int indSt = ind1;
            int indBt = ind3;
            for (int i = 0; i < TOTAL_HALF_TRI; i++)
            {
                // pt1 -> pt2 로 회전시키면서 vertex를 만든다.
                // 이때 pt1과 pt2의 center로부터의 길이가 다르므로, interpolation한다.
                float ratio = (float)(i + 1) / (float)(TOTAL_HALF_TRI + 1);
                // 0.5 * ((ratio - 0.5)/0.5)^2 + 0.5 ==> [0.5,1] 인 포물선
                float scale = VertexFan.InverseFanRotationScale(ratio);
                Vector3 _dir = dir.normalized * (dirLen1 * (1 - ratio) + dirLen2 * ratio) * scale;
                Vector3 newDir = Quaternion.AngleAxis(-90f * ratio, Vector3.up) * _dir;
                Vector3 newPt = newDir + centerPt;
                Vector3 newNormal = newDir.normalized;
                float u = u1 * (1 - ratio) + u2 * ratio;
                int indPt1 = InsertVertex3(ref newPt, ref newNormal, new Vector2(u, 1));
                Vector3 newBottomPt = newDir + centerBottomPt;
                int indPt2 = InsertVertex3(ref newBottomPt, ref newNormal, new Vector2(u, 0));

                AddPolygon(indPt2, indBt, indSt, indPt1);

                indSt = indPt1;
                indBt = indPt2;
            }

            AddPolygon(ind4, indBt, indSt, ind2);
        }
        public void AddWallFanPlane(byte floor, byte cx, byte cy, byte sx, byte sy, byte ex, byte ey, byte normalType, float depth = 0f)
        {
            float bottomOffsetY = cellY * (floor + 1) - depth;
            Vector3 centerPt = VertexInfo.StartPoint(floor, cx, cy);
            centerPt.y -= depth;
            Vector3 centerBottomPt = centerPt;
            centerBottomPt.y -= bottomOffsetY;

            Vector3 pt2 = VertexInfo.StartPoint(floor, sx, sy);
            pt2.y -= depth;
            Vector3 pt2Bottom = pt2;
            pt2Bottom.y -= bottomOffsetY;
            Vector3 normal2;
            //Vector3 normal2 = (pt2 - centerPt).normalized;

            Vector3 pt1 = VertexInfo.StartPoint(floor, ex, ey);
            pt1.y -= depth;
            Vector3 pt1Bottom = pt1;
            pt1Bottom.y -= bottomOffsetY;
            Vector3 normal1;
            //Vector3 normal1 = (pt1 - centerPt).normalized;
            if (normalType == NORM_SW)
            {
                normal2 = NORMALS[NORM_S];
                normal1 = NORMALS[NORM_W];
            }
            else if (normalType == NORM_NW)
            {
                normal2 = NORMALS[NORM_W];
                normal1 = NORMALS[NORM_N];
            }
            else if (normalType == NORM_NE)
            {
                normal2 = NORMALS[NORM_N];
                normal1 = NORMALS[NORM_E];
            }
            else
            {
                normal2 = NORMALS[NORM_E];
                normal1 = NORMALS[NORM_S];
            }

            bool isVertical = normalType == NORM_N || normalType == NORM_S;
            float u2 = isVertical ? pt2.x / cellX * uScale : pt2.z / cellZ * vScale;
            float u1 = isVertical ? pt1.x / cellX * uScale : pt1.z / cellZ * vScale;

            int ind1 = InsertVertex3(ref pt1, ref normal1, new Vector2(u1, 1));
            int ind2 = InsertVertex3(ref pt2, ref normal2, new Vector2(u2, 1));
            int ind3 = InsertVertex3(ref pt1Bottom, ref normal1, new Vector2(u1, 0));
            int ind4 = InsertVertex3(ref pt2Bottom, ref normal2, new Vector2(u2, 0));

            Vector3 dir = pt1 - centerPt;
            float dirLen1 = dir.magnitude;
            float dirLen2 = (pt2 - centerPt).magnitude;
            int indSt = ind1;
            int indBt = ind3;
            for (int i = 0; i < TOTAL_TRI; i++)
            {
                // pt1 -> pt2 로 회전시키면서 vertex를 만든다.
                // 이때 pt1과 pt2의 center로부터의 길이가 다르므로, interpolation한다.
                float ratio = (float)(i + 1) / (float)(TOTAL_TRI + 1);
                float scale = VertexFan.RotationScale(ratio);
                Vector3 _dir = dir.normalized * (dirLen1 * (1 - ratio) + dirLen2 * ratio) * scale;
                Vector3 newDir = Quaternion.AngleAxis(-90f * ratio, Vector3.up) * _dir;
                Vector3 newPt = newDir + centerPt;
                Vector3 newNormal = newDir.normalized;
                float u = u1 * (1 - ratio) + u2 * ratio;
                int indPt1 = InsertVertex3(ref newPt, ref newNormal, new Vector2(u, 1));
                Vector3 newBottomPt = newDir + centerBottomPt;
                int indPt2 = InsertVertex3(ref newBottomPt, ref newNormal, new Vector2(u, 0));

                AddPolygon(indPt2, indBt, indSt, indPt1);

                indSt = indPt1;
                indBt = indPt2;
            }

            AddPolygon(ind4, indBt, indSt, ind2);
        }
        public void AddWaterSidePlane(byte floor, byte sx, byte sy, byte sNormalType, byte ex, byte ey, byte eNormalType, float offset = 0)
        {
            // start point 쪽
            Vector3 st1 = VertexInfo.StartPoint(floor, sx, sy);
            st1.y -= waterDepth;

            // end point쪽
            Vector3 et1 = VertexInfo.StartPoint(floor, ex, ey);
            et1.y -= waterDepth;

            Vector3 avgNorm = (NORMALS[sNormalType] + NORMALS[eNormalType]).normalized;
            Vector2 avgUV = new Vector2(avgNorm.x * uScale, avgNorm.z * vScale);

            int[] pt1ind = InsertWaterVertexInLine(floor, st1, NORMALS[sNormalType], 0, offset);
            int[] pt2ind = InsertWaterVertexInLine(floor, et1, NORMALS[eNormalType], 1, offset);

            int sp1 = pt1ind[0];
            int sp2 = pt1ind[1];
            int sp3 = pt1ind[2];
            int sp4 = pt1ind[3];
            int sp5 = pt1ind[4];

            int ep1 = pt2ind[0];
            int ep2 = pt2ind[1];
            int ep3 = pt2ind[2];
            int ep4 = pt2ind[3];
            int ep5 = pt2ind[4];

            AddPolygon(sp2, ep2, ep1, sp1);
            AddPolygon(sp3, ep3, ep2, sp2);
            AddPolygon(sp4, ep4, ep3, sp3);
            AddPolygon(sp5, ep5, ep4, sp4);
        }

        int[] InsertWaterVertexInLine(byte floor, Vector3 pt1, Vector3 normal, float texU, float offset = 0)
        {
            int[] result = new int[5];
            float height = cellY * (floor + 1);
            float waterUVScale = waterSideScale * (floor + 1);

            // 약간 안쪽 위로 수면을 덮도록 한다. (출렁거리는 물을 가리기 위함)
            Vector3 tempPt = pt1 - normal * linkX * 0.7f;
            tempPt.y += linkY * 0.5f;
            Vector3 tempNormal = Vector3.up;
            Vector2 uv1 = new Vector2(texU, 1);
            //Vector2 uv2 = new Vector2(0, 1);
            result[0] = InsertVertex4(ref tempPt, ref tempNormal, ref uv1);//, uv2);
            float h = tempPt.y + height;

            if (offset != 0)
                pt1 += normal * offset;

            tempNormal = Vector3.up;
            float cur = pt1.y + height;
            Vector2 tempUV = new Vector2(texU, h == 0 ? 0 : cur / h);
            //uv2 = new Vector2(0, h == 0 ? 0 : cur / h);
            result[1] = InsertVertex4(ref pt1, ref tempNormal, ref tempUV);//, uv2);

            tempPt = pt1 + normal * linkX * 0.7f;
            tempPt.y -= linkY * 0.5f;
            tempNormal = (normal + Vector3.up).normalized;
            cur = tempPt.y + height;
            tempUV = new Vector2(texU, h == 0 ? 0 : cur / h);
            //uv2 = new Vector2(0, h == 0 ? 0 : cur / h);
            result[2] = InsertVertex4(ref tempPt, ref tempNormal, ref tempUV);//, uv2);

            tempPt = pt1 + normal * linkX;
            tempPt.y -= linkY;
            tempNormal = normal;
            cur = tempPt.y + height;
            tempUV = new Vector2(texU, h == 0 ? 0 : cur / h);
            //uv2 = new Vector2(0, h == 0 ? 0 : cur / h);
            result[3] = InsertVertex4(ref tempPt, ref tempNormal, ref tempUV);//, uv2);

            tempPt = pt1 + normal * linkX;
            tempPt.y -= (height - waterDepth);
            tempNormal = normal;
            cur = tempPt.y + height;
            tempUV = new Vector2(texU, h == 0 ? 0 : cur / h);
            //uv2 = new Vector2(0, h == 0 ? 0 : cur / h);
            result[4] = InsertVertex4(ref tempPt, ref tempNormal, ref tempUV);//, uv2);
            return result;
        }
        // (pt1)         (center)
        // ex,y *-------* cx,y
        //              |
        //              |
        //              | (pt2)
        //              * sx,y
        public void AddWaterSideFanPlane(byte floor, byte cx, byte cy, byte sx, byte sy, byte sNormalType, byte ex, byte ey, byte eNormalType, float offset = 0)
        {
            float height = cellY * (floor + 1);
            float waterUVScale = waterSideScale * (floor + 1);

            // center point 쪽
            Vector3 center = VertexInfo.StartPoint(floor, cx, cy);
            center.y -= waterDepth;

            // end point쪽 (pt1)
            Vector3 pt1 = VertexInfo.StartPoint(floor, ex, ey);
            pt1.y -= waterDepth;

            // start point 쪽 (pt2)
            Vector3 pt2 = VertexInfo.StartPoint(floor, sx, sy);
            pt2.y -= waterDepth;

            Vector3 avgNorm = ((pt1 - center).normalized + (pt2 - center).normalized).normalized;
            Vector2 avgUV = new Vector2(avgNorm.x * uScale, avgNorm.z * vScale);

            int[] pt1ind = InsertWaterVertexInLine(floor, pt1, NORMALS[eNormalType], 1, offset);
            int[] pt2ind = InsertWaterVertexInLine(floor, pt2, NORMALS[sNormalType], 0, offset);

            for (int i = 0; i < pt1ind.Length; i++)
                tempFanVertexList[i] = new VertexFan(pt1ind[i], vertexList, ref center, pt2ind[i]);

            for (int i = 0; i < TOTAL_TRI; i++)
            {
                // 윗면의 fan 삼각형 그림
                float ratio = (float)(i + 1) / (float)(TOTAL_TRI + 1);
                Quaternion rot = Quaternion.AngleAxis(-90f * ratio, Vector3.up);
                Vector3 newPt = tempFanVertexList[0].Rotation(ref rot, ratio);
                int[] ptind = InsertWaterVertexInLine(floor, newPt, (newPt - tempFanVertexList[0].center).normalized, 1 - ratio, offset);

                AddPolygon(ptind[1], tempFanVertexList[1].index, tempFanVertexList[0].index, ptind[0]);
                AddPolygon(ptind[2], tempFanVertexList[2].index, tempFanVertexList[1].index, ptind[1]);
                AddPolygon(ptind[3], tempFanVertexList[3].index, tempFanVertexList[2].index, ptind[2]);
                AddPolygon(ptind[4], tempFanVertexList[4].index, tempFanVertexList[3].index, ptind[3]);

                for (int k = 0; k < pt1ind.Length; k++)
                    tempFanVertexList[k].index = ptind[k];
            }
            AddPolygon(pt2ind[1], tempFanVertexList[1].index, tempFanVertexList[0].index, pt2ind[0]);
            AddPolygon(pt2ind[2], tempFanVertexList[2].index, tempFanVertexList[1].index, pt2ind[1]);
            AddPolygon(pt2ind[3], tempFanVertexList[3].index, tempFanVertexList[2].index, pt2ind[2]);
            AddPolygon(pt2ind[4], tempFanVertexList[4].index, tempFanVertexList[3].index, pt2ind[3]);
        }
        
        //
        // 12-13---14-15
        // |  |     |  |
        // 8--9----10-11
        // |  |     |  |
        // |  |     |  |
        // |  |     |  |
        // 4--5-----6--7
        // |  |     |  |
        // 0--1-----2--3
        // 이렇게 등록한다.
        //const float TILE_Y = 0.1f;
        //Vector3[] tilePoints = new Vector3[16];
        //Vector2[] tileUV2s = new Vector2[16];
        //public void AddTilePlane(byte floor, byte sx, byte sy, byte neighbor)
        //{
        //    tilePoints[0] = VertexInfo.StartPoint(floor, sx, sy, false);
        //    tilePoints[0].y += TILE_Y;
        //    tilePoints[3] = VertexInfo.StartPoint(floor, (byte)(sx + 1), sy, false);
        //    tilePoints[3].y += TILE_Y;
        //    tilePoints[15] = VertexInfo.StartPoint(floor, (byte)(sx + 1), (byte)(sy + 1), false);
        //    tilePoints[15].y += TILE_Y;
        //    tilePoints[12] = VertexInfo.StartPoint(floor, sx, (byte)(sy + 1), false);
        //    tilePoints[12].y += TILE_Y;

        //    tilePoints[1] = (tilePoints[3] - tilePoints[0]) * TILE_INNER_SPACE_RATIO + tilePoints[0];
        //    tilePoints[2] = (tilePoints[0] - tilePoints[3]) * TILE_INNER_SPACE_RATIO + tilePoints[3];
        //    tilePoints[4] = (tilePoints[12] - tilePoints[0]) * TILE_INNER_SPACE_RATIO + tilePoints[0];
        //    tilePoints[7] = (tilePoints[15] - tilePoints[3]) * TILE_INNER_SPACE_RATIO + tilePoints[3];
        //    tilePoints[5] = (tilePoints[7] - tilePoints[4]) * TILE_INNER_SPACE_RATIO + tilePoints[4];
        //    tilePoints[6] = (tilePoints[4] - tilePoints[7]) * TILE_INNER_SPACE_RATIO + tilePoints[7];
        //    tilePoints[8] = (tilePoints[0] - tilePoints[12]) * TILE_INNER_SPACE_RATIO + tilePoints[12];
        //    tilePoints[11] = (tilePoints[3] - tilePoints[15]) * TILE_INNER_SPACE_RATIO + tilePoints[15];
        //    tilePoints[9] = (tilePoints[11] - tilePoints[8]) * TILE_INNER_SPACE_RATIO + tilePoints[8];
        //    tilePoints[10] = (tilePoints[8] - tilePoints[11]) * TILE_INNER_SPACE_RATIO + tilePoints[11];

        //    // 기본 uv2 세팅
        //    for (int i = 0; i < 16; i++)
        //        tileUV2s[i] = Vector2.zero;
        //    tileUV2s[5] = tileUV2s[6] = tileUV2s[9] = tileUV2s[10] = Vector2.one;

        //    if ((neighbor & 1) != 0)
        //    {
        //        tileUV2s[12] = tileUV2s[13] = tileUV2s[14] = tileUV2s[15] = Vector2.zero;
        //    }

        //    InsertVertex2(byte floor, ref Vector3 vertex, ref Vector3 normal, ref Vector2 uv, ref Vector2 uv2, byte x, byte y, byte pointIndex)

        //    Vector3 normal = Vector3.up;

        //    int ind1 = InsertVertex3(ref pt1, ref normal, VertexInfo.UV(ref pt1));
        //    int ind2 = InsertVertex3(ref pt2, ref normal, VertexInfo.UV(ref pt2));
        //    int ind3 = InsertVertex3(ref pt3, ref normal, VertexInfo.UV(ref pt3));
        //    int ind4 = InsertVertex3(ref pt4, ref normal, VertexInfo.UV(ref pt4));

        //    AddPolygon(ind2, ind1, ind4, ind3);
        //}
        
        const float TILE_INNER_SPACE_RATIO = 0.15f;
        public void AddTilePlane(byte[] tileArr, int width, int height, byte floor, byte sx, byte sy)
        {
            float h = 0.1f;
            Vector3 pt1 = VertexInfo.StartPoint(floor, sx, sy, false);
            pt1.y += h;
            Vector3 pt2 = VertexInfo.StartPoint(floor, (byte)(sx + 1), sy, false);
            pt2.y += h;
            Vector3 pt3 = VertexInfo.StartPoint(floor, (byte)(sx + 1), (byte)(sy + 1), false);
            pt3.y += h;
            Vector3 pt4 = VertexInfo.StartPoint(floor, sx, (byte)(sy + 1), false);
            pt4.y += h;

            Vector3 normal = Vector3.up;

            int n1 = GetNeighborTileIndexAtPoint(tileArr, width, height, sx, sy);
            int n2 = GetNeighborTileIndexAtPoint(tileArr, width, height, (byte)(sx + 1), sy);
            int n3 = GetNeighborTileIndexAtPoint(tileArr, width, height, (byte)(sx + 1), (byte)(sy + 1));
            int n4 = GetNeighborTileIndexAtPoint(tileArr, width, height, sx, (byte)(sy + 1));
            Vector2 uv1 = new Vector2(TILE_INNER_SPACE_RATIO, TILE_INNER_SPACE_RATIO);
            if (n1 == 2 || n2 == 2 + 4)
                uv1 = Vector2.zero;
            else if (n1 == 1 + 2)
                uv1 = new Vector2(TILE_INNER_SPACE_RATIO, 0);
            else if (n1 == 2 + 8)
                uv1 = new Vector2(0, TILE_INNER_SPACE_RATIO);

            Vector2 uv2 = new Vector2(1 - TILE_INNER_SPACE_RATIO, TILE_INNER_SPACE_RATIO);
            if (n2 == 1 || n2 == 1 + 8)
                uv2 = new Vector2(1, 0);
            else if (n2 == 1 + 2)
                uv2 = new Vector2(1 - TILE_INNER_SPACE_RATIO, 0);
            else if (n2 == 1 + 4)
                uv2 = new Vector2(1, TILE_INNER_SPACE_RATIO);

            Vector2 uv3 = new Vector2(1 - TILE_INNER_SPACE_RATIO, 1 - TILE_INNER_SPACE_RATIO);
            if (n3 == 4 || n3 == 4 + 2)
                uv3 = new Vector2(1, 1);
            else if (n3 == 4 + 1)
                uv3 = new Vector2(1, 1 - TILE_INNER_SPACE_RATIO);
            else if (n3 == 4 + 8)
                uv3 = new Vector2(1 - TILE_INNER_SPACE_RATIO, 1);

            Vector2 uv4 = new Vector2(TILE_INNER_SPACE_RATIO, 1 - TILE_INNER_SPACE_RATIO);
            if (n4 == 8 || n4 == 8 + 1)
                uv4 = new Vector2(0, 1);
            else if (n4 == 8 + 2)
                uv4 = new Vector2(0, 1 - TILE_INNER_SPACE_RATIO);
            else if (n4 == 8 + 4)
                uv4 = new Vector2(TILE_INNER_SPACE_RATIO, 1);

            int ind1 = InsertVertex5(ref pt1, ref normal, VertexInfo.UV(ref pt1), uv1);
            int ind2 = InsertVertex5(ref pt2, ref normal, VertexInfo.UV(ref pt2), uv2);
            int ind3 = InsertVertex5(ref pt3, ref normal, VertexInfo.UV(ref pt3), uv3);
            int ind4 = InsertVertex5(ref pt4, ref normal, VertexInfo.UV(ref pt4), uv4);

            AddPolygon(ind2, ind1, ind4, ind3);
        }

        public void AddTileTriangle(byte floor, byte sx, byte sy, int neighbor)
        {
            float h = 0.1f;
            Vector3 pt1 = VertexInfo.StartPoint(floor, sx, sy, false);
            pt1.y += h;
            Vector3 pt2 = VertexInfo.StartPoint(floor, (byte)(sx + 1), sy, false);
            pt2.y += h;
            Vector3 pt3 = VertexInfo.StartPoint(floor, (byte)(sx + 1), (byte)(sy + 1), false);
            pt3.y += h;
            Vector3 pt4 = VertexInfo.StartPoint(floor, sx, (byte)(sy + 1), false);
            pt4.y += h;

            if (neighbor == 3)
                AddFanTriangles(ref pt3, ref pt4, ref pt2);
            else if (neighbor == 6)
                AddFanTriangles(ref pt2, ref pt3, ref pt1);
            else if (neighbor == 12)
                AddFanTriangles(ref pt1, ref pt2, ref pt4);
            else
                AddFanTriangles(ref pt4, ref pt1, ref pt3);
        }

        public void AddFanTriangles(int center, int pt1, int pt2)
        {
            Vector3 ct = vertexList[center];
            Vector3 p1 = vertexList[pt1];
            Vector3 p2 = vertexList[pt2];

            AddFanTriangles(ref ct, ref p1, ref p2);
        }
        // pt1 *--------* center
        //              |
        //              |
        //              |
        //              * pt2
        void AddFanTriangles(ref Vector3 center, ref Vector3 pt1, ref Vector3 pt2)
        {
            Vector3 normal = Vector3.up;
            int ind1 = InsertVertex5(ref center, ref normal, VertexInfo.UV(ref center), new Vector2(0.5f, 0.5f));
            int ind2 = InsertVertex5(ref pt1, ref normal, VertexInfo.UV(ref pt1), new Vector2(0, 0));
            int ind3 = InsertVertex5(ref pt2, ref normal, VertexInfo.UV(ref pt2), new Vector2(1, 0));
            const int TOTAL_TRI = 5;
            Vector3 dir = pt1 - center;
            float dirLen1 = dir.magnitude;
            float dirLen2 = (pt2 - center).magnitude;
            int ind0 = ind2;
            for (int i = 0; i < TOTAL_TRI; i++)
            {
                float ratio = (float)(i + 1) / (float)(TOTAL_TRI + 1);
                Vector3 _dir = dir.normalized * (dirLen1 * (1 - ratio) + dirLen2 * ratio);
                Vector3 newPt = Quaternion.AngleAxis(-90f * ratio, Vector3.up) * _dir + center;
                int ind = InsertVertex5(ref newPt, ref normal, VertexInfo.UV(ref newPt), new Vector2(ratio, 0));
                AddTriangle(ind1, ind, ind0);
                ind0 = ind;
            }
            AddTriangle(ind1, ind3, ind0);
        }
        public void AddBasePlane(byte sx, byte sy, int width, int height)
        {
            byte floor = 0;
            float h = -cellY;
            Vector3 pt1 = VertexInfo.StartPoint(floor, sx, sy, false);
            pt1.y += h;
            Vector3 pt2 = VertexInfo.StartPoint(floor, (byte)(sx + width), sy, false);
            pt2.y += h;
            Vector3 pt3 = VertexInfo.StartPoint(floor, (byte)(sx + width), (byte)(sy + height), false);
            pt3.y += h;
            Vector3 pt4 = VertexInfo.StartPoint(floor, sx, (byte)(sy + height), false);
            pt4.y += h;

            Vector3 normal = Vector3.up;

            int ind1 = InsertVertex3(ref pt1, ref normal, VertexInfo.UV(ref pt1));
            int ind2 = InsertVertex3(ref pt2, ref normal, VertexInfo.UV(ref pt2));
            int ind3 = InsertVertex3(ref pt3, ref normal, VertexInfo.UV(ref pt3));
            int ind4 = InsertVertex3(ref pt4, ref normal, VertexInfo.UV(ref pt4));

            AddPolygon(ind2, ind1, ind4, ind3);
        }

        public class NeighborInfo
        {
            public bool onlyTop;
            public byte normalType;
            public bool hasReverse;
            public NeighborInfo(bool onlyTop, byte normalType, bool hasReverse = false)
            {
                this.onlyTop = onlyTop;
                this.normalType = normalType;
                this.hasReverse = hasReverse;
            }
            public NeighborInfo Adjust(int waterNeighborIndex, int landNeighborIndex)
            {
                if (
                    // 모든 cell이 땅/물로 막혀 있거나
                    (((waterNeighborIndex & 1) != 0 || (landNeighborIndex & 1) != 0)
                    && ((waterNeighborIndex & 2) != 0 || (landNeighborIndex & 2) != 0)
                    && ((waterNeighborIndex & 4) != 0 || (landNeighborIndex & 4) != 0)
                    && ((waterNeighborIndex & 8) != 0 || (landNeighborIndex & 8) != 0))
                    // 물cell 좌우로 땅으로 막힐 경우에는 top plane만 있다.
                    || (waterNeighborIndex == 1 && landNeighborIndex == 6)
                    || (waterNeighborIndex == 2 && landNeighborIndex == 9)
                    || (waterNeighborIndex == 4 && landNeighborIndex == 9)
                    || (waterNeighborIndex == 8 && landNeighborIndex == 6))
                    return new NeighborInfo(true, 0);
                return this;
            }
        }
        //       ^ y
        //       |
        //   +---+---+
        //   | 1 | 2 |
        //   +---0---+-->x
        //   | 4 | 8 |
        //   +---+---+
        public static readonly NeighborInfo[] neighborInfo = new NeighborInfo[16]
        {
            new NeighborInfo(true, 0), // 모두 없을때
            new NeighborInfo(false, NORM_SE),
            new NeighborInfo(false, NORM_SW),
            new NeighborInfo(false, NORM_S),
            new NeighborInfo(false, NORM_NE),
            new NeighborInfo(false, NORM_E),
            new NeighborInfo(false, NORM_SE, true),
            new NeighborInfo(false, NORM_SE),
            new NeighborInfo(false, NORM_NW),
            new NeighborInfo(false, NORM_NE, true),
            new NeighborInfo(false, NORM_W),
            new NeighborInfo(false, NORM_SW),
            new NeighborInfo(false, NORM_N),
            new NeighborInfo(false, NORM_NE),
            new NeighborInfo(false, NORM_NW),
            new NeighborInfo(true, 0),
        };

        public void CreateMesh(Transform parent, string meshName, Material material, bool createCollider, bool castShadow = true, int planeIndex = 0)
        {
            if (triangleList.Count > 0)
            {
                GameObject obj = new GameObject(meshName);
                obj.transform.SetParent(parent);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;

                MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
                Mesh mesh = new Mesh();
                meshFilter.mesh = mesh;
                mesh.vertices = vertexList.ToArray();
                mesh.uv = uvList.ToArray();
                mesh.normals = normalList.ToArray();
                if (uv2List.Count == vertexList.Count)
                    mesh.uv2 = uv2List.ToArray();
                mesh.triangles = triangleList.ToArray();
                mesh.RecalculateBounds();
                MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                renderer.material = material;
                renderer.shadowCastingMode = castShadow ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;

                if (createCollider)
                {
                    MeshCollider collider = obj.AddComponent<MeshCollider>();
                    collider.sharedMesh = mesh;
                    if (planeIndex == PLANE_WATER)
                    {
                        collider.convex = true;
                        collider.isTrigger = true;
                        obj.tag = Definitions.TAG_WATER;
                    }
                }
#if UNITY_EDITOR
                //UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/Prefabs/Test/" + meshName + ".mesh");
#endif
            }
        }
    }
    */
    protected interface CellGroup
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
    /*
    class CellGroupTerrain : CellGroup
    {
        public int index;
        public List<VertexInfo> planeList;
        public GameObject groupObject;
        public GameObject propParentObject;
        bool reset = false;

        List<List<CellGroup.PropData>> propList;
        public CellGroupTerrain(int index, List<TileInfo> tileMaterials, TilePropInfo[] propInfo)
        {
            reset = true;
            this.index = index;
            planeList = new List<VertexInfo>();
            for (int i = 0; i < tileMaterials.Count; i++)
                planeList.Add(new VertexInfo());
            groupObject = null;
            propList = new List<List<CellGroup.PropData>>();
            if (propInfo != null)
            {
                for (int i = 0; i < propInfo.Length; i++)
                    propList.Add(new List<CellGroup.PropData>());
            }
        }
        
        public bool IsReset()
        {
            return reset;
        }
        public void Reset(List<TileInfo> tileMaterials, TilePropInfo[] propInfo)
        {
            if (reset)
                return;
            reset = true;
            for (int i = 0; i < planeList.Count; i++)
                planeList[i].Reset();
            if (tileMaterials != null && tileMaterials.Count > planeList.Count)
            {
                for (int i = planeList.Count; i < tileMaterials.Count; i++)
                    planeList.Add(new VertexInfo());
            }
            if (propInfo != null)
            {
                for (int i = 0; i < propInfo.Length; i++)
                {
                    if (propList[i].Count > 0)
                    {
                        propInfo[i].Return(propList[i]);
                        propList[i].Clear();
                    }
                }
            }
            if (propParentObject != null)
            {
                GameObject.Destroy(propParentObject);
                propParentObject = null;
            }
            if (groupObject != null)
            {
                GameObject.Destroy(groupObject);
                groupObject = null;
            }
        }
        public GameObject AddProp(int propIndex, TilePropInfo info, Vector3 localPosition, int cellIndex, int diffFloor = -1)
        {
            if (propParentObject == null)
            {
                propParentObject = new GameObject("props");
                if (groupObject != null)
                    propParentObject.transform.SetParent(groupObject.transform);
                propParentObject.transform.localPosition = Vector3.zero;
                propParentObject.transform.localRotation = Quaternion.identity;
                propParentObject.transform.localScale = Vector3.one;
            }

            GameObject obj = info.GetProp();
            obj.transform.SetParent(propParentObject.transform);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            propList[propIndex].Add(new CellGroup.PropData(obj, cellIndex));
            obj.GetComponent<WaterFall>()?.SetFogPosition(diffFloor);
            return obj;
        }
        public void RemoveTileProps(TilePropInfo[] propInfo, int cellIndex)
        {
            if (propInfo != null)
            {
                for (int i = 0; i < propInfo.Length; i++)
                {
                    if (propList[i].Count > 0)
                        propInfo[i].ReturnAt(propList[i], cellIndex);
                }
            }
        }
        public void CreateGroup(Transform parent, List<TileInfo> tileMaterials, bool isDefaultTerrain)
        {
            if (groupObject != null)
                return;
            reset = false;
            string prefix = isDefaultTerrain ? "terrain" : "tile";
            byte floor, x, y;
            GetFloorXYFromIndex(index, out floor, out x, out y);
            GameObject group = new GameObject(string.Format("{0}[{1}][{2}][{3}]", prefix, floor, x, y));
            group.transform.SetParent(parent);
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;
            groupObject = group;
            if (propParentObject != null)
            {
                propParentObject.transform.SetParent(groupObject.transform);
                propParentObject.transform.localPosition = Vector3.zero;
                propParentObject.transform.localRotation = Quaternion.identity;
                propParentObject.transform.localScale = Vector3.one;
            }

            for (int i = 0; i < planeList.Count; i++)
            {
                VertexInfo info = planeList[i];
                bool castShadow = isDefaultTerrain;
                info.CreateMesh(group.transform, "plane" + i, tileMaterials[i].material, isDefaultTerrain && (i == PLANE_LAND || i == PLANE_LAND_SIDE || i == PLANE_WALL || i == PLANE_WATER_WALL || i == PLANE_WATER), castShadow, i);
            }
        }
    }
    */
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

    /*
    int AddOneSideVertex(byte floor, VertexInfo info, byte[] cellArr, int width, int height, byte x, byte y, int planeNormalType = -1)
    {
        int neighborIndex = GetNeighborLandIndexAtPoint(floor, cellArr, width, height, x, y);
        info.AddVertex(floor, x, y, VertexInfo.neighborInfo[neighborIndex], PLANE_LAND_SIDE, planeNormalType);
        return neighborIndex;
    }
    void AddTopOneVertex(byte floor, VertexInfo info, byte[] cellArr, int width, int height, byte x, byte y, int planeNormalType = -1)
    {
        info.AddVertex(floor, x, y, VertexInfo.neighborInfo[0], 0, planeNormalType);
    }
    VertexInfo.NeighborInfo AddOneWaterVertex(byte floor, VertexInfo info, byte[] cellArr, int width, int height, byte x, byte y, out int waterIndex, out int landNeighborIndex)
    {
        waterIndex = 0;
        landNeighborIndex = GetNeighborIndexAtPoint(floor, cellArr, width, height, x, y, out waterIndex);
        // 우선은 top만 그린다. 가장자리는 따로 그린다.
        VertexInfo.NeighborInfo nInfo = VertexInfo.neighborInfo[waterIndex];
        VertexInfo.NeighborInfo result = nInfo.Adjust(waterIndex, landNeighborIndex);
        //info.AddVertex(x, y, result, PLANE_WATER);
        info.AddVertex(floor, x, y, VertexInfo.neighborInfo[0], PLANE_WATER);
        return result;
    }
    void AddOneWaterTopWallVertex(byte floor, VertexInfo info, byte[] cellArr, int width, int height, byte x, byte y)
    {
        info.AddVertex(floor, x, y, VertexInfo.neighborInfo[15], PLANE_WATER_WALL);
    }

    //0031 0311 3111 1111
    static readonly byte[] SMOOTH_SMALL_CELL_SW = new byte[DIV * DIV]
    {
        CELL_NONE,CELL_NONE,CELL_NONE,CELL_SMOOTH,
        CELL_NONE,CELL_NONE,CELL_SMOOTH,CELL_LAND,
        CELL_NONE,CELL_SMOOTH,CELL_LAND,CELL_LAND,
        CELL_SMOOTH,CELL_LAND,CELL_LAND,CELL_LAND,
    };
    //1300 1130 1113 1111
    static readonly byte[] SMOOTH_SMALL_CELL_SE = new byte[DIV * DIV]
    {
        CELL_SMOOTH,CELL_NONE,CELL_NONE,CELL_NONE,
        CELL_LAND,CELL_SMOOTH,CELL_NONE,CELL_NONE,
        CELL_LAND,CELL_LAND,CELL_SMOOTH,CELL_NONE,
        CELL_LAND,CELL_LAND,CELL_LAND,CELL_SMOOTH,
    };
    //1111 3111 0311 0031
    static readonly byte[] SMOOTH_SMALL_CELL_NW = new byte[DIV * DIV]
    {
        CELL_SMOOTH,CELL_LAND,CELL_LAND,CELL_LAND,
        CELL_NONE,CELL_SMOOTH,CELL_LAND,CELL_LAND,
        CELL_NONE,CELL_NONE,CELL_SMOOTH,CELL_LAND,
        CELL_NONE,CELL_NONE,CELL_NONE,CELL_SMOOTH,
    };
    //1111 1113 1130 1300 
    static readonly byte[] SMOOTH_SMALL_CELL_NE = new byte[DIV * DIV]
    {
        CELL_LAND,CELL_LAND,CELL_LAND,CELL_SMOOTH,
        CELL_LAND,CELL_LAND,CELL_SMOOTH,CELL_NONE,
        CELL_LAND,CELL_SMOOTH,CELL_NONE,CELL_NONE,
        CELL_SMOOTH,CELL_NONE,CELL_NONE,CELL_NONE,
    };
    //0031 0311 3111 1111
    static readonly byte[] WATER_SMOOTH_SMALL_CELL_SW = new byte[DIV * DIV]
    {
        CELL_NONE,CELL_NONE,CELL_NONE,CELL_WATER_SMOOTH,
        CELL_NONE,CELL_NONE,CELL_WATER_SMOOTH,CELL_WATER,
        CELL_NONE,CELL_WATER_SMOOTH,CELL_WATER,CELL_WATER,
        CELL_WATER_SMOOTH,CELL_WATER,CELL_WATER,CELL_WATER,
    };
    //1300 1130 1113 1111
    static readonly byte[] WATER_SMOOTH_SMALL_CELL_SE = new byte[DIV * DIV]
    {
        CELL_WATER_SMOOTH,CELL_NONE,CELL_NONE,CELL_NONE,
        CELL_WATER,CELL_WATER_SMOOTH,CELL_NONE,CELL_NONE,
        CELL_WATER,CELL_WATER,CELL_WATER_SMOOTH,CELL_NONE,
        CELL_WATER,CELL_WATER,CELL_WATER,CELL_WATER_SMOOTH,
    };
    //1111 3111 0311 0031
    static readonly byte[] WATER_SMOOTH_SMALL_CELL_NW = new byte[DIV * DIV]
    {
        CELL_WATER_SMOOTH,CELL_WATER,CELL_WATER,CELL_WATER,
        CELL_NONE,CELL_WATER_SMOOTH,CELL_WATER,CELL_WATER,
        CELL_NONE,CELL_NONE,CELL_WATER_SMOOTH,CELL_WATER,
        CELL_NONE,CELL_NONE,CELL_NONE,CELL_WATER_SMOOTH,
    };
    //1111 1113 1130 1300 
    static readonly byte[] WATER_SMOOTH_SMALL_CELL_NE = new byte[DIV * DIV]
    {
        CELL_WATER,CELL_WATER,CELL_WATER,CELL_WATER_SMOOTH,
        CELL_WATER,CELL_WATER,CELL_WATER_SMOOTH,CELL_NONE,
        CELL_WATER,CELL_WATER_SMOOTH,CELL_NONE,CELL_NONE,
        CELL_WATER_SMOOTH,CELL_NONE,CELL_NONE,CELL_NONE,
    };
    byte[] ConvertToRealCell(byte[] cellArr, int width, int height, out int realWidth, out int realHeight)
    {
        // 기본 cell을 4*4 로 쪼개어서 세밀한 표현을 한다.
        realWidth = width * DIV;
        realHeight = height * DIV;
        byte[] result = new byte[realWidth * realHeight];
        for (byte floor = 0; floor < MAX_FLOOR; floor++)
        {
            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    byte cellType = cellArr[index];
                    if (cellType == CELL_SMOOTH)
                    {
                        int neighborCell = GetNeighborCellIndex(floor, cellArr, width, height, x, y);
                        int neighborWater = GetNeighborWaterCellIndex(floor, cellArr, width, height, x, y);
                        bool makeWaterCell = false;
                        byte[] arr = null;
                        // norm_sw 방향
                        if ((neighborCell & 4) == 0 && (neighborCell & 8) == 0)
                        {
                            arr = SMOOTH_SMALL_CELL_SW;
                            if ((neighborWater & 4) != 0 || (neighborWater & 8) != 0)
                                makeWaterCell = true;
                        }
                        // norm_se 방향
                        else if ((neighborCell & 2) == 0 && (neighborCell & 4) == 0)
                        {
                            arr = SMOOTH_SMALL_CELL_SE;
                            if ((neighborWater & 2) != 0 || (neighborWater & 4) != 0)
                                makeWaterCell = true;
                        }
                        // norm_nw 방향
                        else if ((neighborCell & 1) == 0 && (neighborCell & 8) == 0)
                        {
                            arr = SMOOTH_SMALL_CELL_NW;
                            if ((neighborWater & 1) != 0 || (neighborWater & 8) != 0)
                                makeWaterCell = true;
                        }
                        // norm_ne 방향
                        else if ((neighborCell & 1) == 0 && (neighborCell & 2) == 0)
                        {
                            arr = SMOOTH_SMALL_CELL_NE;
                            if ((neighborWater & 1) != 0 || (neighborWater & 2) != 0)
                                makeWaterCell = true;
                        }

                        for (int dy = 0; dy < DIV; dy++)
                        {
                            for (int dx = 0; dx < DIV; dx++)
                            {
                                byte type = arr[dx + dy * DIV];
                                int ind = x * DIV + dx + (y * DIV + dy) * realWidth;
                                result[ind] = makeWaterCell && type == CELL_NONE ? CELL_WATER : type;
                            }
                        }
                    }
                    else if (cellType == CELL_WATER_SMOOTH)
                    {
                        byte[] arr = null;
                        int neighborCell = GetNeighborWaterCellIndex(floor, cellArr, width, height, x, y);
                        // norm_sw 방향
                        if ((neighborCell & 4) == 0 && (neighborCell & 8) == 0)
                        {
                            arr = WATER_SMOOTH_SMALL_CELL_SW;
                        }
                        // norm_se 방향
                        else if ((neighborCell & 2) == 0 && (neighborCell & 4) == 0)
                        {
                            arr = WATER_SMOOTH_SMALL_CELL_SE;
                        }
                        // norm_nw 방향
                        else if ((neighborCell & 1) == 0 && (neighborCell & 8) == 0)
                        {
                            arr = WATER_SMOOTH_SMALL_CELL_NW;
                        }
                        // norm_ne 방향
                        else if ((neighborCell & 1) == 0 && (neighborCell & 2) == 0)
                        {
                            arr = WATER_SMOOTH_SMALL_CELL_NE;
                        }

                        for (int dy = 0; dy < DIV; dy++)
                        {
                            for (int dx = 0; dx < DIV; dx++)
                            {
                                byte type = arr[dx + dy * DIV];
                                int ind = x * DIV + dx + (y * DIV + dy) * realWidth;
                                result[ind] = type;
                            }
                        }
                    }
                    else
                    {
                        for (int dy = 0; dy < DIV; dy++)
                        {
                            for (int dx = 0; dx < DIV; dx++)
                            {
                                int ind = x * DIV + dx + (y * DIV + dy) * realWidth;
                                result[ind] = cellType;
                            }
                        }
                    }
                }
            }
        }
        return result;
    }
    */
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

    AssetLoadHelper _helper;
    AssetLoadHelper _GetHelper(string tileId)
    {
        if (null == _helper)
            _helper = new AssetLoadHelper(new AssetBasisInfo(tileId, tileId, eAssetType.MaterialItem));
        return _helper;
    }
    /// <summary>
    /// material을 로딩한 다음에, callback으로 tileIndex, tileItemId 를 return해 준다.
    /// </summary>
    /// <param name="tileId"></param>
    /// <param name="callback"></param>
    public void AddTileMaterial(string tileId, Callback<int, string> callback)
    {
        for (int i = 0; i < tileMaterials.Count; i++)
        {
            if (null != tileMaterials[i] && tileMaterials[i].tileItemID == tileId)
            {
                callback?.Invoke(i, tileId);
                return;
            }
        }
        if (tileMaterials.Count >= MAX_TILE)
        {
            callback?.Invoke(-1, tileId);
            return;
        }
        _GetHelper(tileId);
        _helper.LoadMaterialItem(tileId, (assetRef, param, state) =>
        {
            Debug.Assert(assetRef.isLoaded);
            Debug.Assert(assetRef is AssetRef_Material);
            tileMaterials.Add(new TileInfo(tileId, assetRef.getAsset_Material));
            callback?.Invoke(tileMaterials.Count - 1, tileId);
        });
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
                        Material defaultMat = AssetUtil.LoadResource<Material>(AssetUtil.LOADING_MATERIAL);
                        tileMaterials[i] = new TileInfo(tileItemId, defaultMat);
                    }
                    else
                    {
                        // owner의 함수로 callback하는 이유는, 이 terrain이 destroy되면 callback이 오지 않도록 하기 위함.
                        owner._GetHelper(tileItemId).LoadMaterialItem(tileItemId, owner._Callback_Loaded_Material, i);
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

    void _Callback_Loaded_Material(AssetRef assetRef, AssetLoadParam param, eAssetState state)
    {
        Debug.Assert(null != param);
        Debug.Assert(assetRef.isLoaded);
        Debug.Assert(assetRef is AssetRef_Material);
        int index = param.index1;
        if (null != _param)
            _param.SetTileMaterial(index, new TileInfo(assetRef.itemId, assetRef.getAsset_Material));
    }

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
                CreateTerrain(_param.width, _param.height, _param.cellMap, _param.tileMaterials, _param.tileMap);
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
        CLogger.Log("CreateTerrain - " + width + "," + height);
        foreach (var d in groupMap)
            d.Value.Reset(baseMaterialList, propInfo);
        foreach (var d in tileGroupMap)
            d.Value.Reset(tileMaterials, propInfo);
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

        this.tileMaterials = tileMaterials;
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
            CLogger.LogError("CreateTerrain - width or height must be under 255.");
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
        /*
        int width = terrainWidth;
        int height = terrainHeight;
        CheckCellArr(cellArr, width, height, tileArr);

        for (byte floor = 0; floor < MAX_FLOOR; floor++)
        {
            for (byte y = 0; y < height; y++)
            {
                for (byte x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    int groupIndex = CellGroupTerrain.GetCellGroupIndex(floor, x, y);

                    CellGroup _curGroup;
                    if (!groupMap.TryGetValue(groupIndex, out _curGroup))
                    {
                        _curGroup = new CellGroupTerrain(groupIndex, baseMaterialList, propInfo);
                        groupMap[groupIndex] = _curGroup;
                    }
                    CellGroupTerrain curGroup = (CellGroupTerrain)_curGroup;

                    byte originalCellType = cellArr[index];
                    byte cellType = GetBaseCellOnFloor(floor, originalCellType, x, y);
                    byte tileIndex = tileArr[index];
                    // 바뀐 그룹만 계산한다.
                    if (curGroup.IsReset())
                    {
                        TilePropInfo.SetRandomeSeed(x, y);
                        int cellIndexForProp = VertexInfo.MapIndex(0, x, y);

                        Vector3 startPosition = VertexInfo.StartPoint(floor, x, y);
                        List<VertexInfo> infoList = curGroup.planeList;
                        bool makeWaterCell = cellType == CELL_WATER || cellType >= CELL_HALF_WATER;
                        int wallCreation = 0;
                        if (cellType == CELL_LAND)
                        {
                            VertexInfo info = infoList[PLANE_LAND];
                            VertexInfo info3 = infoList[PLANE_LAND_SIDE];
                            AddTopOneVertex(floor, info, cellArr, width, height, x, y);
                            AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), y);
                            AddTopOneVertex(floor, info, cellArr, width, height, x, (byte)(y + 1));
                            AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));

                            int neighborIndex1 = AddOneSideVertex(floor, info3, cellArr, width, height, x, y);
                            AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), y);
                            int neighborIndex3 = AddOneSideVertex(floor, info3, cellArr, width, height, x, (byte)(y + 1));
                            int neighborIndex4 = AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));

                            //----------------
                            // 1. land plane
                            //----------------
                            info.AddTopPlane(floor, x, y);
                            // bottom side plane을 그려야 하는 경우 : 아래쪽 cell이 비어있을때,
                            if ((neighborIndex1 & 8) == 0)
                                info3.AddTopSidePlane(floor, (byte)(x + 1), y, x, y, false);
                            // left side plane을 그려야 하는 경우 : 왼쪽 cell이 비어있을때,
                            // 단, 대각선 좌상단에 cell이 있다면, isYUp = false로 해서 아랫쪽을 바라보는 vertex로 그려야 한다.
                            if ((neighborIndex1 & 1) == 0)
                                info3.AddTopSidePlane(floor, x, y, x, (byte)(y + 1), (neighborIndex3 & 1) == 0);
                            // top side plane을 그려야 하는 경우 : 위쪽 cell이 비어있을때,
                            if ((neighborIndex4 & 1) == 0)
                                info3.AddTopSidePlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1));
                            // right side plane을 그려야 하는 경우 : 오른쪽 cell이 비어있을때,
                            // 단, 대각선 우상단에 cell이 있다면, isYUp = false로 해서 아랫쪽을 바라보는 vertex로 그려야 한다.
                            if ((neighborIndex4 & 8) == 0)
                                info3.AddTopSidePlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, (neighborIndex4 & 2) == 0);

                            //----------------
                            // 2. wall plane
                            //----------------
                            // 아래쪽 cell이 빌때
                            info = infoList[PLANE_WALL];
                            if ((neighborIndex1 & 8) == 0)
                                info.AddWallPlane(floor, (byte)(x + 1), y, x, y, NORM_S);
                            // 왼쪽이 빌때
                            if ((neighborIndex1 & 1) == 0)
                                info.AddWallPlane(floor, x, y, x, (byte)(y + 1), NORM_W);
                            // 위쪽이 빌때
                            if ((neighborIndex4 & 1) == 0)
                                info.AddWallPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), NORM_N);
                            // 오른쪽이 빌때
                            if ((neighborIndex4 & 8) == 0)
                                info.AddWallPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, NORM_E);

                            if (grassPropInfo1 >= 0 && tileIndex == TILE_NONE)
                                propInfo[grassPropInfo1].PlaceRandomOnRectTile(startPosition, curGroup, cellIndexForProp);
                            if (grassPropInfo2 >= 0 && tileIndex == TILE_NONE)
                                propInfo[grassPropInfo2].PlaceRandomOnRectTile(startPosition, curGroup, cellIndexForProp);
                        }
                        else if (cellType == CELL_SMOOTH || cellType == CELL_SMOOTH + CELL_HALF_WATER)
                        {
                            VertexInfo info = infoList[PLANE_LAND];
                            VertexInfo info2 = infoList[PLANE_WALL];
                            VertexInfo info3 = infoList[PLANE_LAND_SIDE];

                            int neighborCell = GetNeighborCellIndex(floor, cellArr, width, height, x, y);
                            int neighborWater = GetNeighborWaterCellIndex(floor, cellArr, width, height, x, y);
                            bool isNormSW = (neighborCell & 4) == 0 && (neighborCell & 8) == 0;
                            bool isNormSE = (neighborCell & 2) == 0 && (neighborCell & 4) == 0;
                            bool isNormNW = (neighborCell & 1) == 0 && (neighborCell & 8) == 0;
                            bool isNormNE = (neighborCell & 1) == 0 && (neighborCell & 2) == 0;

                            AddTopOneVertex(floor, info, cellArr, width, height, x, y);
                            AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), y);
                            AddTopOneVertex(floor, info, cellArr, width, height, x, (byte)(y + 1));
                            AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));

                            // 뾰족점일 경우에는, normal을 보정한다. 그래야 파묻히지 않는다.
                            AddOneSideVertex(floor, info3, cellArr, width, height, x, y, (isNormNW && (neighborCell & 4) == 0) ? NORM_NW : (isNormSE && (neighborCell & 8) == 0 ? NORM_SE : -1));
                            AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), y, (isNormNE && (neighborCell & 4) == 0) ? NORM_NE : (isNormSW && (neighborCell & 2) == 0 ? NORM_SW : -1));
                            AddOneSideVertex(floor, info3, cellArr, width, height, x, (byte)(y + 1), (isNormNE && (neighborCell & 8) == 0) ? NORM_NE : (isNormSW && (neighborCell & 1) == 0 ? NORM_SW : -1));
                            AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), (byte)(y + 1), (isNormNW && (neighborCell & 2) == 0) ? NORM_NW : (isNormSE && (neighborCell & 1) == 0 ? NORM_SE : -1));

                            byte normalType = 0;
                            // norm_sw 방향
                            if (isNormSW)
                            {
                                normalType = NORM_SW;
                                //info.AddTriangle(pt2, pt3, pt4);
                                //info.AddTopSidePlane(floor, (byte)(x + 1), y, x, (byte)(y + 1), false);
                                info.AddTopFanTriangles(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, x, (byte)(y + 1));
                                info3.AddSideFanTriangles(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, x, (byte)(y + 1), false);

                                //info2.AddWallPlane(floor, (byte)(x + 1), y, x, (byte)(y + 1), NORM_SW);
                                info2.AddWallFanPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, x, (byte)(y + 1), NORM_SW);
                                // 옆면이 없다면, 그려준다.
                                if ((neighborCell & 2) == 0)
                                {
                                    info3.AddTopSidePlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, false);
                                    info2.AddWallPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, NORM_E);
                                    wallCreation += 2;
                                }
                                if ((neighborCell & 1) == 0)
                                {
                                    info3.AddTopSidePlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), true);
                                    info2.AddWallPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), NORM_N);
                                    wallCreation += 1;
                                }
                                //if ((neighborWater & 4) != 0 || (neighborWater & 8) != 0)
                                //  makeWaterCell = true;
                            }
                            // norm_se 방향
                            else if (isNormSE)
                            {
                                normalType = NORM_SE;
                                //info.AddTriangle(pt1, pt3, pt4);
                                //info.AddTopSidePlane(floor, (byte)(x + 1), (byte)(y + 1), x, y, false);
                                info.AddTopFanTriangles(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), x, y);
                                info3.AddSideFanTriangles(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), x, y, false);

                                //info2.AddWallPlane(floor, (byte)(x + 1), (byte)(y + 1), x, y, NORM_SE);
                                info2.AddWallFanPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), x, y, NORM_SE);
                                // 옆면이 없다면, 그려준다.
                                if ((neighborCell & 8) == 0)
                                {
                                    info3.AddTopSidePlane(floor, x, y, x, (byte)(y + 1), false);
                                    info2.AddWallPlane(floor, x, y, x, (byte)(y + 1), NORM_W);
                                    wallCreation += 8;
                                }
                                if ((neighborCell & 1) == 0)
                                {
                                    info3.AddTopSidePlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), true);
                                    info2.AddWallPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), NORM_N);
                                    wallCreation += 1;
                                }
                                //if ((neighborWater & 2) != 0 || (neighborWater & 4) != 0)
                                //  makeWaterCell = true;
                            }
                            // norm_nw 방향
                            else if (isNormNW)
                            {
                                normalType = NORM_NW;
                                //info.AddTriangle(pt2, pt1, pt4);
                                //info.AddTopSidePlane(floor, x, y, (byte)(x + 1), (byte)(y + 1), true);
                                info.AddTopFanTriangles(floor, (byte)(x + 1), y, x, y, (byte)(x + 1), (byte)(y + 1));
                                info3.AddSideFanTriangles(floor, (byte)(x + 1), y, x, y, (byte)(x + 1), (byte)(y + 1), true);

                                //info2.AddWallPlane(floor, x, y, (byte)(x + 1), (byte)(y + 1), NORM_NW);
                                info2.AddWallFanPlane(floor, (byte)(x + 1), y, x, y, (byte)(x + 1), (byte)(y + 1), NORM_NW);
                                // 옆면이 없다면, 그려준다.
                                if ((neighborCell & 2) == 0)
                                {
                                    info3.AddTopSidePlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, false);
                                    info2.AddWallPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, NORM_E);
                                    wallCreation += 2;
                                }
                                if ((neighborCell & 4) == 0)
                                {
                                    info3.AddTopSidePlane(floor, (byte)(x + 1), y, x, y, false);
                                    info2.AddWallPlane(floor, (byte)(x + 1), y, x, y, NORM_S);
                                    wallCreation += 4;
                                }
                                //if ((neighborWater & 1) != 0 || (neighborWater & 8) != 0)
                                //  makeWaterCell = true;
                            }
                            // norm_ne 방향
                            else if (isNormNE)
                            {
                                normalType = NORM_NE;
                                //info.AddTriangle(pt1, pt3, pt2);
                                //info.AddTopSidePlane(floor, x, (byte)(y + 1), (byte)(x + 1), y, true);
                                info.AddTopFanTriangles(floor, x, y, x, (byte)(y + 1), (byte)(x + 1), y);
                                info3.AddSideFanTriangles(floor, x, y, x, (byte)(y + 1), (byte)(x + 1), y, true);

                                //info2.AddWallPlane(floor, x, (byte)(y + 1), (byte)(x + 1), y, NORM_NE);
                                info2.AddWallFanPlane(floor, x, y, x, (byte)(y + 1), (byte)(x + 1), y, NORM_NE);
                                // 옆면이 없다면, 그려준다.
                                if ((neighborCell & 8) == 0)
                                {
                                    info3.AddTopSidePlane(floor, x, y, x, (byte)(y + 1), false);
                                    info2.AddWallPlane(floor, x, y, x, (byte)(y + 1), NORM_W);
                                    wallCreation += 8;
                                }
                                if ((neighborCell & 4) == 0)
                                {
                                    info3.AddTopSidePlane(floor, (byte)(x + 1), y, x, y, false);
                                    info2.AddWallPlane(floor, (byte)(x + 1), y, x, y, NORM_S);
                                    wallCreation += 4;
                                }
                                //if ((neighborWater & 1) != 0 || (neighborWater & 2) != 0)
                                //  makeWaterCell = true;
                            }

                            if (grassPropInfo1 >= 0)
                                propInfo[grassPropInfo1].PlaceRandomOnHalfTile(startPosition, curGroup, normalType, cellIndexForProp);
                            if (grassPropInfo2 >= 0)
                                propInfo[grassPropInfo2].PlaceRandomOnHalfTile(startPosition, curGroup, normalType, cellIndexForProp);
                        }

                        if (makeWaterCell)
                        {
                            // 임시로 바꾼다.
                            cellArr[index] = GetCellOfFloor(floor, CELL_WATER);

                            VertexInfo info = infoList[PLANE_WATER];
                            int waterIndex1, waterIndex2, waterIndex3, waterIndex4;
                            int neighborIndex1, neighborIndex2, neighborIndex3, neighborIndex4;
                            VertexInfo.NeighborInfo neighbor1 = AddOneWaterVertex(floor, info, cellArr, width, height, x, y, out waterIndex1, out neighborIndex1);
                            VertexInfo.NeighborInfo neighbor2 = AddOneWaterVertex(floor, info, cellArr, width, height, (byte)(x + 1), y, out waterIndex2, out neighborIndex2);
                            VertexInfo.NeighborInfo neighbor3 = AddOneWaterVertex(floor, info, cellArr, width, height, x, (byte)(y + 1), out waterIndex3, out neighborIndex3);
                            VertexInfo.NeighborInfo neighbor4 = AddOneWaterVertex(floor, info, cellArr, width, height, (byte)(x + 1), (byte)(y + 1), out waterIndex4, out neighborIndex4);
                            int neighborCell = GetNeighborCellIndex(floor, cellArr, width, height, x, y);

                            VertexInfo info2 = infoList[PLANE_WATER_SIDE];
                            VertexInfo info2_2 = infoList[PLANE_WATER_SIDE2];
                            //----------------
                            // 1. water plane
                            //----------------
                            info.AddTopPlane(floor, x, y);
                            // bottom side plane을 그려야 하는 경우 : 아래쪽 cell이 비어있을때,
                            bool isShownS = (waterIndex1 & 8) == 0 && (neighborCell & 4) == 0 && (wallCreation & 4) == 0;
                            bool isShownW = (waterIndex1 & 1) == 0 && (neighborCell & 8) == 0 && (wallCreation & 8) == 0;
                            bool isShownN = (waterIndex4 & 1) == 0 && (neighborCell & 1) == 0 && (wallCreation & 1) == 0;
                            bool isShownE = (waterIndex4 & 8) == 0 && (neighborCell & 2) == 0 && (wallCreation & 2) == 0;
                            if (isShownS)
                            {
                                info2.AddWaterSidePlane(floor, (byte)(x + 1), y, neighbor2.normalType, x, y, neighbor1.normalType);
                                info2_2.AddWaterSidePlane(floor, (byte)(x + 1), y, neighbor2.normalType, x, y, neighbor1.normalType, waterSide2Offset);
                            }
                            // left side plane을 그려야 하는 경우 : 왼쪽 cell이 비어있을때,
                            // 단, 대각선 좌상단에 cell이 있다면, isYUp = false로 해서 아랫쪽을 바라보는 vertex로 그려야 한다.
                            if (isShownW)
                            {
                                info2.AddWaterSidePlane(floor, x, y, neighbor1.normalType, x, (byte)(y + 1), neighbor3.normalType);
                                info2_2.AddWaterSidePlane(floor, x, y, neighbor1.normalType, x, (byte)(y + 1), neighbor3.normalType, waterSide2Offset);
                            }

                            // top side plane을 그려야 하는 경우 : 위쪽 cell이 비어있을때,
                            if (isShownN)
                            {
                                info2.AddWaterSidePlane(floor, x, (byte)(y + 1), neighbor3.normalType, (byte)(x + 1), (byte)(y + 1), neighbor4.normalType);
                                info2_2.AddWaterSidePlane(floor, x, (byte)(y + 1), neighbor3.normalType, (byte)(x + 1), (byte)(y + 1), neighbor4.normalType, waterSide2Offset);
                            }

                            // right side plane을 그려야 하는 경우 : 오른쪽 cell이 비어있을때,
                            // 단, 대각선 우상단에 cell이 있다면, isYUp = false로 해서 아랫쪽을 바라보는 vertex로 그려야 한다.
                            if (isShownE)
                            {
                                info2.AddWaterSidePlane(floor, (byte)(x + 1), (byte)(y + 1), neighbor4.normalType, (byte)(x + 1), y, neighbor2.normalType);
                                info2_2.AddWaterSidePlane(floor, (byte)(x + 1), (byte)(y + 1), neighbor4.normalType, (byte)(x + 1), y, neighbor2.normalType, waterSide2Offset);
                            }


                            //----------------
                            // 2. wall plane
                            //----------------
                            info = infoList[PLANE_WATER_WALL];
                            AddOneWaterTopWallVertex(floor, info, cellArr, width, height, x, y);
                            AddOneWaterTopWallVertex(floor, info, cellArr, width, height, (byte)(x + 1), y);
                            AddOneWaterTopWallVertex(floor, info, cellArr, width, height, x, (byte)(y + 1));
                            AddOneWaterTopWallVertex(floor, info, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));

                            info.AddTopPlane(floor, x, y);
                            // 아래쪽 cell이 빌때
                            if (isShownS)
                            {
                                info.AddWallPlane(floor, (byte)(x + 1), y, x, y, NORM_S, waterPlaneDepth);
                                if (waterfallPropInfo >= 0)
                                    propInfo[waterfallPropInfo].PlaceWaterFall(startPosition, curGroup, NORM_S, cellIndexForProp, GetDiffFloor(floor, cellArr, width, height, x, y, NORM_S));
                            }
                            // 왼쪽이 빌때
                            if (isShownW)
                            {
                                info.AddWallPlane(floor, x, y, x, (byte)(y + 1), NORM_W, waterPlaneDepth);
                                if (waterfallPropInfo >= 0)
                                    propInfo[waterfallPropInfo].PlaceWaterFall(startPosition, curGroup, NORM_W, cellIndexForProp, GetDiffFloor(floor, cellArr, width, height, x, y, NORM_W));
                            }
                            // 위쪽이 빌때
                            if (isShownN)
                            {
                                info.AddWallPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), NORM_N, waterPlaneDepth);
                                if (waterfallPropInfo >= 0)
                                    propInfo[waterfallPropInfo].PlaceWaterFall(startPosition, curGroup, NORM_N, cellIndexForProp, GetDiffFloor(floor, cellArr, width, height, x, y, NORM_N));
                            }
                            // 오른쪽이 빌때
                            if (isShownE)
                            {
                                info.AddWallPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, NORM_E, waterPlaneDepth);
                                if (waterfallPropInfo >= 0)
                                    propInfo[waterfallPropInfo].PlaceWaterFall(startPosition, curGroup, NORM_E, cellIndexForProp, GetDiffFloor(floor, cellArr, width, height, x, y, NORM_E));
                            }

                            //----------------
                            // 3. land smooth plane.
                            //----------------
                            info = infoList[PLANE_LAND];
                            info2 = infoList[PLANE_WALL];
                            VertexInfo info3 = infoList[PLANE_LAND_SIDE];
                            neighborCell = GetNeighborCellIndex(floor, cellArr, width, height, x, y, true);

                            bool isNormSW = (neighborCell & 1) != 0 && (neighborCell & 2) != 0;
                            bool isNormSE = (neighborCell & 8) != 0 && (neighborCell & 1) != 0;
                            bool isNormNW = (neighborCell & 2) != 0 && (neighborCell & 4) != 0;
                            bool isNormNE = (neighborCell & 4) != 0 && (neighborCell & 8) != 0;

                            if (isNormSW)
                            {
                                AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));
                                AddTopOneVertex(floor, info, cellArr, width, height, x, (byte)(y + 1));
                                AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), y);

                                info.AddTopHalfInverseFanTriangles(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, x, (byte)(y + 1));
                                info2.AddWallHalfInverseFanPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, x, (byte)(y + 1), NORM_SW);

                                AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));
                                AddOneSideVertex(floor, info3, cellArr, width, height, x, (byte)(y + 1));
                                AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), y);

                                info3.AddSideHalfInverseFanTriangles(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, x, (byte)(y + 1), false);
                            }
                            if (isNormNW)
                            {
                                AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));
                                AddTopOneVertex(floor, info, cellArr, width, height, x, y);
                                AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), y);

                                info.AddTopHalfInverseFanTriangles(floor, (byte)(x + 1), y, x, y, (byte)(x + 1), (byte)(y + 1));
                                info2.AddWallHalfInverseFanPlane(floor, (byte)(x + 1), y, x, y, (byte)(x + 1), (byte)(y + 1), NORM_NW);

                                AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));
                                AddOneSideVertex(floor, info3, cellArr, width, height, x, y);
                                AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), y);

                                info3.AddSideHalfInverseFanTriangles(floor, (byte)(x + 1), y, x, y, (byte)(x + 1), (byte)(y + 1), true);
                            }
                            if (isNormNE)
                            {
                                AddTopOneVertex(floor, info, cellArr, width, height, x, (byte)(y + 1));
                                AddTopOneVertex(floor, info, cellArr, width, height, x, y);
                                AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), y);

                                info.AddTopHalfInverseFanTriangles(floor, x, y, x, (byte)(y + 1), (byte)(x + 1), y);
                                info2.AddWallHalfInverseFanPlane(floor, x, y, x, (byte)(y + 1), (byte)(x + 1), y, NORM_NE);

                                AddOneSideVertex(floor, info3, cellArr, width, height, x, (byte)(y + 1));
                                AddOneSideVertex(floor, info3, cellArr, width, height, x, y);
                                AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), y);

                                info3.AddSideHalfInverseFanTriangles(floor, x, y, x, (byte)(y + 1), (byte)(x + 1), y, true);
                            }
                            if (isNormSE)
                            {
                                AddTopOneVertex(floor, info, cellArr, width, height, x, (byte)(y + 1));
                                AddTopOneVertex(floor, info, cellArr, width, height, x, y);
                                AddTopOneVertex(floor, info, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));

                                info.AddTopHalfInverseFanTriangles(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), x, y);
                                info2.AddWallHalfInverseFanPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), x, y, NORM_SE);

                                AddOneSideVertex(floor, info3, cellArr, width, height, x, (byte)(y + 1));
                                AddOneSideVertex(floor, info3, cellArr, width, height, x, y);
                                AddOneSideVertex(floor, info3, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));

                                info3.AddSideHalfInverseFanTriangles(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), x, y, false);
                            }

                            // 다시 원래대로 변경
                            cellArr[index] = originalCellType;
                        }
                        else if (cellType == CELL_WATER_SMOOTH)
                        {
                            VertexInfo info = infoList[PLANE_WATER];
                            VertexInfo info2 = infoList[PLANE_WATER_WALL];
                            VertexInfo info3 = infoList[PLANE_WATER_SIDE];
                            VertexInfo info3_2 = infoList[PLANE_WATER_SIDE2];

                            int waterIndex1, waterIndex2, waterIndex3, waterIndex4;
                            int neighborIndex1, neighborIndex2, neighborIndex3, neighborIndex4;
                            VertexInfo.NeighborInfo neighbor1 = AddOneWaterVertex(floor, info, cellArr, width, height, x, y, out waterIndex1, out neighborIndex1);
                            VertexInfo.NeighborInfo neighbor2 = AddOneWaterVertex(floor, info, cellArr, width, height, (byte)(x + 1), y, out waterIndex2, out neighborIndex2);
                            VertexInfo.NeighborInfo neighbor3 = AddOneWaterVertex(floor, info, cellArr, width, height, x, (byte)(y + 1), out waterIndex3, out neighborIndex3);
                            VertexInfo.NeighborInfo neighbor4 = AddOneWaterVertex(floor, info, cellArr, width, height, (byte)(x + 1), (byte)(y + 1), out waterIndex4, out neighborIndex4);

                            AddOneWaterTopWallVertex(floor, info2, cellArr, width, height, x, y);
                            AddOneWaterTopWallVertex(floor, info2, cellArr, width, height, (byte)(x + 1), y);
                            AddOneWaterTopWallVertex(floor, info2, cellArr, width, height, x, (byte)(y + 1));
                            AddOneWaterTopWallVertex(floor, info2, cellArr, width, height, (byte)(x + 1), (byte)(y + 1));

                            int neighborCell = GetNeighborWaterCellIndex(floor, cellArr, width, height, x, y);

                            int pt1 = info.GetVertexInfo(floor, x, y, PT_TOP);
                            int pt2 = info.GetVertexInfo(floor, (byte)(x + 1), y, PT_TOP);
                            int pt3 = info.GetVertexInfo(floor, x, (byte)(y + 1), PT_TOP);
                            int pt4 = info.GetVertexInfo(floor, (byte)(x + 1), (byte)(y + 1), PT_TOP);

                            int wpt1 = info2.GetVertexInfo(floor, x, y, PT_TOP);
                            int wpt2 = info2.GetVertexInfo(floor, (byte)(x + 1), y, PT_TOP);
                            int wpt3 = info2.GetVertexInfo(floor, x, (byte)(y + 1), PT_TOP);
                            int wpt4 = info2.GetVertexInfo(floor, (byte)(x + 1), (byte)(y + 1), PT_TOP);
                            // norm_sw 방향
                            if ((neighborCell & 4) == 0 && (neighborCell & 8) == 0)
                            {
                                //info.AddTriangle(pt2, pt3, pt4);
                                //info3.AddWaterSidePlane(floor, (byte)(x + 1), y, NORM_S, x, (byte)(y + 1), NORM_W);
                                info.AddFanTriangles(pt4, pt3, pt2);
                                info3.AddWaterSideFanPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, NORM_S, x, (byte)(y + 1), NORM_W);
                                info3_2.AddWaterSideFanPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, NORM_S, x, (byte)(y + 1), NORM_W, waterSide2Offset);

                                info2.AddFanTriangles(wpt4, wpt3, wpt2);
                                info2.AddWallFanPlane(floor, (byte)(x + 1), (byte)(y + 1), (byte)(x + 1), y, x, (byte)(y + 1), NORM_SW, waterPlaneDepth);

                                if (waterfallPropInfo >= 0)
                                    propInfo[waterfallPropInfo].PlaceWaterFall(startPosition, curGroup, NORM_SW, cellIndexForProp, GetDiffFloor(floor, cellArr, width, height, x, y, NORM_SW));
                            }
                            // norm_se 방향
                            else if ((neighborCell & 2) == 0 && (neighborCell & 4) == 0)
                            {
                                //info.AddTriangle(pt1, pt3, pt4);
                                //info3.AddWaterSidePlane(floor, (byte)(x + 1), (byte)(y + 1), NORM_E, x, y, NORM_S);
                                info.AddFanTriangles(pt3, pt1, pt4);
                                info3.AddWaterSideFanPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), NORM_E, x, y, NORM_S);
                                info3_2.AddWaterSideFanPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), NORM_E, x, y, NORM_S, waterSide2Offset);

                                info2.AddFanTriangles(wpt3, wpt1, wpt4);
                                info2.AddWallFanPlane(floor, x, (byte)(y + 1), (byte)(x + 1), (byte)(y + 1), x, y, NORM_SE, waterPlaneDepth);

                                if (waterfallPropInfo >= 0)
                                    propInfo[waterfallPropInfo].PlaceWaterFall(startPosition, curGroup, NORM_SE, cellIndexForProp, GetDiffFloor(floor, cellArr, width, height, x, y, NORM_SE));
                            }
                            // norm_nw 방향
                            else if ((neighborCell & 1) == 0 && (neighborCell & 8) == 0)
                            {
                                //info.AddTriangle(pt2, pt1, pt4);
                                //info3.AddWaterSidePlane(floor, x, y, NORM_W, (byte)(x + 1), (byte)(y + 1), NORM_N);
                                info.AddFanTriangles(pt2, pt4, pt1);
                                info3.AddWaterSideFanPlane(floor, (byte)(x + 1), y, x, y, NORM_W, (byte)(x + 1), (byte)(y + 1), NORM_N);
                                info3_2.AddWaterSideFanPlane(floor, (byte)(x + 1), y, x, y, NORM_W, (byte)(x + 1), (byte)(y + 1), NORM_N, waterSide2Offset);

                                info2.AddFanTriangles(wpt2, wpt4, wpt1);
                                info2.AddWallFanPlane(floor, (byte)(x + 1), y, x, y, (byte)(x + 1), (byte)(y + 1), NORM_NW, waterPlaneDepth);

                                if (waterfallPropInfo >= 0)
                                    propInfo[waterfallPropInfo].PlaceWaterFall(startPosition, curGroup, NORM_NW, cellIndexForProp, GetDiffFloor(floor, cellArr, width, height, x, y, NORM_NW));
                            }
                            // norm_ne 방향
                            else if ((neighborCell & 1) == 0 && (neighborCell & 2) == 0)
                            {
                                //info.AddTriangle(pt1, pt3, pt2);
                                //info3.AddWaterSidePlane(floor, x, (byte)(y + 1), NORM_N, (byte)(x + 1), y, NORM_E);
                                info.AddFanTriangles(pt1, pt2, pt3);
                                info3.AddWaterSideFanPlane(floor, x, y, x, (byte)(y + 1), NORM_N, (byte)(x + 1), y, NORM_E);
                                info3_2.AddWaterSideFanPlane(floor, x, y, x, (byte)(y + 1), NORM_N, (byte)(x + 1), y, NORM_E, waterSide2Offset);

                                info2.AddFanTriangles(wpt1, wpt2, wpt3);
                                info2.AddWallFanPlane(floor, x, y, x, (byte)(y + 1), (byte)(x + 1), y, NORM_NE, waterPlaneDepth);

                                if (waterfallPropInfo >= 0)
                                    propInfo[waterfallPropInfo].PlaceWaterFall(startPosition, curGroup, NORM_NE, cellIndexForProp, GetDiffFloor(floor, cellArr, width, height, x, y, NORM_NE));
                            }
                        }
                    }

                    // 현재 cell이 land일때만 tile을 그린다.
                    if (tileIndex != TILE_NONE && (cellType == CELL_LAND || cellType == CELL_SMOOTH))
                    {
                        bool isSmooth = tileIndex >= TILE_SMOOTH;
                        if (isSmooth)
                            tileIndex -= TILE_SMOOTH;

                        if (tileMaterials != null && tileMaterials.Count > tileIndex)
                        {
                            if (!tileGroupMap.TryGetValue(groupIndex, out _curGroup))
                            {
                                _curGroup = new CellGroupTerrain(groupIndex, tileMaterials, propInfo);
                                tileGroupMap[groupIndex] = _curGroup;
                            }
                            // 바뀐 그룹만 계산한다.
                            if (curGroup.IsReset())
                            {
                                List<VertexInfo> infoList = curGroup.planeList;
                                VertexInfo info = infoList[tileIndex];
                                if (isSmooth)
                                {
                                    int neighbor = GetNeighborTileIndex(tileArr, width, height, x, y);
                                    info.AddTileTriangle(floor, x, y, neighbor);
                                }
                                else
                                    info.AddTilePlane(tileArr, width, height, floor, x, y);

                                //CLogger.Log("render tile " + x + "," + y);
                            }
                        }
                        else if (tileMaterials != null)
                            CLogger.LogWarning("tileIndex is invalid. " + tileIndex + " / " + tileMaterials.Count);
                    }
                }
            }
        }

        foreach (var d in groupMap)
            d.Value.CreateGroup(newBlock.transform, baseMaterialList, true);
        if (tileMaterials != null && tileMaterials.Count > 0)
        {
            foreach (var d in tileGroupMap)
                d.Value.CreateGroup(newBlock.transform, tileMaterials, false);
        }
        */
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
    //public List<ActionStack.CellInfo> GetCellInfos(byte x, byte y)
    //{
    //    int width = terrainWidth;
    //    int height = terrainHeight;
    //    List<ActionStack.CellInfo> cellInfos = new List<ActionStack.CellInfo>();

    //    byte sx = (byte)((x / BLOCK) * BLOCK);
    //    byte sy = (byte)((y / BLOCK) * BLOCK);
    //    for (byte _y = sy; _y < sy + BLOCK; _y++)
    //    {
    //        for (byte _x = sx; _x < sx + BLOCK; _x++)
    //        {
    //            int index = _x + _y * terrainWidth;
    //            if (index >= 0 && index < cellArr.Length)
    //                cellInfos.Add(new ActionStack.CellInfo(cellArr[index], _x, _y));
    //        }
    //    }

    //    if (x > 0)
    //    {
    //        for (int dy = 0; dy <= 1; dy++)
    //        {
    //            int tx = (int)(sx - (BLOCK * 0.5f));
    //            int ty = sy + dy;
    //            int index = tx + ty * terrainWidth;
    //            if (index >= 0 && index < cellArr.Length)
    //                cellInfos.Add(new ActionStack.CellInfo(cellArr[index], (byte)tx, (byte)ty));
    //        }
    //    }

    //    if (x < width - 1)
    //    {
    //        for (int dy = 0; dy <= 1; dy++)
    //        {
    //            int tx = sx + BLOCK;
    //            int ty = sy + dy;
    //            int index = tx + ty * terrainWidth;
    //            if (index >= 0 && index < cellArr.Length)
    //                cellInfos.Add(new ActionStack.CellInfo(cellArr[index], (byte)tx, (byte)ty));
    //        }
    //    }

    //    if (y > 0)
    //    {
    //        for (int dx = 0; dx <= 1; dx++)
    //        {
    //            int tx = sx + dx;
    //            int ty = (int)(sy - (BLOCK * 0.5f));
    //            int index = tx + ty * terrainWidth;
    //            if (index >= 0 && index < cellArr.Length)
    //                cellInfos.Add(new ActionStack.CellInfo(cellArr[index], (byte)tx, (byte)ty));
    //        }
    //    }

    //    if (y < height - 1)
    //    {
    //        for (int dx = 0; dx <= 1; dx++)
    //        {
    //            int tx = sx + dx;
    //            int ty = sy + BLOCK;
    //            int index = tx + ty * terrainWidth;
    //            if (index >= 0 && index < cellArr.Length)
    //                cellInfos.Add(new ActionStack.CellInfo(cellArr[index], (byte)tx, (byte)ty));
    //        }
    //    }

    //    return cellInfos;
    //}
    //public ActionStack.TileInfo[] GetTileInfos(byte x, byte y)
    //{
    //    int count = 0;
    //    ActionStack.TileInfo[] tileInfos = new ActionStack.TileInfo[DIV];

    //    byte sx = (byte)((x / BLOCK) * BLOCK);
    //    byte sy = (byte)((y / BLOCK) * BLOCK);
    //    for (byte _y = sy; _y < sy + BLOCK; _y++)
    //    {
    //        for (byte _x = sx; _x < sx + BLOCK; _x++)
    //        {
    //            int index = _x + _y * terrainWidth;
    //            if (index >= 0 && index < tileArr.Length)
    //            {
    //                tileInfos[count] = new ActionStack.TileInfo(tileArr[index], _x, _y);
    //                count++;
    //            }
    //        }
    //    }

    //    return tileInfos;
    //}

    //public void RevertCell(byte cellX, byte cellY, byte cellType, List<ActionStack.CellInfo> cellInfos, ActionStack.TileInfo[] tileInfos)
    //{
    //    int index = cellX + cellY * terrainWidth;
    //    if (index >= 0 && index < cellArr.Length)
    //    {
    //        byte curType = GetOnlyCellIndex(cellArr[index]);
    //        byte beforeFloor = GetFloor(curType);
    //        byte afterfloor = GetFloor(cellType);

    //        // 땅을 바꾸면, 위에 있던 tile도 지운다.
    //        if (tileArr[index] != TILE_NONE)
    //        {
    //            if (cellType == CELL_SMOOTH)
    //                ModifyTile(cellX, cellY, TILE_SMOOTH, false);
    //            else
    //                ModifyTile(cellX, cellY, TILE_NONE, false);
    //        }

    //        for (int i = 0; i < cellInfos.Count; i++)
    //        {
    //            int ind = cellInfos[i].CellX + cellInfos[i].CellY * terrainWidth;
    //            if (ind >= 0 && ind < cellArr.Length)
    //            {
    //                cellArr[ind] = cellInfos[i].CellType;
    //                ResetCellGroup(afterfloor, cellInfos[i].CellX, cellInfos[i].CellY, false);
    //                if (afterfloor > 0)
    //                {
    //                    for (int f = 0; f <= afterfloor - 1; f++)
    //                        ResetCellGroup((byte)f, cellInfos[i].CellX, cellInfos[i].CellY, false);
    //                }
    //            }
    //        }

    //        for (int j = 0; j < tileInfos.Length; j++)
    //            ModifyTile(tileInfos[j].TileX, tileInfos[j].TileY, tileInfos[j].TileType, false);

    //        ResetCellGroup(afterfloor, cellX, cellY, false);
    //        if (beforeFloor > afterfloor)
    //            ResetCellGroup(beforeFloor, cellX, cellY, false);
    //        if (afterfloor > 0)
    //        {
    //            for (int f = 0; f <= afterfloor - 1; f++)
    //                ResetCellGroup((byte)f, cellX, cellY, false);
    //        }
    //        RefreshTerrain();
    //    }
    //}

    //public void RevertTile(ActionStack.TileInfo[] tileInfos)
    //{
    //    for (int j = 0; j < tileInfos.Length; j++)
    //        ModifyTile(tileInfos[j].TileX, tileInfos[j].TileY, tileInfos[j].TileType, false);

    //    RefreshTerrain();
    //}

    bool IsInTerrain(Vector3 localPos, float EPSILON = 0.05f)
    {
        return -EPSILON <= localPos.x && localPos.x <= GetCellFullWidth() * terrainWidth + EPSILON
            && -EPSILON <= localPos.z && localPos.z <= GetCellFullHeight() * terrainHeight + EPSILON
            && -EPSILON - cellY <= localPos.y && localPos.y <= cellY * MAX_FLOOR + EPSILON;
    }


    // ray로 알맞을 cell을 pickup한다.
    public bool PickCell(Vector3 startWorld, Vector3 directionWorld, out byte floor, out byte x, out byte y)
    {
        floor = 0;
        x = 0;
        y = 0;
        // 전체영역과 만나는 점을 찾는다. 0~2개가 가능하다.
        Vector3 localStart = newBlock.transform.InverseTransformPoint(startWorld);
        Vector3 localDir = newBlock.transform.InverseTransformDirection(directionWorld);
        Ray ray = new Ray(localStart, localDir);

        // 우선 6면과 만나는 점들을 찾아서 그중에 제일 가까운 점을 찾는다.
        Plane[] planes = new Plane[6];
        planes[0] = new Plane(new Vector3(1, 0, 0), 0);
        planes[1] = new Plane(new Vector3(1, 0, 0), -terrainWidth * GetCellFullWidth());
        planes[2] = new Plane(new Vector3(0, 1, 0), cellY);
        planes[3] = new Plane(new Vector3(0, 1, 0), -cellY * MAX_FLOOR);
        planes[4] = new Plane(new Vector3(0, 0, 1), 0);
        planes[5] = new Plane(new Vector3(0, 0, 1), -terrainHeight * GetCellFullHeight());

        bool hasPoint = false;
        float minRatio = 10000000000;
        float maxRatio = -10000000000;
        for (int i = 0; i < planes.Length; i++)
        {
            float ratio;
            planes[i].Raycast(ray, out ratio);
            Vector3 hitPoint = ray.GetPoint(ratio);
            Vector3 pt = ray.origin + ray.direction * ratio;
            if (IsInTerrain(pt))
            {
                hasPoint = true;
                minRatio = Mathf.Min(minRatio, ratio);
                maxRatio = Mathf.Max(maxRatio, ratio);
            }
        }
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
                if (CheckPoint(curPt, checkMap, out _floor, out _x, out _z))
                {
                    Vector3 pickPos = GetCenterPos(_x, _z);
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
    //public void ShowSelection(bool show, byte x, byte y, ConstructModeCommon.eConstructMode mode = ConstructModeCommon.eConstructMode.Cube)
    //{
    //    if (newBlock == null)
    //        return;
    //    GameObject mark = null;
    //    bool isTile = mode == ConstructModeCommon.eConstructMode.Paint;
    //    if (isTile)
    //    {
    //        if (brushMark == null)
    //        {
    //            brushMark = GameObject.Instantiate(brushMarkPrefab);
    //            brushMark.transform.SetParent(newBlock.transform);
    //            brushMark.transform.localRotation = Quaternion.identity;
    //            brushMark.transform.localScale = Vector3.one;
    //        }
    //        mark = brushMark;
    //    }
    //    else
    //    {
    //        if (selectionMark == null)
    //        {
    //            selectionMark = GameObject.Instantiate(selectionMarkPrefab);
    //            selectionMark.transform.SetParent(newBlock.transform);
    //            selectionMark.transform.localRotation = Quaternion.identity;
    //            selectionMark.transform.localScale = Vector3.one;
    //        }
    //        mark = selectionMark;
    //    }

    //    if (show)
    //    {
    //        mark.ShowObject(show);
    //        //float scale = mode == WorldUI.StateWorldEdit.ConstructionButton.TILE ? 0.5f : 1;
    //        byte floor = GetFloor(cellArr[x + y * terrainWidth]);
    //        Vector3 pos = GetPoint(floor, x, y);
    //        if (mode == ConstructModeCommon.eConstructMode.Water || mode == ConstructModeCommon.eConstructMode.Spade)
    //            pos.y -= cellY * 0.7f;
    //        mark.transform.localPosition = pos;
    //        //mark.transform.localScale = Vector3.one * scale;
    //    }
    //    else
    //    {
    //        brushMark.ShowObject(false);
    //        selectionMark.ShowObject(false);
    //    }
    //}

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
        if (null != _helper)
            AssetLoadHelper.Destroy(ref _helper);
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
    //public void SaveTerrain(NetworkModule.SpaceMapInfo spaceMapInfo)
    //{
    //    /*
    //    int totalLength = terrainWidth * terrainHeight;
    //    System.Text.StringBuilder map = new System.Text.StringBuilder(totalLength);
    //    for (int i = 0; i < totalLength; i++)
    //    {
    //        char str = (char)(GetOnlyCellIndex(cellArr[i]) + 'A');
    //        map.Append(str);
    //    }
    //    string cellString = map.ToString();
    //    spaceMapInfo.CellWidth = terrainWidth;
    //    spaceMapInfo.CellHeight = terrainHeight;
    //    spaceMapInfo.CellMap = cellString;

    //    System.Text.StringBuilder map2 = new System.Text.StringBuilder(totalLength);
    //    for (int i = 0; i < totalLength; i++)
    //    {
    //        char str = tileArr[i] == TILE_NONE ? '0' : (char)(tileArr[i] + 'A');
    //        map2.Append(str);
    //    }
    //    string tileString = map2.ToString();
    //    spaceMapInfo.TileMap = tileString;
    //    spaceMapInfo.TileIDs = new List<long>();
    //    if (tileMaterials != null)
    //    {
    //        for (int i = 0; i < tileMaterials.Count; i++)
    //            spaceMapInfo.TileIDs.Add(tileMaterials[i].tileItemID);
    //    }
    //    */
    //    isTerrainChanged = false;
    //}

    // min, max in world coordinate.
    // return world Y
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
    /*
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
#endif*/
}