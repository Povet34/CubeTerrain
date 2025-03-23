using Colorverse;
using Colorverse.AssetSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PlaneSize = Colorverse.ItemSystem.CvClass_PlaneTerrain.PlaneTerrainInfo.PlaneSize;

public class PlaneTerrain : WorldTerrain
{
    #region PlaneTerrainParam
    private class PlaneTerrainParam : TerrainParam
    {
        public TileInfo tileMaterial;

        public bool IsTileLoaded { get { return null == tileMaterial || tileLoaded; } }

        public void LoadTile(string tileItemId, PlaneTerrain owner)
        {
            Reset();
            tileMaterial = null;

            if (string.IsNullOrEmpty(tileItemId) || tileItemId.Equals("null"))
            {
                Material defaultMat = AssetUtil.LoadResource<Material>(AssetUtil.LOADING_MATERIAL);
                tileMaterial = new TileInfo(tileItemId, defaultMat);
            }
            else
            {
                tileMaterial = new TileInfo();
                owner._GetHelper(tileItemId).LoadMaterialItem(tileItemId, owner._Callback_Loaded_Material);
            }
        }

        public void SetTileMaterial(string tileItemId, Material material)
        {
            if (string.IsNullOrEmpty(tileItemId) || null == material)
                tileLoaded = false;
            else
            {
                tileMaterial.tileItemID = tileItemId;
                tileMaterial.material = material;
                tileLoaded = true;
            }
        }
    }

    private AssetLoadHelper _helper;

    private AssetLoadHelper _GetHelper(string tileId)
    {
        if (null == _helper)
            _helper = new AssetLoadHelper(new AssetBasisInfo(tileId, tileId, eAssetType.MaterialItem));
        return _helper;
    }

    private void _Callback_Loaded_Material(AssetRef assetRef, AssetLoadParam param, eAssetState state)
    {
        Debug.Assert(assetRef.isLoaded);
        Debug.Assert(assetRef is AssetRef_Material);
        if (null != _param)
            _param.SetTileMaterial(assetRef.itemId, assetRef.getAsset_Material);
    }
    #endregion

    private GameObject _basePlane;
    private MeshRenderer _basePlaneMeshRenderer;

    private TileInfo _tileMaterial;
    private PlaneTerrainParam _param;

    private Coroutine _reservedCreationCoroutine = null;

    private const float RESIZE_VALUE = 0.2f;

    public override Bounds GetTerrainBounds()
    {
        var meshRenderer = _basePlane.GetComponent<MeshRenderer>();
        return meshRenderer.bounds;
    }

    public int GetWidth()
    {
        return terrainWidth;
    }

    public int GetHeight()
    {
        return terrainHeight;
    }

    public string GetTileItemId()
    {
        return _tileMaterial.tileItemID;
    }

    public void SetTerrainSize(int width, int height, Action onComplete = null)
    {
        _DefaultParamSetting(onComplete);
        _param.width = width;
        _param.height = height;
    }

    public void SetTerrainTile(string tileItemId, Action onComplete = null)
    {
        _DefaultParamSetting(onComplete);
        _param.LoadTile(tileItemId, this);
    }

    public void CreateTerrainImmediately(Action onComplete)
    {
        if (null != _reservedCreationCoroutine)
        {
            StopCoroutine(_reservedCreationCoroutine);
            _reservedCreationCoroutine = null;
        }
        _reservedCreationCoroutine = StartCoroutine(_CreateTerrain(onComplete, false));
    }

    private void _DefaultParamSetting(Action onComplete)
    {
        if (null == _param)
        {
            _param = new PlaneTerrainParam();
            _param.width = terrainWidth;
            _param.height = terrainHeight;
        }
        if (null == _reservedCreationCoroutine && gameObject.activeSelf)
            _reservedCreationCoroutine = StartCoroutine(_CreateTerrain(onComplete));
    }

    private IEnumerator _CreateTerrain(Action onComplete, bool delayCall = true)
    {
        if (delayCall)
            yield return null;

        if (null != _param)
        {
            if (_param.IsTileLoaded)
            {
                _CreateTerrain(_param.width, _param.height, _param.tileMaterial);
                _param = null;
                _reservedCreationCoroutine = null;
                onComplete?.Invoke();
                yield break;
            }
            else
                _CreateTerrain(_param.width, _param.height);
        }

        do
        {
            yield return null;
        }
        while (null != _param && false == _param.IsTileLoaded);

        _CreateTerrain(_param.width, _param.height, _param.tileMaterial);
        _param = null;
        _reservedCreationCoroutine = null;
        onComplete?.Invoke();
    }

    private void _CreateTerrain(int width, int height, TileInfo tileMaterial = null)
    {
        _CreateBasePlane();

        if (null == _basePlane)
            return;

        terrainWidth = width;
        terrainHeight = height;
        CLogger.Log($"PlanTerrain::_CreateTerrain - width : {terrainWidth}, height : {terrainHeight}");

        _SetTerrainSize();

        if (null == tileMaterial)
            return;

        this._tileMaterial = tileMaterial;
        CLogger.Log($"TileItemId : {this._tileMaterial.tileItemID}");

        _SetTerrainMaterial();
    }

    private void _CreateBasePlane()
    {
        if (null == _basePlane)
        {
            _basePlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _basePlane.transform.SetParent(transform);
            _basePlane.transform.localPosition = Vector3.zero;
            _basePlane.transform.localRotation = Quaternion.identity;
            _basePlane.transform.localScale = Vector3.one;
        }

        if (null == _basePlaneMeshRenderer)
        {
            _basePlaneMeshRenderer = _basePlane.GetComponent<MeshRenderer>();
            _basePlaneMeshRenderer.sharedMaterial = basePlaneMaterial;
        }
    }

    private void _SetTerrainSize()
    {
        _basePlane.transform.localScale = new Vector3(terrainWidth * RESIZE_VALUE, 1f, terrainHeight * RESIZE_VALUE);
    }

    private void _SetTerrainMaterial()
    {
        _basePlaneMeshRenderer.sharedMaterial = _tileMaterial.material;
    }
}
