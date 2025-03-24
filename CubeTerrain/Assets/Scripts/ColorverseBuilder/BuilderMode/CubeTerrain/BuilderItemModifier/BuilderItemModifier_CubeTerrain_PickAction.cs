namespace Colorverse.Builder
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    using UnityEngine;

    public struct TerrainSize
    {
        public int width;
        public int height;

        public TerrainSize(int w, int h)
        {
            width = w;
            height = h;
        }
    }

    public struct TerrainPivot
    {
        public int x;
        public int y;

        public TerrainPivot(int pX, int pZ)
        {
            x = pX;
            y = pZ;
        }
    }

    public class RecordCubeTerrain
    {
        public TerrainSize Size = new TerrainSize();
        public TerrainPivot Pivot = new TerrainPivot();
        public string CellMap;
        public string TileMap;
        public List<string> TileList = new List<string>();

        public RecordCubeTerrain()
        {
        }

        public RecordCubeTerrain(TerrainSize size, TerrainPivot pivot, string cellMap, string tileMap, List<string> tileList)
        {
            Size = size;
            Pivot = pivot;
            CellMap = cellMap;
            TileMap = tileMap;
            TileList = tileList;
        }

        public RecordCubeTerrain(RecordCubeTerrain recordWorldTerrain)
        {
            Size = recordWorldTerrain.Size;
            Pivot = recordWorldTerrain.Pivot;
            CellMap = recordWorldTerrain.CellMap;
            TileMap = recordWorldTerrain.TileMap;
            TileList = recordWorldTerrain.TileList;
        }

        public void ClearTerrainInfo()
        {
            Size.width = 0;
            Size.height = 0;
            Pivot.x = 0;
            Pivot.y = 0;
            CellMap = string.Empty;
            TileMap = string.Empty;

            if (TileList.Count > 0)
            {
                TileList.Clear();
            }
        }
    }

    public enum eTerrainToolMode
    {
        None,
        Panning,
        IncLand,
        DecLand,
        MakeWater,
        Paint,
    }

    public enum eFunctionKey
    {
        None,
        Shift,
        CtrlShift,
        Ctrl,
        ShiftCtrl,
    }

    public enum eSizeDirection
    {
        Up,
        Right,
        Down,
        Left,
    }

    public struct PickTerrainInfo
    {
        public byte floor;
        public byte tx;
        public byte ty;
        public float posY;
    }

    public partial class BuilderItemModifier_CubeTerrain
    {
        private RecordCubeTerrain _prevWorldTerrain = new RecordCubeTerrain();

        CubeTerrain _terrain;

        public void SetTerrain(CubeTerrain terrain)
        {
            _terrain = terrain;
        }

        private void _RecordPrevWorldTerrain()
        {
            _prevWorldTerrain.Size.width = _terrain.GetWidth();
            _prevWorldTerrain.Size.height = _terrain.GetHeight();
            _prevWorldTerrain.Pivot.x = _terrain.GetPivotX();
            _prevWorldTerrain.Pivot.y = _terrain.GetPivotY();
            _prevWorldTerrain.CellMap = _terrain.GetTerrainCellMap();
            _prevWorldTerrain.TileMap = _terrain.GetTerrainTileMap();
            _prevWorldTerrain.TileList = _terrain.GetTerrainTileList();
        }

        #region ChangeSize
        private int[] _sizeDirectionCriterion = new int[] { 0, 0, 0, 0 };   //상 우 하 좌
        private int[] _sizeDirectionVariance = new int[] { 0, 0, 0, 0 };    //상 우 하 좌

        private bool _changeSize;

        private void _InitSizeDirectionVariance()
        {
            int width = _terrain.GetWidth();
            int height = _terrain.GetHeight();

            int downVariance = _terrain.GetPivotY();

            int upVariance = (height - (CubeTerrain.DEFAULT_HEIGHT)) / CubeTerrain.GROUP_UNIT_Y - downVariance;
            if (upVariance <= 0)
                upVariance = 0;

            int leftVariance = _terrain.GetPivotX();

            int rightVariance = (width - (CubeTerrain.DEFAULT_WIDTH)) / CubeTerrain.GROUP_UNIT_X - leftVariance;
            if (rightVariance <= 0)
                rightVariance = 0;

            _sizeDirectionCriterion[(int)eSizeDirection.Up] = _sizeDirectionVariance[(int)eSizeDirection.Up] = upVariance;
            _sizeDirectionCriterion[(int)eSizeDirection.Down] = _sizeDirectionVariance[(int)eSizeDirection.Down] = downVariance;
            _sizeDirectionCriterion[(int)eSizeDirection.Left] = _sizeDirectionVariance[(int)eSizeDirection.Left] = leftVariance;
            _sizeDirectionCriterion[(int)eSizeDirection.Right] = _sizeDirectionVariance[(int)eSizeDirection.Right] = rightVariance;
        }

        //private void _UpdateTerrainSize(eUpdateSizeDirection clickDir)
        //{
        //    //CLogger.Log($"clickDir : {clickDir}");
        //    int updateDir = Mathf.FloorToInt((int)clickDir * 0.5f);
        //    if (0 == (int)clickDir % 2)
        //        --_sizeDirectionVariance[updateDir];
        //    else
        //        ++_sizeDirectionVariance[updateDir];

        //    int width = CubeTerrain.DEFAULT_WIDTH + CubeTerrain.GROUP_UNIT_X * (_sizeDirectionVariance[(int)eSizeDirection.Left] + _sizeDirectionVariance[(int)eSizeDirection.Right]);
        //    int height = CubeTerrain.DEFAULT_HEIGHT + CubeTerrain.GROUP_UNIT_Y * (_sizeDirectionVariance[(int)eSizeDirection.Up] + _sizeDirectionVariance[(int)eSizeDirection.Down]);
        //    _terrain.SetTerrainSize(width, height);
        //    _terrain.SetTerrainPivot(_sizeDirectionVariance[(int)eSizeDirection.Left], _sizeDirectionVariance[(int)eSizeDirection.Down]);

        //    string cellMap = _ModifyTerrainMap(width, height, _prevWorldTerrain.CellMap);
        //    _terrain.SetTerrainCellMap(cellMap);

        //    string tileMap = _ModifyTerrainMap(width, height, _prevWorldTerrain.TileMap);
        //    _terrain.SetTerrainTileMap(tileMap);

        //    _terrain.SetTerrainTileList(_prevWorldTerrain.TileList);
        //}

        private string _ModifyTerrainMap(int width, int height, string map)
        {
            char[] maps = map.ToCharArray();
            int deltaPivotX = (_sizeDirectionVariance[(int)eSizeDirection.Left] - _sizeDirectionCriterion[(int)eSizeDirection.Left]) * CubeTerrain.GROUP_UNIT_X;
            int deltaPivotY = (_sizeDirectionVariance[(int)eSizeDirection.Down] - _sizeDirectionCriterion[(int)eSizeDirection.Down]) * CubeTerrain.GROUP_UNIT_Y;
            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if ((deltaPivotY <= y && y < _prevWorldTerrain.Size.height + deltaPivotY)
                        && (deltaPivotX <= x && x < _prevWorldTerrain.Size.width + deltaPivotX))
                        sb.Append(maps[(x - deltaPivotX) + (y - deltaPivotY) * _prevWorldTerrain.Size.width]);
                    else
                        sb.Append('B');
                }
            }
            return sb.ToString();
        }
        #endregion

        #region ChangeCellOrTile
        private PickTerrainInfo _firstPick = new PickTerrainInfo();
        private PickTerrainInfo _lastPick = new PickTerrainInfo();

        private byte _terrainX = CubeTerrain.MAX_TERRAIN_SIZE;
        private byte _terrainY = CubeTerrain.MAX_TERRAIN_SIZE;

        private float _criterionPosY = -1f;

        private bool _changeCellOrTile;

        private bool _PickSameTerrain(byte tx, byte ty)
        {
            return _terrainX == tx && _terrainY == ty;
        }

        private void _RecordPreviousPickTerrain(byte tx, byte ty)
        {
            _terrainX = tx;
            _terrainY = ty;
        }

        private void _InitPickTerrainInfo()
        {
            _firstPick.posY = -1f;
            _firstPick.floor = (byte)0;
            _firstPick.tx = CubeTerrain.MAX_TERRAIN_SIZE;
            _firstPick.ty = CubeTerrain.MAX_TERRAIN_SIZE;

            _lastPick = _firstPick;
        }

        private void _RecordPickTerrainInfo(byte floor, byte tx, byte ty, float posY)
        {
            _lastPick.floor = floor;
            _lastPick.tx = tx;
            _lastPick.ty = ty;
            _lastPick.posY = posY;

            _UpdateFirstPickTerrainInfo();
        }

        private void _UpdateFirstPickTerrainInfo()
        {
            if (CubeTerrain.MAX_TERRAIN_SIZE == _firstPick.tx
                && CubeTerrain.MAX_TERRAIN_SIZE == _firstPick.ty)
                _firstPick = _lastPick;
        }

        private bool _ClickTerrain()
        {
            return (CubeTerrain.MAX_TERRAIN_SIZE != _firstPick.tx && CubeTerrain.MAX_TERRAIN_SIZE != _firstPick.ty);
        }

        private void _InitCriterionPosY()
        {
            if (-1f != _criterionPosY)
                _criterionPosY = -1f;
        }

        public void _UpdateTerrain(eFunctionKey funcKey, eTerrainToolMode toolMode)
        {
            switch (funcKey)
            {
                case eFunctionKey.None:
                    _UpdateCell(toolMode);
                    break;
                case eFunctionKey.Ctrl:
                case eFunctionKey.ShiftCtrl:
                    _UpdateCell_Range(funcKey, toolMode);
                    break;
                case eFunctionKey.Shift:
                case eFunctionKey.CtrlShift:
                    _UpdateCell_Y_Axis(funcKey, toolMode);
                    break;
            }
        }

        private void _UpdateCell(eTerrainToolMode toolMode)
        {
            byte floor;
            if (eTerrainToolMode.IncLand == toolMode)
            {
                if (_lastPick.floor < 9 && _lastPick.floor < _firstPick.floor + 1)
                {
                    floor = (_firstPick.floor < 9) ? (byte)(_firstPick.floor + 1) : (byte)9;
                    _IncreaseLand(_lastPick.tx, _lastPick.ty, _lastPick.tx, _lastPick.ty, floor);
                }
            }
            else if (eTerrainToolMode.DecLand == toolMode)
            {
                if (_lastPick.floor > 0 && _lastPick.floor > _firstPick.floor - 1)
                {
                    floor = (_firstPick.floor > 0) ? (byte)(_firstPick.floor - 1) : (byte)0;
                    _DecreaseLand(_lastPick.tx, _lastPick.ty, _lastPick.tx, _lastPick.ty, floor);
                }
            }
            //TODO : 재질 및 형태 변경 향후 수정
            else if (eTerrainToolMode.MakeWater == toolMode)
                _MakeWater(_lastPick.tx, _lastPick.ty);
            else if (eTerrainToolMode.Paint == toolMode)
                _Paint(_lastPick.tx, _lastPick.ty);
        }

        private void _UpdateCell_Range(eFunctionKey funcKey, eTerrainToolMode toolMode)
        {
            byte firstPickFloor = CubeTerrain.BASE_FLOOR;
            if (eFunctionKey.Ctrl == funcKey)
            {
                if (eTerrainToolMode.IncLand == toolMode)
                    firstPickFloor = (_firstPick.floor < 9) ? (byte)(_firstPick.floor + 1) : (byte)9;
                else if (eTerrainToolMode.DecLand == toolMode)
                    firstPickFloor = (_firstPick.floor > 0) ? (byte)(_firstPick.floor - 1) : CubeTerrain.BASE_FLOOR;
            }
            else if (eFunctionKey.ShiftCtrl == funcKey)
                firstPickFloor = _terrain.GetFloor(_firstPick.tx, _firstPick.ty);

            _RevertCell();

            byte fromTx = (byte)Mathf.Min(_firstPick.tx, _lastPick.tx);
            byte fromTy = (byte)Mathf.Min(_firstPick.ty, _lastPick.ty);
            byte toTx = (byte)Mathf.Max(_firstPick.tx, _lastPick.tx);
            byte toTy = (byte)Mathf.Max(_firstPick.ty, _lastPick.ty);
            if (eTerrainToolMode.IncLand == toolMode)
                _IncreaseLand(fromTx, fromTy, toTx, toTy, firstPickFloor);
            else if (eTerrainToolMode.DecLand == toolMode)
                _DecreaseLand(fromTx, fromTy, toTx, toTy, firstPickFloor);
        }

        private const float DISTANCE_Y_AXIS = 20f;
        private void _UpdateCell_Y_Axis(eFunctionKey funcKey, eTerrainToolMode toolMode)
        {
            IBuilderInput currentInput = null;
            float mousePosY = 0f;
            if (null != currentInput && currentInput.GetPointer(0))
                mousePosY = currentInput.GetPointerXY(0).y;

            byte floor = _terrain.GetFloor(_firstPick.tx, _firstPick.ty);
            byte fromTx = CubeTerrain.MAX_TERRAIN_SIZE;
            byte fromTy = CubeTerrain.MAX_TERRAIN_SIZE;
            byte toTx = CubeTerrain.MAX_TERRAIN_SIZE;
            byte toTy = CubeTerrain.MAX_TERRAIN_SIZE;
            float toPosY = -1f;
            if (eFunctionKey.Shift == funcKey)
            {
                fromTx = _firstPick.tx;
                fromTy = _firstPick.ty;
                toTx = _firstPick.tx;
                toTy = _firstPick.ty;
                toPosY = _firstPick.posY;
            }
            else if (eFunctionKey.CtrlShift == funcKey)
            {
                fromTx = (byte)Mathf.Min(_firstPick.tx, _lastPick.tx);
                fromTy = (byte)Mathf.Min(_firstPick.ty, _lastPick.ty);
                toTx = (byte)Mathf.Max(_firstPick.tx, _lastPick.tx);
                toTy = (byte)Mathf.Max(_firstPick.ty, _lastPick.ty);
                toPosY = _lastPick.posY;
            }

            if (CubeTerrain.MAX_TERRAIN_SIZE == fromTx || CubeTerrain.MAX_TERRAIN_SIZE == fromTy
                || CubeTerrain.MAX_TERRAIN_SIZE == toTx || CubeTerrain.MAX_TERRAIN_SIZE == toTy
                || -1f == toPosY)
                return;

            float distance;
            byte deltaFloor = 0;
            if (eTerrainToolMode.IncLand == toolMode)
            {
                if (-1f == _criterionPosY)
                {
                    _RevertCell();
                    distance = mousePosY - toPosY;
                    if (distance > 0)
                    {
                        deltaFloor = (byte)(Mathf.FloorToInt((distance) / DISTANCE_Y_AXIS));
                        if (deltaFloor < 9)
                            floor += deltaFloor;
                        else
                            floor = 9;
                    }

                    if (floor < 9)
                        _IncreaseLand(fromTx, fromTy, toTx, toTy, floor);
                    else
                        _IncreaseLand(fromTx, fromTy, toTx, toTy, (byte)9);
                    _criterionPosY = mousePosY;
                }
                else
                {
                    if (mousePosY - _criterionPosY > DISTANCE_Y_AXIS)
                    {
                        if (floor < 9)
                            _IncreaseLand(fromTx, fromTy, toTx, toTy, (byte)(floor + 1));
                        _criterionPosY = mousePosY;
                    }
                }
            }
            else if (eTerrainToolMode.DecLand == toolMode)
            {
                if (-1 == _criterionPosY)
                {
                    _RevertCell();
                    distance = toPosY - mousePosY;
                    if (distance > 0)
                    {
                        deltaFloor = (byte)(Mathf.FloorToInt((distance) / DISTANCE_Y_AXIS));
                        if (deltaFloor < 9)
                            floor -= deltaFloor;
                        else
                            floor = 0;
                    }

                    if (floor > 0)
                        _DecreaseLand(fromTx, fromTy, toTx, toTy, floor);
                    else
                        _DecreaseLand(fromTx, fromTy, toTx, toTy, CubeTerrain.BASE_FLOOR);
                    _criterionPosY = mousePosY;
                }
                else
                {
                    if (_criterionPosY - mousePosY > DISTANCE_Y_AXIS)
                    {
                        if (floor > 0)
                            _DecreaseLand(fromTx, fromTy, toTx, toTy, (byte)(floor - 1));
                        _criterionPosY = mousePosY;
                    }
                }
            }
        }

        private void _IncreaseLand(byte fromTx, byte fromTy, byte toTx, byte toTy, byte floor)
        {
            if (null != _terrain)
                _changeCellOrTile = _terrain.ModifyCell(CubeTerrain.CELL_LAND, fromTx, fromTy, toTx, toTy, floor);
        }

        private void _DecreaseLand(byte fromTx, byte fromTy, byte toTx, byte toTy, byte floor)
        {
            if (null != _terrain)
                _changeCellOrTile = _terrain.ModifyCell(CubeTerrain.CELL_NONE, fromTx, fromTy, toTx, toTy, floor);
        }

        private void _MakeWater(byte tx, byte ty)
        {
            if (null != _terrain)
                _changeCellOrTile = _terrain.ModifyCell(CubeTerrain.CELL_WATER, tx, ty);
        }

        private void _Paint(byte tx, byte ty)
        {
            //if (_currentPaintTileIndex < 0)
            //    return;

            //if (null != _terrain)
            //    _changeCellOrTile = _terrain.ModifyTile(tx, ty, (byte)_currentPaintTileIndex);
        }

        private void _Paint(byte tx, byte ty, int paintTileIndex)
        {
            if (paintTileIndex < 0)
                return;

            if (null != _terrain)
                _terrain.ModifyTile(tx, ty, (byte)paintTileIndex);
        }

        private void _RevertCell()
        {
            _terrain.RevertCell(_prevWorldTerrain.CellMap);
            _changeCellOrTile = false;
        }
        #endregion

        #region RecordTerrain
        private void _RecordWorldTerrain()
        {
            TerrainSize size = new TerrainSize(_terrain.GetWidth(), _terrain.GetHeight());
            TerrainPivot pivot = new TerrainPivot(_terrain.GetPivotX(), _terrain.GetPivotY());
            _RecordWorldTerrainState(size, pivot, _terrain.GetTerrainCellMap(), _terrain.GetTerrainTileMap(), _terrain.GetTerrainTileList(), _prevWorldTerrain);
        }

        private void _RecordWorldTerrainState(TerrainSize size, TerrainPivot pivot, string cellMap, string tileMap, List<string> tileList, RecordCubeTerrain prevWorldTerrain)
        {
        }
        #endregion
    }
}
