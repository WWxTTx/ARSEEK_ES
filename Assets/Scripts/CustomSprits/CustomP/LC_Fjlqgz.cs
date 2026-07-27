using System;
using UnityEngine;
using UnityEngine.Events;


public class LC_Fjlqgz : MonoBehaviour, IBaseBehaviour
{
    bool IBaseBehaviour.UseCallback(int step) => false;
    public Type GetStatusEnumType() => typeof(AvailableStatus);
    [SerializeField]
    public enum AvailableStatus
    {
        风机1正常运行 = 0,
        风机2正常运行 = 1,
        风机停机 = 2,
    }

    public AvailableStatus availableStatus;
    public Transform fj1;
    public Transform fj2;

    void Start()
    {
        HighlightEffectManager.Instance.Add(fj1, Color.red, 0.2f);
        HighlightEffectManager.Instance.Add(fj2, Color.red, 0.2f);
    }

    void IBaseBehaviour.Execute(int step, UnityAction callback)
    {
        availableStatus = (AvailableStatus)step;

        HighlightEffectManager.Instance.HighlightFlashing(fj1);
        HighlightEffectManager.Instance.HighlightFlashing(fj2);
    }

    void IBaseBehaviour.SetFinalState()
    {
        availableStatus = AvailableStatus.风机停机;
        HighlightEffectManager.Instance.RemoveHighlightFlashing(fj1);
        HighlightEffectManager.Instance.RemoveHighlightFlashing(fj2);
    }

    Vector3 currentRotation;
    float rotationSpeed = 1000;
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="step"></param>
    void Update()
    {
        if(availableStatus == AvailableStatus.风机1正常运行)
        {
            currentRotation = fj1.eulerAngles;
            currentRotation.y += rotationSpeed * Time.deltaTime;
            fj1.eulerAngles = currentRotation;
        }
        else if (availableStatus == AvailableStatus.风机2正常运行)
        {
            currentRotation = fj2.eulerAngles;
            currentRotation.y += rotationSpeed * Time.deltaTime;
            fj2.eulerAngles = currentRotation;
        }
    }
}
