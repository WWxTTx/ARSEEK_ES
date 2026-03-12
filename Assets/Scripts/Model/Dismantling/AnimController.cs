using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 动画播放控制器
/// </summary>
public class AnimController : MonoBehaviour
{
    #region 动画
    /// <summary>
    /// 当前道具下的动画列表
    /// </summary>
    public Dictionary<string, AnimInfo> PlayableList = new Dictionary<string, AnimInfo>();
    /// <summary>
    /// 当前动画
    /// </summary>
    public PlayableDirector Playable;
    /// <summary>
    /// 当前动画道具名称
    /// </summary>
    public string CurrentPlayableProp;
    /// <summary>
    /// 是否正在播放
    /// </summary>
    public bool IsAnimPlay;
    #endregion

    #region 透明度
    /// <summary>
    /// 当前的透明度操作
    /// </summary>
    public OperationBase currentAlphaOperation
    {
        get
        {
            return _currentAlphaOperation;
        }
        set
        {
            if (_currentAlphaOperation != null && !_currentAlphaOperation.Equals(value))
            {
                ResetAlpha(_currentAlphaOperation);
            }

            _currentAlphaOperation = value;
        }
    }
    private OperationBase _currentAlphaOperation;
    /// <summary>
    /// 透明度材质组
    /// </summary>
    public List<Material> materials;
    public int currentAlphaIndex = -1;
    public bool canAlpha;
    #endregion

    /// <summary>
    /// 操作名称
    /// </summary>
    private static string SelectOperationName = "选中";
    private static string DeselectOperationName = "取消选中";
    private static string AlphaOperationName = "更改透明度";

    private ModelOperation RootOperation;

    public void InitAnimList()
    {
        RootOperation = GetComponent<ModelOperation>();

        //init anim list
        foreach (Transform child in transform)
        {
            ModelInfo modelInfo = child.GetComponent<ModelInfo>();
            if (modelInfo && modelInfo.PropType == PropType.Animation)
            {
                PlayableList.Add(child.name, new AnimInfo(modelInfo.ID, child.name, modelInfo.Name, modelInfo.GetComponent<PlayableDirector>()));
            }
        }

        canAlpha = HasOperation(RootOperation, AlphaOperationName);
        if (canAlpha && RootOperation.GetOperations().TryGetValue(AlphaOperationName, out OperationBase alphaOperation))
        {
            currentAlphaOperation = alphaOperation;

            foreach (var OperationEvent in alphaOperation.behaveBases)
            {
                OperationEvent.SaveInitialState();
            }
        }
    }

    private bool HasOperation(ModelOperation modelOperation, string operationName)
    {
        if (modelOperation == null || modelOperation.operations == null)
            return false;
        return modelOperation.operations.FindIndex(o => o.name.Equals(operationName)) >= 0;
    }

    #region 动画

    /// <summary>
    /// 选中动画
    /// </summary>
    public void SelectAnime(string propName)
    {
        if (PlayableList == null || PlayableList.Count == 0)
            return;

        if (!string.IsNullOrEmpty(propName) && PlayableList.TryGetValue(propName, out AnimInfo animInfo))
        {
            Playable = animInfo.PlayableDirector;
            CurrentPlayableProp = propName;
        }
        else
        {
            //默认选中第一个动画
            CurrentPlayableProp = PlayableList.Keys.ToList()[0];
            Playable = PlayableList[CurrentPlayableProp].PlayableDirector;
        }

        if (Playable)
        {
            ModelOperation modelOperation = Playable.GetComponent<ModelOperation>();
            if (modelOperation && modelOperation.GetOperations().TryGetValue(SelectOperationName, out OperationBase OperationBase))
            {
                foreach (BehaveBase behave in OperationBase.behaveBases)
                {
                    if (behave is BehaveMoveCamera)
                        continue;
                    behave.Execute();
                }
            }
        }
    }

    /// <summary>
    /// 取消选中动画
    /// </summary>
    public void DeselectAnime()
    {
        if (Playable != null)
        {
            //重置当前选中动画
            Playable.initialTime = 0;
            Playable.time = 0;
            Playable.Evaluate();

            //执行当前动画道具取消选中操作
            ModelOperation modelOperation = Playable.GetComponent<ModelOperation>();
            if (modelOperation && modelOperation.GetOperations().TryGetValue(DeselectOperationName, out OperationBase OperationBase))
            {
                foreach (BehaveBase behave in OperationBase.behaveBases)
                    behave.Execute();
            }
        }
    }
    #endregion

    #region 透明度
    public void SetAlpha(int index)
    {
        if (index == -1)
        {
            ResetAlpha(currentAlphaOperation);
            return;
        }

        currentAlphaIndex = index;

        if (currentAlphaOperation == null)
            return;

        foreach (var operationEvent in currentAlphaOperation?.behaveBases)
        {
            if (operationEvent is BehaveAlpha && materials.Count > index)
            {
                Material[] temp = new Material[] { materials[index] };
                {
                    foreach (var target in (operationEvent as BehaveAlpha).targets)
                    {
                        if (target == null)
                            continue;
                        target.materials = temp;
                    }
                }
            }
            else
            {
                operationEvent.Execute();
            }
        }
    }
    public void ResetAlpha(OperationBase target)
    {
        if (target == null)
            return;

        foreach (var targetEvent in target.behaveBases)
        {
            targetEvent.SetInitialState();
        }
    }
    #endregion

    public class AnimInfo
    {
        public string UUID;
        public string PropName { get; set; }
        public string Title { get; set; }
        public PlayableDirector PlayableDirector { get; set; }

        public AnimInfo(string uuid, string propName, string title, PlayableDirector playableDirector)
        {
            UUID = uuid;
            PropName = propName;
            Title = title;
            PlayableDirector = playableDirector;
        }
    }
}