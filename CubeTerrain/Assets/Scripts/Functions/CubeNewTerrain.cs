using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeNewTerrain : CubeTerrain
{
    public AnimationCurve edgeCurve = new AnimationCurve();
    public float LODFadeRatio = 0.05f;
    const int SUB_CELL_DIV = 4;
    NewVertexInfo basePlaneInfo;
    class CellGroupNewTerrain : CellGroup
    {
        public int index;
        public List<NewVertexInfo> planeList;
        public GameObject groupObject;
        public GameObject propParentObject;
        byte floor;
        public byte startX;
        public byte startY;
        bool reset = false;
        CubeNewTerrain owner;
        List<List<CellGroup.PropData>> propList;
        byte[] cellArr;
        int width;
        int height;
        LODGroup lodGroup;
        List<Renderer> lodRenderer;
        public List<Bounds> waterBoundsList;
        public CellGroupNewTerrain(CubeNewTerrain owner, int index, byte floor, byte startX, byte startY, List<TileInfo> tileMaterials, TilePropInfo[] propInfo, byte[] cellArr, int width, int height)
        {
            this.owner = owner;
            reset = true;
            this.index = index;
            planeList = new List<NewVertexInfo>();
            for (int i = 0; i < tileMaterials.Count; i++)
                planeList.Add(new NewVertexInfo());
            this.floor = floor;
            this.startX = startX;
            this.startY = startY;
            this.cellArr = cellArr;
            this.width = width;
            this.height = height;
            groupObject = null;
            propList = new List<List<CellGroup.PropData>>();
            if (propInfo != null)
            {
                for (int i = 0; i < propInfo.Length; i++)
                    propList.Add(new List<CellGroup.PropData>());
            }
            lodRenderer = new List<Renderer>();
            waterBoundsList = new List<Bounds>();
        }
        public bool IsReset()
        {
            return reset;
        }

        void ResetPlane()
        {
            for (int i = 0; i < planeList.Count; i++)
                planeList[i].Reset();
            if (waterBoundsList != null)
                waterBoundsList.Clear();
        }
        public void Reset(List<TileInfo> tileMaterials, TilePropInfo[] propInfo)
        {
            if (tileMaterials != null && tileMaterials.Count > planeList.Count)
            {
                for (int i = planeList.Count; i < tileMaterials.Count; i++)
                    planeList.Add(new NewVertexInfo());
            }

            if (reset)
                return;
            reset = true;
            ResetPlane();

            if (groupObject != null)
            {
                MeshFilter[] meshes = groupObject.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < meshes.Length; i++)
                {
                    if (meshes[i].sharedMesh != null)
                    {
                        Mesh.Destroy(meshes[i].sharedMesh);
                        meshes[i].sharedMesh = null;
                    }
                }
            }
            lodRenderer.Clear();
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                lods[0].renderers = lodRenderer.ToArray();
                lodGroup.SetLODs(lods);
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
            WaterFall fall = obj.GetComponent<WaterFall>();
            if (fall != null)
            {
                fall.SetFogPosition(diffFloor);
                lodRenderer.AddRange(fall.renderers);
            }
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
            lodGroup = group.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = true;
            LOD[] lods = lodGroup.GetLODs();
            // 화면크기의 10%가 되면, hide한다.
            for (int i=0; i<lods.Length; i++)
            {
                float scale = isDefaultTerrain ? 1 : 0.5f;
                lods[i].screenRelativeTransitionHeight = (owner.LODFadeRatio + 0.02f * (lods.Length - 1 - i)) * scale;
            }   
            lodGroup.SetLODs(lods);
            lodRenderer.Clear();
            if (owner.editMode)
                lodGroup.enabled = false;

            if (propParentObject != null)
            {
                propParentObject.transform.SetParent(groupObject.transform);
                propParentObject.transform.localPosition = Vector3.zero;
                propParentObject.transform.localRotation = Quaternion.identity;
                propParentObject.transform.localScale = Vector3.one;
            }
        }

        public void CreatePlaneMeshes(List<TileInfo> tileMaterials, bool isDefaultTerrain)
        {
            for (int i = 0; i < planeList.Count; i++)
            {
                NewVertexInfo info = planeList[i];
                if (info.vertexList.Count > 0)
                {
                    bool castShadow = isDefaultTerrain;
                    Renderer renderer = info.CreateMesh(groupObject.transform, isDefaultTerrain ? "plane" + i : "tile" + i, tileMaterials[i].material, isDefaultTerrain && (i == PLANE_LAND || i == PLANE_LAND_SIDE || i == PLANE_WALL || i == PLANE_WATER_WALL || i == PLANE_WATER), castShadow, i, this);
                    if (renderer != null && (!isDefaultTerrain || i == PLANE_LAND_SIDE || i == PLANE_WATER_SIDE || i == PLANE_WATER_SIDE2))
                        lodRenderer.Add(renderer);
                }
            }

            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                lods[0].renderers = lodRenderer.ToArray();
                lodGroup.SetLODs(lods);
            }

            ResetPlane();
        }

        public static float[] POINT_OFFSET = null;
        const int TOTAL_DIV = DIV * SUB_CELL_DIV;

        // 우리가 polygon을 최적화 하기 위해서 cell group을 가로축부터 traverse를 해서 모은다.
        // 그래서 세로쪽이 찢어질 수 있으므로, 세로축과 관련된 방향은 모두 세로축방향으로만 offset을 준다.
        static readonly bool[] APPLY_OFFSET_X = new bool[8]
        {
            false,
            false,
            true,
            false,
            false,
            false,
            true,
            false,
        };
        static readonly bool[] APPLY_OFFSET_Y = new bool[8]
        {
            true,
            true,
            false,
            true,
            true,
            true,
            false,
            true,
        };
        public static Vector3 Point(byte floor, int x, int y, bool offset = true)
        {
            bool offsetX = offset;// dir == NORM_NIL || APPLY_OFFSET_X[dir];
            bool offsetY = offset;//dir == NORM_NIL || APPLY_OFFSET_Y[dir];
            return new Vector3((float)x / (float)SUB_CELL_DIV * GetCellFullWidth() + (offsetX ? POINT_OFFSET[y % TOTAL_DIV] : 0), floor * cellY, (float)y / (float)SUB_CELL_DIV * GetCellFullHeight() + (offsetY ? POINT_OFFSET[x % TOTAL_DIV] : 0));
        }
        public static Vector3 Point(byte floor, float x, float y, bool offset = true)
        {
            bool offsetX = offset;// dir == NORM_NIL || APPLY_OFFSET_X[dir];
            bool offsetY = offset;//dir == NORM_NIL || APPLY_OFFSET_Y[dir];
            return new Vector3(x / (float)SUB_CELL_DIV * GetCellFullWidth() + (offsetX ? POINT_OFFSET[(int)(y % TOTAL_DIV)] : 0), floor * cellY, y / (float)SUB_CELL_DIV * GetCellFullHeight() + (offsetY ? POINT_OFFSET[(int)(x % TOTAL_DIV)] : 0));
        }
        public static Vector2 UV(ref Vector3 pos)
        {
            return new Vector2(pos.x / cellX * uScale, pos.z / cellZ * vScale);
        }
        public static Vector2 WallUV(ref Vector3 pos)
        {
            return new Vector2(pos.x / cellX * uScale + pos.z / cellZ * uScale, pos.y / cellY * vScale);
        }

        // land related.
        #region LandRelatedMesh
        int AddVert(Vector3 pt, int tileIndex = PLANE_LAND)
        {
            NewVertexInfo info = planeList[tileIndex];
            Vector3 normal = Vector3.up;
            Vector2 uv = UV(ref pt);
            info.vertexList.Add(pt);
            info.normalList.Add(normal);
            info.uvList.Add(uv);
            return info.vertexList.Count - 1;
        }
        void AddTri(int a, int b, int c, int tileIndex = PLANE_LAND)
        {
            NewVertexInfo info = planeList[tileIndex];
            info.triangleList.Add(a);
            info.triangleList.Add(b);
            info.triangleList.Add(c);
        }

        int[] AddCornerVertexToLandSide(byte x, byte y, Vector3 offset, NewVertexInfo.NeighborInfo neighborInfo)
        {
            NewVertexInfo info = planeList[PLANE_LAND_SIDE];
            NewVertex vertex = info.AddVertex(floor, (short)(x * SUB_CELL_DIV), (short)(y * SUB_CELL_DIV), neighborInfo, PLANE_LAND_SIDE, offset);
            return vertex.vertIndices;
        }
        int[] AddMidVertexToLandSide(int x, int y, byte normal, Vector3 offset)
        {
            NewVertexInfo info = planeList[PLANE_LAND_SIDE];
            NewVertex vertex = info.AddVertex(floor, (short)x, (short)y, new NewVertexInfo.NeighborInfo(false, normal), PLANE_LAND_SIDE, offset);
            return vertex.vertIndices;
        }

        void AddLandSideTri(int[] lastSideIndices, int []curSideIndices, bool isLastYUp = true, bool isCurYUp = true)
        {
            int lastStart = (!isLastYUp && lastSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT) ? PT_REV_LINK_START : PT_LINK_START;
            int curStart = (!isCurYUp && curSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT) ? PT_REV_LINK_START : PT_LINK_START;

            NewVertexInfo info = planeList[PLANE_LAND_SIDE];
            info.AddPolygon(curSideIndices[curStart], lastSideIndices[lastStart], lastSideIndices[0], curSideIndices[0]);
            info.AddPolygon(curSideIndices[curStart + 1], lastSideIndices[lastStart + 1], lastSideIndices[lastStart], curSideIndices[curStart]);
            info.AddPolygon(curSideIndices[curStart + 2], lastSideIndices[lastStart + 2], lastSideIndices[lastStart + 1], curSideIndices[curStart + 1]);
        }

        void ClearWallSideVertexMap()
        {
            NewVertexInfo info = planeList[PLANE_WALL];
            info.cellPointMap.Clear();
        }
        int[] AddVertexToWallSide(int x, int y, byte normal, Vector3 offset, int planeNormal = -1)
        {
            NewVertexInfo info = planeList[PLANE_WALL];
            NewVertex vertex = info.AddVertex(floor, (short)x, (short)y, new NewVertexInfo.NeighborInfo(false, normal), PLANE_WALL, offset, planeNormal);
            return vertex.vertIndices;
        }
        void AddWallSideTri(int[] lastSideIndices, int[] curSideIndices)
        {
            NewVertexInfo info = planeList[PLANE_WALL];
            info.AddPolygon(curSideIndices[1], lastSideIndices[1], lastSideIndices[0], curSideIndices[0]);
        }
        #endregion LandRelatedMesh

        // water related.
        #region WaterRelatedMesh
        int AddWaterVert(Vector3 pt, bool isGround)
        {
            pt.y -= isGround ? waterPlaneDepth : waterDepth;
            NewVertexInfo info = planeList[isGround ? PLANE_WATER_WALL : PLANE_WATER];
            Vector3 normal = Vector3.up;
            Vector2 uv = UV(ref pt);
            info.vertexList.Add(pt);
            info.normalList.Add(normal);
            info.uvList.Add(uv);
            return info.vertexList.Count - 1;
        }
        void AddWaterTri(int a, int b, int c, bool isGround)
        {
            NewVertexInfo info = planeList[isGround ? PLANE_WATER_WALL : PLANE_WATER];
            info.triangleList.Add(a);
            info.triangleList.Add(b);
            info.triangleList.Add(c);
        }

        int[] AddCornerVertexToWaterSide(byte x, byte y, Vector3 offset, NewVertexInfo.NeighborInfo neighborInfo)
        {
            NewVertexInfo info = planeList[PLANE_WATER_SIDE];
            NewVertex vertex = info.AddVertex(floor, (short)(x * SUB_CELL_DIV), (short)(y * SUB_CELL_DIV), neighborInfo, PLANE_WATER_SIDE, offset);
            return vertex.vertIndices;
        }
        int[] AddMidVertexToWaterSide(int x, int y, byte normal)
        {
            NewVertexInfo info = planeList[PLANE_WATER_SIDE];
            NewVertex vertex = info.AddVertex(floor, (short)x, (short)y, new NewVertexInfo.NeighborInfo(false, normal), PLANE_WATER_SIDE, Vector3.zero);
            return vertex.vertIndices;
        }
        void AddWaterSideTri(int[] lastSideIndices, int[] curSideIndices, bool isLastYUp = true, bool isCurYUp = true)
        {
            int lastStart = (!isLastYUp && lastSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT) ? PT_REV_LINK_START : PT_LINK_START;
            int curStart = (!isCurYUp && curSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT) ? PT_REV_LINK_START : PT_LINK_START;

            int lastTop = lastStart == PT_REV_LINK_START ? lastSideIndices.Length - 1 : 0;
            int curTop = curStart == PT_REV_LINK_START ? curSideIndices.Length - 1 : 0;

            NewVertexInfo info = planeList[PLANE_WATER_SIDE];
            info.AddPolygon(curSideIndices[curStart], lastSideIndices[lastStart], lastSideIndices[lastTop], curSideIndices[curTop]);
            info.AddPolygon(curSideIndices[curStart + 1], lastSideIndices[lastStart + 1], lastSideIndices[lastStart], curSideIndices[curStart]);
            info.AddPolygon(curSideIndices[curStart + 2], lastSideIndices[lastStart + 2], lastSideIndices[lastStart + 1], curSideIndices[curStart + 1]);
        }
        void ClearWaterWallSideVertexMap()
        {
            NewVertexInfo info = planeList[PLANE_WATER_WALL];
            info.cellPointMap.Clear();
        }
        int[] AddVertexToWaterWallSide(int x, int y, byte normal, Vector3 offset, int planeNormal = -1)
        {
            NewVertexInfo info = planeList[PLANE_WATER_WALL];
            NewVertex vertex = info.AddVertex(floor, (short)x, (short)y, new NewVertexInfo.NeighborInfo(false, normal), PLANE_WATER_WALL, offset, planeNormal);
            return vertex.vertIndices;
        }
        void AddWaterWallSideTri(int[] lastSideIndices, int[] curSideIndices)
        {
            NewVertexInfo info = planeList[PLANE_WATER_WALL];
            info.AddPolygon(curSideIndices[1], lastSideIndices[1], lastSideIndices[0], curSideIndices[0]);
        }

        /*
        void _AddWaterTri(ref Vector3 point, ref int lastIndex, int centerIndex)
        {
            int index = AddWaterVert(point, false);
            AddWaterTri(lastIndex, centerIndex, index, false);
            lastIndex = index;
        }
        void _AddWaterSideTri(int x, int y, ref int[] lastSideIndices, ref bool lastYUp, bool curYUp)
        {
            int[] curSideIndices = AddCornerVertexToWaterSide((byte)x, (byte)y);
            if (lastSideIndices != null)
                AddWaterSideTri(lastSideIndices, curSideIndices, lastYUp, curYUp);
            lastSideIndices = curSideIndices;
            lastYUp = curYUp;
        }
        void _AddWaterGroundTri(ref Vector3 point, ref int lastGroundIndex, int centerGroundIndex)
        {
            int groundIndex = AddWaterVert(point, true);
            AddWaterTri(lastGroundIndex, centerGroundIndex, groundIndex, true);
            lastGroundIndex = groundIndex;
        }
        void _AddWaterWallSideTri(int _x, int _y, byte curNormal, ref int[] lastWallIndices)
        {
            int[] curWallIndices = AddVertexToWaterWallSide(_x, _y, curNormal);
            if (lastWallIndices != null)
                AddWaterWallSideTri(lastWallIndices, curWallIndices);
            lastWallIndices = curWallIndices;
        }
        void _AddWaterSideTriDiv(int ri, int x, int y, int _x, int _y, byte norm, ref int[] lastSideIndices, ref bool lastYUp, bool curYUp)
        {
            int[] curSideIndices = ri == SUB_CELL_DIV ? AddCornerVertexToWaterSide((byte)x, (byte)y) : AddMidVertexToWaterSide(_x, _y, norm);
            AddWaterSideTri(lastSideIndices, curSideIndices, lastYUp, curYUp);
            lastSideIndices = curSideIndices;
            if (ri == SUB_CELL_DIV)
                lastYUp = curYUp;
        }
        */
        void _AddOneEdgeWaterVertex(CellStripeInfo curInfo, bool makeStartVert, byte x, byte y, int centerIndex, byte curNormal, bool curYUp, ref bool lastYUp, ref int lastIndex, ref int[] lastSideIndices, ref int[] lastWallIndices, ref int secIndex, byte curDirection, ref int lastGroundIndex, int centerGroundIndex, ref int secGroundIndex)
        {
            //bool drawSecondVertex, drawSecondNow;
            NewVertexInfo.NeighborInfo neighborInfo;
            Vector3 offset = GetVertexOffset(curInfo, x, y, curDirection, ref curNormal, out neighborInfo);//, out drawSecondVertex, out drawSecondNow);

            // land plane / land side / wall side의 normal방향이 같아야 한다. 그래야 random처리가 동일하게 된다.

            if (makeStartVert || lastSideIndices == null)
            {
                int[] curSideIndices = AddCornerVertexToWaterSide((byte)x, (byte)y, offset, neighborInfo);
                if (lastSideIndices != null)
                    AddWaterSideTri(lastSideIndices, curSideIndices, lastYUp, curYUp);
                lastSideIndices = curSideIndices;
                lastYUp = curYUp;
            }
            // wall side
            if (makeStartVert || lastWallIndices == null)
            {
                int[] curWallIndices = AddVertexToWaterWallSide(x * SUB_CELL_DIV, y * SUB_CELL_DIV, neighborInfo.normalType, offset);
                if (lastWallIndices != null)
                    AddWaterWallSideTri(lastWallIndices, curWallIndices);
                lastWallIndices = curWallIndices;
            }

            if (makeStartVert || lastIndex == -1)
            {
                Vector3 point = Point(floor, x * SUB_CELL_DIV, y * SUB_CELL_DIV);

                int index, groundIndex;
                //int _secIndex = -1;
                //int _secGroundIndex = -1;
                //if (drawSecondVertex)
                //{
                //    index = AddWaterVert(point, false);
                //    groundIndex = AddWaterVert(point, true);
                //    // 약간 삐져나온 vertex를 생성한다. (round처리용)
                //    _secIndex = AddWaterVert(point + offset, false);
                //    _secGroundIndex = AddWaterVert(point + offset, true);
                //    // 만일 시작점이라면, 지금 바로 그린다.
                //    if (drawSecondNow)
                //    {
                //        secIndex = _secIndex;
                //        _secIndex = -1;
                //        secGroundIndex = _secGroundIndex;
                //        _secGroundIndex = -1;
                //    }
                //}
                //else
                //{
                    index = AddWaterVert(point + offset, false);
                    groundIndex = AddWaterVert(point + offset, true);
                //}
                if (lastIndex != -1)
                {
                    AddWaterTri(lastIndex, centerIndex, index, false);
                    AddWaterTri(lastGroundIndex, centerGroundIndex, groundIndex, true);
                    //안쪽으로 round처리하기 위함.
                    //if (secIndex != -1)
                    //{
                    //    AddWaterTri(lastIndex, index, secIndex, false);
                    //    AddWaterTri(lastGroundIndex, groundIndex, secGroundIndex, true);
                    //    secIndex = -1;
                    //    secGroundIndex = -1;
                    //}
                }
                lastIndex = index;
                lastGroundIndex = groundIndex;
                //if (_secIndex != -1)
                //{
                //    secIndex = _secIndex;
                //    secGroundIndex = _secGroundIndex;
                //}
            }
        }
        void _AddOneWaterVert(CellStripeInfo curInfo, byte x, byte y, int centerIndex, ref int lastIndex, ref int secIndex, byte curDirection, ref int lastGroundIndex, int centerGroundIndex, ref int secGroundIndex)
        {
            //bool drawSecondVertex, drawSecondNow;
            NewVertexInfo.NeighborInfo neighborInfo;
            byte curNormal = NORM_N;
            Vector3 offset = GetVertexOffset(curInfo, x, y, curDirection, ref curNormal, out neighborInfo);//, out drawSecondVertex, out drawSecondNow);

            Vector3 point = Point(floor, x * SUB_CELL_DIV, y * SUB_CELL_DIV);

            int index, groundIndex;
            //int _secIndex = -1;
            //int _secGroundIndex = -1;
            //if (drawSecondVertex)
            //{
            //    index = AddWaterVert(point, false);
            //    groundIndex = AddWaterVert(point, true);
            //    // 약간 삐져나온 vertex를 생성한다. (round처리용)
            //    _secIndex = AddWaterVert(point + offset, false);
            //    _secGroundIndex = AddWaterVert(point + offset, true);
            //    // 만일 시작점이라면, 지금 바로 그린다.
            //    if (drawSecondNow)
            //    {
            //        secIndex = _secIndex;
            //        _secIndex = -1;
            //        secGroundIndex = _secGroundIndex;
            //        _secGroundIndex = -1;
            //    }
            //}
            //else
            //{
                index = AddWaterVert(point + offset, false);
                groundIndex = AddWaterVert(point + offset, true);
            //}
            if (lastIndex != -1)
            {
                AddWaterTri(lastIndex, centerIndex, index, false);
                AddWaterTri(lastGroundIndex, centerGroundIndex, groundIndex, true);
                //안쪽으로 round처리하기 위함.
                //if (secIndex != -1)
                //{
                //    AddWaterTri(lastIndex, index, secIndex, false);
                //    AddWaterTri(lastGroundIndex, groundIndex, secGroundIndex, true);
                //    secIndex = -1;
                //    secGroundIndex = -1;
                //}
            }
            lastIndex = index;
            lastGroundIndex = groundIndex;
            //if (_secIndex != -1)
            //{
            //    secIndex = _secIndex;
            //    secGroundIndex = _secGroundIndex;
            //}
        }
        void _AddOneMidWaterVertex(int _x, int _y, int centerIndex, byte curNormal, bool curYUp, ref bool lastYUp, ref int lastIndex, ref int[] lastSideIndices, ref int[] lastWallIndices, ref int secIndex, ref int lastGroundIndex, int centerGroundIndex, ref int secGroundIndex)
        {
            Vector3 offset = Vector3.zero;
            // land plane / land side / wall side의 normal방향이 같아야 한다. 그래야 random처리가 동일하게 된다.

            Vector3 point = Point(floor, _x, _y);
            int index = AddWaterVert(point + offset, false);
            int groundIndex = AddWaterVert(point + offset, true);
            if (lastIndex != -1)
            {
                AddWaterTri(lastIndex, centerIndex, index, false);
                AddWaterTri(lastGroundIndex, centerGroundIndex, groundIndex, true);
                //안쪽으로 round처리하기 위함.
                //if (secIndex != -1)
                //{
                //    AddWaterTri(lastIndex, index, secIndex, false);
                //    AddWaterTri(lastGroundIndex, groundIndex, secGroundIndex, true);
                //    secIndex = -1;
                //    secGroundIndex = -1;
                //}
            }
            lastIndex = index;
            lastGroundIndex = groundIndex;

            // water side
            int[] curSideIndices = AddMidVertexToWaterSide(_x, _y, curNormal);
            if (lastSideIndices != null)
                AddWaterSideTri(lastSideIndices, curSideIndices, lastYUp, curYUp);
            lastSideIndices = curSideIndices;

            // water ground
            int[] curWallIndices = AddVertexToWaterWallSide(_x, _y, curNormal, offset, curNormal);
            if (lastWallIndices != null)
                AddWaterWallSideTri(lastWallIndices, curWallIndices);
            lastWallIndices = curWallIndices;
        }
        void _AddWaterFallProp(int x, int y, byte curNormal)
        {
            int _x = curNormal == NORM_E ? x + 1 : (curNormal == NORM_W ? x - 1 : x);
            int _y = curNormal == NORM_N ? y + 1 : (curNormal == NORM_S ? y - 1 : y);
            byte neighborCell = owner.GetCellData(cellArr, width, height, _x, _y);
            if (owner.waterfallPropInfo >= 0 && (neighborCell == CELL_NONE || GetFloor(neighborCell) < floor))
            {
                int cellIndexForProp = CellIndex(floor, (byte)x, (byte)y);
                owner.propInfo[owner.waterfallPropInfo].PlaceWaterFall(Point(floor, x * SUB_CELL_DIV, y * SUB_CELL_DIV), this, curNormal, cellIndexForProp, owner.GetDiffFloor(floor, cellArr, width, height, (byte)x, (byte)y, curNormal));
            }
        }

        #endregion WaterRelatedMesh
        public void CreateWaterMesh(CellStripeInfo curInfo, List<TileInfo> tileMaterials)
        {
            Vector3 center = curInfo.GetCenter();
            float waterCenterY = (waterPlaneDepth + waterDepth) * 0.5f;
            float waterSizeY = (waterCenterY - waterDepth) * 2;
            waterBoundsList.Add(new Bounds(center - new Vector3(0, waterCenterY, 0), curInfo.GetSize() + new Vector3(0, waterSizeY, 0)));
            int centerIndex = AddWaterVert(center, false);
            int centerGroundIndex = AddWaterVert(center, true);

            // 밑의 line과 center point간의 triangle을 추가한다.
            int x = curInfo.startX;
            int y = curInfo.startY;

            Vector3 point = Vector3.zero;

            ClearWaterWallSideVertexMap();
            byte curNormal = NORM_S;
            byte curDirection = NORM_E;
            bool curYUp = false;// 아래쪽이므로
            bool lastYUp = false;
            int lastIndex = -1;
            int secIndex = -1;
            int lastGroundIndex = -1;
            int secGroundIndex = -1;
            int[] lastSideIndices = null;
            int[] lastWallIndices = null;
            bool makeStartVert = false;
            _AddOneEdgeWaterVertex(curInfo, true, (byte)x, (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
            for (x = curInfo.startX; x <= curInfo.endX; x++)
            {
                curYUp = false;
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 아래가 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 4) == 0)
                {
                    int _x = x * SUB_CELL_DIV;
                    int _y = y * SUB_CELL_DIV;
                    _AddOneEdgeWaterVertex(curInfo, makeStartVert, (byte)x, (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    makeStartVert = false;

                    // 반만 split 한다. 최적화를 위해서
                    for (int i = 1; i <= SUB_CELL_DIV/2; i++)
                    {
                        int ri = i * 2;
                        _x = x * SUB_CELL_DIV + ri;
                        _y = y * SUB_CELL_DIV;
                        if (ri == SUB_CELL_DIV)
                            _AddOneEdgeWaterVertex(curInfo, true, (byte)(x + 1), (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                        else
                            _AddOneMidWaterVertex(_x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    }

                    _AddWaterFallProp(x, y, curNormal);
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    // 이층에 대해서는 없다면, mid까지는 그려준다. (land만)
                    bool render = false;
                    bool isEmpty = (neighborThisFloor & 4) == 0;
                    if (isEmpty)
                    {
                        if (x == curInfo.startX || x == curInfo.endX)
                        {
                            render = true;
                            int i = 2;
                            int _x = x * SUB_CELL_DIV + i;
                            int _y = y * SUB_CELL_DIV;
                            _AddOneMidWaterVertex(_x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                        }
                    }

                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    // 마지막일 경우,
                    if (isEmpty && (render || x != curInfo.startX) && x == curInfo.endX)
                        _AddOneEdgeWaterVertex(curInfo, true, (byte)(x + 1), (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    else
                        _AddOneWaterVert(curInfo, (byte)(x + 1), (byte)y, centerIndex, ref lastIndex, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    //else
                    //    makeStartVert = true;
                    lastSideIndices = null;
                    lastWallIndices = null;
                }
            }

            // 이러면, 이 점은 2개로 분리가 된다는 뜻이다. 그래서 새로운 면을 그릴때 다시 설정해 주어야 한다.
            if (lastSideIndices != null && lastSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT)
                lastSideIndices = null;

            // 오른쪽 부분을 그린다.
            //ClearWaterWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_E;
            curDirection = NORM_N;
            makeStartVert = false;
            x = curInfo.endX;
            for (y = curInfo.startY; y <= curInfo.endY; y++)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 오른쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 2) == 0)
                {
                    int _x = (x + 1) * SUB_CELL_DIV;
                    int _y = y * SUB_CELL_DIV;
                    curYUp = true;
                    _AddOneEdgeWaterVertex(curInfo, makeStartVert, (byte)(x + 1), (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV/2; i++)
                    {
                        int ri = i * 2;
                        _x = (x + 1) * SUB_CELL_DIV;
                        _y = y * SUB_CELL_DIV + ri;
                        curYUp = false;
                        if (ri == SUB_CELL_DIV)
                            _AddOneEdgeWaterVertex(curInfo, true, (byte)(x + 1), (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                        else
                            _AddOneMidWaterVertex(_x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    }

                    _AddWaterFallProp(x, y, curNormal);
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    curYUp = false;
                    // 이층에 대해서는 없다면, mid까지는 그려준다. (land만)
                    bool render = false;
                    bool isEmpty = (neighborThisFloor & 2) == 0;
                    if (isEmpty)
                    {
                        if (y == curInfo.startY || y == curInfo.endY)
                        {
                            render = true;
                            int i = 2;
                            int _x = (x + 1) * SUB_CELL_DIV;
                            int _y = y * SUB_CELL_DIV + i;
                            _AddOneMidWaterVertex(_x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                        }
                    }
                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    // 마지막일 경우,
                    if (isEmpty && (render || y != curInfo.startY) && y == curInfo.endY)
                        _AddOneEdgeWaterVertex(curInfo, true, (byte)(x + 1), (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    else
                        _AddOneWaterVert(curInfo, (byte)(x + 1), (byte)(y + 1), centerIndex, ref lastIndex, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    //else
                    //    makeStartVert = true;
                    lastSideIndices = null;
                    lastWallIndices = null;
                }
            }

            // 이러면, 이 점은 2개로 분리가 된다는 뜻이다. 그래서 새로운 면을 그릴때 다시 설정해 주어야 한다.
            if (lastSideIndices != null && lastSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT)
                lastSideIndices = null;

            // 윗부분을 그린다.
            //ClearWaterWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_N;
            curDirection = NORM_W;
            y = curInfo.endY;
            makeStartVert = false;
            for (x = curInfo.endX; x >= curInfo.startX; x--)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 윗쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 1) == 0)
                {
                    int _x = (x + 1) * SUB_CELL_DIV;
                    int _y = (y + 1) * SUB_CELL_DIV;
                    curYUp = true;
                    _AddOneEdgeWaterVertex(curInfo, makeStartVert, (byte)(x + 1), (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV/2; i++)
                    {
                        int ri = i * 2;
                        _x = (x + 1) * SUB_CELL_DIV - ri;
                        _y = (y + 1) * SUB_CELL_DIV;

                        curYUp = true;
                        if (ri == SUB_CELL_DIV)
                            _AddOneEdgeWaterVertex(curInfo, true, (byte)x, (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                        else
                            _AddOneMidWaterVertex(_x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    }

                    _AddWaterFallProp(x, y, curNormal);
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    curYUp = true;
                    // 이층에 대해서는 없다면, mid까지는 그려준다. (land만)
                    bool render = false;
                    bool isEmpty = (neighborThisFloor & 1) == 0;
                    if (isEmpty)
                    {
                        if (x == curInfo.endX || x == curInfo.startX)
                        {
                            render = true;
                            int i = 2;
                            int _x = (x + 1) * SUB_CELL_DIV - i;
                            int _y = (y + 1) * SUB_CELL_DIV;
                            _AddOneMidWaterVertex(_x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                        }
                    }

                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    // 마지막일 경우,
                    if (isEmpty && (render || x != curInfo.endX) && x == curInfo.startX)
                        _AddOneEdgeWaterVertex(curInfo, true, (byte)x, (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    else
                        _AddOneWaterVert(curInfo, (byte)x, (byte)(y + 1), centerIndex, ref lastIndex, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    //else
                    //    makeStartVert = true;
                    lastSideIndices = null;
                    lastWallIndices = null;
                }
            }

            // 이러면, 이 점은 2개로 분리가 된다는 뜻이다. 그래서 새로운 면을 그릴때 다시 설정해 주어야 한다.
            if (lastSideIndices != null && lastSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT)
                lastSideIndices = null;

            // 왼쪽 부분을 그린다.
            //ClearWaterWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_W;
            curDirection = NORM_S;
            makeStartVert = false;
            x = curInfo.startX;
            for (y = curInfo.endY; y >= curInfo.startY; y--)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 왼쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 8) == 0)
                {
                    int _x = x * SUB_CELL_DIV;
                    int _y = (y + 1) * SUB_CELL_DIV;
                    curYUp = false;
                    _AddOneEdgeWaterVertex(curInfo, makeStartVert, (byte)x, (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV/2; i++)
                    {
                        int ri = i * 2;
                        _x = x * SUB_CELL_DIV;
                        _y = (y + 1) * SUB_CELL_DIV - ri;

                        curYUp = true;
                        if (ri == SUB_CELL_DIV)
                            _AddOneEdgeWaterVertex(curInfo, true, (byte)x, (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                        else
                            _AddOneMidWaterVertex(_x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    }

                    _AddWaterFallProp(x, y, curNormal);
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    curYUp = true;
                    // 이층에 대해서는 없다면, mid까지는 그려준다. (land만)
                    bool render = false;
                    bool isEmpty = (neighborThisFloor & 8) == 0;
                    if (isEmpty)
                    {
                        if (y == curInfo.endY || y == curInfo.startY)
                        {
                            render = true;
                            int i = 2;
                            int _x = x * SUB_CELL_DIV;
                            int _y = (y + 1) * SUB_CELL_DIV - i;
                            _AddOneMidWaterVertex(_x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                        }
                    }
                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    // 마지막일 경우,
                    if (isEmpty && (render || y != curInfo.endY) && y == curInfo.startY)
                        _AddOneEdgeWaterVertex(curInfo, true, (byte)x, (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    else
                        _AddOneWaterVert(curInfo, (byte)x, (byte)y, centerIndex, ref lastIndex, ref secIndex, curDirection, ref lastGroundIndex, centerGroundIndex, ref secGroundIndex);
                    //else
                    //    makeStartVert = true;
                    lastSideIndices = null;
                    lastWallIndices = null;
                }
            }
        }

        Vector3 GetVertexOffset(CellStripeInfo curInfo, byte x, byte y, byte curDirection, ref byte curNormal, out NewVertexInfo.NeighborInfo neighborInfo)//, out bool drawSecondVertex, out bool drawSecondNow)
        {
            Vector3 offset = Vector3.zero;
            //drawSecondVertex = false;
            //drawSecondNow = false;
            // 둥글게 표현하기 위해서 꼭지점을 약간 안쪽으로 그린다.
            // 이거는 +1까지 체크해야 되므로, stride가 width + 1이다.
            int neighborIndex = curInfo.neighborInfoListAtVertex[x + y * (width+1)];
            neighborInfo = NewVertexInfo.neighborInfo[neighborIndex];
            float scale = linkX * 1.5f;
            if (neighborIndex == 1 || neighborIndex == 2 || neighborIndex == 4 || neighborIndex == 8)
            {
                offset = -NORMALS[neighborInfo.normalType] * scale;
                curNormal = neighborInfo.normalType;
            }
            else if (neighborIndex == 14 || neighborIndex == 13 || neighborIndex == 11 || neighborIndex == 7)
            {
                //drawSecondVertex = true;
                //// 방향이 다를때는 지금 그려야 한다.
                //if (
                //    (neighborIndex == 14 && (curDirection == NORM_E || curDirection == NORM_S))
                //    || (neighborIndex == 13 && (curDirection == NORM_W || curDirection == NORM_S))
                //    || (neighborIndex == 11 && (curDirection == NORM_E || curDirection == NORM_N))
                //    || (neighborIndex == 7 && (curDirection == NORM_W || curDirection == NORM_N))
                //    )
                //    drawSecondNow = true;
                offset = NORMALS[neighborInfo.normalType] * scale;
                curNormal = neighborInfo.normalType;
            }   
            return offset;
        }
        const float LAND_OFFSET_SCALE = 1;
        void _AddOneEdgeVertex(CellStripeInfo curInfo, bool makeStartVert, byte x, byte y, int centerIndex, byte curNormal, bool curYUp, ref bool lastYUp, ref int lastIndex, ref int[] lastSideIndices, ref int[] lastWallIndices, ref int secIndex, byte curDirection)
        {
            //bool drawSecondVertex, drawSecondNow;
            NewVertexInfo.NeighborInfo neighborInfo;
            Vector3 offset = GetVertexOffset(curInfo, x, y, curDirection, ref curNormal, out neighborInfo) * LAND_OFFSET_SCALE;//, out drawSecondVertex, out drawSecondNow);

            // land plane / land side / wall side의 normal방향이 같아야 한다. 그래야 random처리가 동일하게 된다.

            // plane land side
            if (makeStartVert || lastSideIndices == null)
            {
                int[] curSideIndices = AddCornerVertexToLandSide((byte)x, (byte)y, offset, neighborInfo);
                if (lastSideIndices != null)
                    AddLandSideTri(lastSideIndices, curSideIndices, lastYUp, curYUp);
                lastSideIndices = curSideIndices;
                lastYUp = curYUp;
            }
            // wall side
            if (makeStartVert || lastWallIndices == null)
            {
                int[] curWallIndices = AddVertexToWallSide(x * SUB_CELL_DIV, y * SUB_CELL_DIV, neighborInfo.normalType, offset, -1);
                if (lastWallIndices != null)
                    AddWallSideTri(lastWallIndices, curWallIndices);
                lastWallIndices = curWallIndices;
            }

            // plane land
            if (makeStartVert || lastIndex == -1)
            {
                Vector3 point = Point(floor, x * SUB_CELL_DIV, y * SUB_CELL_DIV);
                int index;
                //int _secIndex = -1;
                //if (drawSecondVertex)
                //{
                //    index = AddVert(point + Vector3.zero);
                //    // 약간 삐져나온 vertex를 생성한다. (round처리용)
                //    _secIndex = AddVert(point + offset);
                //    // 만일 시작점이라면, 지금 바로 그린다.
                //    if (drawSecondNow)
                //    {
                //        secIndex = _secIndex;
                //        _secIndex = -1;
                //    }
                //}
                //else
                //{
                    index = AddVert(point + offset);
                //}
                if (lastIndex != -1)
                {
                    AddTri(lastIndex, centerIndex, index);
                    //안쪽으로 round처리하기 위함.
                    //if (secIndex != -1)
                    //{
                    //    AddTri(lastIndex, index, secIndex);
                    //    secIndex = -1;
                    //}
                }
                lastIndex = index;
                //if (_secIndex != -1)
                  //  secIndex = _secIndex;
            }
        }
        void _AddOneVert(CellStripeInfo curInfo, byte x, byte y, int centerIndex, ref int lastIndex, ref int secIndex, byte curDirection)
        {
            //bool drawSecondVertex, drawSecondNow;
            NewVertexInfo.NeighborInfo neighborInfo;
            byte curNormal = NORM_N;
            Vector3 offset = GetVertexOffset(curInfo, x, y, curDirection, ref curNormal, out neighborInfo) * LAND_OFFSET_SCALE;//, out drawSecondVertex, out drawSecondNow);

            Vector3 point = Point(floor, x * SUB_CELL_DIV, y * SUB_CELL_DIV);
            int index;
            //int _secIndex = -1;
            //if (drawSecondVertex)
            //{
            //    index = AddVert(point + Vector3.zero);
            //    // 약간 삐져나온 vertex를 생성한다. (round처리용)
            //    _secIndex = AddVert(point + offset);
            //    // 만일 시작점이라면, 지금 바로 그린다.
            //    if (drawSecondNow)
            //    {
            //        secIndex = _secIndex;
            //        _secIndex = -1;
            //    }
            //}
            //else
            //{
                index = AddVert(point + offset);
            //}
            if (lastIndex != -1)
            {
                AddTri(lastIndex, centerIndex, index);
                //안쪽으로 round처리하기 위함.
                //if (secIndex != -1)
                //{
                //    AddTri(lastIndex, index, secIndex);
                //    secIndex = -1;
                //}
            }
            lastIndex = index;
            //if (_secIndex != -1)
              //  secIndex = _secIndex;
        }
        void _AddOneMidVertex(CellStripeInfo curInfo, byte curDirection, int _x, int _y, int centerIndex, byte curNormal, bool curYUp, ref bool lastYUp, ref int lastIndex, ref int[] lastSideIndices, ref int[] lastWallIndices, ref int secIndex)
        {
            Vector3 offset = Vector3.zero;
            /*
            int x1 = Mathf.FloorToInt(_x / SUB_CELL_DIV);
            int y1 = Mathf.FloorToInt(_y / SUB_CELL_DIV);
            int xFrag = _x - x1 * SUB_CELL_DIV;
            int yFrag = _y - y1 * SUB_CELL_DIV;
            if (xFrag == 1 || xFrag == 3 || yFrag == 1 || yFrag == 3)
            {
                byte x = xFrag <= 2 ? (byte)x1 : (byte)(x1 + 1);
                byte y = yFrag <= 2 ? (byte)y1 : (byte)(y1 + 1);
                NewVertexInfo.NeighborInfo neighborInfo;
                offset = GetVertexOffset(curInfo, x, y, curDirection, ref curNormal, out neighborInfo) * 0.3f * LAND_OFFSET_SCALE;
            }//*/

            // land plane / land side / wall side의 normal방향이 같아야 한다. 그래야 random처리가 동일하게 된다.
            int index = AddVert(Point(floor, _x, _y) + offset);
            if (lastIndex != -1)
            {
                AddTri(lastIndex, centerIndex, index);
                //안쪽으로 round처리하기 위함.
                //if (secIndex != -1)
                //{
                //    AddTri(lastIndex, index, secIndex);
                //    secIndex = -1;
                //}
            }
            lastIndex = index;

            int[] curSideIndices = AddMidVertexToLandSide(_x, _y, curNormal, offset);
            if (lastSideIndices != null)
                AddLandSideTri(lastSideIndices, curSideIndices, lastYUp, curYUp);
            lastSideIndices = curSideIndices;

            // wall side
            int[] curWallIndices = AddVertexToWallSide(_x, _y, curNormal, offset, curNormal);
            if (lastWallIndices != null)
                AddWallSideTri(lastWallIndices, curWallIndices);
            lastWallIndices = curWallIndices;
        }
        public void CreateLandMesh(CellStripeInfo curInfo, List<TileInfo> tileMaterials)
        {
            int centerIndex = AddVert(curInfo.GetCenter());

            // 밑의 line과 center point간의 triangle을 추가한다.
            int x = curInfo.startX;
            int y = curInfo.startY;

            ClearWallSideVertexMap();
            byte curNormal = NORM_S;
            byte curDirection = NORM_E;
            bool curYUp = false; // 아래쪽이므로
            bool lastYUp = false;
            int lastIndex = -1;
            int secIndex = -1;
            int[] lastSideIndices = null;
            int[] lastWallIndices = null;
            bool makeStartVert = false;
            _AddOneEdgeVertex(curInfo, true, (byte)x, (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
            for (x = curInfo.startX; x <= curInfo.endX; x++)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                curYUp = false;
                // 아래가 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 4) == 0)
                {
                    int _x = x * SUB_CELL_DIV;
                    int _y = y * SUB_CELL_DIV;
                    _AddOneEdgeVertex(curInfo, makeStartVert, (byte)x, (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV; i++)
                    {
                        _x = x * SUB_CELL_DIV + i;
                        _y = y * SUB_CELL_DIV;
                        if (i == SUB_CELL_DIV)
                            _AddOneEdgeVertex(curInfo, true, (byte)(x+1), (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                        else
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                    }
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    // 정말 옆에 없다면, 시작과 끝일때만 그려준다.
                    bool render = false;
                    bool isEmpty = (neighborThisFloor & 4) == 0;
                    if (isEmpty)
                    {
                        if (x == curInfo.startX)
                        {
                            render = true;
                            int i = 1;
                            int _x = x * SUB_CELL_DIV + i;
                            int _y = y * SUB_CELL_DIV;
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                        }
                        if (x == curInfo.endX)
                        {
                            int i = SUB_CELL_DIV - 1;
                            int _x = x * SUB_CELL_DIV + i;
                            int _y = y * SUB_CELL_DIV;
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                        }
                    }

                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    if (isEmpty && (render || x != curInfo.startX) && x == curInfo.endX)
                        _AddOneEdgeVertex(curInfo, true, (byte)(x + 1), (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                    else
                        _AddOneVert(curInfo, (byte)(x + 1), (byte)y, centerIndex, ref lastIndex, ref secIndex, curDirection);
                    //else
                    //    makeStartVert = true;

                    lastSideIndices = null;
                    lastWallIndices = null;
                }
            }

            // 이러면, 이 점은 2개로 분리가 된다는 뜻이다. 그래서 새로운 면을 그릴때 다시 설정해 주어야 한다.
            if (lastSideIndices != null && lastSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT)
                lastSideIndices = null;

            // 오른쪽 부분을 그린다.
            //ClearWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_E;
            curDirection = NORM_N;
            makeStartVert = false;
            x = curInfo.endX;
            for (y = curInfo.startY; y <= curInfo.endY; y++)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 오른쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 2) == 0)
                {
                    int _x = (x + 1) * SUB_CELL_DIV;
                    int _y = y * SUB_CELL_DIV;
                    curYUp = true;
                    _AddOneEdgeVertex(curInfo, makeStartVert, (byte)(x + 1), (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV; i++)
                    {
                        _x = (x + 1) * SUB_CELL_DIV;
                        _y = y * SUB_CELL_DIV + i;
                        curYUp = false;
                        if (i == SUB_CELL_DIV)
                            _AddOneEdgeVertex(curInfo, true, (byte)(x + 1), (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                        else
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                    }
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    curYUp = false;
                    // 정말 옆에 없다면, 시작과 끝일때만 그려준다.
                    bool render = false;
                    bool isEmpty = (neighborThisFloor & 2) == 0;
                    if (isEmpty)
                    {
                        if (y == curInfo.startY)
                        {
                            render = true;
                            int i = 1;
                            int _x = (x + 1) * SUB_CELL_DIV;
                            int _y = y * SUB_CELL_DIV + i;
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                        }
                        if (y == curInfo.endY)
                        {
                            int i = SUB_CELL_DIV - 1;
                            int _x = (x + 1) * SUB_CELL_DIV;
                            int _y = y * SUB_CELL_DIV + i;
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                        }
                    }
                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    // 마지막일 경우,
                    if (isEmpty && (render || y != curInfo.startY) && y == curInfo.endY)
                        _AddOneEdgeVertex(curInfo, true, (byte)(x + 1), (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                    else
                        _AddOneVert(curInfo, (byte)(x + 1), (byte)(y + 1), centerIndex, ref lastIndex, ref secIndex, curDirection);
                    //else
                    //    makeStartVert = true;
                    lastSideIndices = null;
                    lastWallIndices = null;
                }
            }

            // 이러면, 이 점은 2개로 분리가 된다는 뜻이다. 그래서 새로운 면을 그릴때 다시 설정해 주어야 한다.
            if (lastSideIndices != null && lastSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT)
                lastSideIndices = null;

            // 윗부분을 그린다.
            //ClearWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_N;
            curDirection = NORM_W;
            y = curInfo.endY;
            makeStartVert = false;
            for (x = curInfo.endX; x >= curInfo.startX; x--)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 윗쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 1) == 0)
                {
                    int _x = (x + 1) * SUB_CELL_DIV;
                    int _y = (y + 1) * SUB_CELL_DIV;
                    curYUp = true;
                    _AddOneEdgeVertex(curInfo, makeStartVert, (byte)(x + 1), (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                    makeStartVert = false;
                    
                    for (int i = 1; i <= SUB_CELL_DIV; i++)
                    {
                        _x = (x + 1) * SUB_CELL_DIV - i;
                        _y = (y + 1) * SUB_CELL_DIV;
                        
                        curYUp = true;
                        if (i == SUB_CELL_DIV)
                            _AddOneEdgeVertex(curInfo, true, (byte)x, (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                        else
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                    }
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    curYUp = true;
                    bool render = false;
                    bool isEmpty = (neighborThisFloor & 1) == 0;
                    // 정말 옆에 없다면, 시작과 끝일때만 그려준다.
                    if (isEmpty)
                    {
                        if (x == curInfo.endX)
                        {
                            render = true;
                            int i = 1;
                            int _x = (x + 1) * SUB_CELL_DIV - i;
                            int _y = (y + 1) * SUB_CELL_DIV;
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                        }
                        if (x == curInfo.startX)
                        {
                            int i = SUB_CELL_DIV - 1;
                            int _x = (x + 1) * SUB_CELL_DIV - i;
                            int _y = (y + 1) * SUB_CELL_DIV;
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                        }
                    }
                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    // 마지막일 경우,
                    if (isEmpty && (render || x != curInfo.endX) && x == curInfo.startX)
                        _AddOneEdgeVertex(curInfo, true, (byte)x, (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                    else
                        _AddOneVert(curInfo, (byte)x, (byte)(y + 1), centerIndex, ref lastIndex, ref secIndex, curDirection);
                    //else
                    //    makeStartVert = true;
                    lastSideIndices = null;
                    lastWallIndices = null;
                }
            }

            // 이러면, 이 점은 2개로 분리가 된다는 뜻이다. 그래서 새로운 면을 그릴때 다시 설정해 주어야 한다.
            if (lastSideIndices != null && lastSideIndices.Length >= PT_REV_LINK_START + TOTAL_LINK_POINT)
                lastSideIndices = null;

            // 왼쪽 부분을 그린다.
            //ClearWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_W;
            curDirection = NORM_S;
            makeStartVert = false;
            x = curInfo.startX;
            for (y = curInfo.endY; y >= curInfo.startY; y--)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 왼쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 8) == 0)
                {
                    int _x = x * SUB_CELL_DIV;
                    int _y = (y + 1) * SUB_CELL_DIV;
                    curYUp = false;
                    _AddOneEdgeVertex(curInfo, makeStartVert, (byte)x, (byte)(y + 1), centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                    makeStartVert = false;
                    
                    for (int i = 1; i <= SUB_CELL_DIV; i++)
                    {
                        _x = x * SUB_CELL_DIV;
                        _y = (y + 1) * SUB_CELL_DIV - i;

                        curYUp = true;
                        if (i == SUB_CELL_DIV)
                            _AddOneEdgeVertex(curInfo, true, (byte)x, (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                        else
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                    }
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    curYUp = true;
                    bool render = false;
                    bool isEmpty = (neighborThisFloor & 8) == 0;
                    // 정말 옆에 없다면, 시작과 끝일때만 그려준다.
                    if (isEmpty)
                    {
                        if (y == curInfo.endY)
                        {
                            render = true;
                            int i = 1;
                            int _x = x * SUB_CELL_DIV;
                            int _y = (y + 1) * SUB_CELL_DIV - i;
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                        }
                        if (y == curInfo.startY)
                        {
                            int i = SUB_CELL_DIV - 1;
                            int _x = x * SUB_CELL_DIV;
                            int _y = (y + 1) * SUB_CELL_DIV - i;
                            _AddOneMidVertex(curInfo, curDirection, _x, _y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex);
                        }
                    }
                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    // 마지막일 경우,
                    if (isEmpty && (render || y != curInfo.endY) && y == curInfo.startY)
                        _AddOneEdgeVertex(curInfo, true, (byte)x, (byte)y, centerIndex, curNormal, curYUp, ref lastYUp, ref lastIndex, ref lastSideIndices, ref lastWallIndices, ref secIndex, curDirection);
                    else
                        _AddOneVert(curInfo, (byte)x, (byte)y, centerIndex, ref lastIndex, ref secIndex, curDirection);
                    //else
                    //    makeStartVert = true;
                    lastSideIndices = null;
                    lastWallIndices = null;
                }
            }
        }

        const float TILE_OFFSET_Y = 0.04f;
        const float TILE_OFFSET = linkX * 0.7f;
        void _AddOneTileVert(CellStripeInfo curInfo, byte x, byte y, int centerIndex, ref int lastIndex, byte curDirection)
        {
            //bool drawSecondVertex, drawSecondNow;
            NewVertexInfo.NeighborInfo neighborInfo;
            byte curNormal = NORM_N;
            Vector3 offset = GetVertexOffset(curInfo, x, y, curDirection, ref curNormal, out neighborInfo) * LAND_OFFSET_SCALE;//, out drawSecondVertex, out drawSecondNow);
            offset.y += TILE_OFFSET_Y;
            // 조금씩 안쪽으로 shrink했다.
            if (!neighborInfo.hasReverse)
                offset -= NORMALS[neighborInfo.normalType] * TILE_OFFSET;
            Vector3 point = Point(floor, x * SUB_CELL_DIV, y * SUB_CELL_DIV);
            int index = AddVert(point + offset, curInfo.cellType);

            if (lastIndex != -1)
                AddTri(lastIndex, centerIndex, index, curInfo.cellType);
            lastIndex = index;
        }
        void _AddOneMidTileVertex(CellStripeInfo curInfo, byte curDirection, byte x, byte y, int _x, int _y, int centerIndex, ref int lastIndex)
        {
            Vector3 offset = Vector3.zero;
            
            int neighborIndex = curInfo.neighborInfoListAtVertex[x + y * (width + 1)];
            NewVertexInfo.NeighborInfo neighborInfo = NewVertexInfo.neighborInfo[neighborIndex];
            // 조금씩 안쪽으로 shrink했다.
            if (!neighborInfo.hasReverse)
                offset -= NORMALS[neighborInfo.normalType] * TILE_OFFSET;
            offset.y += TILE_OFFSET_Y;
            /*
            int x1 = Mathf.FloorToInt(_x / SUB_CELL_DIV);
            int y1 = Mathf.FloorToInt(_y / SUB_CELL_DIV);
            int xFrag = _x - x1 * SUB_CELL_DIV;
            int yFrag = _y - y1 * SUB_CELL_DIV;
            if (xFrag == 1 || xFrag == 3 || yFrag == 1 || yFrag == 3)
            {
                byte x = xFrag <= 2 ? (byte)x1 : (byte)(x1 + 1);
                byte y = yFrag <= 2 ? (byte)y1 : (byte)(y1 + 1);
                NewVertexInfo.NeighborInfo neighborInfo;
                offset = GetVertexOffset(curInfo, x, y, curDirection, ref curNormal, out neighborInfo) * 0.3f * LAND_OFFSET_SCALE;
            }//*/

            // land plane / land side / wall side의 normal방향이 같아야 한다. 그래야 random처리가 동일하게 된다.
            int index = AddVert(Point(floor, _x, _y) + offset, curInfo.cellType);
            if (lastIndex != -1)
                AddTri(lastIndex, centerIndex, index, curInfo.cellType);
            lastIndex = index;
        }
        public void CreateTileMesh(CellStripeInfo curInfo)
        {
            int centerIndex = AddVert(curInfo.GetCenter(), curInfo.cellType);

            // 밑의 line과 center point간의 triangle을 추가한다.
            int x = curInfo.startX;
            int y = curInfo.startY;

            //ClearWallSideVertexMap();
            byte curNormal = NORM_S;
            byte curDirection = NORM_E;
            int lastIndex = -1;
            bool makeStartVert = false;
            _AddOneTileVert(curInfo, (byte)x, (byte)y, centerIndex, ref lastIndex, curDirection);
            for (x = curInfo.startX; x <= curInfo.endX; x++)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 아래가 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 4) == 0)
                {
                    int _x = x * SUB_CELL_DIV;
                    int _y = y * SUB_CELL_DIV;
                    if (makeStartVert)
                        _AddOneTileVert(curInfo, (byte)x, (byte)y, centerIndex, ref lastIndex, curDirection);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV; i++)
                    {
                        _x = x * SUB_CELL_DIV + i;
                        _y = y * SUB_CELL_DIV;
                        if (i == SUB_CELL_DIV)
                            _AddOneTileVert(curInfo, (byte)(x+1), (byte)y, centerIndex, ref lastIndex, curDirection);
                        else
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)x, (byte)y, _x, _y, centerIndex, ref lastIndex);
                    }
                }
                // 아래에 무언가 있다면, skip한다.
                else
                {
                    // 정말 옆에 없다면, 시작과 끝일때만 그려준다.
                    bool isEmpty = (neighborThisFloor & 4) == 0;
                    if (isEmpty)
                    {
                        if (x == curInfo.startX)
                        {
                            int i = 1;
                            int _x = x * SUB_CELL_DIV + i;
                            int _y = y * SUB_CELL_DIV;
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)x, (byte)y, _x, _y, centerIndex, ref lastIndex);
                        }
                        if (x == curInfo.endX)
                        {
                            int i = SUB_CELL_DIV - 1;
                            int _x = x * SUB_CELL_DIV + i;
                            int _y = y * SUB_CELL_DIV;
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)x, (byte)y, _x, _y, centerIndex, ref lastIndex);
                        }
                    }

                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    _AddOneTileVert(curInfo, (byte)(x + 1), (byte)y, centerIndex, ref lastIndex, curDirection);
                }
            }

            // 오른쪽 부분을 그린다.
            //ClearWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_E;
            curDirection = NORM_N;
            makeStartVert = false;
            x = curInfo.endX;
            for (y = curInfo.startY; y <= curInfo.endY; y++)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 오른쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 2) == 0)
                {
                    int _x = (x + 1) * SUB_CELL_DIV;
                    int _y = y * SUB_CELL_DIV;
                    if (makeStartVert)
                        _AddOneTileVert(curInfo, (byte)(x+1), (byte)y, centerIndex, ref lastIndex, curDirection);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV; i++)
                    {
                        _x = (x + 1) * SUB_CELL_DIV;
                        _y = y * SUB_CELL_DIV + i;
                        if (i == SUB_CELL_DIV)
                            _AddOneTileVert(curInfo, (byte)(x + 1), (byte)(y+1), centerIndex, ref lastIndex, curDirection);
                        else
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)(x+1), (byte)y, _x, _y, centerIndex, ref lastIndex);
                    }
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    // 정말 옆에 없다면, 시작과 끝일때만 그려준다.
                    bool isEmpty = (neighborThisFloor & 2) == 0;
                    if (isEmpty)
                    {
                        if (y == curInfo.startY)
                        {
                            int i = 1;
                            int _x = (x + 1) * SUB_CELL_DIV;
                            int _y = y * SUB_CELL_DIV + i;
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)(x + 1), (byte)y, _x, _y, centerIndex, ref lastIndex);
                        }
                        if (y == curInfo.endY)
                        {
                            int i = SUB_CELL_DIV - 1;
                            int _x = (x + 1) * SUB_CELL_DIV;
                            int _y = y * SUB_CELL_DIV + i;
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)(x + 1), (byte)y, _x, _y, centerIndex, ref lastIndex);
                        }
                    }
                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    _AddOneTileVert(curInfo, (byte)(x + 1), (byte)(y+1), centerIndex, ref lastIndex, curDirection);
                }
            }

            // 윗부분을 그린다.
            //ClearWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_N;
            curDirection = NORM_W;
            y = curInfo.endY;
            makeStartVert = false;
            for (x = curInfo.endX; x >= curInfo.startX; x--)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 윗쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 1) == 0)
                {
                    int _x = (x + 1) * SUB_CELL_DIV;
                    int _y = (y + 1) * SUB_CELL_DIV;
                    if (makeStartVert)
                        _AddOneTileVert(curInfo, (byte)(x + 1), (byte)(y+1), centerIndex, ref lastIndex, curDirection);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV; i++)
                    {
                        _x = (x + 1) * SUB_CELL_DIV - i;
                        _y = (y + 1) * SUB_CELL_DIV;

                        if (i == SUB_CELL_DIV)
                            _AddOneTileVert(curInfo, (byte)x, (byte)(y + 1), centerIndex, ref lastIndex, curDirection);
                        else
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)(x + 1), (byte)(y+1), _x, _y, centerIndex, ref lastIndex);
                    }
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    bool isEmpty = (neighborThisFloor & 1) == 0;
                    // 정말 옆에 없다면, 시작과 끝일때만 그려준다.
                    if (isEmpty)
                    {
                        if (x == curInfo.endX)
                        {
                            int i = 1;
                            int _x = (x + 1) * SUB_CELL_DIV - i;
                            int _y = (y + 1) * SUB_CELL_DIV;
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)(x + 1), (byte)(y + 1), _x, _y, centerIndex, ref lastIndex);
                        }
                        if (x == curInfo.startX)
                        {
                            int i = SUB_CELL_DIV - 1;
                            int _x = (x + 1) * SUB_CELL_DIV - i;
                            int _y = (y + 1) * SUB_CELL_DIV;
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)(x + 1), (byte)(y + 1), _x, _y, centerIndex, ref lastIndex);
                        }
                    }
                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    _AddOneTileVert(curInfo, (byte)x, (byte)(y + 1), centerIndex, ref lastIndex, curDirection);
                }
            }

            // 왼쪽 부분을 그린다.
            //ClearWallSideVertexMap();
            //lastWallIndices = null;
            curNormal = NORM_W;
            curDirection = NORM_S;
            makeStartVert = false;
            x = curInfo.startX;
            for (y = curInfo.endY; y >= curInfo.startY; y--)
            {
                int neighbor = curInfo.neighborInfoList[x + y * width];
                int neighborThisFloor = curInfo.neighborInfoListOnlyThisFloor[x + y * width];
                // 왼쪽이 없으면, SUB_CELL_DIV 만큼 나눠서 vertex를 등록한다.
                if ((neighbor & 8) == 0)
                {
                    int _x = x * SUB_CELL_DIV;
                    int _y = (y + 1) * SUB_CELL_DIV;
                    if (makeStartVert)
                        _AddOneTileVert(curInfo, (byte)x, (byte)(y + 1), centerIndex, ref lastIndex, curDirection);
                    makeStartVert = false;

                    for (int i = 1; i <= SUB_CELL_DIV; i++)
                    {
                        _x = x * SUB_CELL_DIV;
                        _y = (y + 1) * SUB_CELL_DIV - i;

                        if (i == SUB_CELL_DIV)
                            _AddOneTileVert(curInfo, (byte)x, (byte)y, centerIndex, ref lastIndex, curDirection);
                        else
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)x, (byte)(y + 1), _x, _y, centerIndex, ref lastIndex);
                    }
                }
                // 아래에 무언가 있다면, skip한다.
                // 단, 마지막일 경우에는 그린다.
                else
                {
                    bool isEmpty = (neighborThisFloor & 8) == 0;
                    // 정말 옆에 없다면, 시작과 끝일때만 그려준다.
                    if (isEmpty)
                    {
                        if (y == curInfo.endY)
                        {
                            int i = 1;
                            int _x = x * SUB_CELL_DIV;
                            int _y = (y + 1) * SUB_CELL_DIV - i;
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)x, (byte)(y + 1), _x, _y, centerIndex, ref lastIndex);
                        }
                        if (y == curInfo.startY)
                        {
                            int i = SUB_CELL_DIV - 1;
                            int _x = x * SUB_CELL_DIV;
                            int _y = (y + 1) * SUB_CELL_DIV - i;
                            _AddOneMidTileVertex(curInfo, curDirection, (byte)x, (byte)(y + 1), _x, _y, centerIndex, ref lastIndex);
                        }
                    }
                    // random point로 인해서 중간이 끊어질수 있으므로, 무조건 vertex들은 추가해야 된다.
                    _AddOneTileVert(curInfo, (byte)x, (byte)y, centerIndex, ref lastIndex, curDirection);
                }
            }
        }
    }
    protected override void Awake()
    {
        base.Awake();
        _PrepareOffset();
    }

    void _PrepareOffset()
    {
        const int MAX_SAMPLE = 20;
        CellGroupNewTerrain.POINT_OFFSET = new float[MAX_SAMPLE];
        for (int i = 0; i < MAX_SAMPLE; i++)
            CellGroupNewTerrain.POINT_OFFSET[i] = edgeCurve.Evaluate((float)i / (float)MAX_SAMPLE) * cellX * 0.05f;
    }


    // neighborIndex
    //       +---+
    //       | 1 |
    //   +---+---+---+
    //   | 8 |   | 2 |
    //   +---+---+---+
    //       | 4 |
    //       +---+
    protected class CellStripeInfo
    {
        public byte floor;
        public byte startX;
        public byte startY;
        public byte endX;
        public byte endY;
        public Dictionary<int, int> neighborInfoList;
        public Dictionary<int, int> neighborInfoListOnlyThisFloor;
        public Dictionary<int, int> neighborInfoListAtVertex;
        public Dictionary<int, int> neighborInfoListLand;
        public byte cellType;
        CubeNewTerrain terrain;
        bool forTile;
        public CellStripeInfo(byte floor, byte startX, byte startY, byte cellType, CubeNewTerrain terrain, bool forTile = false)
        {
            this.cellType = cellType;
            this.terrain = terrain;
            this.floor = floor;
            this.startX = startX;
            this.startY = startY;

            this.endX = startX;
            this.endY = startY;
            this.forTile = forTile;

            neighborInfoList = new Dictionary<int, int>();
            neighborInfoListOnlyThisFloor = new Dictionary<int, int>();
            if (forTile)
            {
                int value = terrain.GetNeighborTileIndex(cellType, floor, startX, startY);
                neighborInfoList[startX + startY * terrain.terrainWidth] = value;
                neighborInfoListOnlyThisFloor[startX + startY * terrain.terrainWidth] = value;
            }
            else
            {
                neighborInfoList[startX + startY * terrain.terrainWidth] = terrain.GetNeighborCellIndexWithType(cellType, floor, terrain.cellArr, terrain.terrainWidth, terrain.terrainHeight, startX, startY);
                neighborInfoListOnlyThisFloor[startX + startY * terrain.terrainWidth] = terrain.GetNeighborCellIndexWithType(cellType, floor, terrain.cellArr, terrain.terrainWidth, terrain.terrainHeight, startX, startY, true);
            }
        }
        public void Add(byte x, byte y)
        {
            this.endX = (byte)Mathf.Max(startX, x);
            this.endY = (byte)Mathf.Max(startY, y);
            if (forTile)
            {
                int value = terrain.GetNeighborTileIndex(cellType, floor, x, y);
                neighborInfoList[x + y * terrain.terrainWidth] = value;
                neighborInfoListOnlyThisFloor[x + y * terrain.terrainWidth] = value;
            }
            else
            {
                neighborInfoList[x + y * terrain.terrainWidth] = terrain.GetNeighborCellIndexWithType(cellType, floor, terrain.cellArr, terrain.terrainWidth, terrain.terrainHeight, x, y);
                neighborInfoListOnlyThisFloor[x + y * terrain.terrainWidth] = terrain.GetNeighborCellIndexWithType(cellType, floor, terrain.cellArr, terrain.terrainWidth, terrain.terrainHeight, x, y, true);
            }
        }

        public void Finish()
        {
            // 이거는 +1까지 체크해야 되므로, stride가 width + 1이다.
            neighborInfoListAtVertex = new Dictionary<int, int>();
            if (forTile)
            {
                for (int y = startY; y <= endY + 1; y++)
                    for (int x = startX; x <= endX + 1; x++)
                        neighborInfoListAtVertex[x + y * (terrain.terrainWidth + 1)] = terrain.GetNeighborTileIndexAtPoint(cellType, floor, (byte)x, (byte)y);
            }
            else
            {
                for (int y = startY; y <= endY + 1; y++)
                    for (int x = startX; x <= endX + 1; x++)
                        neighborInfoListAtVertex[x + y * (terrain.terrainWidth + 1)] = terrain.GetNeighborCellIndexAtPoint(cellType, floor, terrain.cellArr, terrain.terrainWidth, terrain.terrainHeight, (byte)x, (byte)y);
            }
            
        }
        public Vector3 GetCenter()
        {
            float x = (startX + endX+1) * GetCellFullWidth() * 0.5f;
            float y = floor * cellY;
            float z = (startY + endY+1) * GetCellFullHeight() * 0.5f;
            return new Vector3(x, y, z);
        }
        public Vector3 GetSize()
        {
            float x = (endX + 1) * GetCellFullWidth();
            float y = floor * cellY;
            float z = (endY + 1) * GetCellFullHeight();
            return (new Vector3(x, y, z) - GetCenter()) * 2;
        }
    }
    protected override Vector3 GetPoint(byte floor, byte x, byte y)
    {
        return CellGroupNewTerrain.Point(floor, x * SUB_CELL_DIV, y * SUB_CELL_DIV, false);
    }
    protected override Vector3 GetPoint(byte floor, float x, float y)
    {
        return CellGroupNewTerrain.Point(floor, x * SUB_CELL_DIV, y * SUB_CELL_DIV, false);
    }
    protected override void CreateBasePlane()
    {
        basePlaneInfo = new NewVertexInfo();
        basePlaneInfo.AddBasePlane(0, 0, terrainWidth, terrainHeight);
        basePlaneInfo.CreateMesh(newBlock.transform, "base", basePlaneMaterial, true);
    }
    protected override void RefreshTerrain()
    {
        int width = terrainWidth;
        int height = terrainHeight;
        CheckCellArr(cellArr, width, height, tileArr);

        int totalGroupX = Mathf.CeilToInt((float)width / (float)GROUP_UNIT_X);
        int totalGroupY = Mathf.CeilToInt((float)height / (float)GROUP_UNIT_Y);
        for (byte floor = 0; floor < MAX_FLOOR; floor++)
        {
            for (byte gy = 0; gy < totalGroupY; gy++)
            {
                for (byte gx = 0; gx < totalGroupX; gx++)
                {
                    byte startX = (byte)(gx * GROUP_UNIT_X);
                    byte startY = (byte)(gy * GROUP_UNIT_Y);
                    int groupIndex = GetCellGroupIndex(floor, startX, startY);

                    {
                        CellGroup _curGroup;
                        if (!groupMap.TryGetValue(groupIndex, out _curGroup))
                        {
                            _curGroup = new CellGroupNewTerrain(this, groupIndex, floor, startX, startY, baseMaterialList, propInfo, cellArr, width, height);
                            groupMap[groupIndex] = _curGroup;
                        }
                        if (_curGroup.IsReset())
                        {
                            CellGroupNewTerrain curGroup = (CellGroupNewTerrain)_curGroup;
                            curGroup.CreateGroup(newBlock.transform, baseMaterialList, true);

                            CheckPlaneTypeOnGroup(floor, startX, startY, CELL_LAND, curGroup);
                            CheckPlaneTypeOnGroup(floor, startX, startY, CELL_WATER, curGroup);
                        }
                    }

                    CellGroup _tileGroup;
                    if (!tileGroupMap.TryGetValue(groupIndex, out _tileGroup))
                    {
                        _tileGroup = new CellGroupNewTerrain(this, groupIndex, floor, startX, startY, tileMaterials, propInfo, tileArr, width, height);
                        tileGroupMap[groupIndex] = _tileGroup;
                    }
                    Dictionary<int, bool> usingMaterials;
                    if (_tileGroup.IsReset() && HasTileIndex(floor, startX, startY, out usingMaterials))
                    {
                        CellGroupNewTerrain curGroup = (CellGroupNewTerrain)_tileGroup;
                        curGroup.CreateGroup(newBlock.transform, tileMaterials, false);

                        foreach (var d in usingMaterials)
                            CheckTileOnGroup(floor, startX, startY, (byte)d.Key, curGroup);
                    }
                }
            }
        }

        foreach (var d in groupMap)
            ((CellGroupNewTerrain)d.Value).CreatePlaneMeshes(baseMaterialList, true);
        if (tileMaterials != null && tileMaterials.Count > 0)
        {
            foreach (var d in tileGroupMap)
                ((CellGroupNewTerrain)d.Value).CreatePlaneMeshes(tileMaterials, false);
        }
    }

    bool HasTileIndex(byte floor, byte startX, byte startY, out Dictionary<int, bool> usingMaterials)
    {
        usingMaterials = new Dictionary<int, bool>();
        int width = terrainWidth;
        int height = terrainHeight;
        for (byte y = startY; y < startY + GROUP_UNIT_Y && y < height; y++)
        {
            for (byte x = startX; x < startX + GROUP_UNIT_X && x < width; x++)
            {
                int index = x + y * width;
                int tileIndex = tileArr[index];
                if (tileIndex != TILE_NONE && tileMaterials != null && tileMaterials.Count > tileIndex)
                {
                    byte originalCellType = cellArr[index];
                    byte cellType = GetBaseCellOnFloor(floor, originalCellType, x, y);
                    if (cellType == CELL_LAND || cellType == CELL_SMOOTH)
                        usingMaterials[tileIndex] = true;
                }
            }
        }
        return usingMaterials.Count > 0;
    }

    bool IsTile(byte floor, byte x, byte y, byte tileInd)
    {
        int index = x + y * terrainWidth;
        int tileIndex = tileArr[index];
        if (tileIndex == tileInd)
        {
            byte originalCellType = cellArr[index];
            byte cellType = GetBaseCellOnFloor(floor, originalCellType, x, y);
            if (cellType == CELL_LAND || cellType == CELL_SMOOTH)
                return true;
        } 
        return false;
    }
    void CheckTileOnGroup(byte floor, byte startX, byte startY, byte tileIndex, CellGroupNewTerrain curGroup)
    {
        int width = terrainWidth;
        int height = terrainHeight;
        // 가로축부터 연결되는 cell Stripe 을 구한다. 그리고, 세로축으로 하나씩 올라가면서 최대한 큰 직사각형을 만든다.
        // 이걸로 polygon 최적화를 한다.
        Dictionary<int, bool> usedMap = new Dictionary<int, bool>();
        for (byte y = startY; y < startY + GROUP_UNIT_Y && y < height; y++)
        {
            for (byte x = startX; x < startX + GROUP_UNIT_X && x < width; x++)
            {
                int index = x + y * width;
                if (usedMap.ContainsKey(index))
                    continue;

                if (IsTile(floor, x, y, tileIndex))
                {
                    usedMap[index] = true;
                    CellStripeInfo curInfo = new CellStripeInfo(floor, x, y, tileIndex, this, true);

                    byte endX = x;
                    // 여기서부터 시작해서 다시 체크한다.
                    for (byte sx = (byte)(x + 1); sx < startX + GROUP_UNIT_X && sx < width; sx++)
                    {
                        int s_index = sx + y * width;
                        if (usedMap.ContainsKey(s_index))
                            break;
                        if (IsTile(floor, sx, y, tileIndex))
                        {
                            usedMap[s_index] = true;
                            endX = sx;
                            curInfo.Add(sx, y);
                        }
                        else
                            break;
                    }
                    // x ~ endX 를 기준으로 y축으로 위로 올려봐서 cover되는지 체크한다.
                    for (byte sy = (byte)(y + 1); sy < startY + GROUP_UNIT_Y && sy < height; sy++)
                    {
                        bool canCover = true;
                        for (byte sx = x; sx <= endX; sx++)
                        {
                            int s_index = sx + sy * width;
                            if (usedMap.ContainsKey(s_index))
                            {
                                canCover = false;
                                break;
                            }
                            if (!IsTile(floor, sx, sy, tileIndex))
                            {
                                canCover = false;
                                break;
                            }
                        }
                        if (canCover)
                        {
                            for (byte sx = x; sx <= endX; sx++)
                            {
                                int s_index = sx + sy * width;
                                usedMap[s_index] = true;
                                curInfo.Add(sx, sy);
                            }
                        }
                        else
                            break;
                    }
                    curInfo.Finish();
                        
                    curGroup.CreateTileMesh(curInfo);
                }
            }
        }
    }
    void CheckPlaneTypeOnGroup(byte floor, byte startX, byte startY, byte checkCellType, CellGroupNewTerrain curGroup)
    {
        int width = terrainWidth;
        int height = terrainHeight;
        // 가로축부터 연결되는 cell Stripe 을 구한다. 그리고, 세로축으로 하나씩 올라가면서 최대한 큰 직사각형을 만든다.
        // 이걸로 polygon 최적화를 한다.
        Dictionary<int, bool> usedMap = new Dictionary<int, bool>();
        for (byte y = startY; y < startY + GROUP_UNIT_Y && y < height; y++)
        {
            for (byte x = startX; x < startX + GROUP_UNIT_X && x < width; x++)
            {
                int index = x + y * width;
                if (usedMap.ContainsKey(index))
                    continue;

                byte originalCellType = cellArr[index];
                byte cellType = GetBaseCellOnFloor(floor, originalCellType, x, y);
                bool makeCell = cellType == checkCellType;
                if (checkCellType == CELL_WATER)
                    makeCell |= cellType >= CELL_HALF_WATER;
                //byte tileIndex = tileArr[index];
                if (makeCell)
                {
                    usedMap[index] = true;
                    CellStripeInfo curInfo = new CellStripeInfo(floor, x, y, cellType, this);

                    byte endX = x;
                    // 여기서부터 시작해서 다시 체크한다.
                    for (byte sx = (byte)(x + 1); sx < startX + GROUP_UNIT_X && sx < width; sx++)
                    {
                        int s_index = sx + y * width;
                        if (usedMap.ContainsKey(s_index))
                            break;
                        byte s_originalCellType = cellArr[s_index];
                        byte s_cellType = GetBaseCellOnFloor(floor, s_originalCellType, sx, y);
                        if (s_cellType == checkCellType)
                        {
                            usedMap[s_index] = true;
                            endX = sx;
                            curInfo.Add(sx, y);
                        }
                        else
                            break;
                    }
                    // x ~ endX 를 기준으로 y축으로 위로 올려봐서 cover되는지 체크한다.
                    for (byte sy = (byte)(y + 1); sy < startY + GROUP_UNIT_Y && sy < height; sy++)
                    {
                        bool canCover = true;
                        for (byte sx = x; sx <= endX; sx++)
                        {
                            int s_index = sx + sy * width;
                            if (usedMap.ContainsKey(s_index))
                            {
                                canCover = false;
                                break;
                            }
                            byte s_originalCellType = cellArr[s_index];
                            byte s_cellType = GetBaseCellOnFloor(floor, s_originalCellType, sx, sy);
                            if (s_cellType != checkCellType)
                            {
                                canCover = false;
                                break;
                            }
                        }
                        if (canCover)
                        {
                            for (byte sx = x; sx <= endX; sx++)
                            {
                                int s_index = sx + sy * width;
                                usedMap[s_index] = true;
                                curInfo.Add(sx, sy);
                            }
                        }
                        else
                            break;
                    }
                    curInfo.Finish();

                    if (checkCellType == CELL_LAND)
                        curGroup.CreateLandMesh(curInfo, baseMaterialList);
                    else if (checkCellType == CELL_WATER)
                        curGroup.CreateWaterMesh(curInfo, baseMaterialList);
                }
            }
        }
    }


    const int TOTAL_LINK_POINT = 3;
    const byte PT_TOP = 0;
    const byte PT_LINK_START = 1;    // 1개뿐일때 혹은 y up 방향 normal인 vertex
    const byte PT_REV_LINK_START = PT_LINK_START + TOTAL_LINK_POINT; // y down 방향 normal인 vertex

    // 부채꼴을 만들기 위한 삽입되는 vertex수
    const int TOTAL_TRI = 3;

    const float linkX = cellX * 0.08f / POLYGON_SCALE;
    const float linkY = cellY * 0.1f;
    class NewVertex
    {
        public Vector3[] points;
        public byte normalType;
        static Vector3[] tempNormals;
        static Vector2[] tempUVs;
        public int[] vertIndices;
        //
        //       +---+
        //      .|   |
        //   +---+---+
        //   |   | '
        //   +---+
        //   이렇게 block이 cross로 연결된 경우, link point는 2종류로 생긴다.
        // 기본 normal type이 아닌 경우에 대한 link point들. (대부분의 경우 null이다)

        // 가정 : 1개 terrain 의 cell index값은 [0,255] 사이값 이고, 각각 SUB_CELL_DIV 만큼 나뉘어져있다.
        // 즉 x = cell index * SUB_CELL_DIV
        public NewVertex(NewVertexInfo info, byte floor, short x, short y, NewVertexInfo.NeighborInfo neighbor, int planeType, Vector3 offset, int planeNormalType = -1)
        {
            Vector3 startPosition = CellGroupNewTerrain.Point(floor, x, y) + offset;
            bool isWaterSide = planeType == PLANE_WATER_SIDE || planeType == PLANE_WATER_SIDE2;
            if (planeType == PLANE_WATER || isWaterSide)
                startPosition.y -= waterDepth;
            else if (planeType == PLANE_WATER_WALL)
                startPosition.y -= waterPlaneDepth;

            bool hasReverseTop = isWaterSide && neighbor.hasReverse;

            int total = neighbor.onlyTop ? 1 : (neighbor.hasReverse ? PT_REV_LINK_START + TOTAL_LINK_POINT : PT_LINK_START + TOTAL_LINK_POINT);
            if (planeType == PLANE_WALL || planeType == PLANE_WATER_WALL)
                total = 2;
            // 물이 출렁거려서, 마지막에 reverse용 top을 하나 더 둔다.
            else if (hasReverseTop)
                total += 1;

            points = new Vector3[total];
            vertIndices = new int[total];
            points[PT_TOP] = startPosition;
            this.normalType = neighbor.normalType;
            Vector3 normal = (NORMALS[normalType].z < 0 && neighbor.hasReverse) ? -NORMALS[normalType] : NORMALS[normalType];
            if (planeNormalType != -1)
                normal = NORMALS[planeNormalType];

            // 물이 출렁거려서, 마지막에 reverse용 top을 하나 더 둔다.
            if (tempNormals == null)
                tempNormals = new Vector3[PT_REV_LINK_START + TOTAL_LINK_POINT + 1];
            if (tempUVs == null)
                tempUVs = new Vector2[PT_REV_LINK_START + TOTAL_LINK_POINT + 1];

            if (planeType == PLANE_WALL || planeType == PLANE_WATER_WALL)
            {
                tempNormals[PT_TOP] = normal;
                tempUVs[PT_TOP] = CellGroupNewTerrain.WallUV(ref points[PT_TOP]);

                int index = PT_TOP + 1;
                points[index] = points[PT_TOP];
                if (planeType == PLANE_WATER_WALL)
                    points[index].y -= (cellY * (1 + floor) - waterPlaneDepth);
                else
                    points[index].y -= cellY * (1 + floor);
                tempNormals[index] = normal;
                tempUVs[index] = CellGroupNewTerrain.WallUV(ref points[index]);
            }
            else
            {
                tempNormals[PT_TOP] = Vector3.up;
                tempUVs[PT_TOP] = CellGroupNewTerrain.UV(ref points[PT_TOP]);
            }

            Vector3 topPoint = points[PT_TOP];
            Vector3 reverseTopPoint = points[PT_TOP];
            const float XRATIO = 1f;
            const float UV_SCALE = 1f;
            if (isWaterSide)
            {
                points[PT_TOP] = startPosition - normal * linkX * XRATIO;
                points[PT_TOP].y += linkY * 0.5f;

                topPoint = points[PT_TOP];
            }
            if (hasReverseTop)
            {
                // 마지막에 reverse용 top을 넣는다.
                int index = points.Length - 1;
                points[index] = startPosition + normal * linkX * XRATIO;
                points[index].y = points[PT_TOP].y;
                tempNormals[index] = tempNormals[PT_TOP];
                tempUVs[index] = tempUVs[PT_TOP];

                reverseTopPoint = points[index];
            }
            if (!neighbor.onlyTop && planeType != PLANE_WALL && planeType != PLANE_WATER_WALL)
            {
                //  * top
                //    *
                //    *
                //  *
                //  
                //
                //
                //  * bottom
                points[PT_LINK_START] = topPoint + normal * linkX * XRATIO;
                points[PT_LINK_START].y -= linkY * 0.5f;
                tempNormals[PT_LINK_START] = (normal + Vector3.up).normalized;
                if (!(isWaterSide || planeType == PLANE_LAND_SIDE))
                {
                    float dist = (topPoint - points[PT_LINK_START]).magnitude;
                    tempUVs[PT_LINK_START] = tempUVs[PT_TOP] + cellY * dist * UV_SCALE * new Vector2(normal.x, normal.z);
                }

                int index = PT_LINK_START + 1;
                points[index] = topPoint + normal * linkX * XRATIO * 1.3f;
                points[index].y -= linkY * 1f;
                tempNormals[index] = (normal - Vector3.up).normalized;
                if (!(isWaterSide || planeType == PLANE_LAND_SIDE))
                {
                    float dist = (topPoint - points[index]).magnitude;
                    tempUVs[index] = tempUVs[PT_TOP] + cellY * dist * UV_SCALE * new Vector2(normal.x, normal.z);
                }

                index = PT_LINK_START + 2;
                if (isWaterSide)
                {
                    points[index] = points[index - 1];
                    points[index].y -= (cellY * (floor + 1) - waterDepth);
                    tempNormals[index] = normal;
                }
                else
                {
                    points[index] = topPoint;
                    points[index].y -= linkY * 2;
                    tempNormals[index] = -Vector3.up;
                    if (planeType != PLANE_LAND_SIDE)
                    {
                        float dist = (topPoint - points[index]).magnitude;
                        tempUVs[index] = tempUVs[PT_TOP] + cellY * dist * UV_SCALE * new Vector2(normal.x, normal.z);
                    }
                }

                if (neighbor.hasReverse)
                {
                    points[PT_REV_LINK_START] = reverseTopPoint - normal * linkX * XRATIO;
                    points[PT_REV_LINK_START].y -= linkY * 0.5f;
                    tempNormals[PT_REV_LINK_START] = (-normal + Vector3.up).normalized;
                    if (!(isWaterSide || planeType == PLANE_LAND_SIDE))
                    {
                        float dist = (reverseTopPoint - points[PT_REV_LINK_START]).magnitude;
                        tempUVs[PT_REV_LINK_START] = tempUVs[PT_TOP] - cellY * dist * UV_SCALE * new Vector2(normal.x, normal.z);
                    }

                    index = PT_REV_LINK_START + 1;
                    points[index] = reverseTopPoint - normal * linkX * XRATIO * 1.3f;
                    points[index].y -= linkY * 1f;
                    tempNormals[index] = (-normal - Vector3.up).normalized;
                    if (!(isWaterSide || planeType == PLANE_LAND_SIDE))
                    {
                        float dist = (reverseTopPoint - points[index]).magnitude;
                        tempUVs[index] = tempUVs[PT_TOP] - cellY * dist * UV_SCALE * new Vector2(normal.x, normal.z);
                    }

                    index = PT_REV_LINK_START + 2;
                    if (isWaterSide)
                    {
                        points[index] = points[index - 1];
                        points[index].y -= (cellY * (floor + 1) - waterDepth);
                        tempNormals[index] = -normal;
                    }
                    else
                    {
                        points[index] = reverseTopPoint;
                        points[index].y -= linkY * 2;
                        tempNormals[index] = -Vector3.up;
                        if (planeType != PLANE_LAND_SIDE)
                        {
                            float dist = (reverseTopPoint - points[index]).magnitude;
                            tempUVs[index] = tempUVs[PT_TOP] - cellY * dist * UV_SCALE * new Vector2(normal.x, normal.z);
                        }
                    }
                }
            }

            for (byte i = 0; i < points.Length; i++)
            {
                //Vector2 uv = VertextInfo.UV(ref points[i]);
                //info.InsertVertex(ref points[i], ref tempNormals[i], ref uv, x, y, i);
                if (planeType == PLANE_LAND_SIDE)
                {
                    int lastIndex = hasReverseTop ? (PT_LINK_START + TOTAL_LINK_POINT - 1) : points.Length - 1;
                    float h = points[0].y - points[lastIndex].y;
                    float cur = points[i].y - points[lastIndex].y;
                    Vector2 uv2 = new Vector2(0, h == 0 ? 0 : Mathf.Sqrt(cur / h));
                    Vector2 uv = CellGroupNewTerrain.UV(ref points[i]);
                    vertIndices[i] = info.InsertVertex2(floor, ref points[i], ref tempNormals[i], ref uv, ref uv2, x, y, i);
                }
                else if (isWaterSide)
                {
                    int lastIndex = hasReverseTop ? (PT_LINK_START + TOTAL_LINK_POINT - 1) : points.Length - 1;
                    float h = points[0].y - points[lastIndex].y;
                    float cur = points[i].y - points[lastIndex].y;
                    Vector2 uv = Vector2.zero;
                    uv.x = (points[i].x / cellX + points[i].z / cellZ) * 1.5f;
                    uv.y = h == 0 ? 0 : cur / h;
                    vertIndices[i] = info.InsertVertex(floor, ref points[i], ref tempNormals[i], ref uv, x, y, i);
                }
                else
                    vertIndices[i] = info.InsertVertex(floor, ref points[i], ref tempNormals[i], ref tempUVs[i], x, y, i);
            }
        }
    }

    class NewVertexInfo
    {
        // key : terrain map index(y<<8+x)
        public Dictionary<long, NewVertex> cellPointMap;
        public List<Vector3> vertexList;
        public List<Vector3> normalList;
        public List<Vector2> uvList;
        public List<Vector2> uv2List;
        // key : terrain map index + point index in Vertex class(y<<8+x + index<<16)
        // value : vertexList index
        public Dictionary<long, int> vertexIndexMap;
        public List<int> triangleList;

        public NewVertexInfo()
        {
            cellPointMap = new Dictionary<long, NewVertex>();
            vertexList = new List<Vector3>();
            normalList = new List<Vector3>();
            uvList = new List<Vector2>();
            uv2List = new List<Vector2>();
            vertexIndexMap = new Dictionary<long, int>();
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
        // 가정 : 1개 terrain 의 cell index값은 [0,255] 사이값 이고, 각각 SUB_CELL_DIV 만큼 나뉘어져있다.
        // 즉 x = cell index * SUB_CELL_DIV
        public static long MapIndex(byte floor, short x, short y)
        {
            return (long)(floor << 32) + (long)(y << 16) + x;
        }
        public static long VertexIndex(byte floor, short x, short y, byte pointIndex)
        {
            return (long)(pointIndex << 40) + (long)(floor << 32) + (long)(y << 16) + x;
        }
        public NewVertex AddVertex(byte floor, short x, short y, NeighborInfo info, int planeType, Vector3 offset, int planeNormalType = -1)
        {
            NewVertex vertex;
            long mapIndex = MapIndex(floor, x, y);
            if (!cellPointMap.TryGetValue(mapIndex, out vertex))
            {
                vertex = new NewVertex(this, floor, x, y, info, planeType, offset, planeNormalType);
                cellPointMap[mapIndex] = vertex;
            }
            return vertex;
        }
        public int InsertVertex(byte floor, ref Vector3 vertex, ref Vector3 normal, ref Vector2 uv, short x, short y, byte pointIndex)
        {
            vertexList.Add(vertex);
            normalList.Add(normal);
            uvList.Add(uv);
            int index = vertexList.Count - 1;
            vertexIndexMap[VertexIndex(floor, x, y, pointIndex)] = index;
            return index;
        }
        public int InsertVertex2(byte floor, ref Vector3 vertex, ref Vector3 normal, ref Vector2 uv, ref Vector2 uv2, short x, short y, byte pointIndex)
        {
            vertexList.Add(vertex);
            normalList.Add(normal);
            uvList.Add(uv);
            uv2List.Add(uv2);
            int index = vertexList.Count - 1;
            vertexIndexMap[VertexIndex(floor, x, y, pointIndex)] = index;
            return index;
        }
        public int InsertVertex3(ref Vector3 vertex, ref Vector3 normal, Vector2 uv)
        {
            vertexList.Add(vertex);
            normalList.Add(normal);
            uvList.Add(uv);
            return vertexList.Count - 1;
        }
        public int GetVertexInfo(byte floor, short x, short y, byte pointIndex)
        {
            int result = 0;
            if (vertexIndexMap.TryGetValue(VertexIndex(floor, x, y, pointIndex), out result))
                return result;
            return result;
        }
        public bool HasVertexInfo(byte floor, short x, short y, byte pointIndex)
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
        public void AddBasePlane(byte sx, byte sy, int width, int height)
        {
            byte floor = 0;
            float h = -cellY;
            Vector3 pt1 = CellGroupNewTerrain.Point(floor, sx * SUB_CELL_DIV, sy * SUB_CELL_DIV, false);
            pt1.y += h;
            Vector3 pt2 = CellGroupNewTerrain.Point(floor, (sx + width) * SUB_CELL_DIV, sy * SUB_CELL_DIV, false);
            pt2.y += h;
            Vector3 pt3 = CellGroupNewTerrain.Point(floor, (sx + width) * SUB_CELL_DIV, (sy + height) * SUB_CELL_DIV, false);
            pt3.y += h;
            Vector3 pt4 = CellGroupNewTerrain.Point(floor, sx * SUB_CELL_DIV, (sy + height) * SUB_CELL_DIV, false);
            pt4.y += h;

            Vector3 normal = Vector3.up;

            int ind1 = InsertVertex3(ref pt1, ref normal, CellGroupNewTerrain.UV(ref pt1));
            int ind2 = InsertVertex3(ref pt2, ref normal, CellGroupNewTerrain.UV(ref pt2));
            int ind3 = InsertVertex3(ref pt3, ref normal, CellGroupNewTerrain.UV(ref pt3));
            int ind4 = InsertVertex3(ref pt4, ref normal, CellGroupNewTerrain.UV(ref pt4));

            AddPolygon(ind2, ind1, ind4, ind3);
        }
        /*
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
            Vector3 startPt = CellGroupNewTerrain.Point(floor, sx * SUB_CELL_DIV, sy * SUB_CELL_DIV);
            startPt.y -= depth;
            Vector3 startBottomPt = startPt;
            startBottomPt.y -= cellY * (floor + 1) - depth;
            Vector3 endPt = CellGroupNewTerrain.Point(floor, ex, ey);
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
        */
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
        
        public Renderer CreateMesh(Transform parent, string meshName, Material material, bool createCollider, bool castShadow = true, int planeIndex = 0, CellGroupNewTerrain owner = null)
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
                    if (planeIndex == PLANE_WATER)
                    {
                        if (owner != null)
                        {
                            GameObject box = new GameObject("water_box");
                            box.transform.SetParent(obj.transform);
                            box.transform.localPosition = Vector3.zero;
                            box.transform.localRotation = Quaternion.identity;
                            box.transform.localScale = Vector3.one;
                            box.tag = Definitions.TAG_WATER;
                            box.layer = Definitions.LAYER_WATER;

                            if (owner.waterBoundsList != null)
                            {
                                for (int b=0; b< owner.waterBoundsList.Count; b++)
                                {
                                    BoxCollider boxCol = box.AddComponent<BoxCollider>();
                                    boxCol.center = owner.waterBoundsList[b].center;
                                    boxCol.size = owner.waterBoundsList[b].size;
                                    boxCol.isTrigger = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        MeshCollider collider = obj.AddComponent<MeshCollider>();
                        collider.sharedMesh = mesh;
                    }
                }

#if UNITY_EDITOR
                //UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/Prefabs/Test/" + parent.name + meshName + "_r.mesh");
#endif

                return renderer;
            }
            return null;
        }
    }

    public override void SetEditMode(bool on)
    {
        bool prev = editMode;
        base.SetEditMode(on);
        if (newBlock != null && prev != on)
        {
            LODGroup[] list = newBlock.GetComponentsInChildren<LODGroup>();
            for (int i = 0; i < list.Length; i++)
                list[i].enabled = !on;
        }
    }

    public void CopyAssetsFrom(CubeNewTerrain source)
    {
        selectionMarkPrefab = source.selectionMarkPrefab;
        touchMarkPrefab = source.touchMarkPrefab;
        brushMarkPrefab = source.brushMarkPrefab;
        if (null != source.propInfo && source.propInfo.Length > 0)
        {
            propInfo = new TilePropInfo[source.propInfo.Length];
            for (int i=0; i< source.propInfo.Length; i++)
            {
                propInfo[i] = new TilePropInfo();
                propInfo[i].CopyFrom(source.propInfo[i]);
            }   
        }
        edgeCurve = source.edgeCurve;
        _PrepareOffset();
        _InitProp();
    }
}
