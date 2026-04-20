using UnityEngine;
using UnityFramework.Runtime;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// 注册窗口
/// </summary>
public class RegisterModule : UIModuleBase
{
    public static int currentTime = 0;
    private const int time = 60;

    private Dictionary<string, List<System.DateTime>> codeSpanSave;
    private const int codeSpanMax = 10;
    private Text TimeText;

    private Toggle Agreement;
    private Transform AgreementText;

    protected override float joinAnimePlayTime => 0.3f;

    public override void Open(UIData uiData = null)
    {
        base.Open();

        InitEvent();
        InitState();
        //JoinAnim();
    }

    private void InitEvent()
    {
        TimeText = this.GetComponentByChildName<Text>("TimeText");
        Agreement = this.GetComponentByChildName<Toggle>("Agreement");
        AgreementText = this.FindChildByName("AgreementText");

        var Enter = this.GetComponentByChildName<Button>("Enter");
        {
            var Agreement = this.GetComponentByChildName<Toggle>("Agreement");
            var PhonenumberError = this.FindChildByName("PhonenumberError").gameObject;
            var PasswordError = this.FindChildByName("PasswordError").gameObject;
            var NikenameError = this.FindChildByName("NikenameError").gameObject;
            var CodeError = this.FindChildByName("CodeError").gameObject;

            System.Func<bool> check = default;

            bool pass = false;

            var Phonenumber = this.GetComponentByChildName<InputField>("Phonenumber");
            {
                var PhonenumberErrorTip = this.GetComponentByChildName<Text>("PhonenumberErrorTip");

                Phonenumber.onValueChanged.AddListener(content =>
                {
                    PhonenumberError.SetActive(false);
                    PhonenumberErrorTip.text = string.Empty;
                });

                Phonenumber.onEndEdit.AddListener(content =>
                {
                    if (string.IsNullOrEmpty(Phonenumber.text))
                    {
                        PhonenumberError.SetActive(true);
                        PhonenumberErrorTip.text = "请输入手机号";
                    }
                    else if (Phonenumber.text.Length != 11)
                    {
                        PhonenumberError.SetActive(true);
                        PhonenumberErrorTip.text = "手机号格式不正确";
                    }

                    pass = check.Invoke();
                });
            }

            var Password = this.GetComponentByChildName<InputField>("Password");
            {
                var PasswordErrorTip = this.GetComponentByChildName<Text>("PasswordErrorTip");

                Password.onValueChanged.AddListener(content =>
                {
                    PasswordError.SetActive(false);
                    PasswordErrorTip.text = string.Empty;
                });

                Password.onEndEdit.AddListener(content =>
                {
                    if (string.IsNullOrEmpty(Password.text))
                    {
                        PasswordError.SetActive(true);
                        PasswordErrorTip.text = "请输入密码";
                    }
                    else if (Password.text.Length < 6)
                    {
                        PasswordError.SetActive(true);
                        PasswordErrorTip.text = "请输入6-16位数字、字母或数字字母组合";
                    }

                    pass = check.Invoke();
                });
            }

            var Nikename = this.GetComponentByChildName<InputField>("Nikename");
            {
                var NikenameErrorTip = this.GetComponentByChildName<Text>("NikenameErrorTip");

                Nikename.onValueChanged.AddListener(content =>
                {
                    NikenameError.SetActive(false);
                    NikenameErrorTip.text = string.Empty;
                    Nikename.text = Nikename.text.RemoveSpecialSymbols_Chinese();
                });

                Nikename.onEndEdit.AddListener(content =>
                {
                    if (string.IsNullOrEmpty(Nikename.text))
                    {
                        NikenameError.SetActive(true);
                        NikenameErrorTip.text = "请输入姓名";
                    }
                    else if (Nikename.text.Length < 2)
                    {
                        NikenameError.SetActive(true);
                        NikenameErrorTip.text = "请输入2-4位中文";
                    }

                    pass = check.Invoke();
                });
            }

            var Code = this.GetComponentByChildName<InputField>("Code");
            {
                var CodeErrorTip = this.GetComponentByChildName<Text>("CodeErrorTip");

                Code.onValueChanged.AddListener(content =>
                {
                    CodeError.SetActive(false);
                    CodeErrorTip.text = string.Empty;
                });

                Code.onEndEdit.AddListener(content =>
                {
                    if (string.IsNullOrEmpty(Code.text))
                    {
                        CodeError.SetActive(true);
                        CodeErrorTip.text = "请输入验证码";
                    }
                    else if (Code.text.Length < 5)
                    {
                        CodeError.SetActive(true);
                        CodeErrorTip.text = "请输入5位验证码";
                    }

                    pass = check.Invoke();
                });
            }

            Agreement.onValueChanged.AddListener(isOn =>
            {
                //pass = check.Invoke();
            });

            check = () =>
            {
                return
                Phonenumber.text.Length > 0 &&
                Password.text.Length > 0 &&
                Code.text.Length > 0 &&
                Nikename.text.Length > 0 &&
                !PhonenumberError.activeSelf &&
                !PasswordError.activeSelf &&
                !NikenameError.activeSelf &&
                !CodeError.activeSelf;//&&
                                      // Agreement.isOn;
            };

            var inRequest = false;
            Enter.onClick.AddListener(() =>
            {
                if (!Agreement.isOn)
                {
                    AgreementAnim();
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("请先勾选用户协议"));
                    return;
                }

                Phonenumber.onEndEdit.Invoke(Phonenumber.text);
                Password.onEndEdit.Invoke(Password.text);
                Code.onEndEdit.Invoke(Code.text);
                Nikename.onEndEdit.Invoke(Nikename.text);

                if (!pass)
                {
                    return;
                }

                if (inRequest)
                {
                    return;
                }

                inRequest = true;
                RequestManager.Instance.Register(Phonenumber.text, Nikename.text, Password.text, Code.text, () =>
                {
                    Enter.transform.parent.gameObject.SetActive(false);
                    this.FindChildByName("Success").gameObject.SetActive(true);
                    this.GetComponentByChildName<Button>("GoBack").onClick.AddListener(Exit);
                }, (code, msg) =>
                {
                    switch (code)
                    {
                        case 102:
                            CodeError.SetActive(true);
                            this.GetComponentByChildName<Text>("PhonenumberErrorTip").text = "手机号已存在";
                            break;
                        case 106:
                            CodeError.SetActive(true);
                            this.GetComponentByChildName<Text>("CodeErrorTip").text = "验证码错误";
                            break;
                        default:
                            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("注册失败"));
                            Log.Error($"注册失败 {code} {msg}");
                            break;
                    }
                    inRequest = false;
                });
            });

            bool inRequestCode = false;
            this.GetComponentByChildName<Button>("GetCode").onClick.AddListener(() =>
            {
                Phonenumber.onEndEdit.Invoke(Phonenumber.text);

                //手机号正确了才能发送验证码
                if (PhonenumberError.activeInHierarchy)
                {
                    return;
                }

                if (inRequestCode)
                {
                    return;
                }

                if (!codeSpanSave.ContainsKey(Phonenumber.text))
                {
                    codeSpanSave.Add(Phonenumber.text, new List<System.DateTime>());
                }

                if (codeSpanSave[Phonenumber.text].Count >= codeSpanMax)
                {
                    CodeError.SetActive(true);
                    this.GetComponentByChildName<Text>("CodeErrorTip").text = "今日验证码获取次数已达上限";
                    return;
                }

                inRequestCode = true;

                RequestManager.Instance.RegisterCaptcha(Phonenumber.text, () =>
                {
                    codeSpanSave[Phonenumber.text].Add(System.DateTime.Now);

                    currentTime = time;
                    OpenTimer();
                    UpdateTime(this.GetCancellationTokenOnDestroy()).Forget();

                    inRequestCode = false;
                }, (code, msg) =>
                {
                    inRequestCode = false;
                    switch (code)
                    {
                        case 102:
                            PhonenumberError.SetActive(true);
                            this.GetComponentByChildName<Text>("PhonenumberErrorTip").text = "手机号已存在";
                            break;
                        default:
                            Log.Error($"获取验证码失败 {code} {msg}");
                            break;
                    }
                });
            });
        }

        this.GetComponentByChildName<Button>("UserAgreement").onClick.AddListener(() =>
        {
            UIManager.Instance.HideModuleUI<RegisterModule>(ParentPanel, null, () =>
            {
                UIManager.Instance.OpenModuleUI<UserAgreementModule>(ParentPanel, ParentPanel.transform);
            });
        });
        this.GetComponentByChildName<Button>("Exit").onClick.AddListener(Exit);
    }
    private void InitState()
    {
        codeSpanSave = JsonTool.DeSerializable<Dictionary<string, List<System.DateTime>>>(PlayerPrefs.GetString(GlobalInfo.codeSpanKey));
        {
            var span = new System.TimeSpan(1, 0, 0, 0);

            if (codeSpanSave == null)
            {
                codeSpanSave = new Dictionary<string, List<System.DateTime>>();
            }

            foreach (var key in codeSpanSave.Keys?.ToList())
            {
                while (codeSpanSave[key].Count > 0)
                {
                    if (System.DateTime.Now - codeSpanSave[key][0] > span)
                    {
                        codeSpanSave[key].RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }

                if (codeSpanSave[key].Count == 0)
                {
                    codeSpanSave.Remove(key);
                }
            }
        }

        if (currentTime > 0)
        {
            UpdateTime(this.GetCancellationTokenOnDestroy()).Forget();
        }
    }

    /// <summary>
    /// 打开计时器 为了关闭页面也能计时
    /// </summary>
    private void OpenTimer()
    {
        Timer.AddTimer(1, typeof(RegisterModule).ToString(), true).OnCompleted(() =>
        {
            if (currentTime-- <= 0)
            {
                Timer.DelTimer(typeof(RegisterModule).ToString());
            }
        });
    }

    /// <summary>
    /// 更新计时UI
    /// </summary>
    private async UniTaskVoid UpdateTime(System.Threading.CancellationToken ct)
    {
        TimeText.text = $"重新获取({currentTime.ToString("D2")})";

        TimeText.transform.parent.gameObject.SetActive(true);

        while (currentTime > 0)
        {
            TimeText.text = $"重新获取({currentTime.ToString("D2")})";
            await UniTask.Yield(ct);
        }

        TimeText.transform.parent.gameObject.SetActive(false);
    }
    private void Exit()
    {
        //ExitAnim(() =>
        //{
        //    PlayerPrefs.SetString(GlobalInfo.codeSpanKey, JsonTool.Serializable(codeSpanSave));
        //    SendMsg(new MsgBase((ushort)LoginEvent.Login));
        //});
        UIManager.Instance.CloseModuleUI<RegisterModule>(ParentPanel, null, () =>
        {
            PlayerPrefs.SetString(GlobalInfo.codeSpanKey, JsonTool.Serializable(codeSpanSave));
            SendMsg(new MsgBase((ushort)LoginEvent.Login));
        });
    }

    /// <summary>
    /// 进场动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void JoinAnim(UnityAction callback)
    {
        transform.localRotation = Quaternion.Euler(0, -90f, 0);
        JoinSequence.Append(transform.DOLocalRotate(Vector3.up * 0, JoinAnimePlayTime).SetEase(Ease.Linear));
        base.JoinAnim(callback);
    }

    /// <summary>
    /// 退场动画
    /// </summary>
    /// <param name="callback">回调</param>
    public override void ExitAnim(UnityAction callback)
    {
        ExitSequence.Append(transform.DOLocalRotate(Vector3.down * 90f, ExitAnimePlayTime).SetEase(Ease.Linear));
        base.ExitAnim(callback);
    }

    /// <summary>
    /// 用户协议动效
    /// </summary>
    private void AgreementAnim()
    {
        DOTween.Kill("AgreementAnim", true);
        Sequence sequence = DOTween.Sequence();
        var posText = AgreementText.localPosition;
        var posToggle = Agreement.transform.localPosition;
        float offset = 0;
        offset = posText.x - 10f;
        sequence.Join(AgreementText.DOLocalMoveX(offset, 0.1f).SetLoops(3, LoopType.Yoyo));
        offset = posToggle.x - 10f;
        sequence.Join(Agreement.transform.DOLocalMoveX(offset, 0.1f).SetLoops(3, LoopType.Yoyo));
        sequence.OnComplete(() =>
        {
            //将坐标还原
            AgreementText.localPosition = posText;
            Agreement.transform.localPosition = posToggle;
        });
        sequence.SetId("AgreementAnim");
    }

    private static DateTime lastFocusTime;
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            lastFocusTime = DateTime.Now;
        }
        else
        {
            currentTime -= (int)(DateTime.Now - lastFocusTime).TotalSeconds;
            if (currentTime <= 0)
            {
                Timer.DelTimer(typeof(RegisterModule).ToString());
            }
        }
    }
}