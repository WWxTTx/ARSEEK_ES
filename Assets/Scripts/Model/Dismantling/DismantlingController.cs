using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 拆解控制器
/// </summary>
public class DismantlingController : MonoBehaviour
{
    public class SelectEvent : UnityEvent<GameObject> { };

    /// <summary>
    /// 选中功能脚本
    /// </summary>
    private SelectionModel mSelectionCtrl;
    public SelectionModel SelectionCtrl
    {
        get { return mSelectionCtrl; }
    }
    /// <summary>
    /// 选中物体改变事件
    /// </summary>
    public SelectEvent onSelectionChanged = new SelectEvent();

    /// <summary>
    /// 当前选择物体
    /// </summary>
    public GameObject selectModel;

    #region 拆解
    /// <summary>
    /// 当前选择物体操作
    /// </summary>
    private ModelOperation modelOperation;
    /// <summary>
    /// 当前选择物体父对象操作
    /// </summary>
    private ModelOperation parentModelOperation;

    private bool canFold;
    /// <summary>
    /// 能否进行组合
    /// </summary>
    public bool CanFold { get { return canFold && !InCheckMode; } }

    private bool canUnpick;
    /// <summary>
    /// 能否进行拆分
    /// </summary>
    public bool CanUnpick { get { return canUnpick && !InCheckMode; } }

    private bool inCheckMode = false;
    /// <summary>
    /// 是否单独显示
    /// </summary>
    public bool InCheckMode
    {
        get{ return inCheckMode; }
        set{ inCheckMode = value; }
    }
    /// <summary>
    /// 能否查看
    /// </summary>
    public bool CanLook
    {
        get
        {
            if (selectModel != null)
            {
                if (selectModel.transform.parent)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 当前本地选择物体
    /// </summary>
    public GameObject localSelectModel;
    /// <summary>
    /// 当前本地选择物体操作
    /// </summary>
    private ModelOperation localModelOperation;
    /// <summary>
    /// 当前本地选择物体父对象操作
    /// </summary>
    private ModelOperation localParentModelOperation;

    private bool canLocalFold;
    /// <summary>
    /// 本地能否进行组合
    /// </summary>
    public bool CanLocalFold { get { return canLocalFold/* && !InCheckMode*/; } }

    private bool canLocalUnpick;
    /// <summary>
    /// 本地能否进行拆分
    /// </summary>
    public bool CanLocalUnpick { get { return canLocalUnpick/* && !InCheckMode*/; } }

    /// <summary>
    /// 本地能否查看
    /// </summary>
    public bool CanLocalLook
    {
        get
        {
            if (localSelectModel != null)
            {
                if (localSelectModel.transform.parent)
                    return true;
            }
            return false;
        }
    }
    /// <summary>
    /// 是否正在拆分
    /// </summary>
    public bool isDispersing = false;
    /// <summary>
    /// 是否正在组合
    /// </summary>
    public bool isFolding = false;
    /// <summary>
    /// 是否正在一键还原中
    /// </summary>
    public bool isResetting = false;

    /// <summary>
    /// 可组装物体状态保存
    /// </summary>
    public ModelOperation latestFoldableModel;
    /// <summary>
    /// 正在查看的模型
    /// </summary>
    public GameObject showCtrl;
    /// <summary>
    /// 当前取得操作权的用户
    /// </summary>
    public int controlUser;
    #endregion

    /// <summary>
    /// 操作名称
    /// </summary>
    private static string UnpickOperationName = "拆解";
    private static string FoldOperationName = "组合";
    private static string SelectOperationName = "选中";
    private static string DeselectOperationName = "取消选中";
    private static string ObserveOperationName = "观察";
    private static string UnObserveOperationName = "取消观察";

    ///// <summary>
    ///// 初始距相机距离
    ///// </summary>
    //private float distance;
    //public float centerDis
    //{
    //    get { return distance; }
    //    set { distance = value; }
    //}

    void Start()
    {
        mSelectionCtrl = gameObject.AutoComponent<SelectionModel>();
        mSelectionCtrl.onSelectModel.AddListener(LocalSelection);

        onSelectionChanged.AddListener((go) =>
        {
            if (go)
            {
                ModelManager.Instance.AdaptModelRestrict(go);
                ModelManager.Instance.RevertCameraPose();
                ModelManager.Instance.ResetCameraPose();
            }
        });

        mSelectionCtrl.CloseAllCollider();
        SetColliderState();

        //打开模型层级模块
        FormMsgManager.Instance.SendMsg(new MsgBase((ushort)CoursePanelEvent.HierarchyBtn));
    }

    private void SetColliderState()
    {
        if (transform.GetComponent<ModelOperation>())
        {
            mSelectionCtrl.SetSelectState(transform, true);
        }
        else
        {
            //初始包含多个可拆分模型的情况
            foreach (Transform child in transform)
            {
                if (child.GetComponent<ModelOperation>())
                {
                    mSelectionCtrl.SetSelectState(child, true);
                }
            }
        }
    }

    /// <summary>
    /// 设置本地选中的模型
    /// </summary>
    /// <param name="obj"></param>
    private void LocalSelection(GameObject obj, int userId)
    {
        if (GlobalInfo.IsLiveMode() && !GlobalInfo.ShouldProcess(userId))
        {
            return;
        }

        localSelectModel = obj;
        if (obj)
        {          
            localParentModelOperation = obj.transform.parent.GetComponent<ModelOperation>();
            if (localParentModelOperation)
                canLocalFold = HasOperation(localParentModelOperation, FoldOperationName);
            else
                canLocalFold = false;

            localModelOperation = obj.GetComponent<ModelOperation>();
            if (localModelOperation == null)
            {
                canLocalUnpick = false;
                onSelectionChanged?.Invoke(obj);
            }
            else
            {
                canLocalUnpick = HasOperation(localModelOperation, UnpickOperationName);

                //选中表现
                if (localModelOperation.GetOperations().TryGetValue(SelectOperationName, out OperationBase op) && op.behaveBases?.Count > 0)
                {
                    for (int i = 0; i < op.behaveBases.Count; i++)
                    {
                        BehaveBase behave = op.behaveBases[i];
                        if (i < op.behaveBases.Count - 1)
                        {
                            if (!(behave is BehaveCenter))
                                behave.Execute();
                        }
                        else
                        {
                            if (behave is BehaveCenter)
                            {
                                onSelectionChanged?.Invoke(obj);
                            }
                            else
                            {
                                behave.Execute(() =>
                                {
                                    onSelectionChanged?.Invoke(obj);
                                });
                            }
                        }
                    }
                }
                else
                {
                    onSelectionChanged?.Invoke(obj);
                }
            }
        }
        else
        {
            localModelOperation = null;
            localParentModelOperation = null;

            canLocalUnpick = false;
            canLocalFold = false;

            onSelectionChanged?.Invoke(obj);
        }
    }

    /// <summary>
    /// 修改选中物体
    /// </summary>
    /// <param name="go"></param>
    public void UpdateSelect(GameObject go)
    {
        selectModel = go;
        if (selectModel)
        {
            parentModelOperation = selectModel.transform.parent.GetComponent<ModelOperation>();
            if (parentModelOperation)
                canFold = HasOperation(parentModelOperation, FoldOperationName);
            else
                canFold = false;

            modelOperation = selectModel.GetComponent<ModelOperation>();
            if (modelOperation == null)
            {
                canUnpick = false;
            }
            else
            {
                canUnpick = HasOperation(modelOperation, UnpickOperationName);

                //选中表现
                if (modelOperation.GetOperations().TryGetValue(SelectOperationName, out OperationBase op))
                {
                    foreach (BehaveBase b in op.behaveBases)
                    {
                        if (!(b is BehaveCenter))
                            b.Execute();
                    }
                }
            }

            latestFoldableModel = parentModelOperation;
        }
        else
        {
            modelOperation = null;
            parentModelOperation = null;
            canUnpick = false;
            canFold = false;
        }
    }

    #region 拆分组合查看

    /// <summary>
    /// 拆分
    /// </summary>
    public void Disperse()
    {
        if (canUnpick)
        {
            showCtrl = null;
            if (modelOperation.GetOperations().TryGetValue(UnpickOperationName, out OperationBase unpickOperation))
            {
                isDispersing = true;
                FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, false));
                //拆分过程中暂停操作同步
                NetworkManager.Instance.IsIMSync = false;

                //拆解隐藏同级模型
                if (selectModel.transform.parent)
                {
                    foreach (Transform item in selectModel.transform.parent)
                    {
                        item.gameObject.SetActive(item == selectModel.transform);
                    }
                }

                //取消同级可选中
                if (selectModel.transform.parent)
                {
                    for (int i = 0; i < selectModel.transform.parent.childCount; i++)
                    {
                        mSelectionCtrl.SetSelectState(selectModel.transform.parent.GetChild(i), false);
                    }
                }

                Transform selectModelTrans = modelOperation.transform;
                ModelOperation temp = modelOperation;

                //取消选中
                mSelectionCtrl.SelectModel(null, controlUser);

                latestFoldableModel = temp;

                //执行拆解操作       
                Execute(unpickOperation.behaveBases, () =>
                {
                    Transform child;
                    //拆解后子级可选中
                    for (int j = 0; j < selectModelTrans.childCount; j++)
                    {
                        child = selectModelTrans.GetChild(j);
                        mSelectionCtrl.SetSelectState(child, true);

                        ModelOperation modelOperation = child.GetComponent<ModelOperation>();
                        if (modelOperation && modelOperation.GetOperations().ContainsKey(SelectOperationName))
                        {
                            mSelectionCtrl.SelectModel(child.gameObject, controlUser);
                        }
                    }
                    //拆解完成
                    isDispersing = false;
                    FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, true));
                });
            }
        }
    }

    /// <summary>
    /// 组合
    /// </summary>
    public void Fold()
    {
        if (canFold)
        {
            showCtrl = null;

            if (parentModelOperation.GetOperations().TryGetValue(FoldOperationName, out OperationBase foldOperation))
            {
                isFolding = true;
                FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, false));
                //组合过程中暂停操作同步
                NetworkManager.Instance.IsIMSync = false;

                Transform selectModelParent = parentModelOperation.transform;

                //取消选中
                mSelectionCtrl.SelectModel(null, GlobalInfo.account.id);

                //显示子级模型
                if (selectModelParent)
                {
                    foreach (Transform item in selectModelParent)
                    {
                        item.gameObject.SetActive(true);
                    }
                }

                //取消同级可选中
                for (int i = 0; i < selectModelParent.childCount; i++)
                {
                    mSelectionCtrl.SetSelectState(selectModelParent.GetChild(i), false);
                }

                //执行组合操作              
                Execute(foldOperation.behaveBases, () =>
                {
                    //显示同级模型
                    if (selectModelParent.parent)
                    {
                        foreach (Transform item in selectModelParent.parent)
                        {
                            item.gameObject.SetActive(true);
                        }
                    }

                    //组合后同级可选中    
                    if (selectModelParent.parent)
                    {
                        for (int j = 0; j < selectModelParent.parent.childCount; j++)
                        {
                            mSelectionCtrl.SetSelectState(selectModelParent.parent.GetChild(j), true);
                        }
                    }
                    //组合完成后的拆解层级
                    latestFoldableModel = selectModelParent.parent.GetComponent<ModelOperation>();

                    //设置选中
                    mSelectionCtrl.SelectModel(selectModelParent.gameObject, controlUser);

                    //组合完成
                    isFolding = false;
                    FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, true));
                });
            }
        }
    }

    /// <summary>
    /// 拆解组合操作执行序列
    /// </summary>
    private Sequence sequence;
    /// <summary>
    /// 拆解组合操作表现子序列
    /// </summary>
    private Sequence subSequence;

    /// <summary>
    /// 执行
    /// </summary>
    /// <param name="behaveBases"></param>
    /// <param name="onComplete"></param>
    private void Execute(List<BehaveBase> behaveBases, UnityAction onComplete)
    {
        sequence = DOTween.Sequence();
        foreach (BehaveBase behaveBase in behaveBases)
        {
            if (behaveBase is BehaveDotween)
            {
                subSequence = ((BehaveDotween)behaveBase).ExecuteSequence();
                if (behaveBase.useCallBack)
                    sequence.Append(subSequence);
                else
                    sequence.Join(subSequence);
            }
            else
            {
                sequence.AppendCallback(() => behaveBase.Execute());
            }
        }
        sequence.OnComplete(() =>
        {
            onComplete?.Invoke();
        });
        sequence.SetId("unpick");
    }

    /// <summary>
    /// 查看
    /// </summary>
    /// <param name="isCheck"></param>
    /// <param name="callback"></param>
    public void Check(bool isCheck, UnityAction callback = null)
    {
        if (selectModel == null || selectModel.transform.parent == null)
        {
            callback?.Invoke();
            return;
        }

        GlobalInfo.InSingleMode = isCheck;
        InCheckMode = isCheck;

        //还原相机机位
        ModelManager.Instance.AdaptModelRestrict(selectModel);
        ModelManager.Instance.RevertCameraPose();
        ModelManager.Instance.ResetCameraPose(true, false, () =>
        {
            if (isCheck)
            {
                foreach (Transform item in selectModel.transform.parent)
                {
                    item.gameObject.SetActive(item == selectModel.transform);
                }

                mSelectionCtrl.Unhighlight(selectModel);
                mSelectionCtrl.CanDeselect = false;

                //执行观察操作 需要配置
                ModelOperation modelOperation = selectModel.GetComponent<ModelOperation>();
                if (modelOperation && modelOperation.GetOperations().TryGetValue(ObserveOperationName, out OperationBase op) && op.behaveBases.Count > 0)
                {
                    for(int i = 0; i < op.behaveBases.Count; i++)
                    {
                        if (i == op.behaveBases.Count - 1)
                            op.behaveBases[i].Execute(callback);
                        else
                            op.behaveBases[i].Execute();
                    }
                }
                else
                {
                    callback?.Invoke();
                }
            }
            else
            {
                //SetAlpha(-1);

                if(!GlobalInfo.IsLiveMode() || GlobalInfo.IsUserOperator(controlUser))
                    mSelectionCtrl.Highlight(selectModel, controlUser);
                mSelectionCtrl.CanDeselect = true;

                foreach (Transform item in selectModel.transform.parent)
                {
                    item.gameObject.SetActive(true);
                }

                //执行取消观察操作 需要配置
                ModelOperation modelOperation = selectModel.GetComponent<ModelOperation>();
                if (modelOperation && modelOperation.GetOperations().TryGetValue(UnObserveOperationName, out OperationBase op) && op.behaveBases.Count > 0)
                {
                    for (int i = 0; i < op.behaveBases.Count; i++)
                    {
                        if (i == op.behaveBases.Count - 1)
                            op.behaveBases[i].Execute(callback);
                        else
                            op.behaveBases[i].Execute();
                    }
                }
                else
                {
                    callback?.Invoke();
                }
            }
        });
    }

    private bool HasOperation(ModelOperation modelOperation, string operationName)
    {
        return modelOperation.operations.FindIndex(o => o.name.Equals(operationName)) >= 0;
    }

    #endregion

    #region 知识点选中/状态同步
    /// <summary>
    /// 跳转拆解层级并选中
    /// </summary>
    /// <param name="go"></param>
    /// <param name="userId"></param>
    /// <param name="playAnim">是否默认播放动画</param>
    public void JumpToSelect(GameObject go, int userId, bool observeMode)
    {
        if (go == null)
            return;
        FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, false));
        NetworkManager.Instance.IsIMSync = false;

        //取消当前选中
        SelectionCtrl.ClearSelection();
        onSelectionChanged?.Invoke(null);

        GameObject goParent = go.transform.parent.gameObject;

        if (goParent.transform == ModelManager.Instance.modelRoot)
            goParent = null;

        latestFoldableModel = goParent?.GetComponent<ModelOperation>();
        JumpToState(goParent);

        //选中 
        this.WaitTime(0.1f, () =>
        {
            if(!NetworkManager.Instance.IsIMSyncState || userId != GlobalInfo.account.id)
            {
                if(!GlobalInfo.IsLiveMode() || GlobalInfo.IsUserOperator(userId))
                    SelectionCtrl.SelectModel(go, userId);
            }

            ////todo
            //if(userId != GlobalInfo.account.id)
            //{
            //    ModelManager.Instance.AdaptModelRestrict(goParent == null ? gameObject : goParent);
            //    ModelManager.Instance.RevertCameraPose();
            //    ModelManager.Instance.ResetCameraPose();
            //}

            if (observeMode)
            {
                MsgBase msgString = new MsgBase((ushort)IntegrationModuleEvent.Check);
                FormMsgManager.Instance.SendMsg(new MsgBrodcastOperate()
                {
                    senderId = userId,
                    msgId = msgString.msgId,
                    data = JsonTool.Serializable(msgString)
                });
            }
            else
            {
                FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, true));
            }
        });
    }

    /// <summary>
    /// 跳转拆解层级
    /// </summary>
    /// <param name="go"></param>   
    public void JumpToState(GameObject go)
    {
        //全部组合
        FoldChild(GetComponent<ModelOperation>());

        if (go == null)
        {
            mSelectionCtrl.CloseAllCollider();
            mSelectionCtrl.SetSelectState(transform, true);
        }
        else
        {
            //隐藏同级和上层物体
            HideOtherSibling(go, go?.transform.parent?.GetComponent<ModelOperation>());
            //自身拆解
            ToUnpickInstant(go);
        }
    }

    private void HideOtherSibling(GameObject self, ModelOperation parentModelOperation)
    {
        if (parentModelOperation == null)
            return;

        mSelectionCtrl.SetSelectState(parentModelOperation.transform, false);

        //同级物体散开
        if (parentModelOperation.GetOperations().TryGetValue(UnpickOperationName, out OperationBase OperationBase))
        {
            foreach (BehaveBase behave in OperationBase.behaveBases)
            {
                behave.SetFinalState();
            }
        }

        //确保隐藏未设置为操作表现执行对象的物体
        Transform child;
        for (int i = 0; i < parentModelOperation.transform.childCount; i++)
        {
            child = parentModelOperation.transform.GetChild(i);
            mSelectionCtrl.SetSelectState(child, false);
            child.gameObject.SetActive(child.gameObject == self);
        }

        HideOtherSibling(parentModelOperation.gameObject, parentModelOperation.transform.parent?.GetComponent<ModelOperation>());
    }

    /// <summary>
    /// 组合自身及子级
    /// </summary>
    /// <param name="modelOperation"></param>
    private void FoldChild(ModelOperation modelOperation)
    {
        if (modelOperation == null)
            return;

        Transform temp;

        if (modelOperation.GetOperations().TryGetValue(FoldOperationName, out OperationBase foldOps))
        {
            foreach (BehaveBase behave in foldOps.behaveBases)
            {
                behave.SetFinalState();
            }
        }
        for (int i = 0; i < modelOperation.transform.childCount; i++)
        {
            temp = modelOperation.transform.GetChild(i);
            temp.gameObject.SetActive(true);
            mSelectionCtrl.SetSelectState(temp, false);
        }

        mSelectionCtrl.SetSelectState(modelOperation.transform, false);

        foreach (Transform child in modelOperation.transform)
        {
            FoldChild(child.GetComponent<ModelOperation>());
        }
    }

    private void ToUnpick(GameObject gameObject, bool unpick, UnityAction callback = null)
    {
        ModelOperation modelOperation = gameObject.GetComponent<ModelOperation>();
        if (modelOperation && modelOperation.GetOperations().TryGetValue(unpick ? UnpickOperationName : FoldOperationName, out OperationBase operation))
        {
            Execute(operation.behaveBases, () =>
            {
                callback?.Invoke();
            });
        }

        //临时解决方案
        Transform child;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            child = gameObject.transform.GetChild(i);
            child.gameObject.SetActive(true);
            mSelectionCtrl.SetSelectState(child, unpick);
        }
    }

    private void ToUnpickInstant(GameObject gameObject)
    {
        ModelOperation modelOperation = gameObject.GetComponent<ModelOperation>();
        if (modelOperation && modelOperation.GetOperations().TryGetValue(UnpickOperationName, out OperationBase operation))
        {
            foreach (BehaveBase behave in operation.behaveBases)
            {
                behave.SetFinalState();
            }
        }

        //临时解决方案
        Transform child;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            child = gameObject.transform.GetChild(i);
            child.gameObject.SetActive(true);
            mSelectionCtrl.SetSelectState(child, true);
        }
    }

    /// <summary>
    /// 一键组合
    /// </summary>
    public void FoldAll()
    {
        FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, false));
        NetworkManager.Instance.IsIMSync = false;
        GlobalInfo.playTimeRatio = 0.3f;

        mSelectionCtrl.SelectModel(null, GlobalInfo.account.id);
        mSelectionCtrl.ClearSelection();
        //localSelectModel = null;

        //中断正在执行的拆组操作
        if (latestFoldableModel)
        {
            if (isDispersing && latestFoldableModel.GetOperations().TryGetValue(UnpickOperationName, out OperationBase unpickOperation))
            {
                DOTween.Kill("unpick");
                foreach (var OperationEvent in unpickOperation.behaveBases)
                {
                    OperationEvent.SetFinalState();
                }
            }

            if (isFolding && latestFoldableModel.GetOperations().TryGetValue(FoldOperationName, out OperationBase foldOperation))
            {
                DOTween.Kill("unpick");
                foreach (var OperationEvent in foldOperation.behaveBases)
                {
                    OperationEvent.SetInitialState();
                }
            }
        }

        FoldParent(latestFoldableModel?.transform, () =>
        {
            if (gameObject.TryGetComponent(out ModelOperation modelOperation) && modelOperation.GetOperations().TryGetValue(SelectOperationName, out OperationBase op))
            {
                for (int i = 0; i < op.behaveBases.Count; i++)
                {
                    BehaveBase behave = op.behaveBases[i];
                    if (i < op.behaveBases.Count - 1)
                        behave.Execute();
                    else
                    {
                        behave.Execute(() =>
                        {
                            ModelManager.Instance.AdaptModelRestrict(gameObject);
                            ModelManager.Instance.RevertCameraPose();
                            ModelManager.Instance.ResetCameraPose(true, false, () =>
                            {
                                GlobalInfo.playTimeRatio = 1f;
                                isResetting = false;
                                FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, true));
                            });
                        });
                    }
                }
            }
            else
            {
                ModelManager.Instance.AdaptModelRestrict(gameObject);
                ModelManager.Instance.RevertCameraPose();
                ModelManager.Instance.ResetCameraPose(true, false, () =>
                {
                    GlobalInfo.playTimeRatio = 1f;
                    isResetting = false;
                    FormMsgManager.Instance.SendMsg(new MsgBool((ushort)HierarchyEvent.Interactable, true));
                });
            }

            mSelectionCtrl.CloseAllCollider();
            SetColliderState();
            latestFoldableModel = null;
        });
    }

    /// <summary>
    /// 组合自身并向上组合
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="callback"></param>
    private void FoldParent(Transform transform, UnityAction callback)
    {
        ModelOperation modelOperation = transform?.GetComponent<ModelOperation>();
        if (modelOperation == null)
        {
            if (transform == this.transform)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).gameObject.SetActive(true);
                }
            }
            //组合完成
            callback?.Invoke();
            return;
        }
        ToUnpick(modelOperation.gameObject, false, () =>
        {
            FoldParent(modelOperation.transform.parent, callback);
        });
    }
    #endregion
}