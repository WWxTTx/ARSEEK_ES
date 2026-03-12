using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 仿真系统
/// </summary>
public class BaseSimuSystem : MonoBase
{
    public UISmallSceneModule SmallSceneModule { get; protected set; }
    public SmallFlowCtrl SmallFlowCtrl { get; protected set; }

    protected bool initialized;

    public bool Initialized
    {
        get { return initialized; }
    }

    public virtual void Init(UISmallSceneModule smallSceneModule, SmallFlowCtrl smallFlowCtrl)
    {
        this.SmallSceneModule = smallSceneModule;
        this.SmallFlowCtrl = smallFlowCtrl;
    }

    /// <summary>
    /// TODO 状态重置
    /// </summary>
    public virtual void ResetSystem()
    {

    }

    public virtual string GetSystemState()
    {
        return string.Empty;
    }

    /// <summary>
    /// 恢复系统
    /// </summary>
    /// <param name="stateJson"></param>
    public virtual void RecoverSystem(string stateJson)
    {

    }
}