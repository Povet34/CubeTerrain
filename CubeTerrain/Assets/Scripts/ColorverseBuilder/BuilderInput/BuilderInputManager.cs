namespace Colorverse.Builder
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    [DefaultExecutionOrder(-1000)]
    public partial class BuilderInputManager : AbstractSingleton<BuilderInputManager>
    {
        //public enum EBuilderInputType 
        //{          
        //    None,
        //    Direct,
        //    Web,
        //    COUNT,
        //}

        //public IBuilderInput CurrentInput { get { return _allInputs[(int)CurrentInputType]; } }

        //public void Awake()
        //{
        //    _InitDebug();

        //    ChangeBuilderInput(EBuilderInputType.Direct);
        //}

        //public void Update()
        //{
        //    CurrentInput?.Update();

        //    _UpdateDebug();
        //}        

        //public void ChangeBuilderInput(EBuilderInputType inputType)
        //{
        //    if (CurrentInputType == inputType)
        //        return ;

        //    CurrentInputType = inputType;            

        //    CinemachineCore.GetInputAxis = CurrentInput.GetAxis;

        //    CurrentInput.OnChangedInput();            
        //}

        //public void OnChangedScreenSize(int w, int h)
        //{            
        //    for(int ii = 0; ii < _allInputs.Length; ++ii)
        //    {
        //        _allInputs[ii].OnChangedScreenSize(w, h);
        //    }            
        //}

        //public void LockInput(bool bLock)
        //{ 
        //    if (bLock)
        //    {
        //        _lockBackup = CurrentInputType;
        //        CurrentInputType = EBuilderInputType.None;
        //    }
        //    else
        //    {
        //        CurrentInputType = _lockBackup;
        //        _lockBackup = EBuilderInputType.None;
        //    }
        //}

        //#region IBuilderMessageHandler 구현
        //public void ProcessMessage(BuilderInputMsg msg)
        //{
        //    CurrentInput?.EnqueueMsg(msg);
        //}

        //public BuilderInputMsg ParseWebMessage(string json)
        //{
        //    BuilderInputMsg header = JsonUtility.FromJson<BuilderInputMsg>(json);
        //    if ((null != header) && (header.GetMsgType() != eBuilderInputMsgType.None))
        //    {
        //        return _ParseBuilderInputMsg_ByMsgType(header.GetMsgType(), json);
        //    }

        //    CLogger.LogError($"BuilderInputManager.ParseWebMessage() : this message is not a BuilderInputMsg.. data = {json}");
        //    return null;
        //}

        //private BuilderInputMsg _ParseBuilderInputMsg_ByMsgType(eBuilderInputMsgType msgType, string json)
        //{
        //    switch (msgType)
        //    {
        //        case eBuilderInputMsgType.w2c_q_sendFocusOut:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_sendFocusOut>(json);

        //        case eBuilderInputMsgType.w2c_q_shiftMouse:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_shiftMouse>(json);

        //        case eBuilderInputMsgType.w2c_q_startShiftMouse:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_startShiftMouse>(json);

        //        case eBuilderInputMsgType.w2c_q_endShiftMouse:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_endShiftMouse>(json);

        //        case eBuilderInputMsgType.w2c_q_sendMouseOut:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_sendMouseOut>(json);
        //        case eBuilderInputMsgType.w2c_q_shiftCameraZoom:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_shiftCameraZoom>(json);
        //        case eBuilderInputMsgType.w2c_q_startPressKeyboard:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_startPressKeyboard>(json);
        //        case eBuilderInputMsgType.w2c_q_pressKeyboard:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_pressKeyboard>(json);

        //        case eBuilderInputMsgType.w2c_q_endPressKeyboard:
        //            return JsonUtility.FromJson<BuilderInputMsg_w2c_q_endPressKeyboard>(json);
        //    }

        //    CLogger.LogError($"BuilderInputManager.ParseWebMessage() : Message did not handled.. msgType = {msgType}");
        //    return null;
        //}

        //public bool IsValidMessage(BuilderInputMsg msg)
        //{
        //    return (msg.GetMsgType() != eBuilderInputMsgType.None);
        //}
        //#endregion
    }
}