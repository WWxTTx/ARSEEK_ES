using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
using System.Collections.Generic;
using DG.Tweening;


public class TimePicker : MonoBehaviour
{
    public Text CaptionText;
    private Button Button;
    public Transform ExtendContent;

    private List<string> options;

    public UnityAction OnTimeAssert;
    public UnityAction<TimeSpan> OnTimeSelected;

    private int timeInterval = 15;//15

    private void OnEnable()
    {
        InitUIComponent();
    }

    private void InitUIComponent()
    {
        options = new List<string>();
        for (int h = 0; h < 24; h++)
        {
            for (int m = 0; m < 60; m += timeInterval)
            {
                options.Add($"{h:D2}:{m:D2}");
            }
        }

        Button = GetComponent<Button>();
        extendContentImage = this.GetComponentByChildName<Image>("ExtendContent");
        mask = extendContentImage.AutoComponent<Mask>();

        ExtendContent.transform.FindChildByName("Content").RefreshItemsView(options, (item, info) =>
        {
            item.GetComponentInChildren<Text>().text = info;

            Button temp = item.GetComponentByChildName<Button>("Input");
            {
                temp.onClick.RemoveAllListeners();
                temp.onClick.AddListener(() =>
                {
                    CaptionText.text = info;
                    OnTimeSelected?.Invoke(GetSelectedTime(info));
                    CloseDropdown(() => ExtendContent.gameObject.SetActive(false));
                });
            }
        });

        var CommonAccount = JsonTool.DeSerializable<Dictionary<string, string>>(PlayerPrefs.GetString(GlobalInfo.commonAccount));

        Button.onClick.AddListener(() =>
        {
            ExtendContent.gameObject.SetActive(true);
            OpenDropdown();
        });

        this.GetComponentByChildName<Button>("CloseExtendButton").onClick.AddListener(() => CloseDropdown(() => ExtendContent.gameObject.SetActive(false)));

        OnTimeSelected?.Invoke(GetNearestTime());
    }

    private TimeSpan GetNearestTime()
    {
        TimeSpan now = DateTime.Now.TimeOfDay;
        int hour = now.Hours;
        int nextMinute = ((now.Minutes / timeInterval) + 1) * timeInterval;
        if (nextMinute >= 60)
        {
            if (hour < 23)
            {
                hour += 1;
                nextMinute = 0;
            }
            else
                nextMinute = 60 - timeInterval;
        }
        TimeSpan selected = new TimeSpan(hour, nextMinute, 0);
        CaptionText.text = $"{selected.Hours:D2}:{selected.Minutes:D2}";
        ExtendContent.transform.FindChildByName("Content").GetComponent<RectTransform>().anchoredPosition = options.FindIndex(o => o.Equals(CaptionText.text)) * 37 * Vector2.up;
        return selected;
    }


    private TimeSpan GetSelectedTime(string option)
    {
        string[] times = option.Split(':');
        int hour = int.Parse(times[0]);
        int minute = int.Parse(times[1]);
        
        TimeSpan selected = new TimeSpan(hour, minute, 0);
        TimeSpan now = DateTime.Now.TimeOfDay;

        if(selected.Hours < now.Hours)
        {
            hour = now.Hours;
            int nextMinute = ((now.Minutes / timeInterval) + 1) * timeInterval;
            if (nextMinute >= 60 && hour < 23)
            {
                hour += 1;
                nextMinute = 0;
            }
            selected = new TimeSpan(hour, nextMinute, 0);
            OnTimeAssert?.Invoke();
        }
        else if (selected.Hours == now.Hours && selected.Minutes <= now.Minutes)
        {
            int nextMinute = ((now.Minutes / timeInterval) + 1) * timeInterval;
            if (nextMinute >= 60)
            {
                if (hour < 23)
                {
                    hour += 1;
                    nextMinute = 0;
                }
                else
                    nextMinute = 60 - timeInterval;
            }
            selected = new TimeSpan(hour, nextMinute, 0);
            OnTimeAssert?.Invoke();
        }

        CaptionText.text = $"{selected.Hours:D2}:{selected.Minutes:D2}";
        ExtendContent.transform.FindChildByName("Content").GetComponent<RectTransform>().anchoredPosition = options.FindIndex(o => o.Equals(CaptionText.text)) * 37 * Vector2.up;
        return selected;
    }


    #region ¶¯Ð§
    private float animeTime = 0.3f;
    private Image extendContentImage;
    private Mask mask;

    private void OpenDropdown()
    {
        mask.enabled = true;
        extendContentImage.fillAmount = 0;
        DOTween.To(() => extendContentImage.fillAmount, value => extendContentImage.fillAmount = value, 1, animeTime).OnComplete(() =>
        {
            mask.enabled = false;
        });
    }
    private void CloseDropdown(UnityAction callBack)
    {
        mask.enabled = true;
        extendContentImage.fillAmount = 1;
        DOTween.To(() => extendContentImage.fillAmount, value => extendContentImage.fillAmount = value, 0, animeTime).OnComplete(() =>
        {
            mask.enabled = false;
            callBack.Invoke();
        });
    }
    #endregion
}
