using DG.Tweening;
using UnityEngine;

/// <summary>
/// 控制对象朝向目标物体
/// </summary>
public class LookAtTagert : MonoBehaviour
{
    /// <summary>
    /// 物体目标点，不指定时为main Camera
    /// </summary>
    [Tooltip("物体目标点，不指定时为main Camera")]
    public Transform target;
    /// <summary>
    /// 是否是Z轴正向朝向目标物体，true为Z轴正向朝向目标物体，false为Z轴负向朝向目标物体
    /// </summary>
    [Tooltip("是否是Z轴正向朝向目标物体，true为Z轴正向朝向目标物体，false为Z轴负向朝向目标物体")]
    public bool isFront = true;

    public float playTime = 0.1f;
    /// <summary>
    /// 旋转轴限制
    /// </summary>
    [Tooltip("旋转轴限制")]
    public AxisConstraint axisConstraint;

    private Tweener angleTweener;

    void Awake()
    {
        if (target == null)
            target = Camera.main.transform;

        if (angleTweener != null)
            angleTweener.Kill();

        if (isFront)
            angleTweener = transform.DOLookAt(target.position, playTime, axisConstraint).SetAutoKill(false);
        else
            angleTweener = transform.DOLookAt(transform.position - target.position + transform.position, playTime, axisConstraint).SetAutoKill(false);
    }

    public void Init(Transform target, bool isFront, float playTime, AxisConstraint axisConstraint = AxisConstraint.None)
    {
        this.target = target;
        this.isFront = isFront;
        this.playTime = playTime;
        this.axisConstraint = axisConstraint;
        Awake();
    }

    void Update()
    {
        if (isFront)
            angleTweener.ChangeEndValue(target.position, true).Restart();
        else
            angleTweener.ChangeEndValue(transform.position - target.position + transform.position, true).Restart();
    }
}
