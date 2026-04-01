using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class Option_GeneralModule : UIModuleBase
{
    /// <summary>
    /// 体积雾开关
    /// </summary>
    public static bool volume = false;

    /// <summary>
    /// 将滑动条值映射到速度系数
    /// 滑动条 0-0.5 → 系数 0.5-1
    /// 滑动条 0.5-1 → 系数 1-2
    /// </summary>
    private static float SliderToCoefficient(float sliderValue)
    {
        return sliderValue <= 0.5f ? 0.5f + sliderValue : sliderValue * 2f;
    }

    /// <summary>
    /// 将速度系数映射回滑动条值
    /// </summary>
    private static float CoefficientToSlider(float coefficient)
    {
        return coefficient <= 1f ? coefficient - 0.5f : coefficient / 2f;
    }

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        Init();
        RefreshView();
    }
    private void Init()
    {
        this.GetComponentByChildName<Toggle>("Low").onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                Low();
                PlayerPrefs.SetString(GlobalInfo.qualityCacheKey, "Low");
            }
        });

        this.GetComponentByChildName<Toggle>("Middle").onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                Middle();
                PlayerPrefs.SetString(GlobalInfo.qualityCacheKey, "Middle");
            }
        });

        this.GetComponentByChildName<Toggle>("High").onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                High();
                PlayerPrefs.SetString(GlobalInfo.qualityCacheKey, "High");
            }
        });

        var volumeSlider = this.GetComponentByChildName<Slider>("VolumeSlider");
        {
            volumeSlider.value = PlayerPrefs.GetFloat(GlobalInfo.volumeCacheKey, 1f);// SoundManager.Instance.volume;
            var SliderValue = volumeSlider.transform.parent.GetComponentByChildName<Text>("value");
            SliderValue.text = Mathf.Floor(volumeSlider.value * 100) + "%";
            volumeSlider.onValueChanged.AddListener(value =>
            {
                SoundManager.Instance.volume = value;
                SliderValue.text = Mathf.Floor(value * 100) + "%";
            });
        }

        var MoveSpeed = this.GetComponentByChildName<Slider>("MoveSpeedSlider");
        {
            float moveCoefficient = PlayerPrefs.GetFloat(GlobalInfo.moveSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);
            MoveSpeed.value = CoefficientToSlider(moveCoefficient);
            var SliderValue = MoveSpeed.transform.parent.GetComponentByChildName<Text>("value");
            SliderValue.text = Mathf.Floor(moveCoefficient * 100) + "%";
            MoveSpeed.onValueChanged.AddListener(value =>
            {
                float coefficient = SliderToCoefficient(value);
                PlayerPrefs.SetFloat(GlobalInfo.moveSpeedCacheKey, coefficient);
                SliderValue.text = Mathf.Floor(coefficient * 100) + "%";
            });
        }

        var RotateSpeed = this.GetComponentByChildName<Slider>("RotateSpeedSlider");
        {
            float rotateCoefficient = PlayerPrefs.GetFloat(GlobalInfo.rotateSpeedCacheKey, GlobalInfo.defaultSpeedCoefficient);
            RotateSpeed.value = CoefficientToSlider(rotateCoefficient);
            var SliderValue = RotateSpeed.transform.parent.GetComponentByChildName<Text>("value");
            SliderValue.text = Mathf.Floor(rotateCoefficient * 100) + "%";
            RotateSpeed.onValueChanged.AddListener(value =>
            {
                float coefficient = SliderToCoefficient(value);
                PlayerPrefs.SetFloat(GlobalInfo.rotateSpeedCacheKey, coefficient);
                SliderValue.text = Mathf.Floor(coefficient * 100) + "%";
            });
        }

        var CourseVoice = this.GetComponentByChildName<Dropdown>("CourseVoiceInputDevice");

        CourseVoice.SetValueWithoutNotify(PlayerPrefs.GetInt(GlobalInfo.courseVoice, 1));

        if (!PlayerPrefs.HasKey(GlobalInfo.courseVoice))
        {
            PlayerPrefs.SetInt(GlobalInfo.courseVoice, 1);
        }
        CourseVoice.onValueChanged.AddListener(index =>
        {
            PlayerPrefs.SetInt(GlobalInfo.courseVoice, index);
            if(index == 1)
            {
                //标记初始化语音数据
                SpeechManager.Instance.dataInited = false;
                SpeechManager.Instance.LoadData();
            }
        });

        //        var ChangeFileSavePath = this.GetComponentByChildName<Button>("ChangeFileSavePath");
        //        {
        //#if UNITY_STANDALONE
        //            ChangeFileSavePath.onClick.AddListener(() =>
        //            {
        //                FormTool.OpenFolderDialog("选择录屏存储路径", PlayerPrefs.GetString(GlobalInfo.fileSavePathCacheKey), path =>
        //                {
        //                    PlayerPrefs.SetString(GlobalInfo.fileSavePathCacheKey, path);
        //                    this.GetComponentByChildName<Text>("FileSavePath").EllipsisText(PlayerPrefs.GetString(GlobalInfo.fileSavePathCacheKey), "...");
        //                });
        //            });
        //#else
        //            ChangeFileSavePath.interactable = false;
        //            ChangeFileSavePath.FindChildByName("Image").gameObject.SetActive(false);
        //#endif
        //        }

        //        var ChangeInput = this.GetComponentByChildName<Button>("ChangeInput");
        //        {
        //#if UNITY_STANDALONE
        //            ChangeInput.onClick.AddListener(() =>
        //            {
        //                //
        //            });
        //#else
        //            ChangeInput.interactable = false;
        //            ChangeInput.FindChildByName("Image").gameObject.SetActive(false);
        //#endif
        //        }

        this.GetComponentByChildName<Button>("Clear").onClick.AddListener(() =>
        {
            var popupDic = new System.Collections.Generic.Dictionary<string, PopupButtonData>();
            {
                popupDic.Add("确定", new PopupButtonData(() =>
                {
                    FileTool.CleanCache();
                    ((OptionPanel)ParentPanel).ShowToast(true, "清除成功");
                    RefreshCache();
                }, true));
                popupDic.Add("取消", new PopupButtonData(null));
                UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "确定清除缓存？", popupDic));
            }
        });
    }
    private void RefreshView()
    {
        var Quality = PlayerPrefs.GetString(GlobalInfo.qualityCacheKey);
        {
            var target = this.GetComponentByChildName<Toggle>(Quality);
            {
                target.isOn = true;
                target.group.allowSwitchOff = false;
            }
        }

        //this.GetComponentByChildName<Text>("FileSavePath").EllipsisText(PlayerPrefs.GetString(GlobalInfo.fileSavePathCacheKey), "...");

        //var InputDevice = this.GetComponentByChildName<Text>("InputDevice");
        //{
        //    var device = PlayerPrefs.GetString(GlobalInfo.inputDeviceCacheKey);

        //    if (Microphone.devices.Length == 0)
        //    {
        //        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("未找到任何输入设备!"));
        //        InputDevice.text = "无设备";

        //        var ChangeInput = this.GetComponentByChildName<Button>("ChangeInput");
        //        {
        //            ChangeInput.interactable = false;
        //            ChangeInput.FindChildByName("Image").gameObject.SetActive(false);
        //        }
        //    }
        //    else
        //    {
        //        if (Microphone.devices.Contains(device))
        //        {
        //            InputDevice.text = device;
        //        }
        //        else
        //        {
        //            InputDevice.text = Microphone.devices[0];
        //        }
        //        this.GetComponentByChildName<Button>("ChangeInput").interactable = true;
        //    }
        //}

        RefreshCache();
    }

    private void RefreshCache()
    {
        long totalSize = FileTool.GetDirectorySize($"{ResManager.Instance.resourcesRootPath}/Cache");
        this.GetComponentByChildName<Text>("CacheText").text = (totalSize / 1024f / 1024f).ToString("f1") + "M";
    }

    public static void InitQuality()
    {
        if (string.IsNullOrEmpty(PlayerPrefs.GetString(GlobalInfo.qualityCacheKey)))
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            PlayerPrefs.SetString(GlobalInfo.qualityCacheKey, "High");
#else
            PlayerPrefs.SetString(GlobalInfo.qualityCacheKey, "Low");
#endif
        }

        if (string.IsNullOrEmpty(PlayerPrefs.GetString(GlobalInfo.fileSavePathCacheKey)))
        {
            var defaultPath = $"{Application.persistentDataPath}/Cache/Record";
            {
                if (!System.IO.Directory.Exists(defaultPath))
                {
                    System.IO.Directory.CreateDirectory(defaultPath);
                }

                PlayerPrefs.SetString(GlobalInfo.fileSavePathCacheKey, defaultPath);
            }
        }

        switch (PlayerPrefs.GetString(GlobalInfo.qualityCacheKey))
        {
            case "Low":
                Low();
                break;
            case "Middle":
                Middle();
                break;
            case "High":
            default:
                High();
                break;
        }
    }

    /// <summary>
    /// 低
    /// 低 低
    /// 低 灯光+阴影
    /// 低 灯光和影+阴影
    /// </summary>
    private static void Low()
    {
        ModelManager.Instance.ControlQualityLevel(1);
        ModelManager.Instance.ControlPostProcessing(false);
        volume = false;
    }
    private static void Middle()
    {
        ModelManager.Instance.ControlQualityLevel(2);
        ModelManager.Instance.ControlPostProcessing(true);
        volume = true;
    }
    private static void High()
    {
        ModelManager.Instance.ControlQualityLevel(3);
        ModelManager.Instance.ControlPostProcessing(true);
        volume = true;
    }
}
