using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityFramework.Runtime;


[Serializable]
public class InputData
{
    [Tooltip("标题")]
    public string title;
    [Tooltip("输入提示")]
    public string input;
}

/// <summary>
/// 输入观察UI
/// </summary>
public class UIInput : UIObserve
{
    /// <summary>
    /// 输入数据列表
    /// </summary>
    public List<InputData> inputs;

    private Text title;
    private InputField inputField;
    private Text placeholderComponent;

    /// <summary>
    /// 显示文本
    /// </summary>
    private Text showText;
    private Text tipText;

    private Button enterBtn;

    private int fontSize_Android = 30;

    protected override void InitUI()
    {
        title = transform.GetComponentByChildName<Text>("Title");
        inputField = transform.GetComponentInChildren<InputField>(true);
        placeholderComponent = transform.GetComponentByChildName<Text>("Placeholder");
        showText = inputField.GetComponentByChildName<Text>("ShowText");
        tipText = inputField.GetComponentByChildName<Text>("TipText");
        enterBtn = transform.GetComponentByChildName<Button>("Btn2");
        enterBtn.interactable = false;

#if UNITY_ANDROID
        RectTransform rect = transform.GetChild(0).GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1000, 660);
        rect.anchoredPosition = 194f * Vector2.up;

        title.fontSize = fontSize_Android;
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = -86.8f * Vector2.up;

        RectTransform backgroundRect = inputField.transform.parent.GetComponent<RectTransform>();
        backgroundRect.sizeDelta = new Vector2(856, 436);
        backgroundRect.anchoredPosition = -35f * Vector2.up;

        placeholderComponent.fontSize = fontSize_Android;
        inputField.textComponent.fontSize = fontSize_Android;
        showText.fontSize = fontSize_Android;
        tipText.fontSize = fontSize_Android;

        RectTransform buttonRect = enterBtn.transform.parent.GetComponent<RectTransform>();
        buttonRect.offsetMax *= 2;
        buttonRect.offsetMin *= 2;
        enterBtn.GetComponent<RectTransform>().sizeDelta *= 2;
        Text btnText = enterBtn.GetComponentInChildren<Text>();
        btnText.fontSize = fontSize_Android;
        RectTransform btnTextRect = btnText.GetComponent<RectTransform>();
        btnTextRect.sizeDelta = new Vector2(60, 40);
#endif
        base.InitUI();
    }

    protected override void OnEnter()
    {
        base.OnEnter();
        inputField.ActivateInputField();
    }

    protected override void CheckInput(int index, Action<string> onFinish, Action onFail)
    {
        base.CheckInput(index, onFinish, onFail);

        if (index >= inputs.Count)
        {
            main.SetActive(false);

            if (this.behaveObserve != null)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.AppendInterval(behaveObserve.stayTime);
                sequence.Append(Camera.main.transform.DOMove(camPosition, behaveObserve.time).SetEase((Ease)behaveObserve.ease));
                sequence.Join(Camera.main.transform.DORotate(camAngle, behaveObserve.time).SetEase((Ease)behaveObserve.ease));
                sequence.OnComplete(() =>
                {
                    onFinish?.Invoke(inputField.text);
                });

            }
            else
            {
                onFinish?.Invoke(inputField.text);
            }
            return;
        }


        InputData inputData = inputs[index];

        //title.text = inputData.title;
        inputField.text = string.Empty;

        //添加非考试模式下的提示效果
        //if (!GlobalInfo.IsExamMode())
        //{
        //    inputField.textComponent.SetAlpha(0);
        //    placeholderComponent.SetAlpha(0);
        //    tipText.text = inputData.input;

        //    inputField.onValueChanged.RemoveAllListeners();
        //    inputField.onValueChanged.AddListener((value) =>
        //    {
        //        showText.text = value;
        //    });
        //}
        //else
        {
            inputField.textComponent.SetAlpha(1);
            placeholderComponent.SetAlpha(0.5f);
            tipText.text = "";
            showText.text = "";
        }

        inputField.onValueChanged.AddListener((str) =>
        {
            enterBtn.interactable = !string.IsNullOrEmpty(str.Trim());
        });

        //输入限制逻辑，判断是否正确
        inputField.onValidateInput = (string text, int charIndex, char addedChar) =>
        {
            //Enter提交
            if (addedChar == '\n')
            {
                enterBtn.onClick?.Invoke();
                return '\0';
            }

            //输入验证逻辑
            //if (!GlobalInfo.IsExamMode() && !string.IsNullOrEmpty(inputData.input))
            //{
            //    if (text.Length != charIndex)
            //        return '\0';

            //    if (inputData.input.Contains(text + addedChar) && inputData.input[0] == (text + addedChar)[0])
            //        return addedChar;
            //    else
            //        return '\0';
            //}
            //else
            {
                return addedChar;
            }
        };

        enterBtn.onClick.RemoveAllListeners();
        enterBtn.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(inputField.text))
            {
                //根据输入内容进行判断
                //if (!string.IsNullOrEmpty(inputField.text) && (GlobalInfo.IsExamMode() || inputField.text.Equals(inputData.input)))
                {
                    CheckInput(++index, onFinish, onFail);
                }
                //else
                //{
                //    onFail?.Invoke();
                //}
            }
        });
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.SelectFlow:
                if (main.activeSelf)
                {
                    main.SetActive(false);
                }
                break;
        }
    }
}
