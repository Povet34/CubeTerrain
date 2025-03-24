namespace Colorverse.Builder
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    //using WebSocketSharp.Server;
    //---------------------------------------------------------------------------------------------
    // Error 정의
    //---------------------------------------------------------------------------------------------
    public enum eBuilderError
    {
        None,
        FailedChangeMode_NotSavedData,  //저장되지 않는 데이터가 존재합니다.

        InternalError,
    }

    //---------------------------------------------------------------------------------------------
    // Socket 관련
    //---------------------------------------------------------------------------------------------
    
    public enum EBuilderSocketStatus
    {
        None,
        Connecting,
        Open,
        Closing,
        Closed
    }

    public interface IBuilderMessageHandler<T>       
    {
        void ProcessMessage(T msg);
        T    ParseWebMessage(string json);

        bool IsValidMessage(T msg);
    }
    
    public interface IBuilderSocket<T>
    {
        EBuilderSocketStatus GetSocketStatus();

        void Open();
        void Close();

        void Update();

        void SendMsg(T msg);
        void ProcessMsg(T msg);
    }
        
    //---------------------------------------------------------------------------------------------
    // BuilderInput
    //---------------------------------------------------------------------------------------------
    public interface IBuilderInput
    {
        float GetAxis(string axisName);

        bool GetKeyDown(KeyCode key);
        bool GetKeyUp(KeyCode key);
        bool GetKey(KeyCode key);

        bool IsAnyKey();
        bool IsAnyKeyDown();

        Vector3 GetPointerXY(int pointer);
        bool GetPointerDown(int index);
        bool GetPointerUp(int index);
        bool GetPointer(int index);

        //Net Input 지원
        void Update();

        //Event 처리
        void OnChangedScreenSize(int w, int h);
        void OnChangedInput();
    }
    //---------------------------------------------------------------------------------------------
    // BuilderMessage. signaling, input, builder 에서 기본 클래스로 사용. 
    //---------------------------------------------------------------------------------------------
    public class BuilderMsgBase<T> : System.IEquatable<BuilderMsgBase<T>>
        where T : System.Enum
    {
        public string msgType;
        [System.NonSerialized] public string version;
        [System.NonSerialized] protected T eMsgType;
        protected virtual T eDefaultMsgType => (T)System.Enum.Parse(typeof(T), "None", true);
        public T GetMsgType()
        {
            if (eDefaultMsgType.Equals(eMsgType))
            {
                var split = msgType.Split("/");
                if (split.Length < 5)
                {
                    eMsgType = (T)System.Enum.Parse(typeof(T), msgType, true);
                    if (eMsgType.Equals(eDefaultMsgType))
                        Debug.LogError($"BuilderMessageBase::ParseMsgType - msgType is wrong. {msgType}");
                }
                else
                {
                    version = split[1];
                    eMsgType = (T)System.Enum.Parse(typeof(T), $"{split[2]}_{split[3]}_{split[4]}", true);
                    if (eMsgType.Equals(eDefaultMsgType))
                        Debug.LogError($"BuilderMessageBase::ParseMsgType - msgType is wrong. {msgType}");
                }
            }
            return eMsgType;
        }
        public virtual bool Equals(BuilderMsgBase<T> other)
        {
            return other.msgType.Equals(msgType);
        }
        public BuilderMsgBase() { }
        public BuilderMsgBase(T msgType, string version)
        {
            eMsgType = msgType;
            this.version = version;
            
            var split = msgType.ToString().Split("_");
            if (split.Length >= 3)
            {
                this.msgType = $"/{version}/{split[0]}/{split[1]}/{split[2]}";
            }
            else
                this.msgType = msgType.ToString();
            
        }
    }

    //---------------------------------------------------------------------------------------------
    // BuilderSystem
    //---------------------------------------------------------------------------------------------
    public interface IBuilderSystem
    {
        void Init();
        void Update();

        void ChangeMode(eBuilderMode eMode, StartModeParam param);

        //IBuilderCamera GetBuilderCamera();

        IBuilderAvatar GetBuilderAvatar();

        void LockInput(bool bLock);
        void RefreshWebUI();

        Transform GetRemovedItemRoot();
    }

    //---------------------------------------------------------------------------------------------
    // BuilderMode
    //---------------------------------------------------------------------------------------------    
    [System.Serializable]
    public enum eBuilderMode
    {
        None,
        Land,
        Item,
        Play,
        CubeTerrain,
        AssetCreator,               // 임시. 나중에 아이템 매니저를 통해서 들어오던?.. 기획단 정해지면 수정 필요함.
        ItemEditor,
        PlaneTerrain,
        House,
        AvatarItemEditor,
        Max,
    }

    public struct StartModeParam
    {
        //공통 --------------------------------------

        // Mode간 이동 시 각 Mode의 마지막 위치 저장. (land <-> terrain 간에는 같은 위치로 세팅)
        public Vector3 LastCampos;
        public Quaternion LastCamRot;

        //Land, CubeTerrain <---> Play --------------
        public string SpaceJson;

        //ItemMode ----------------------------------
        public string StartItemId;                  
    }

    public interface IBuilderUndo
    {
        void Undo();
        void Redo();
        void PurgeAll();

        void BeginRecord();
        void CreateRecord();
        void EndRecord();
    }

    public interface IBuilderMode
    {
        eBuilderMode GetMode();

        void Init();
        void Update();
        void PostUpdate();

        void StartMode(StartModeParam param);
        void RestartMode();
        void OnPostStartMode();
        void EndMode(eBuilderMode after, StartModeParam param, System.Action<StartModeParam> endModeDone);

        bool HasChanged();
        void OnScreenSizeChanged(int w, int h);
    }

    //---------------------------------------------------------------------------------------------
    // BuilderCamera
    //---------------------------------------------------------------------------------------------
    public interface IBuilderCamera
    {
        //void Init(IBuilderSystem builderSystem);
        //Camera GetCamera();

        //void ChangeMode(eBuilderMode mode);

        //void SetPos(Vector3 pos);
        //void SetRotation(Quaternion rot);
        //Vector3 GetPos();
        //Quaternion GetRotation();
        //Vector3 GetLastPos(eBuilderMode mode);
        //Quaternion GetLastRotation(eBuilderMode mode);

        //Vector3 GetForwardDir();
        //Vector3 GetRightDir();
        //Vector3 GetUpDir();

        //void EnableCamera(bool bEnable);
        //void AttachToScene(string sceneName);
        
    }

    //---------------------------------------------------------------------------------------------
    // BuilderAvatar (Play 모드 지원)
    //---------------------------------------------------------------------------------------------
    public interface IBuilderAvatar
    {
        void Init();

        Transform GetTransform();

        void Teleport(Vector3 pos, float angY);        
        void EnableAvatar(bool active);
    }

    //---------------------------------------------------------------------------------------------
    // ModelHolder (ItemBuilder 모드 지원)
    //---------------------------------------------------------------------------------------------
    public interface IModelHolder
    {
        Transform GetTransform();

        void SetActive(bool active);
    }
}