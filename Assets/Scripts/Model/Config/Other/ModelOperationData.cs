using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityFramework.Runtime;

/// <summary>
/// 行为
/// </summary>
[System.Serializable]
public class OperationBase
{
    /// <summary>
    /// 操作名称
    /// </summary>
    public string name = "操作名称";
    /// <summary>
    /// 操作完成提示
    /// </summary>
    [Tooltip("操作完成提示")]
    public string hint_success = "完成提示";
    /// <summary>
    /// 操作限制
    /// </summary>
    [Tooltip("操作限制")]
    public List<OpRestrict> conditions = new List<OpRestrict>();
    /// <summary>
    /// 触发事件
    /// </summary>
    [SerializeReference]
    public List<BehaveBase> behaveBases = new List<BehaveBase>();
    /// <summary>
    /// 操作联动
    /// </summary>
    [Tooltip("操作联动")]
    public List<OpLinkage> actions = new List<OpLinkage>();

#if UNITY_EDITOR
    /// <summary>
    /// EDITOR显隐状态
    /// </summary>
    public bool state = true;
#endif
    public virtual OperationBase Clone()
    {
        OperationBase clone = (OperationBase)Activator.CreateInstance(GetType());
        // 然后复制所有字段
        var fields = GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var field in fields)
        {
            // 如果是值类型或者是string，直接复制
            if (field.FieldType.IsValueType || field.FieldType == typeof(string))
            {
                field.SetValue(clone, field.GetValue(this));
            }
            else
            {
                // 如果是引用类型，且需要深拷贝，这里我们简单处理：只复制值（浅拷贝）或者递归深拷贝（复杂）
                // 由于我们不知道引用类型的具体情况，这里我们只做浅拷贝
                field.SetValue(clone, field.GetValue(this));
            }
        }
        return clone;
    }
}

[Serializable]
public class OpRestrict
{
    /// <summary>
    /// 操作对象
    /// </summary>
    [Tooltip("操作对象")]
    public ModelOperation operation;
    /// <summary>
    /// 操作选项
    /// </summary>
    [Tooltip("操作选项")]
    public string optionName;

#if UNITY_EDITOR
    /// <summary>
    /// EDITOR显隐状态
    /// </summary>
    public bool state = true;
#endif
}
[Serializable]
public class OpLinkage
{
    /// <summary>
    /// 联动对象
    /// </summary>
    [Tooltip("联动对象")]
    public ModelOperation operation;
    /// <summary>
    /// 联动表现
    /// </summary>
    [Tooltip("联动表现")]
    public string optionName;

    [Tooltip("是否等待当前行为执行完毕")]
    public bool useCallback;

#if UNITY_EDITOR
    /// <summary>
    /// EDITOR显隐状态
    /// </summary>
    public bool state = true;
#endif
}

/// <summary>
/// 操作表现
/// </summary>
[Serializable]
public class BehaveBase
{
    /// <summary>
    /// 表现执行对象
    /// </summary>
    public GameObject ctrlGO;

    /// <summary>
    /// 表现类型
    /// </summary>
    public BehaveType behaveType;

    /// <summary>
    /// 是否启用回调
    /// </summary>
    public bool useCallBack = false;

    /// <summary>
    /// 系数
    /// </summary>
    public float multiplier = 1f;

#if UNITY_EDITOR
    /// <summary>
    /// EDITOR显隐状态
    /// </summary>
    public bool state = true;
#endif

    /// <summary>
    /// 执行操作
    /// </summary>
    /// <param name="callback"></param>
    public virtual void Execute(UnityAction callback = null)
    {
        SaveInitialState();
    }

    /// <summary>
    /// 设置为初始状态
    /// </summary>
    public virtual void SetInitialState()
    {

    }

    /// <summary>
    /// 设置为最终状态
    /// </summary>
    public virtual void SetFinalState()
    {
        SaveInitialState();
    }

    /// <summary>
    /// 记录为初始状态
    /// </summary>
    public virtual void SaveInitialState()
    {

    }

    public virtual void SetMultiplier(float value)
    {
        multiplier = value;
    }
}

/// <summary>
/// 模型居中
/// </summary>
[Serializable]
public class BehaveCenter : BehaveBase
{
    public Vector3 position;
    public float playTime = 1f;

    public BehaveCenter()
    {
        behaveType = BehaveType.Center;
        position = new Vector3(-0.427031f, -0.6570363f, -0.1672773f);
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        if (ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }

        ModelManager.Instance.modelGo.transform.SetParent(null);
        ModelManager.Instance.modelRoot.position = ctrlGO.transform.position;
        ModelManager.Instance.modelGo.transform.SetParent(ModelManager.Instance.modelRoot);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(ModelManager.Instance.modelRoot.DOMove(position, playTime * GlobalInfo.playTimeRatio));
        sequence.timeScale = multiplier;
        sequence.OnComplete(() => callback?.Invoke());
    }
}

[Serializable]
public class BehaveCustomScript : BehaveBase
{
    IBaseBehaviour behaviour;
    public BehaveCustomScript()
    {
        behaveType = BehaveType.CustomScript;
        Step = 0;
    }
    public int Step;
    bool GetBehaviour()
    {
        if (behaviour == null)
        {
            behaviour = ctrlGO.GetComponent<IBaseBehaviour>();
            if (behaviour == null)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 顺序执行
    /// </summary>
    /// <param name="callback"></param>
    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        if (GetBehaviour())
        {
            behaviour.Execute(Step, callback);
        }
    }

    /// <summary>
    /// 设置为默认状态
    /// </summary>
    public override void SetFinalState()
    {
        if (GetBehaviour())
        {
            behaviour.SetFinalState();
            SaveInitialState();
        }
    }
}

[Serializable]
public class Thermometry : BehaveBase
{
    public Thermometry()
    {
        behaveType = BehaveType.Thermometring;
        heatSensitivity = 1;
        useTime = 2;
    }
    public float useTime;
    public float heatSensitivity;

    /// <summary>
    /// 热成像仪复位
    /// </summary>
    /// <param name="callback"></param>
    void ThermometryLater(UnityAction callback = null)
    {
        DOVirtual.DelayedCall(useTime, () =>
        {
            ctrlGO.gameObject.SetActive(true);
            ctrlGO.transform.localRotation = Quaternion.Euler(0, 90, 0);
            ctrlGO.transform.DOScale(0.4f, 1);
            ctrlGO.transform.DOLocalMove(new Vector3(-0.184f, 0.234f, -0.329f), 1f).OnComplete(() =>
            {
                callback?.Invoke();
                DOVirtual.DelayedCall(1, () =>
                {
                    ctrlGO.transform.DOScale(1f, 0);
                    ctrlGO.gameObject.SetActive(false);
                });
            });
        });
    }

    /// <summary>
    /// 顺序执行
    /// </summary>
    /// <param name="callback"></param>
    public override void Execute(UnityAction callback = null)
    {
        if (ctrlGO != null)
        {
            ctrlGO.SetActive(true);
            ctrlGO.transform.DOScale(1f, 0);
            ctrlGO.transform.DOLocalMove(new Vector3(0f, 0.05f, 0f), 0f);
            ctrlGO.transform.localRotation = Quaternion.Euler(0, 85, 0);
            Material Sensitivity = ctrlGO.transform.Find("Root/PM").GetComponent<MeshRenderer>().material;
            Sensitivity.SetFloat("_HeatSensitivity", heatSensitivity);

            //显示
            ctrlGO.transform.DOLocalMove(new Vector3(-0.28f, 0.2f, -0.28f), 1f).OnComplete(() =>
            {
                DOVirtual.DelayedCall(useTime, () =>
                {
                    ctrlGO.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    ctrlGO.transform.DOScale(0.4f, 1);
                    ctrlGO.transform.DOLocalMove(new Vector3(-0.184f, 0.234f, -0.329f), 1f).OnComplete(() =>
                    {
                        callback?.Invoke();
                    });
                });
            });
        }
    }
}


/// <summary>
/// Dotween操作表现基类
/// </summary>
[Serializable]
public class BehaveDotween : BehaveBase
{
    /// <summary>
    /// 用于终止动画
    /// </summary>
    public Sequence sequence;
    /// <summary>
    /// 动画曲线
    /// </summary>
    public EaseType ease = EaseType.InOutQuad;

    /// <summary>
    /// 执行操作，返回动画序列
    /// </summary>
    /// <returns></returns>
    public Sequence ExecuteSequence()
    {
        Execute();
        return sequence;
    }

    public override void SetMultiplier(float value)
    {
        base.SetMultiplier(value);
        if (sequence != null && value != sequence.timeScale)
        {
            sequence.timeScale = value;
        }
    }
}

/// <summary>
/// 移动相机
/// </summary>
[Serializable]
public class BehaveMoveCamera : BehaveDotween
{
    /// <summary>
    /// 设置为最终状态时是否执行动画过程，默认false
    /// </summary>
    public bool tween;
    public List<Vector3> positions = new List<Vector3>();
    public List<Vector3> eulerAngles = new List<Vector3>();
    public List<float> playTimes = new List<float>();

#if UNITY_EDITOR
    public bool isAutoTime = false;
#endif

    public BehaveMoveCamera()
    {
        behaveType = BehaveType.MoveCamera;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        var camera = Camera.main.transform;
        {
            DOTween.Kill("BehaveMoveCamera", true);

            
            sequence = DOTween.Sequence();
            {
                for (int i = 0; i < positions.Count; i++)
                {
                    sequence.Append(camera.DOMove(positions[i], playTimes[i] * GlobalInfo.playTimeRatio).SetEase((Ease)ease));
                    sequence.Join(camera.DORotate(eulerAngles[i], playTimes[i] * GlobalInfo.playTimeRatio).SetEase((Ease)ease));
                }

                List<MonoBehaviour> cameraComponent = new List<MonoBehaviour>();
                {
                    if (camera.TryGetComponent(out CameraRotate cameraRotate))
                    {
                        cameraRotate.enabled = false;
                        cameraComponent.Add(cameraRotate);
                    }

                    if (camera.TryGetComponent(out CameraMove cameraMove))
                    {
                        cameraMove.enabled = false;
                        cameraComponent.Add(cameraMove);
                    }

                    if (camera.TryGetComponent(out CameraZoom cameraZoom))
                    {
                        cameraZoom.enabled = false;
                        cameraComponent.Add(cameraZoom);
                    }
                    sequence.timeScale = multiplier;
                    sequence.OnComplete(() =>
                    {
                        foreach (var child in cameraComponent)
                        {
                            child.enabled = true;
                        }
                        
                        callback?.Invoke();
                    });
                }
            }
            sequence.SetId("BehaveMoveCamera");
        }
    }

    public override void SetInitialState()
    {
        base.SetInitialState();
        sequence.Kill();

        if (ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>() != null)
            return;
        if (positions.Count > 0)
        {
            Camera.main.transform.position = positions[0];
            Camera.main.transform.eulerAngles = eulerAngles[0];
            
            ModelManager.Instance.UpdateCameraPose();
        }
    }

    public override void SetFinalState()
    {
        base.SetFinalState();
        sequence.Kill();

        if (ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>() != null)
            return;
        if (positions.Count > 0)
        {
            if (tween)
            {
                Execute();
            }
            else
            {
                DOTween.Kill("BehaveMoveCamera");

                Camera.main.transform.position = positions[positions.Count - 1];
                Camera.main.transform.eulerAngles = eulerAngles[positions.Count - 1];

                
                ModelManager.Instance.UpdateCameraPose();
            }
        }
    }
}

/// <summary>
/// 缩进相机
/// </summary>
[Serializable]
public class BehaveZoomCamera : BehaveDotween
{
    public float distance;
    public float playTime = -1f;

    private Vector3 initPosition;

    public BehaveZoomCamera()
    {
        behaveType = BehaveType.ZoomCamera;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        var camera = Camera.main.transform;
        {
            DOTween.Kill("BehaveZoomCamera", true);

            
            sequence = DOTween.Sequence();
            {
                Vector3 targetPosition = ModelManager.Instance.modelBoundsCenter - camera.forward * distance;
                if (playTime < 0)
                {
                    float posDelta = Vector3.Distance(camera.position, targetPosition);
                    playTime = Mathf.Clamp(posDelta / 5f, 0, 0.7f);
                }

                sequence.Append(camera.DOMove(targetPosition, playTime * GlobalInfo.playTimeRatio).SetEase((Ease)ease)); ;

                List<MonoBehaviour> cameraComponent = new List<MonoBehaviour>();
                {
                    if (camera.TryGetComponent(out CameraRotate cameraRotate))
                    {
                        cameraRotate.enabled = false;
                        cameraComponent.Add(cameraRotate);
                    }

                    if (camera.TryGetComponent(out CameraMove cameraMove))
                    {
                        cameraMove.enabled = false;
                        cameraComponent.Add(cameraMove);
                    }

                    if (camera.TryGetComponent(out CameraZoom cameraZoom))
                    {
                        cameraZoom.enabled = false;
                        cameraComponent.Add(cameraZoom);
                    }
                    sequence.timeScale = multiplier;
                    sequence.OnComplete(() =>
                    {
                        foreach (var child in cameraComponent)
                        {
                            child.enabled = true;
                        }

                        ModelManager.Instance.UpdateCameraPose();
                        
                        callback?.Invoke();
                    });
                }
            }
            sequence.SetId("BehaveZoomCamera");
        }
    }

    public override void SetInitialState()
    {
        base.SetInitialState();
        sequence.Kill();
        Camera.main.transform.position = initPosition;
    }

    public override void SetFinalState()
    {
        base.SetFinalState();
        sequence.Kill();
        Transform camera = Camera.main.transform;
        camera.position = ModelManager.Instance.modelBoundsCenter - camera.forward * distance;
        
        ModelManager.Instance.UpdateCameraPose();
    }

    public override void SaveInitialState()
    {
        base.SaveInitialState();
        initPosition = Camera.main.transform.position;
    }
}

/// <summary>
/// 移动
/// </summary>
[Serializable]
public class BehaveMove : BehaveDotween
{
    public List<Vector3> positions = new List<Vector3>();
    public float playTime = 1f;

    public BehaveMove()
    {
        behaveType = BehaveType.Move;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        if (positions.Count == 0 || ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }

        DOTween.Kill($"BehaveMove_{ctrlGO.GetInstanceID()}");

        sequence = DOTween.Sequence();
        {
            sequence.timeScale = multiplier;
            sequence.Append(ctrlGO.transform.DOLocalPath(positions.ToArray(), playTime * GlobalInfo.playTimeRatio).SetEase((Ease)ease)).OnComplete(() =>
            {
                callback?.Invoke();
            });
            sequence.SetId($"BehaveMove_{ctrlGO.GetInstanceID()}");
        }
    }

    public override void SetInitialState()
    {
        sequence.Kill();
        if (positions.Count == 0 || ctrlGO == null)
            return;
        ctrlGO.transform.localPosition = positions[0];
    }

    public override void SetFinalState()
    {
        sequence.Kill();
        if (positions.Count == 0 || ctrlGO == null)
            return;
        ctrlGO.transform.localPosition = positions.Last();
    }
}

/// <summary>
/// 旋转
/// </summary>
[Serializable]
public class BehaveRotate : BehaveDotween
{
    public List<Vector3> angles = new List<Vector3>();
    public float playTime = 1f;

    public BehaveRotate()
    {
        behaveType = BehaveType.Rotate;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        if (angles.Count == 0 || ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }

        DOTween.Kill($"BehaveRotate_{ctrlGO.GetInstanceID()}");

        sequence = DOTween.Sequence();
        {
            //for (int i = 1; i < angles.Count; i++)
            for (int i = 0; i < angles.Count; i++)
            {
                sequence.Append(ctrlGO.transform.DOLocalRotate(angles[i], playTime * GlobalInfo.playTimeRatio / angles.Count).SetEase((Ease)ease));
            }
            sequence.timeScale = multiplier;
            sequence.OnComplete(() =>
            {
                callback?.Invoke();
            });
            sequence.SetId($"BehaveRotate_{ctrlGO.GetInstanceID()}");
        }
    }

    public override void SetInitialState()
    {
        sequence.Kill();
        if (angles.Count == 0 || ctrlGO == null)
            return;
        ctrlGO.transform.localEulerAngles = angles[0];
    }
    public override void SetFinalState()
    {
        sequence.Kill();
        if (angles.Count == 0 || ctrlGO == null)
            return;
        ctrlGO.transform.localEulerAngles = angles.Last();
    }
}

/// <summary>
/// 缩放
/// </summary>
[Serializable]
public class BehaveScale : BehaveDotween
{
    public List<Vector3> scales = new List<Vector3>();
    public float playTime = 1f;

    public BehaveScale()
    {
        behaveType = BehaveType.Zoom;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        if (scales.Count == 0 || ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }

        DOTween.Kill($"BehaveScale_{ctrlGO.GetInstanceID()}");

        sequence = DOTween.Sequence();
        {
            for (int i = 1; i < scales.Count; i++)
            {
                sequence.Append(ctrlGO.transform.DOScale(scales[i], playTime * GlobalInfo.playTimeRatio / scales.Count).SetEase((Ease)ease));
            }
            sequence.timeScale = multiplier;
            sequence.OnComplete(() => callback?.Invoke());
            sequence.SetId($"BehaveScale_{ctrlGO.GetInstanceID()}");
        }
    }

    public override void SetInitialState()
    {
        sequence.Kill();
        if (scales.Count == 0 || ctrlGO == null)
            return;
        ctrlGO.transform.localScale = scales[0];
    }
    public override void SetFinalState()
    {
        sequence.Kill();
        if (scales.Count == 0 || ctrlGO == null)
            return;
        ctrlGO.transform.localScale = scales.Last();
    }
}

/// <summary>
/// 透明度
/// 这个类很特殊 只记录了对象和初始化行为 具体执行行为实际上是在客户端实现
/// </summary>
[Serializable]
public class BehaveAlpha : BehaveBase
{
    private Dictionary<Renderer, Material[]> materialSaves = new Dictionary<Renderer, Material[]>();
    public List<Renderer> targets = new List<Renderer>();

    public BehaveAlpha()
    {
        behaveType = BehaveType.Alpha;
    }

    public override void SetInitialState()
    {
        foreach (var materialSave in materialSaves)
        {
            if (materialSave.Key != null)
            {
                materialSave.Key.materials = materialSave.Value;
            }
        }
    }
    public override void SaveInitialState()
    {
        foreach (var target in targets)
        {
            if (target == null)
                continue;
            if (!materialSaves.ContainsKey(target))
            {
                materialSaves.Add(target, target.materials);
            }
        }
    }
}

/// <summary>
/// 显隐
/// </summary>
[Serializable]
public class BehaveActivate : BehaveBase
{
    public bool startActive;
    public bool isActive;

    public BehaveActivate()
    {
        behaveType = BehaveType.Activate;
    }
    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback); 
        if (ctrlGO != null)
            ctrlGO.SetActive(isActive);
        callback?.Invoke();
    }
    public override void SetInitialState()
    {
        base.SetInitialState();
        if (ctrlGO != null)
            ctrlGO.SetActive(startActive);
    }
    public override void SetFinalState()
    {
        base.SetFinalState();
        if (ctrlGO != null)
            ctrlGO.SetActive(isActive);
    }
}

/// <summary>
/// 显隐组
/// </summary>
[Serializable]
public class BehaveActivates : BehaveBase
{
    public bool startActive;
    public bool isActive;
    public List<GameObject> targets = new List<GameObject>();

    public BehaveActivates()
    {
        behaveType = BehaveType.Activates;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(isActive);
        }
        callback?.Invoke();
    }

    public override void SetInitialState()
    {
        base.SetInitialState();
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(startActive);
        }
    }
    public override void SetFinalState()
    {
        base.SetFinalState();
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] != null)
                targets[i].SetActive(isActive);
        }
    }
}

/// <summary>
/// 材质
/// </summary>
[Serializable]
public class BehaveMaterial : BehaveBase
{
    public List<Material> materials = new List<Material>();
    public bool isCtrlChild = true;
    /// <summary>
    /// 用于复原的记录值
    /// </summary>
    private Dictionary<MeshRenderer, Material[]> value = new Dictionary<MeshRenderer, Material[]>();

    public BehaveMaterial()
    {
        behaveType = BehaveType.Material;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        if (ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }
        //MeshRenderer[] renderer = ctrlGO.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer[] renderer = null;
        if (isCtrlChild)
            renderer = ctrlGO.GetComponentsInChildren<MeshRenderer>();
        else
            renderer = new MeshRenderer[] { ctrlGO.GetComponent<MeshRenderer>() };

        foreach (MeshRenderer mesh in renderer)
            mesh.materials = materials.ToArray();

        callback?.Invoke();
    }

    public override void SetInitialState()
    {
        foreach (var item in value)
        {
            item.Key.materials = item.Value;
        }
    }
    public override void SetFinalState()
    {
        base.SetFinalState();
        if (ctrlGO == null)
            return;
        //MeshRenderer[] renderer = ctrlGO.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer[] renderer = null;
        if (isCtrlChild)
            renderer = ctrlGO.GetComponentsInChildren<MeshRenderer>();
        else
            renderer = new MeshRenderer[] { ctrlGO.GetComponent<MeshRenderer>() };

        foreach (MeshRenderer mesh in renderer)
            mesh.materials = materials.ToArray();
    }
    public override void SaveInitialState()
    {
        if (ctrlGO == null)
            return;
        value = new Dictionary<MeshRenderer, Material[]>();
        if (isCtrlChild)
        {
            foreach (MeshRenderer mesh in ctrlGO.GetComponentsInChildren<MeshRenderer>())
            {
                value.Add(mesh, mesh.materials);
            }
        }
        else
        {
            MeshRenderer mesh = ctrlGO.GetComponent<MeshRenderer>();
            if (mesh) value.Add(mesh, mesh.materials);
        }
    }
}

/// <summary>
/// 材质组
/// </summary>
[Serializable]
public class BehaveMaterials : BehaveBase
{
    public List<Material> materials = new List<Material>();
    public List<GameObject> targets = new List<GameObject>();
    public bool isCtrlChild = true;
    /// <summary>
    /// 用于复原的记录值
    /// </summary>
    private Dictionary<MeshRenderer, Material[]> value = new Dictionary<MeshRenderer, Material[]>();

    public BehaveMaterials()
    {
        behaveType = BehaveType.Materials;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        foreach (var target in targets)
        {
            if (target == null)
                continue;
            if (isCtrlChild)
            {
                foreach (MeshRenderer mesh in target.GetComponentsInChildren<MeshRenderer>())
                {
                    mesh.materials = materials.ToArray();
                }
            }
            else
            {
                MeshRenderer mesh = target.GetComponent<MeshRenderer>();
                if (mesh) mesh.materials = materials.ToArray();
            }
        }

        callback?.Invoke();
    }
    public override void SetInitialState()
    {
        foreach (var item in value)
        {
            item.Key.materials = item.Value;
        }
    }
    public override void SetFinalState()
    {
        base.SetFinalState();

        foreach (var target in targets)
        {
            if (target == null)
                continue;
            if (isCtrlChild)
            {
                foreach (MeshRenderer mesh in target.GetComponentsInChildren<MeshRenderer>())
                {
                    mesh.materials = materials.ToArray();
                }
            }
            else
            {
                MeshRenderer mesh = target.GetComponent<MeshRenderer>();
                if (mesh) mesh.materials = materials.ToArray();
            }
        }
    }
    public override void SaveInitialState()
    {
        value = new Dictionary<MeshRenderer, Material[]>();

        foreach (var target in targets)
        {
            if (target == null)
                continue;
            if (isCtrlChild)
            {
                foreach (MeshRenderer mesh in target.GetComponentsInChildren<MeshRenderer>())
                {
                    value.Add(mesh, mesh.materials);
                }
            }
            else
            {
                MeshRenderer mesh = target.GetComponent<MeshRenderer>();
                if (mesh) value.Add(mesh, mesh.materials);
            }
        }
    }
}

/// <summary>
/// 音频
/// </summary>
[Serializable]
public class BehaveAudio : BehaveBase
{
    public AudioClip audioClip;
    public bool isPlay;
    public bool isLoop;

    public BehaveAudio()
    {
        behaveType = BehaveType.Audio;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        if (ctrlGO != null && ctrlGO.TryGetComponent(out AudioSource audioSource))
        {
            audioSource.volume = multiplier;
            audioSource.loop = isLoop;
            if (isPlay)
            {
                if (isLoop)
                {
                    if (audioClip != null)
                        audioSource.clip = audioClip;
                    audioSource.Play();
                }
                else
                {
                    if (audioClip != null)
                        audioSource.PlayOneShot(audioClip);
                    else
                        audioSource.Play();
                }
            }
            else
            {
                audioSource.Stop();
            }
        }

        callback?.Invoke();
    }

    public override void SetInitialState()
    {
        if (ctrlGO != null && ctrlGO.TryGetComponent(out AudioSource audioSource))
        {
            audioSource.volume = multiplier;
            audioSource.loop = isLoop;
            if (!isPlay)
            {
                if (isLoop)
                {
                    if (audioClip != null)
                        audioSource.clip = audioClip;
                    audioSource.Play();
                }
                else
                {
                    if (audioClip != null)
                        audioSource.PlayOneShot(audioClip);
                    else
                        audioSource.Play();
                }
            }
            else
            {
                audioSource.Stop();
            }
        }
    }
    public override void SetFinalState()
    {
        if (ctrlGO != null && ctrlGO.TryGetComponent(out AudioSource audioSource))
        {
            audioSource.volume = multiplier;
            audioSource.loop = isLoop;
            if (isPlay && isLoop)
            {
                if(audioClip != audioSource.clip)
                {
                    if (audioClip != null)
                    {
                        audioSource.clip = audioClip;
                    }
                }
                if (!audioSource.isPlaying)
                {
                    audioSource.Play();
                }
            }
            else
            {
                audioSource.Stop();
            }
        }
    }

    public override void SetMultiplier(float value)
    {
        base.SetMultiplier(value);
        if (ctrlGO != null && ctrlGO.TryGetComponent(out AudioSource audioSource))
        {
            audioSource.volume = value;
        }
    }
}

/// <summary>
/// 粒子
/// </summary>
[Serializable]
public class BehaveParticle : BehaveBase
{
    public bool isPlay;

    public BehaveParticle()
    {
        behaveType = BehaveType.Particle;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        if (ctrlGO != null)
        {
            Light light = ctrlGO.GetComponentInChildren<Light>();
            ParticleSystem[] particles = ctrlGO.GetComponentsInChildren<ParticleSystem>();
            if (particles.Length > 0)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    if (isPlay)
                    {
                        if (light && !light.gameObject.activeSelf)
                            light.gameObject.SetActive(true);

                        particles[i].Play();
                    }
                    else
                    {
                        particles[i].Stop();

                        if (light && light.gameObject.activeSelf)
                            light.gameObject.SetActive(false);
                    }
                }
            }
        }
        callback?.Invoke();
    }

    public override void SetInitialState()
    {
        if (ctrlGO == null)
            return;
        Light light = ctrlGO.GetComponentInChildren<Light>();
        ParticleSystem[] particles = ctrlGO.GetComponentsInChildren<ParticleSystem>();
        if (particles.Length > 0)
        {
            for (int k = 0; k < particles.Length; k++)
            {
                int index = k;
                if (!isPlay)
                {
                    if (light && !light.gameObject.activeSelf)
                        light.gameObject.SetActive(true);

                    particles[index].Play();
                }
                else
                {
                    particles[index].Stop();

                    if (light && light.gameObject.activeSelf)
                        light.gameObject.SetActive(false);
                }
            }
        }
    }

    public override void SetFinalState()
    {
        if (ctrlGO == null)
            return;
        Light light = ctrlGO.GetComponentInChildren<Light>();
        ParticleSystem[] particles = ctrlGO.GetComponentsInChildren<ParticleSystem>();
        if (particles.Length > 0)
        {
            for (int k = 0; k < particles.Length; k++)
            {
                int index = k;
                if (isPlay && particles[index].main.loop)
                {
                    if (light && !light.gameObject.activeSelf)
                        light.gameObject.SetActive(true);

                    particles[index].Play();
                }
                else
                {
                    particles[index].Stop();

                    if (light && light.gameObject.activeSelf)
                        light.gameObject.SetActive(false);
                }
            }
        }
    }
}

/// <summary>
/// TimeLine
/// </summary>
[Serializable]
public class BehaveAnimator : BehaveBase
{
    public AnimatorCtrl animatorCtrl;
    private UnityAction callback;

    public BehaveAnimator()
    {
        behaveType = BehaveType.Animator;
    }

    public override void Execute(UnityAction callback = null)
    {
        //正常播放
        if (GlobalInfo.playTimeRatio > 0)
        {
            //TODO 待确认是否需要
            this.callback = callback;
            base.Execute(callback);

            if (ctrlGO == null)
            {
                callback?.Invoke();
                return;
            }

            if (ctrlGO.TryGetComponent(out PlayableDirector target))
            {
                switch (animatorCtrl)
                {
                    case AnimatorCtrl.Play:
                        target.time = 0;
                        target.Play();
                        if (target.playableGraph.IsValid())
                        {
                            target.playableGraph.GetRootPlayable(0).SetSpeed(multiplier * GlobalInfo.playTimeRatio);
                        }
                        break;
                    case AnimatorCtrl.Pause:
                        target.Pause();
                        break;
                    case AnimatorCtrl.Stop:
                        target.Stop();
                        break;
                }

                target.stopped += ExecuteCallBack;
            }
        }
        else//跳过播放过程
        {
            if (ctrlGO != null && ctrlGO.TryGetComponent(out PlayableDirector target))
            {
                target.time = target.duration;
                target.Evaluate();
            }

            callback?.Invoke();
        }
    }

    public void ExecuteCallBack(PlayableDirector playableDirector)
    {
        //执行回调之前先取消注册事件，避免回调中调用SetFinalState, playableDirector.Stop()会再次触发stopped
        playableDirector.stopped -= ExecuteCallBack;
        callback?.Invoke();
    }

    public override void SetInitialState()
    {
        if (ctrlGO == null)
            return;
        var playableDirector = ctrlGO.GetComponent<PlayableDirector>();
        {
            playableDirector.time = 0;
            playableDirector.Evaluate();
            playableDirector.Stop();
        }
    }
    public override void SetFinalState()
    {
        if (ctrlGO == null)
            return;
        var playableDirector = ctrlGO.GetComponent<PlayableDirector>();
        {
            playableDirector.time = playableDirector.duration;
            playableDirector.Evaluate();
            playableDirector.Stop();
        }
    }

    public override void SetMultiplier(float value)
    {
        base.SetMultiplier(value);

        if (ctrlGO != null && ctrlGO.TryGetComponent(out PlayableDirector target))
        {
            if (target != null && target.playableGraph.IsValid())
            {
                target.playableGraph.GetRootPlayable(0).SetSpeed(multiplier * GlobalInfo.playTimeRatio);
            }
        }
    }
}

/// <summary>
/// Animator
/// </summary>
[Serializable]
public class BehaveAnimatorState : BehaveBase
{
    public string stateName;
    /// <summary>
    /// 状态过渡时间
    /// </summary>
    public float transitionTime;

    public BehaveAnimatorState()
    {
        behaveType = BehaveType.AnimatorState;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        //TODO 待测试
        if (ctrlGO != null && ctrlGO.TryGetComponent(out Animator target))
        {
            target.CrossFadeInFixedTime(stateName, transitionTime);
            callback?.Invoke();
        }
        else
        {
            callback?.Invoke();
            Debug.LogError($"配置错误 {ctrlGO}不包含Animator");
        }
    }

    public override void SetFinalState()
    {
        if (ctrlGO != null && ctrlGO.TryGetComponent(out Animator target))
        {
            bool active = ctrlGO.activeSelf;
            ctrlGO.SetActive(true);
            target.CrossFadeInFixedTime(stateName, 0);
            ctrlGO.SetActive(active);
        }
    }
}

/// <summary>
/// Animator
/// </summary>
[Serializable]
public class BehaveAnimator_Anime : BehaveBase
{
    public string stateName;
    public bool ctrl;
    ///// <summary>
    ///// 淡入淡出
    ///// </summary>
    //public bool crossFade;

    public BehaveAnimator_Anime()
    {
        behaveType = BehaveType.Animator_Anime;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        //TODO 待测试
        if (ctrlGO != null && ctrlGO.TryGetComponent(out Animator target))
        {
            target.enabled = ctrl;
            //正常播放
            if (GlobalInfo.playTimeRatio > 0)
            {
                if (useCallBack)
                {
                    DOTween.Sequence().AppendCallback(() =>
                    {
                        if (true/*crossFade*/)
                        {
                            target.CrossFadeInFixedTime(stateName, 0.5f, 0, 0);
                        }
                        else
                        {
                            target.Play(stateName, 0, 0);
                        }
                        target.speed = GlobalInfo.playTimeRatio;
                    }).AppendInterval(GetAnimationClipLength(target, stateName) + 0.5f /*+ (crossFade ? 5f : 0f)*/)
                      .AppendCallback(() =>
                      {
                          callback?.Invoke();
                      });
                }
                else
                {
                    target.CrossFadeInFixedTime(stateName, 0.5f, 0, 0);
                    callback?.Invoke();
                }
            }
            else
            {
                //跳过播放过程
                target.Play(stateName, 0, 1);
                callback?.Invoke();
            }
        }
        else
        {
            callback?.Invoke();
            Debug.LogError($"配置错误 {ctrlGO}不包含Animator");
        }

        ////TODO 待删除
        ////if (ctrlGO.TryGetComponent(out Animator target))
        ////{
        ////    target.enabled = ctrl;
        ////    target.Play(stateName, 0, 0);
        ////}
        ////else
        ////{
        ////    Debug.LogError($"配置错误 {ctrlGO}不包含Animator");
        ////}
        //callback?.Invoke();
    }

    public override void SetFinalState()
    {
        if (ctrlGO != null && ctrlGO.TryGetComponent(out Animator target))
        {
            bool active = ctrlGO.activeSelf;
            ctrlGO.SetActive(true);
            target.enabled = ctrl;
            target.Play(stateName, 0, 1);
            ctrlGO.SetActive(active);
        }
    }

    private float GetAnimationClipLength(Animator target, string animName)
    {
        RuntimeAnimatorController ac = target.runtimeAnimatorController;
        foreach (AnimationClip clip in ac.animationClips)
        {
            if (clip.name == animName)
            {
                return clip.length;
            }
        }
        //Take 001
        if (ac.animationClips.Length > 0)
        {
            return ac.animationClips[0].length;
        }
        return 0f;
    }
}

/// <summary>
/// 监控 联动试图
/// </summary>
[Serializable]
public class BehaveMonitor : BehaveDotween
{
    public Camera monitorCamera;
    private RenderTexture renderTexture;

    /// <summary>
    /// 显示时长
    /// </summary>
    public float stayTime = 3f;

    public BehaveMonitor()
    {
        behaveType = BehaveType.Monitor;
    }

    public override void Execute(UnityAction callback = null)
    {
        UISmallSceneModule uISmallSceneModule = UnityEngine.Object.FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>();
        if(uISmallSceneModule != null)
            uISmallSceneModule.CameraView.gameObject.SetActive(true);

        renderTexture = new RenderTexture(456, 404, 0, RenderTextureFormat.ARGB32);
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.Create();

        monitorCamera = new GameObject("MonitorCamera").AutoComponent<Camera>();
        monitorCamera.transform.position = ctrlGO.transform.position;
        monitorCamera.transform.eulerAngles = ctrlGO.transform.eulerAngles;
        monitorCamera.targetTexture = renderTexture;
        Transform.FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>().CameraView.GetComponentInChildren<RawImage>().texture = renderTexture;
        monitorCamera.cullingMask = ~(1 << 2 | 1 << 5 | 1 << 7);
        monitorCamera.backgroundColor = "#505C73".HexToColor();
        monitorCamera.clearFlags = CameraClearFlags.SolidColor;
        monitorCamera.nearClipPlane = 0.01f;
        //两边的ID不方便同步
        FormMsgManager.Instance.SendMsg(new MsgBehaveEvent()
        {
            msgId = ushort.MaxValue,
            arg = this,
            behaveTrans = monitorCamera.transform
        });
        callback?.Invoke();

        sequence = DOTween.Sequence();
        sequence.AppendInterval(stayTime * GlobalInfo.playTimeRatio);
        sequence.OnComplete(() =>
        {
            //关闭监控
            UnityEngine.Object.FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>().CameraView.gameObject.SetActive(false);
            this.SetInitialState();
            FormMsgManager.Instance.SendMsg(new MsgTransform()
            {
                msgId = (ushort)SmallFlowModuleEvent.HideMonitor,
                arg = monitorCamera != null ? monitorCamera.transform : null
            });
        });
    }

    public override void SetInitialState()
    {
        //尝试修复  rendertexture  报错
        if (monitorCamera)
        {
            monitorCamera.targetTexture = null;
            UnityEngine.Object.Destroy(monitorCamera.gameObject);
        }
        base.SetInitialState();
        if (renderTexture)
        {
            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
        }
    }

    public override void SetFinalState()
    {
        base.SetFinalState();
        sequence?.Kill(true);
    }
}

/// <summary>
/// 观察
/// </summary>
[Serializable]
public class BehaveObserve : BehaveDotween
{
    /// <summary>
    /// 动画时间
    /// </summary>
    public float time;
    /// <summary>
    /// 观察停留时长
    /// </summary>
    public float stayTime = 2f;

    private Vector3 position;
    private Vector3 angle;

    public BehaveObserve()
    {
        behaveType = BehaveType.Observe;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        if (ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }
        DOTween.Kill("BehaveObserve");
        

        //观察开始后语音才变更到过程提示语音，需要等待一下避免计时错误读取步骤开始语音
        DOVirtual.DelayedCall(0.1f, () =>
        {
            
            AddSequence(callback);
        });
    }

    void AddSequence(UnityAction callback = null)
    {
        sequence = DOTween.Sequence();
        var camera = Camera.main.transform;
        position = Camera.main.transform.position;
        angle = Camera.main.transform.eulerAngles;

        sequence.Append(camera.DOMove(ctrlGO.transform.position, time * GlobalInfo.playTimeRatio).SetEase((Ease)ease));
        sequence.Join(camera.DORotate(ctrlGO.transform.eulerAngles, time * GlobalInfo.playTimeRatio).SetEase((Ease)ease));
        {
            //将等待时间和语音播放关联起来
            float waitTime = stayTime * GlobalInfo.playTimeRatio;
            float audioLength = 0;
            if (SpeechManager.Instance.SpeechMode && SpeechManager.Instance.audioSource != null && SpeechManager.Instance.audioSource.clip != null)
            {
                audioLength = SpeechManager.Instance.audioSource.clip.length - SpeechManager.Instance.audioSource.time + 1;
            }
            waitTime = Mathf.Max(stayTime, audioLength);
            sequence.AppendInterval(waitTime);
            Log.Debug("等待时间" + waitTime);
        }

        sequence.OnComplete(() => callback?.Invoke());
        sequence.timeScale = multiplier;
        sequence.SetId("BehaveObserve");
    }

    public override void SetInitialState()
    {
        base.SetInitialState();
        sequence.Kill();
        Camera.main.transform.position = position;
        Camera.main.transform.eulerAngles = angle;
    }
    public override void SetFinalState()
    {
        base.SetFinalState();
        sequence.Kill();
        Camera.main.transform.position = position;
        Camera.main.transform.eulerAngles = angle;
        
    }

    public override void SaveInitialState()
    {
        base.SaveInitialState();
        position = Camera.main.transform.position;
        angle = Camera.main.transform.eulerAngles;
    }
}

/// <summary>
/// 吸附
/// </summary>
[Serializable]
public class BehaveAdsorb : BehaveDotween
{
    public float playTime;

    public BehaveAdsorb()
    {
        behaveType = BehaveType.Adsorb;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        callback?.Invoke();
    }

    public override void SetInitialState()
    {
    }

    public override void SetFinalState()
    {
    }
}

/// <summary>
/// 设置姿态
/// </summary>
[Serializable]
public class BehavePose : BehaveBase
{
    public Vector3 position;
    public Vector3 angle;

    private Vector3 initPosition;
    private Vector3 initEuler;

    public BehavePose()
    {
        behaveType = BehaveType.Pose;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        if (ctrlGO != null)
        {
            ctrlGO.transform.localPosition = position;
            ctrlGO.transform.localEulerAngles = angle;
        }
        callback?.Invoke();
    }

    public override void SetInitialState()
    {
        if (ctrlGO == null)
            return;
        ctrlGO.transform.localPosition = initPosition;
        ctrlGO.transform.localEulerAngles = initEuler;
    }

    public override void SetFinalState()
    {
        Execute();
    }

    public override void SaveInitialState()
    {
        base.SaveInitialState(); 
        if (ctrlGO == null)
            return;
        initPosition = ctrlGO.transform.localPosition;
        initEuler = ctrlGO.transform.localEulerAngles;
    }
}

public enum Axis
{
    X,
    Y,
    Z
}

/// <summary>
/// 所有子物体沿局部坐标轴移动
/// </summary>
[Serializable]
public class BehaveMoveAxis : BehaveDotween
{
    public Axis axis;
    public float distance;
    public float playTime = 1f;

    private List<Vector3> initPosition;

    public BehaveMoveAxis()
    {
        behaveType = BehaveType.MoveAxis;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        if (ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }

        DOTween.Kill($"BehaveMoveAxis_{ctrlGO.GetInstanceID()}");

        sequence = DOTween.Sequence();
        foreach (Transform t in ctrlGO.transform)
        {
            switch (axis)
            {
                case Axis.X:
                    sequence.Join(t.DOMove(t.transform.position + distance * t.right, playTime * GlobalInfo.playTimeRatio));
                    break;
                case Axis.Y:
                    sequence.Join(t.DOMove(t.transform.position + distance * t.up, playTime * GlobalInfo.playTimeRatio));
                    break;
                case Axis.Z:
                    sequence.Join(t.DOMove(t.transform.position + distance * t.forward, playTime * GlobalInfo.playTimeRatio));
                    break;
            }
        }
        sequence.timeScale = multiplier;
        sequence.OnComplete(() => callback?.Invoke());
        sequence.SetId($"BehaveMoveAxis_{ctrlGO.GetInstanceID()}");
    }

    public override void SetInitialState()
    {
        base.SetInitialState();
        sequence.Kill();
        if (ctrlGO == null || initPosition == null)
            return;
        for (int i = 0; i < ctrlGO.transform.childCount; i++)
        {
            ctrlGO.transform.GetChild(i).position = initPosition[i];
        }
    }

    public override void SetFinalState()
    {
        base.SetFinalState();
        sequence.Kill();
        if (ctrlGO == null)
            return;
        Transform child;
        for (int i = 0; i < ctrlGO.transform.childCount; i++)
        {
            child = ctrlGO.transform.GetChild(i);
            switch (axis)
            {
                case Axis.X:
                    child.position = (initPosition == null ? child.position : initPosition[i]) + child.right * distance;
                    break;
                case Axis.Y:
                    child.position = (initPosition == null ? child.position : initPosition[i]) + child.up * distance;
                    break;
                case Axis.Z:
                    child.position = (initPosition == null ? child.position : initPosition[i]) + child.forward * distance;
                    break;
            }
        }
    }

    public override void SaveInitialState()
    {
        base.SaveInitialState();
        if (initPosition == null && ctrlGO != null)
        {
            initPosition = new List<Vector3>(ctrlGO.transform.childCount);
            for (int i = 0; i < ctrlGO.transform.childCount; i++)
            {
                initPosition.Add(ctrlGO.transform.GetChild(i).position);
            }
        }
    }
}


/// <summary>
/// 所有子物体沿局部坐标轴旋转指定圈数
/// </summary>
[Serializable]
public class BehaveRotateAxis : BehaveDotween
{
    public Axis axis;
    public float loop;
    public float playTime = 1f;

    public BehaveRotateAxis()
    {
        behaveType = BehaveType.RotateAxis;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        if (ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }

        DOTween.Kill($"BehaveRotateAxis_{ctrlGO.GetInstanceID()}");

        sequence = DOTween.Sequence();
        Vector3 vec = Vector3.one;
        foreach (Transform t in ctrlGO.transform)
        {
            switch (axis)
            {
                case Axis.X:
                    vec = 360f * loop * Vector3.right;
                    break;
                case Axis.Y:
                    vec = 360f * loop * Vector3.up;
                    break;
                case Axis.Z:
                    vec = 360f * loop * Vector3.forward;
                    break;
            }
        }
        for (int i = 0; i < ctrlGO.transform.childCount; i++)
        {
            sequence.Join(ctrlGO.transform.GetChild(i).DOLocalRotate(vec, playTime * GlobalInfo.playTimeRatio, RotateMode.LocalAxisAdd)).SetEase(Ease.Linear);
        }
        sequence.timeScale = multiplier;
        sequence.OnComplete(() => callback?.Invoke());
        sequence.SetId($"BehaveRotateAxis_{ctrlGO.GetInstanceID()}");
    }

    public override void SetInitialState()
    {
        sequence.Kill();
    }

    public override void SetFinalState()
    {
        sequence.Kill();
    }
}

/// <summary>
/// 控制灯光
/// </summary>
[Serializable]
public class BehaveLight : BehaveBase
{
    public Vector3 position;
    public Vector3 angle;

    public BehaveLight()
    {
        behaveType = BehaveType.Light;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        if (ModelManager.Instance.sceneLight != null)
        {
            ModelManager.Instance.sceneLight.transform.position = position;
            ModelManager.Instance.sceneLight.transform.eulerAngles = angle;
        }
        callback?.Invoke();
    }

    public override void SetInitialState()
    {
        ModelManager.Instance.ResetSceneLight();
    }

    public override void SetFinalState()
    {
        Execute();
    }
}

/// <summary>
/// 控制操作道具状态
/// </summary>
[Serializable]
public class BehaveState : BehaveBase
{
    public string behaveState;

    public BehaveState()
    {
        behaveType = BehaveType.State;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        if (ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }
        ModelOperation modelOperation = ctrlGO.GetComponent<ModelOperation>();
        ModelInfo modelInfo = ctrlGO.GetComponent<ModelInfo>();
        if (modelOperation == null || modelInfo == null)
        {
            callback?.Invoke();
            return;
        }
        //修改状态，不执行表现
        modelOperation.currentState = behaveState;
        switch (modelInfo.InfoData.InteractMode)
        {
            //2D操作修改UI显示
            case InteractMode.Menu2D:
                Dropdown dropdown = modelInfo.GetComponentInChildren<Dropdown>();
                dropdown.SetValueWithoutNotify(dropdown.options.FindIndex(o => o.text.Equals(modelOperation.currentState)));
                break;
            default:
                break;
        }
        callback?.Invoke();
    }

    public override void SetInitialState()
    {

    }

    public override void SetFinalState()
    {
        Execute();
    }
}

/// <summary>
/// 角色漫游 相机跟随
/// </summary>
[Serializable]
public class BehaveCameraFollow : BehaveDotween
{
    /// <summary>
    /// 切换时长
    /// </summary>
    public float duration;

    public BehaveCameraFollow()
    {
        behaveType = BehaveType.CameraFollow;
        useCallBack = true;
    }

    public override void Execute(UnityAction callback = null)
    {

        base.Execute(callback);

        var camera = Camera.main.transform;
        {
            DOTween.Kill("BehaveMoveCamera", true);

            
            sequence = DOTween.Sequence();
            sequence.Append(camera.DOMove(ctrlGO.transform.position, duration * GlobalInfo.playTimeRatio).SetEase((Ease)ease));
            sequence.Join(camera.DORotate(ctrlGO.transform.eulerAngles, duration * GlobalInfo.playTimeRatio).SetEase((Ease)ease));

            sequence.timeScale = multiplier;
            sequence.OnComplete(() =>
            {
                
                callback?.Invoke();
            });
            sequence.SetId("BehaveMoveCamera");
        }
    }
}

/// <summary>
/// 角色移动
/// </summary>
[Serializable]
public class BehaveMovePlayer : BehaveDotween
{
    /// <summary>
    /// 设置为最终状态时是否执行动画过程，默认false
    /// </summary>
    public bool tween;
    public List<Vector3> positions = new List<Vector3>();
    public List<Vector3> eulerAngles = new List<Vector3>();
    public List<float> playTimes = new List<float>();

    public bool constantSpeed;
    public float speed = 1f;

    public BehaveMovePlayer()
    {
        behaveType = BehaveType.MovePlayer;
        useCallBack = true;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        PlayerController playerController = ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>();
        if (playerController == null)
        {
            callback?.Invoke();
            return;
        }

        DOTween.Kill("BehaveMovePlayer", true);

        playerController.Model.GetComponent<Animator>().SetBool("isMove", true);
        sequence = DOTween.Sequence();
        {
            float time = 0;
            Vector3 target;
            float targetAngle;
            float angleDiff = 0;

            for (int i = 0; i < positions.Count; i++)
            {
                //if (constantSpeed)
                {
                    target = positions[i];

                    if (speed <= 0) speed = 1;
                    if (i == 0)
                    {
                        //移除navmesh网格yoffset的影响
                        time = Vector3.Distance(new Vector3(playerController.transform.position.x, positions[i].y, playerController.transform.position.z), positions[i]) / speed;
                        targetAngle = Quaternion.LookRotation(target - playerController.transform.position, Vector3.up).eulerAngles.y;
                        angleDiff = targetAngle - playerController.transform.eulerAngles.y;
                    }
                    else
                    {
                        time = Vector3.Distance(positions[i - 1], positions[i]) / speed;
                        targetAngle = Quaternion.LookRotation(target - positions[i - 1], Vector3.up).eulerAngles.y;
                        angleDiff = targetAngle - eulerAngles[i - 1].y;
                    }

                    if (time > 0.01f)//todo
                    {
                        sequence.Append(playerController.transform.DOLookAt(target, angleDiff.NormalizedAngle180() / 90f * GlobalInfo.playTimeRatio, AxisConstraint.Y).SetEase(Ease.Linear));
                        sequence.Append(playerController.transform.DOMove(positions[i], time * GlobalInfo.playTimeRatio).SetEase(Ease.Linear));
                        angleDiff = eulerAngles[i].y - targetAngle;
                    }
                    else
                    {
                        if (i == 0)
                            angleDiff = eulerAngles[i].y - playerController.transform.eulerAngles.y;
                        else
                            angleDiff = eulerAngles[i].y - eulerAngles[i - 1].y;
                    }
                    sequence.Append(playerController.transform.DORotate(eulerAngles[i], angleDiff.NormalizedAngle180() / 90f * GlobalInfo.playTimeRatio).SetEase(Ease.Linear));
                }
            }
        }
        //wait camerarotatetweener
        sequence.AppendInterval(playerController.cameraRotateDuration);
        sequence.timeScale = multiplier;
        sequence.OnComplete(() =>
        {
            playerController.Model.GetComponent<Animator>().SetBool("isMove", false);
            callback?.Invoke();
        });
        sequence.SetId("BehaveMovePlayer");
    }

    public override void SetFinalState()
    {
        sequence.Kill();

        PlayerController playerController = ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>();
        if (playerController == null)
            return;

        if (tween)
        {
            Execute();
        }
        else
        {
            if (positions.Count != 0)
            {
                playerController.transform.position = positions[positions.Count - 1];
                playerController.transform.eulerAngles = eulerAngles[positions.Count - 1];
            }
        }
    }
}

/// <summary>
/// 延迟
/// </summary>
[Serializable]
public class BehaveDelay : BehaveDotween
{
    /// <summary>
    /// 时长
    /// </summary>
    public float duration;

    public BehaveDelay()
    {
        behaveType = BehaveType.Delay;
        useCallBack = true;
    }

    public override void Execute(UnityAction callback = null)
    {
        sequence = DOTween.Sequence();
        {
            sequence.AppendInterval(duration);
        }
        sequence.timeScale = multiplier;
        sequence.OnComplete(() =>
        {
            callback?.Invoke();
        });
        sequence.SetId("BehaveDelay");
    }

    public override void SetFinalState()
    {
        base.SetFinalState();
        sequence.Kill();
    }
}

/// <summary>
/// 围绕观察
/// </summary>
[Serializable]
public class BehaveObserveRotate : BehaveDotween
{
    /// <summary>
    /// 动画时间
    /// </summary>
    public float time;
    /// <summary>
    /// 观察停留时长
    /// </summary>
    public float stayTime;
    /// <summary>
    /// 长轴半径
    /// </summary>
    public float aRadius;
    /// <summary>
    /// 短轴半径
    /// </summary>
    public float bRadius;
    /// <summary>
    /// 旋转角度 默认360度
    /// </summary>
    public float angle = 360f;
    /// <summary>
    /// 旋转初始角度
    /// </summary>
    public float startAngleOffset;
    /// <summary>
    /// y轴偏移量
    /// </summary>
    public float yOffset;
    /// <summary>
    /// 俯仰角
    /// </summary>
    public float pitch;

    public BehaveObserveRotate()
    {
        behaveType = BehaveType.ObserveRotate;
        useCallBack = true;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        if (ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }
        var camera = Camera.main.transform;
        {
            DOTween.Kill("BehaveObserveRotate");

            
            sequence = DOTween.Sequence();
            {
                Vector3 pivotPos = ctrlGO.transform.position;
                float startAngle = Quaternion.FromToRotation(ctrlGO.transform.right, Vector3.right).eulerAngles.y + 90f + startAngleOffset;
                Vector3 startPosition = new Vector3(pivotPos.x + aRadius * Mathf.Cos(startAngle * Mathf.Deg2Rad), pivotPos.y + yOffset, pivotPos.z + bRadius * Mathf.Sin(startAngle * Mathf.Deg2Rad));
                Vector3 startEuler = Quaternion.LookRotation(ctrlGO.transform.position - startPosition, Vector3.up).eulerAngles;

                sequence.Append(camera.DOMove(startPosition, time));
                sequence.Join(camera.DORotate(new Vector3(pitch, startEuler.y, startEuler.z), time));
                sequence.Append(DOTween.To(() => startAngle, x => startAngle = x, startAngle + angle, stayTime).SetEase(Ease.Linear)
                    .OnUpdate(() =>
                    {
                        float radian = startAngle * Mathf.Deg2Rad;
                        float x = pivotPos.x + aRadius * Mathf.Cos(radian);
                        float z = pivotPos.z + bRadius * Mathf.Sin(radian);
                        camera.position = new Vector3(x, camera.position.y, z);
                        camera.LookAt(ctrlGO.transform);
                        camera.eulerAngles = new Vector3(/*startEuler.x*/pitch, camera.eulerAngles.y, startEuler.z);
                    }));

                sequence.AppendCallback(() =>
                {
                    
                    callback?.Invoke();
                });
            }
            sequence.timeScale = multiplier;
            sequence.SetId("BehaveObserveRotate");
        }
    }
    public override void SetInitialState()
    {
        base.SetInitialState();
        sequence.Kill();
    }
    public override void SetFinalState()
    {
        base.SetFinalState();
        sequence.Kill();
        
    }
}

/// <summary>
/// 角色移动
/// </summary>
[Serializable]
public class BehavePlayerNavigation : BehaveDotween
{
    /// <summary>
    /// 最小的需要导航的距离
    /// </summary>
    public float minMoveDictance = 1;

    public class MonoStub : MonoBehaviour { }

    public BehavePlayerNavigation()
    {
        behaveType = BehaveType.PlayerNavigation;
        useCallBack = true;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        //标注 待增加自由模式下 取消自动导航到下一个点位
        if (ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }

        PlayerController playerController = ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>();
        if (playerController == null)
        {
            callback?.Invoke();
            return;
        }

        //联机模式中有时ctrlGO会再开始协程后变成false导致协程执行报错，改为异步执行
        playerController.Model.GetComponent<Animator>().SetBool("isMove", true);
        StartNavigationAsync(playerController, callback).Forget();
    }

    private async UniTask StartNavigationAsync(PlayerController playerController, UnityAction callback)
    {
        if (Vector3.Distance(ctrlGO.transform.position, playerController.transform.position) > minMoveDictance)
        {
            playerController.StartNavigation(ctrlGO.transform);

            // 等待导航完成 避免退出没有停住
            await UniTask.WaitUntil(() => !playerController || playerController.NavPathComplete);

            playerController.Model.GetComponent<Animator>().SetBool("isMove", false);
            float angleDiff = ctrlGO.transform.eulerAngles.y - playerController.transform.eulerAngles.y;
            playerController.EndNavigation(ctrlGO.transform, angleDiff.NormalizedAngle180() / 90f * GlobalInfo.playTimeRatio);

            // 等待导航结束
            await UniTask.WaitUntil(() => playerController.NavEnd);

            // 等待相机旋转时间
            await UniTask.WaitForSeconds(playerController.cameraRotateDuration);
        }
        else
        {
            playerController.Model.GetComponent<Animator>().SetBool("isMove", false);
        }

        callback?.Invoke();
    }

    public override void SetFinalState()
    {
        if (GlobalInfo.SetCerrenstate)
            return;

        sequence.Kill();
        if (ctrlGO == null)
            return;
        PlayerController playerController = ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>();
        if (playerController == null)
            return;
        bool agentEnabled = playerController.Agent.enabled;
        playerController.Agent.enabled = false;
        playerController.transform.position = ctrlGO.transform.position;
        playerController.transform.eulerAngles = ctrlGO.transform.eulerAngles;
        playerController.Agent.enabled = agentEnabled;
    }
}

/// <summary>
/// 聚焦
/// </summary>
[Serializable]
public class BehaveFocus : BehaveDotween
{
    public BehaveFocus()
    {
        behaveType = BehaveType.Focus;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);

        if(ctrlGO == null)
        {
            callback?.Invoke();
            return;
        }

        //todo
        float time = Mathf.Clamp(Mathf.Max(Mathf.Pow(Vector3.Distance(Camera.main.transform.position, ctrlGO.transform.position) / 0.5f, 3),
            Mathf.Abs(Vector3.Angle(Camera.main.transform.eulerAngles, ctrlGO.transform.eulerAngles) / 90f)), 0.5f, 1.5f);

        var camera = Camera.main.transform;
        {
            DOTween.Kill("BehaveFocus");
            

            sequence = DOTween.Sequence();
            {
                sequence.Append(camera.DOMove(ctrlGO.transform.position, time * GlobalInfo.playTimeRatio).SetEase((Ease)ease));
                sequence.Join(camera.DORotate(ctrlGO.transform.eulerAngles, time * GlobalInfo.playTimeRatio).SetEase((Ease)ease)).OnComplete(() =>
                {
                    callback?.Invoke();
                });
            }
            sequence.timeScale = multiplier;
            sequence.SetId("BehaveFocus");
        }
    }

    public override void SetInitialState()
    {
        base.SetInitialState();
        sequence.Kill();
    }

    public override void SetFinalState()
    {
        base.SetFinalState();
        sequence.Kill();

        
    }
}

/// <summary>
/// 系数
/// </summary>
[Serializable]
public class BehaveMultiplier : BehaveBase
{
    public string operationName;

    public float value;

    public BehaveMultiplier()
    {
        behaveType = BehaveType.Multiplier;
    }

    public override void Execute(UnityAction callback = null)
    {
        base.Execute(callback);
        ChangeMultiplier(ctrlGO, value, callback);
    }

    private void ChangeMultiplier(GameObject go, float value, UnityAction callback)
    {
        if (go == null)
        {
            callback?.Invoke();
            return;
        }

        if (go.TryGetComponent(out ModelOperation modelOperation))
        {
            OperationBase operation = modelOperation.operations.Find(o => o.name.Equals(operationName));
            if (operation != null && operation.behaveBases != null)
            {
                foreach (BehaveBase behave in operation.behaveBases)
                {
                    behave.SetMultiplier(value);
                }
            }
        }
        callback?.Invoke();
    }

    public override void SetInitialState()
    {
        //todo
        //ChangeMultiplier(ctrlGO, 1, null);
    }

    public override void SetFinalState()
    {
        Execute();
    }
}

/// <summary>
/// 弹窗
/// </summary>
[Serializable]
public class BehavePopup : BehaveBase
{
    public string message;

    public BehavePopup()
    {
        behaveType = BehaveType.Popup;
    }

    //弹窗默认将确定和关闭事件都加上回调
    public override void Execute(UnityAction callback = null)
    {
        Dictionary<string, PopupButtonData> popupData = new Dictionary<string, PopupButtonData>();
        popupData.Add("确定", new PopupButtonData(callback, true));
        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", message, popupData, callback));
    }

    public override void SetInitialState()
    {
        base.SetInitialState();
    }
}

#region Enum
/// <summary>
/// 操作表现类型
/// </summary>
[Serializable]
public enum BehaveType
{
    /// <summary>
    /// 移动
    /// </summary>
    Move,
    /// <summary>
    /// 旋转
    /// </summary>
    Rotate,
    /// <summary>
    /// 缩放
    /// </summary>
    Zoom,
    /// <summary>
    /// 改变显隐状态
    /// </summary>
    Activate,
    /// <summary>
    /// 改变材质
    /// </summary>
    Material,
    /// <summary>
    /// 音频
    /// </summary>
    Audio,
    /// <summary>
    /// 粒子特效
    /// </summary>
    Particle,
    /// <summary>
    /// TimeLine
    /// </summary>
    Animator,
    /// <summary>
    /// 居中
    /// </summary>
    Center,
    /// <summary>
    /// 移动镜头
    /// </summary>
    MoveCamera,
    /// <summary>
    /// 缩放镜头
    /// </summary>
    ZoomCamera,
    /// <summary>
    /// 改变显隐状态组
    /// </summary>
    Activates,
    /// <summary>
    /// 改变材质组
    /// </summary>
    Materials,
    /// <summary>
    /// 改变透明度
    /// </summary>
    Alpha,
    /// <summary>
    /// 监控
    /// </summary>
    Monitor = 17,
    /// <summary>
    /// 观察
    /// </summary>
    Observe,
    /// <summary>
    /// Animator
    /// </summary>
    Animator_Anime,
    /// <summary>
    /// 吸附
    /// </summary>
    Adsorb,
    /// <summary>
    /// 改变姿态
    /// </summary>
    Pose,
    /// <summary>
    /// 沿轴移动
    /// </summary>
    MoveAxis,
    /// <summary>
    /// 沿轴旋转
    /// </summary>
    RotateAxis,
    /// <summary>
    /// 控制灯光
    /// </summary>
    Light,
    /// <summary>
    /// 控制操作道具状态
    /// </summary>
    State,
    /// <summary>
    /// 控制动画状态
    /// </summary>
    AnimatorState,
    /// <summary>
    /// 相机跟随
    /// </summary>
    CameraFollow,
    /// <summary>
    /// 角色移动
    /// </summary>
    MovePlayer,
    /// <summary>
    /// 延迟
    /// </summary>
    Delay,
    /// <summary>
    /// 围绕观察
    /// </summary>
    ObserveRotate,
    /// <summary>
    /// 角色寻路
    /// </summary>
    PlayerNavigation,
    /// <summary>
    /// 聚焦
    /// </summary>
    Focus,
    /// <summary>
    /// 修改表现系数
    /// </summary>
    Multiplier,
    /// <summary>
    /// 弹窗
    /// </summary>
    Popup,
    /// <summary>
    /// 自定义脚本
    /// </summary>
    CustomScript,
    /// <summary>
    /// 测量温度
    /// </summary>
    Thermometring,
}

/// <summary>
/// 动画播放控制
/// </summary>
public enum AnimatorCtrl
{
    [Tooltip("播放")]
    Play,
    [Tooltip("暂停")]
    Pause,
    [Tooltip("停止")]
    Stop
}
/// <summary>
/// 动画播放控制
/// </summary>
public enum AnimatorCtrl_Anime
{
    [Tooltip("播放")]
    Play,
    [Tooltip("暂停")]
    Pause,
    [Tooltip("继续")]
    RePlay,
    [Tooltip("停止")]
    Stop
}
[Serializable]
public enum EaseType
{
    Unset = Ease.Unset,
    OutSine = Ease.OutSine,
    OutQuint = Ease.OutQuint,
    OutQuart = Ease.OutQuart,
    OutQuad = Ease.OutQuad,
    OutFlash = Ease.OutFlash,
    OutExpo = Ease.OutExpo,
    OutElastic = Ease.OutElastic,
    OutCubic = Ease.OutCubic,
    OutCirc = Ease.OutCirc,
    OutBounce = Ease.OutBounce,
    OutBack = Ease.OutBack,
    Linear = Ease.Linear,
    INTERNAL_Zero = Ease.INTERNAL_Zero,
    INTERNAL_Custom = Ease.INTERNAL_Custom,
    InSine = Ease.InSine,
    InQuint = Ease.InQuint,
    InQuart = Ease.InQuart,
    InQuad = Ease.InQuad,
    InOutSine = Ease.InOutSine,
    InOutQuint = Ease.InOutQuint,
    InOutQuart = Ease.InOutQuart,
    InOutQuad = Ease.InOutQuad,
    InOutFlash = Ease.InOutFlash,
    InOutExpo = Ease.InOutExpo,
    InOutElastic = Ease.InOutElastic,
    InOutCubic = Ease.InOutCubic,
    InOutCirc = Ease.InOutCirc,
    InOutBounce = Ease.InOutBounce,
    InOutBack = Ease.InOutBack,
    InFlash = Ease.InFlash,
    InExpo = Ease.InExpo,
    InElastic = Ease.InElastic,
    InCubic = Ease.InCubic,
    InCirc = Ease.InCirc,
    InBounce = Ease.InBounce,
    InBack = Ease.InBack,
    Flash = Ease.Flash,
}
#endregion