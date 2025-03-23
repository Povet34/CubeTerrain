using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class CubeTerrain : WorldTerrain
{
    //public const int MAX_FENCE              = 50;
    //public const byte FENCE_NONE            = (byte)255;

    //public readonly Vector3 X_SHAPE_ROT     = new Vector3(-90, 0, 0);
    //public readonly Vector3 T_SHAPE_ROT_TBL = new Vector3(-90, 0, 0);           // ㅓ
    //public readonly Vector3 T_SHAPE_ROT_TBR = new Vector3(-90, 180, 0);         // ㅏ
    //public readonly Vector3 T_SHAPE_ROT_TLR = new Vector3(-90, 90, 0);          // ㅗ
    //public readonly Vector3 T_SHAPE_ROT_BLR = new Vector3(-90, 270, 0);         // ㅜ
    //public readonly Vector3 L_SHAPE_ROT_TL  = new Vector3(-90, 90, 0);          // ┘
    //public readonly Vector3 L_SHAPE_ROT_TR  = new Vector3(-90, 180, 0);         //└
    //public readonly Vector3 L_SHAPE_ROT_BL  = new Vector3(-90, 0, 0);           // ┐
    //public readonly Vector3 L_SHAPE_ROT_BR  = new Vector3(-90, 270, 0);         //┌
    //public readonly Vector3 STRAIGHT_ROT_TB  = new Vector3(-90, 90, 0);         // ㅣ
    //public readonly Vector3 STRAIGHT_ROT_LR = new Vector3(-90, 0, 0);           // ㅡ

    //private byte[] fenceArr;
    //private List<FenceInfo> fenceInfos;
    //private Dictionary<int, ResourceLoader.ResourceObject> fenceObjMap = new Dictionary<int, ResourceLoader.ResourceObject>();

    //private bool isChangedFence = false;

    //public class FenceInfo
    //{
    //    public long fenceItemID;

    //    public FenceInfo(long fenceItemID)
    //    {
    //        this.fenceItemID = fenceItemID;
    //    }
    //}

    //public bool IsChangedFence()
    //{
    //    return isChangedFence;
    //}

    //public void CreateFences(int width, int height, List<FenceInfo> fenceInfos = null, string fenceMap = null)
    //{
    //    isChangedFence = false;
    //    fenceArr = new byte[width * height];

    //    bool fenceOk = false;
    //    if (!string.IsNullOrEmpty(fenceMap))
    //    {
    //        char[] arr = fenceMap.ToCharArray();
    //        if (arr.Length == (width * height))
    //        {
    //            fenceOk = true;
    //            for (int i = 0; i < arr.Length; i++)
    //                fenceArr[i] = arr[i] == '0' ? FENCE_NONE : (byte)((int)arr[i] - (int)'A');
    //        }
    //    }

    //    if (!fenceOk)
    //    {
    //        for (int i = 0; i < width * height; i++)
    //            fenceArr[i] = FENCE_NONE;
    //    }

    //    this.fenceInfos = fenceInfos;
    //    if (this.fenceInfos == null)
    //        this.fenceInfos = new List<FenceInfo>();

    //    for (int i = 0; i < fenceArr.Length; i++)
    //    {
    //        if (fenceArr[i] != FENCE_NONE)
    //        {
    //            if (null != this.fenceInfos[fenceArr[i]])
    //            {
    //                long itemID = this.fenceInfos[fenceArr[i]].fenceItemID;
    //                GameDB.ItemInfo itemInfo = GameDB.Instance.GetItemInfo(itemID);
    //                if (null != itemInfo)
    //                {
    //                    byte x = (byte)(i % terrainWidth);
    //                    byte y = (byte)(i == 0 ? 0 : ((i % terrainHeight) == 0) ? (i / terrainWidth) - 1 : (i / terrainWidth));
    //                    InstallFence(itemInfo, x, y, fenceArr[i], false);
    //                }
    //            }
    //        }
    //    }

    //    FullSurveyFences();
    //}
    //public bool InstallFence(GameDB.ItemInfo itemInfo, byte x, byte y, byte fenceType, bool isSurveyFence = true)
    //{
    //    int index = x + y * terrainWidth;
    //    if (null != fenceArr && index >= 0 && index < fenceArr.Length)
    //    {
    //        byte _cell = GetOnlyCellIndex(cellArr[index]);
    //        byte floor = GetFloor(_cell);
    //        byte cellType = GetBaseCellOnFloor(floor, _cell, x, y);

    //        if (cellType == CELL_WATER)
    //        {
    //            // to do oink8407s : 울타리 설치 불가 사운드?
    //            return false;
    //        }

    //        fenceArr[index] = fenceType;
    //        PlaceFence(itemInfo, x, y, isSurveyFence);
    //        isChangedFence = true;
    //        return true;
    //    }
    //    else
    //    {
    //        CLogger.LogWarning("InstallFence - index range error.");
    //        return false;
    //    }
    //}
    //public void PlaceFence(GameDB.ItemInfo itemInfo, byte x, byte y, bool isSurveyFence = true)
    //{
    //    int index = x + y * terrainWidth;
    //    if (null != fenceObjMap && !fenceObjMap.ContainsKey(index))
    //    {
    //        ResourceLoader.ResourceObject resObj = ResourceLoader.Instance.GetInstance(itemInfo.GetResource());
    //        fenceObjMap.Add(index, resObj);

    //        if (resObj.IsLoaded())
    //            LoadedFence(resObj, x, y, isSurveyFence);
    //        else
    //            resObj.SetCallback((res) =>
    //            {
    //                LoadedFence(resObj, x, y, isSurveyFence);
    //            });
    //    }
    //}
    //public bool RemoveFence(byte x, byte y, bool isSurveyFence = true)
    //{
    //    int index = x + y * terrainWidth;
    //    if (null != fenceArr && index >= 0 && index < fenceArr.Length)
    //    {
    //        if (fenceArr[index] != FENCE_NONE)
    //        {
    //            if (null != fenceInfos && null != fenceInfos[fenceArr[index]])
    //            {
    //                fenceArr[index] = FENCE_NONE;
    //                if (fenceObjMap.ContainsKey(index))
    //                {
    //                    fenceObjMap[index].Return();
    //                    fenceObjMap.Remove(index);
    //                }

    //                if (isSurveyFence)
    //                    NeighborSurveyFences(index);

    //                return true;
    //            }
    //        }
    //    }
    //    else
    //    {
    //        CLogger.LogWarning("RemoveFence - index range error.");
    //        return false;
    //    }

    //    return false;
    //}
    //public void SaveFences(NetworkModule.SpaceMapInfo spaceMapInfo)
    //{
    //    if (null == fenceArr)
    //        return;

    //    int totalLength = terrainWidth * terrainHeight;
    //    System.Text.StringBuilder map = new System.Text.StringBuilder(totalLength);
    //    for (int i = 0; i < totalLength; i++)
    //    {
    //        char str = fenceArr[i] == FENCE_NONE ? '0' : (char)(fenceArr[i] + 'A');
    //        map.Append(str);
    //    }

    //    string fenceString = map.ToString();
    //    spaceMapInfo.FenceMap = fenceString;
    //    spaceMapInfo.FenceIDs = new List<long>();
    //    if (null != fenceInfos)
    //    {
    //        for (int i = 0; i < fenceInfos.Count; i++)
    //            spaceMapInfo.FenceIDs.Add(fenceInfos[i].fenceItemID);
    //    }
    //    isChangedFence = false;
    //}
    //public void ReleaseFences()
    //{
    //    foreach (KeyValuePair<int, ResourceLoader.ResourceObject> fence in fenceObjMap)
    //        fence.Value.Return();

    //    fenceObjMap.Clear();
    //}
    //public void ChangeFenceState(byte x, byte y)
    //{
    //    if (null == fenceArr)
    //        return;

    //    int index = x + y * terrainWidth;
    //    byte curType = GetOnlyCellIndex(cellArr[index]);

    //    // 물로 바꾸면, 위에 있던 fence를 지운다.
    //    if (curType == CELL_WATER || curType == CELL_WATER2 || curType == CELL_WATER3)
    //    {
    //        if (fenceArr[index] != FENCE_NONE)
    //            RemoveFence(x, y);
    //    }
    //    else
    //    {
    //        // Cell 층이 바뀌면 그에 따른 위치 값을 변경해준다.
    //        if (fenceArr[index] != FENCE_NONE)
    //        {
    //            Vector3 worldPos = GetSelectedCellCenterPos(x, y);
    //            Vector3 localPos = transform.InverseTransformPoint(worldPos);
    //            if (null != fenceObjMap && fenceObjMap.ContainsKey(index))
    //            {
    //                ResourceLoader.ResourceObject resObj = fenceObjMap[index];
    //                resObj.obj.transform.localPosition = localPos;
    //            }
    //        }
    //    }
    //}
    //public int AddFenceInfo(FenceInfo fence)
    //{
    //    for (int i = 0; i < fenceInfos.Count; i++)
    //    {
    //        if (null != fenceInfos[i] && fenceInfos[i].fenceItemID == fence.fenceItemID)
    //            return i;
    //    }

    //    if (fenceInfos.Count >= MAX_FENCE)
    //    {
    //        CLogger.LogWarning("AddFenceInfo - over max.");
    //        return 0;
    //    }

    //    fenceInfos.Add(fence);
    //    return fenceInfos.Count - 1;
    //}

    //public long GetFenceID(byte x, byte y)
    //{
    //    if (null == fenceArr)
    //        return 0;

    //    long result = 0;
    //    int index = x + y * terrainWidth;
    //    if (index >= 0 && index < fenceArr.Length)
    //    {
    //        if (fenceArr[index] != FENCE_NONE)
    //        {
    //            if (null != fenceInfos && null != fenceInfos[fenceArr[index]])
    //                result = this.fenceInfos[fenceArr[index]].fenceItemID;
    //        }
    //    }
        
    //    return result;
    //}
    //public byte GetFenceType(byte x, byte y)
    //{
    //    if (null == fenceArr)
    //        return FENCE_NONE;

    //    int index = x + y * terrainWidth;
    //    if (index >= 0 && index < fenceArr.Length)
    //    {
    //        if (fenceArr[index] != FENCE_NONE)
    //        {
    //            if (null != fenceInfos && null != fenceInfos[fenceArr[index]])
    //                return fenceArr[index];
    //        }
    //    }
    //    return FENCE_NONE;
    //}

    //private void LoadedFence(ResourceLoader.ResourceObject resObj, byte x, byte y, bool isSurveyFence)
    //{
    //    Vector3 worldPos = GetSelectedCellCenterPos(x, y);
    //    Vector3 localPos = transform.InverseTransformPoint(worldPos);
    //    Transform tr = resObj.obj.transform;
    //    tr.SetParent(transform);
    //    tr.localPosition = localPos;
    //    tr.localScale = Vector3.one;
    //    tr.localEulerAngles = Vector3.one;

    //    if (isSurveyFence)
    //    {
    //        int index = x + y * terrainWidth;
    //        SurveyFence(index);
    //        NeighborSurveyFences(index);
    //    }
    //}
    //private void ConversionFence(int index, Definitions.Fence fenceType, Vector3 fenceRotation, bool isIsolatedFence)
    //{
    //    if (null != fenceObjMap && fenceObjMap.ContainsKey(index))
    //    {
    //        ItemFence item = fenceObjMap[index].obj.GetComponent<ItemFence>();
    //        if (null != item)
    //            item.CreateFence(fenceType, fenceRotation, isIsolatedFence);
    //    }
    //}
    //private void FullSurveyFences()
    //{
    //    if (null != fenceArr)
    //    {
    //        for (int i = 0; i < fenceArr.Length; i++)
    //            SurveyFence(i);
    //    }
    //}
    //private void NeighborSurveyFences(int index)
    //{
    //    if (null != fenceArr && 0 <= index && index < fenceArr.Length)
    //    {
    //        int topIdx = index - (terrainWidth * BLOCK);
    //        int bottomIdx = index + (terrainWidth * BLOCK);
    //        int leftIdx = index + BLOCK;
    //        int rightIdx = index - BLOCK;
    //        SurveyFence(topIdx);
    //        SurveyFence(bottomIdx);
    //        SurveyFence(leftIdx);
    //        SurveyFence(rightIdx);
    //    }
    //}
    //private void SurveyFence(int index)
    //{
    //    if (null != fenceArr && 0 <= index && index < fenceArr.Length)
    //    {
    //        if (fenceArr[index] != FENCE_NONE)
    //        {
    //            bool isTopExist = false;
    //            int topIdx = index - (terrainWidth * BLOCK);
    //            if (0 <= topIdx)
    //                isTopExist = fenceArr[topIdx] != FENCE_NONE ? true : false;

    //            bool isBottomExist = false;
    //            int bottomIdx = index + (terrainWidth * BLOCK);
    //            if (bottomIdx < fenceArr.Length)
    //                isBottomExist = fenceArr[bottomIdx] != FENCE_NONE ? true : false;

    //            bool isLeftExist = false;
    //            int leftIdx = index + BLOCK;
    //            if (leftIdx < fenceArr.Length)
    //                isLeftExist = fenceArr[leftIdx] != FENCE_NONE ? true : false;

    //            bool isRightExist = false;
    //            int rightIdx = index - BLOCK;
    //            if (0 <= rightIdx)
    //                isRightExist = fenceArr[rightIdx] != FENCE_NONE ? true : false;

    //            Definitions.Fence fenceType;
    //            Vector3 fenceRotation;
    //            bool isIsolatedFence;

    //            GetFenceData(out fenceType, out fenceRotation, out isIsolatedFence, isTopExist, isBottomExist, isLeftExist, isRightExist);
    //            ConversionFence(index, fenceType, fenceRotation, isIsolatedFence);
    //        }
    //    }
    //}

    //private void GetFenceData(out Definitions.Fence fenceType, out Vector3 fenceRotation, out bool isIsolatedFence,
    //    bool isTopExist, bool isBottomExist, bool isLeftExist, bool isRightExist)
    //{
    //    fenceType = Definitions.Fence.NONE;
    //    fenceRotation = Vector3.zero;
    //    isIsolatedFence = false;

    //    if (IsXShape(isTopExist, isBottomExist, isLeftExist, isRightExist))
    //    {
    //        fenceType = Definitions.Fence.X_SHAPE;
    //        fenceRotation = X_SHAPE_ROT;
    //    }
    //    else if (IsTShape(isTopExist, isBottomExist, isLeftExist, isRightExist))
    //    {
    //        fenceType = Definitions.Fence.T_SHAPE;

    //        if (!isTopExist) 
    //            fenceRotation = T_SHAPE_ROT_BLR;

    //        if (!isBottomExist)
    //            fenceRotation = T_SHAPE_ROT_TLR;

    //        if (!isRightExist) 
    //            fenceRotation = T_SHAPE_ROT_TBL;

    //        if (!isLeftExist) 
    //            fenceRotation = T_SHAPE_ROT_TBR;
    //    }
    //    else if (IsLShape(isTopExist, isBottomExist, isLeftExist, isRightExist))
    //    {
    //        fenceType = Definitions.Fence.L_SHAPE;

    //        if (!isBottomExist && !isRightExist)
    //            fenceRotation = L_SHAPE_ROT_TL;

    //        if (!isBottomExist && !isLeftExist)
    //            fenceRotation = L_SHAPE_ROT_TR;

    //        if (!isTopExist && !isRightExist)
    //            fenceRotation = L_SHAPE_ROT_BL;

    //        if (!isTopExist && !isLeftExist)
    //            fenceRotation = L_SHAPE_ROT_BR;
    //    }
    //    else 
    //    {
    //        fenceType = Definitions.Fence.STRAIGHT;

    //        if (isTopExist || isBottomExist)
    //            fenceRotation = STRAIGHT_ROT_TB;
    //        else
    //            fenceRotation = STRAIGHT_ROT_LR;

    //        if (!isTopExist && !isBottomExist && !isLeftExist && !isRightExist)
    //            isIsolatedFence = true;
    //    }
    //}
    //private bool IsXShape(bool isTopExist, bool isBottomExist, bool isLeftExist, bool isRightExist)
    //{
    //    return isTopExist && isBottomExist && isLeftExist && isRightExist;
    //}
    //private bool IsTShape(bool isTopExist, bool isBottomExist, bool isLeftExist, bool isRightExist)
    //{
    //    if (isTopExist && isBottomExist && isLeftExist && !isRightExist)
    //        return true;

    //    if (isTopExist && isBottomExist && isRightExist && !isLeftExist)
    //        return true;

    //    if (isLeftExist && isRightExist && isTopExist && !isBottomExist)
    //        return true;

    //    if (isLeftExist && isRightExist && isBottomExist && !isTopExist)
    //        return true;

    //    return false;
    //}
    //private bool IsLShape(bool isTopExist, bool isBottomExist, bool isLeftExist, bool isRightExist)
    //{
    //    if (isTopExist && isLeftExist && !isBottomExist && !isRightExist)
    //        return true;

    //    if (isTopExist && isRightExist && !isBottomExist && !isLeftExist)
    //        return true;

    //    if (isBottomExist && isLeftExist && !isTopExist && !isRightExist)
    //        return true;

    //    if (isBottomExist && isRightExist && !isTopExist && !isLeftExist)
    //        return true;

    //    return false;
    //}
}