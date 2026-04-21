using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;

/// <summary>
/// 检查点击UI
/// </summary>
public class UIClick : UIObserve
{
    private Button btn;

    protected override void InitUI()
    {
        btn = transform.GetComponentByChildName<Button>("Btn");
        base.InitUI();
    }

    protected override void OnEnter()
    {
        base.OnEnter();
    }

    protected override void CheckInput(int index, Action<string> onFinish, Action onFail)
    {
        base.CheckInput(index, onFinish, onFail);

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                main.SetActive(false);

                if (this.behaveObserve != null)
                {
                    Sequence sequence = DOTween.Sequence();
                    sequence.Append(Camera.main.transform.DOMove(camPosition, behaveObserve.time).SetEase((Ease)behaveObserve.ease));
                    sequence.Join(Camera.main.transform.DORotate(camAngle, behaveObserve.time).SetEase((Ease)behaveObserve.ease));
                    sequence.OnComplete(() =>
                    {
                        onFinish?.Invoke(string.Empty);
                    });
                }
                else
                {
                    onFinish?.Invoke(string.Empty);
                }
            });
        }
    }
}