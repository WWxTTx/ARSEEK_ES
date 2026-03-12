using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class Option_UserInfoModule : UIModuleBase
{
    private Toggle MaskToggle;
    private InputField changeName;
    private InputField changePhonenumber;
    private Text MaskPhonenumber;
    private InputField changeUserNo;
    private Text MaskUserNo;
    private InputField changeUserOrg;
    private Text MaskUserOrg;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        this.GetComponentByChildName<Text>("CompanyName").text = GlobalInfo.account.schoolName;
        this.GetComponentByChildName<Text>("UserType").text = GlobalInfo.account.roleDescription;

        #region 修改真实姓名
        changeName = this.GetComponentByChildName<InputField>("ChangeNameInputField");
        changeName.text = GlobalInfo.account.nickname;//设置初始值
        changeName.interactable = false;//设置初始不能编辑
        Button nameEditor = changeName.GetComponentByChildName<Button>("Editor");
        nameEditor.gameObject.SetActive(true);//设置初始显示编辑按钮
        nameEditor.onClick.AddListener(() =>
        {
            if (GlobalInfo.canEditUserInfo)
            {
                //编辑按钮点击事件
                changeName.interactable = true;
                changeName.Select();
                nameEditor.gameObject.SetActive(false);
            }
            else
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间内无法修改个人信息"));
            }
        });

        changeName.onValueChanged.AddListener(content =>
        {
            changeName.text = changeName.text.RemoveSpecialSymbols_Chinese();
        });
        changeName.onEndEdit.AddListener(content =>
        {   //编辑完成事件
            changeName.interactable = false;
            nameEditor.gameObject.SetActive(true);

            if (content == GlobalInfo.account.nickname)
                return;

            if (content.Length < 2)
            {
                changeName.text = GlobalInfo.account.nickname;
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("请输入2-4个中文字符!"));
                return;
            }

            RequestManager.Instance.ChangeNickName(content, () =>
            {
                Relogin(() =>
                {
                    GlobalInfo.account.nickname = content;
                    changeName.text = content;
                    SendMsg(new MsgBase((ushort)OptionPanelEvent.Name));
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("修改成功!"));
                }, null);//重新登录  
            }, (code, errorMessage) =>
            {
                changeName.text = GlobalInfo.account.nickname;
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(errorMessage));
            });
        });
        #endregion

        #region 修改手机号
        changePhonenumber = this.GetComponentByChildName<InputField>("PhonenumberInputField");
        //设置初始值
        changePhonenumber.text = GlobalInfo.account.mobile;
        changePhonenumber.textComponent.gameObject.SetActive(false);
        changePhonenumber.placeholder.gameObject.SetActive(false);
        changePhonenumber.interactable = false;//设置初始不能编辑

        MaskPhonenumber = changePhonenumber.GetComponentByChildName<Text>("MaskPhonenumber");
        MaskPhonenumber.text = GlobalInfo.account.mobile;// PhonenumberMask(GlobalInfo.account.mobile);
        MaskPhonenumber.gameObject.SetActive(true);

        Button PhonenumberEditor = changePhonenumber.GetComponentByChildName<Button>("Editor");
        //隐藏编辑按钮
        PhonenumberEditor.gameObject.SetActive(false);
        PhonenumberEditor.onClick.AddListener(() =>
        {
            if (GlobalInfo.canEditUserInfo)
            {
                //编辑按钮点击事件
                changePhonenumber.textComponent.gameObject.SetActive(true);
                changePhonenumber.placeholder.gameObject.SetActive(true);
                changePhonenumber.interactable = true;
                changePhonenumber.Select();
                PhonenumberEditor.gameObject.SetActive(false);
                MaskPhonenumber.gameObject.SetActive(false);
            }
            else
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间内无法修改个人信息"));
            }
        });
        changePhonenumber.onEndEdit.AddListener(content =>
        {
            MaskPhonenumber.gameObject.SetActive(true);
            PhonenumberEditor.gameObject.SetActive(true);
            changePhonenumber.textComponent.gameObject.SetActive(false);
            changePhonenumber.placeholder.gameObject.SetActive(false);
            changePhonenumber.interactable = false;

            if (content == GlobalInfo.account.mobile)
                return;

            if (content.Length != 11)
            {
                changePhonenumber.text = GlobalInfo.account.mobile;
                MaskPhonenumber.text = PhonenumberMask(GlobalInfo.account.mobile);
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("请输入正确格式的手机号码"));
                return;
            }

            MaskPhonenumber.text = PhonenumberMask(content);
            UIManager.Instance.OpenUI<SMSVerificationPanel>(UILevel.PopUp, uiData: new SMSVerificationPanel.PanelData()
            {
                title = "绑定手机",
                path = ApiData.Captcha_BindPhoneNumber_Old,
                hint = $"原绑定手机号{GlobalInfo.account.mobile}",
                phoneNumber = "",
                onCancel = () => 
                {
                    changePhonenumber.text = GlobalInfo.account.mobile;
                    MaskPhonenumber.text = PhonenumberMask(GlobalInfo.account.mobile);
                },
                callBack = code =>
                {
                    RequestManager.Instance.ReBindPhoneNumber(code, passport =>
                    {
                        UIManager.Instance.OpenUI<SMSVerificationPanel>(UILevel.PopUp, uiData: new SMSVerificationPanel.PanelData()
                        {
                            title = "绑定手机",
                            path = ApiData.Captcha_BindPhoneNumber_New,
                            hint = $"新绑定手机号{content}",
                            phoneNumber = content,
                            onCancel = () =>
                            {
                                changePhonenumber.text = GlobalInfo.account.mobile;
                                MaskPhonenumber.text = PhonenumberMask(GlobalInfo.account.mobile);
                            },
                            callBack = code =>
                            {
                                RequestManager.Instance.BindPhoneNumber(code, passport, content, () =>
                                {
                                    PlayerPrefs.SetString(GlobalInfo.accountCacheKey, content);
                                    Relogin(() =>
                                    {
                                        GlobalInfo.account.mobile = content;
                                        changePhonenumber.text = content;
                                        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("手机号修改成功"));
                                    }, null);//重新登录

                                }, (bingErrorCode, bindError) =>
                                {
                                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(bindError));
                                });
                            }
                        });

                    }, (rebingErrorCode, rebindError) =>
                    {
                        changePhonenumber.text = GlobalInfo.account.mobile;
                        MaskPhonenumber.text = PhonenumberMask(GlobalInfo.account.mobile);
                        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(rebindError));
                    });
                }
            });
        });
        #endregion

        #region 修改密码

        //设置初始值
        InputField changePassword = this.GetComponentByChildName<InputField>("PasswordInputField");
        changePassword.textComponent.gameObject.SetActive(false);
        changePassword.placeholder.gameObject.SetActive(false);
        changePassword.interactable = false;//设置初始不能编辑

        Text MaskPassword = changePassword.GetComponentByChildName<Text>("MaskPassword");
        MaskPassword.text = PasswordMask(PlayerPrefs.GetString(GlobalInfo.passwordCacheKey));
        MaskPassword.gameObject.SetActive(true);

        Button PasswordEditor = changePassword.GetComponentByChildName<Button>("Editor");
        PasswordEditor.gameObject.SetActive(true);//设置初始显示编辑按钮
        PasswordEditor.onClick.AddListener(() =>
        {
            if (GlobalInfo.canEditUserInfo)
            {
                #region 通过验证码修改密码
                ////编辑按钮点击事件
                //changePassword.text = "";
                //changePassword.textComponent.gameObject.SetActive(true);
                //changePassword.placeholder.gameObject.SetActive(true);
                //changePassword.interactable = true;
                //changePassword.Select();
                //PasswordEditor.gameObject.SetActive(false);
                //MaskPassword.gameObject.SetActive(false);
                #endregion

                //通过旧密码修改密码
                UIManager.Instance.OpenUI<ChangePasswordPanel>(UILevel.PopUp, uiData: new ChangePasswordPanel.PanelData()
                {
                    callBack = (oldPassword, newPassword) => RequestManager.Instance.ChangePassword(newPassword, oldPassword, () =>
                    {
                        PlayerPrefs.SetString(GlobalInfo.passwordCacheKey, newPassword);
                        Relogin(() =>
                        {
                            MaskPassword.text = PasswordMask(newPassword);
                            UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("密码修改成功"));
                        }, null);

                    }, (code, msg) =>
                    {
                        MaskPassword.text = PasswordMask(oldPassword);
                        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(msg));
                    })
                });
            }
            else
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间内无法修改个人信息"));
            }
        });

        #region 通过验证码修改密码
        //changePassword.onEndEdit.AddListener(content =>
        //{
        //    changePassword.textComponent.gameObject.SetActive(false);
        //    changePassword.placeholder.gameObject.SetActive(false);
        //    changePassword.interactable = false;
        //    PasswordEditor.gameObject.SetActive(true);
        //    MaskPassword.gameObject.SetActive(true);

        //    //if (content == GlobalInfo.account.mobile)
        //    //    return;

        //    if (content.Length < 6)
        //    {
        //        MaskPassword.text = PasswordMask(PlayerPrefs.GetString(GlobalInfo.passwordCacheKey));
        //        UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("请输入6-16位数字、字母或数字字母组合"));
        //        return;
        //    }

        //    UIManager.Instance.OpenUI<SMSVerificationPanel>(UILevel.PopUp, uiData: new SMSVerificationPanel.PanelData()
        //    {
        //        title = "修改密码",
        //        path = ApiData.Captcha_ChangePassword,
        //        hint = GlobalInfo.account.mobile,
        //        phoneNumber = "",
        //        callBack = code =>
        //        {
        //            RequestManager.Instance.ChangePassword_Captcha(code, content, () =>
        //            {
        //                PlayerPrefs.SetString(GlobalInfo.passwordCacheKey, content);//重新登录
        //                Relogin(() =>
        //                {
        //                    MaskPassword.text = PasswordMask(content);
        //                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("密码修改成功"));
        //                }, null);
        //            }, (code, error) =>
        //            {
        //                MaskPassword.text = PasswordMask(content);
        //                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(error));
        //            });
        //        }
        //    });
        //});
        #endregion
        #endregion

        #region 修改工号
        changeUserNo = this.GetComponentByChildName<InputField>("ChangeUserNoInputField");
        changeUserNo.text = GlobalInfo.account.userNo;
        changeUserNo.textComponent.gameObject.SetActive(false);
        changeUserNo.placeholder.gameObject.SetActive(false);
        changeUserNo.interactable = false;

        MaskUserNo = changeUserNo.GetComponentByChildName<Text>("MaskUserNo");
        MaskUserNo.EllipsisText(GlobalInfo.account.userNo, "...");
        MaskUserNo.gameObject.SetActive(true);

        Button userNoEditor = changeUserNo.GetComponentByChildName<Button>("Editor");
        userNoEditor.gameObject.SetActive(true);
        userNoEditor.onClick.AddListener(() =>
        {
            if (GlobalInfo.canEditUserInfo)
            {
                //编辑按钮点击事件
                changeUserNo.textComponent.gameObject.SetActive(true);
                changeUserNo.placeholder.gameObject.SetActive(true);
                changeUserNo.interactable = true;
                changeUserNo.Select();
                StartCoroutine(MoveLast(changeUserNo));
                userNoEditor.gameObject.SetActive(false);
                MaskUserNo.gameObject.SetActive(false);
            }
            else
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间内无法修改个人信息"));
            }
        });

        changeUserNo.onEndEdit.AddListener(content =>
        {
            MaskUserNo.gameObject.SetActive(true);
            userNoEditor.gameObject.SetActive(true);
            changeUserNo.textComponent.gameObject.SetActive(false);
            changeUserNo.placeholder.gameObject.SetActive(false);
            changeUserNo.interactable = false;

            //if (content.Equals(GlobalInfo.account.userNo) ||
            //    (GlobalInfo.account.userNo == null && string.IsNullOrEmpty(content)))
            //    return;

            if (content.Equals(GlobalInfo.account.userNo))
                return;

            //不允许将工号改为空
            if (string.IsNullOrEmpty(content))
            {
                changeUserNo.text = GlobalInfo.account.userNo;
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("请输入工号"));
                return;
            }  

            RequestManager.Instance.ChangeUserNo(content, () =>
            {
                PlayerPrefs.SetString(GlobalInfo.accountCacheKey, content);
                Relogin(() =>
                {
                    changeUserNo.text = GlobalInfo.account.userNo;
                    MaskUserNo.EllipsisText(GlobalInfo.account.userNo, "...");
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("修改成功!"));
                }, null);//重新登录  
            }, (code, errorMessage) =>
            {
                changeUserNo.text = GlobalInfo.account.userNo;
                MaskUserNo.EllipsisText(GlobalInfo.account.userNo, "...");
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(errorMessage));
            });
        });
        #endregion

        #region 修改单位
        changeUserOrg = this.GetComponentByChildName<InputField>("ChangeUserOrgInputField");
        changeUserOrg.text = GlobalInfo.account.userOrgName;
        changeUserOrg.textComponent.gameObject.SetActive(false);
        changeUserOrg.placeholder.gameObject.SetActive(false);
        changeUserOrg.interactable = false;

        MaskUserOrg = changeUserOrg.GetComponentByChildName<Text>("MaskUserOrg");
        MaskUserOrg.EllipsisText(GlobalInfo.account.userOrgName, "...");
        MaskUserOrg.gameObject.SetActive(true);

        Button userOrgEditor = changeUserOrg.GetComponentByChildName<Button>("Editor");
        userOrgEditor.gameObject.SetActive(true);
        userOrgEditor.onClick.AddListener(() =>
        {
            if (GlobalInfo.canEditUserInfo)
            {
                changeUserOrg.textComponent.gameObject.SetActive(true);
                changeUserOrg.placeholder.gameObject.SetActive(true);
                changeUserOrg.interactable = true;
                changeUserOrg.Select();
                StartCoroutine(MoveLast(changeUserOrg));
                userOrgEditor.gameObject.SetActive(false);
                MaskUserOrg.gameObject.SetActive(false);
            }
            else
            {
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("房间内无法修改个人信息"));
            }
        });


        changeUserOrg.onEndEdit.AddListener(content =>
        {
            MaskUserOrg.gameObject.SetActive(true);
            userOrgEditor.gameObject.SetActive(true);
            changeUserOrg.textComponent.gameObject.SetActive(false);
            changeUserOrg.placeholder.gameObject.SetActive(false);
            changeUserOrg.interactable = false;

            if (content.Equals(GlobalInfo.account.userOrgName) ||
                (GlobalInfo.account.userOrgName == null && string.IsNullOrEmpty(content)))
                return;

            RequestManager.Instance.ChangeUserOrg(content, () =>
            {
                Relogin(() =>
                {
                    changeUserOrg.text = GlobalInfo.account.userOrgName;
                    MaskUserOrg.EllipsisText(GlobalInfo.account.userOrgName, "...");
                    SendMsg(new MsgBase((ushort)OptionPanelEvent.Org));
                    UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo("修改成功!"));
                }, null);//重新登录  
            }, (code, errorMessage) =>
            {
                changeUserOrg.text = GlobalInfo.account.userOrgName;
                MaskUserOrg.EllipsisText(GlobalInfo.account.userOrgName, "...");
                UIManager.Instance.OpenModuleUI<ToastPanel>(ParentPanel, UILevel.PopUp, new ToastPanelInfo(errorMessage));
            });
        });
        #endregion

        //点击空白取消编辑
        MaskToggle = this.GetComponentByChildName<Toggle>("MaskToggle");
        {
            MaskToggle.onValueChanged.AddListener(isOn =>
            {
                changeName.text = GlobalInfo.account.nickname;
                changePhonenumber.text = GlobalInfo.account.mobile;
                MaskPhonenumber.text = PhonenumberMask(GlobalInfo.account.mobile);
                changeUserNo.text = GlobalInfo.account.userNo;
                MaskUserNo.EllipsisText(GlobalInfo.account.userNo, "...");
                changeUserOrg.text = GlobalInfo.account.userOrgName;
                MaskUserOrg.EllipsisText(GlobalInfo.account.userOrgName, "...");
                //RefreshView();
            });
        }

        Init();
        //RefreshView();
    }

    private IEnumerator MoveLast(InputField inputField)
    {
        yield return new WaitForEndOfFrame();
        inputField.MoveTextEnd(true);
    }

    private void Init()
    {
        #region 加入单位
        //        this.GetComponentByChildName<Button>("ChangeCompany").onClick.AddListener(() =>
        //        {
        //            var inputFieldData = new List<InputFieldData>();
        //            {
        //                inputFieldData.Add(new InputFieldData(string.Empty, "输入N位激活码", string.Empty, false, extendAction: inputItem =>
        //                {
        //#if UNITY_ANDROID || UNITY_IOS
        //                    #region 旧接口
        //                    //var QRButton = inputItem.GetComponentByChildName<Button>("QRButton");
        //                    //{
        //                    //    QRButton.onClick.AddListener(() =>
        //                    //    {
        //                    //        UIManager.Instance.OpenUI<QRPanel>(UILevel.Normal, new QRPanel.PanelData()
        //                    //        {
        //                    //            callBack = (result, data) =>
        //                    //            {
        //                    //                VerifyCompany(data, RefreshView, (index, code) =>
        //                    //                { 
        //                    //                    switch (index)
        //                    //                    {
        //                    //                        //验证单位
        //                    //                        case 0:
        //                    //                            switch ((ResultCode.VerifyCompany)code)
        //                    //                            {
        //                    //                                case ResultCode.VerifyCompany.InternetError:
        //                    //                                    UIManager.Instance.InternetError();
        //                    //                                    break;
        //                    //                                case ResultCode.VerifyCompany.Service_Exception:
        //                    //                                    UIManager.Instance.ServiceException();
        //                    //                                    break;
        //                    //                                case ResultCode.VerifyCompany.Invalid_Parameter:
        //                    //                                    UIManager.Instance.Tip(ParentPanel, "激活码错误");
        //                    //                                    break;
        //                    //                                case ResultCode.VerifyCompany.Device_SchoolAuth_Exit:
        //                    //                                    UIManager.Instance.Tip(ParentPanel, "已加入此学校");
        //                    //                                    break;
        //                    //                                case ResultCode.VerifyCompany.Auth_Expired:
        //                    //                                    UIManager.Instance.Tip(ParentPanel, "激活码已过期");
        //                    //                                    break;
        //                    //                                case ResultCode.VerifyCompany.Refresh_Token_Faile:
        //                    //                                case ResultCode.VerifyCompany.Successful:
        //                    //                                default:
        //                    //                                    break;
        //                    //                            }
        //                    //                            break;
        //                    //                        //加入单位
        //                    //                        case 1:
        //                    //                            switch ((ResultCode.JoinCompany)code)
        //                    //                            {
        //                    //                                case ResultCode.JoinCompany.InternetError:
        //                    //                                    UIManager.Instance.InternetError();
        //                    //                                    break;
        //                    //                                case ResultCode.JoinCompany.Service_Exception:
        //                    //                                    UIManager.Instance.ServiceException();
        //                    //                                    break;
        //                    //                                case ResultCode.JoinCompany.Expired_Request:
        //                    //                                    UIManager.Instance.Tip(ParentPanel, "验证请求已过期");
        //                    //                                    break;
        //                    //                                case ResultCode.JoinCompany.Update_Failed:
        //                    //                                case ResultCode.JoinCompany.Successful:
        //                    //                                default:
        //                    //                                    break;
        //                    //                            }
        //                    //                            break;
        //                    //                        //重新登录
        //                    //                        case 2:
        //                    //                            ParentPanel.GotoLogout();
        //                    //                            break;
        //                    //                    }
        //                    //                });
        //                    //            }
        //                    //        });
        //                    //    });

        //                    //    QRButton.gameObject.SetActive(true);
        //                    //}
        //                    #endregion

        //                    var QRButton = inputItem.GetComponentByChildName<Button>("QRButton");
        //                    {
        //                        QRButton.onClick.AddListener(() =>
        //                        {
        //                            UIManager.Instance.OpenUI<QRPanel>(UILevel.Normal, new QRPanel.PanelData()
        //                            {
        //                                callBack = (result, data) =>
        //                                {
        //                                    if (string.IsNullOrEmpty(data))
        //                                        return;

        //                                    JoinCompanyByQRCode(data, RefreshView, (code, msg) =>
        //                                    {
        //                                        switch (code)
        //                                        {
        //                                            default:
        //                                                UIManager.Instance.Tip(ParentPanel, $"加入单位失败, {msg}");
        //                                                Log.Error($"加入单位失败，{code}, {msg}");
        //                                                break;
        //                                        }
        //                                    }, () => ParentPanel.GotoLogout());
        //                                }
        //                            });
        //                        });
        //                        QRButton.gameObject.SetActive(true);
        //                    }
        //#endif
        //                }));
        //            }

        //            var popupDic = new Dictionary<string, InputPopupButtonData>();
        //            {
        //                popupDic.Add("验证", new InputPopupButtonData(inputFields =>
        //                {
        //                    if (string.IsNullOrEmpty(inputFields[0].text))
        //                    {
        //                        inputFields[0].ShowTip("请输入激活码");
        //                        inputFields[0].Select();
        //                        return false;
        //                    }

        //                    JoinCompanyByInviteCode(inputFields[0].text, RefreshView, (code, msg) =>
        //                    {
        //                        switch (code)
        //                        {
        //                            default:
        //                                UIManager.Instance.Tip(ParentPanel, $"加入单位失败, {msg}");
        //                                Log.Error($"加入单位失败: {code}, {msg}");
        //                                break;
        //                        }
        //                    }, () =>
        //                    {
        //                        ParentPanel.GotoLogout();
        //                    });

        //                    #region 旧接口
        //                    //VerifyCompany(inputFields[0].text, RefreshView, (index, code) =>
        //                    //{
        //                    //    switch (index)
        //                    //    {
        //                    //        //验证单位
        //                    //        case 0:
        //                    //            switch ((ResultCode.VerifyCompany)code)
        //                    //            {
        //                    //                case ResultCode.VerifyCompany.InternetError:
        //                    //                    UIManager.Instance.InternetError();
        //                    //                    break;
        //                    //                case ResultCode.VerifyCompany.Service_Exception:
        //                    //                    UIManager.Instance.ServiceException();
        //                    //                    break;
        //                    //                case ResultCode.VerifyCompany.Invalid_Parameter:
        //                    //                    UIManager.Instance.Tip(ParentPanel, "激活码错误");
        //                    //                    break;
        //                    //                case ResultCode.VerifyCompany.Device_SchoolAuth_Exit:
        //                    //                    UIManager.Instance.Tip(ParentPanel, "已加入此学校");
        //                    //                    break;
        //                    //                case ResultCode.VerifyCompany.Auth_Expired:
        //                    //                    UIManager.Instance.Tip(ParentPanel, "激活码已过期");
        //                    //                    break;
        //                    //                case ResultCode.VerifyCompany.Refresh_Token_Faile:
        //                    //                case ResultCode.VerifyCompany.Successful:
        //                    //                default:
        //                    //                    break;
        //                    //            }
        //                    //            break;
        //                    //        //加入单位
        //                    //        case 1:
        //                    //            switch ((ResultCode.JoinCompany)code)
        //                    //            {
        //                    //                case ResultCode.JoinCompany.InternetError:
        //                    //                    UIManager.Instance.InternetError();
        //                    //                    break;
        //                    //                case ResultCode.JoinCompany.Service_Exception:
        //                    //                    UIManager.Instance.ServiceException();
        //                    //                    break;
        //                    //                case ResultCode.JoinCompany.Expired_Request:
        //                    //                    UIManager.Instance.Tip(ParentPanel, "验证请求已过期");
        //                    //                    break;
        //                    //                case ResultCode.JoinCompany.Update_Failed:
        //                    //                case ResultCode.JoinCompany.Successful:
        //                    //                default:
        //                    //                    break;
        //                    //            }
        //                    //            break;
        //                    //        //重新登录
        //                    //        case 2:
        //                    //            ParentPanel.GotoLogout();
        //                    //            break;
        //                    //    }
        //                    //});
        //                    #endregion
        //                    return true;
        //                }, true));
        //            }

        //            UIManager.Instance.OpenUI<InputPopupPanel>(UILevel.Normal, new UIInputPopupData("更换单位", inputFieldData, popupDic));
        //        });
        #endregion
    }

    public override void Close(UIData uiData = null, UnityAction callback = null)
    {
        UIManager.Instance.CloseUI<ChangePasswordPanel>();
        UIManager.Instance.CloseUI<SMSVerificationPanel>();
        base.Close(uiData, callback);
    }

    #region 更换单位相关
    /// <summary>
    /// 通过扫描二维码加入单位
    /// </summary>
    /// <param name="url"></param>
    /// <param name="success"></param>
    /// <param name="failure"></param>
    public static void JoinCompanyByQRCode(string url, UnityAction success, UnityAction<int, string> joinFailure, UnityAction reloginFailure)
    {
        RequestManager.Instance.JoinOrg(url, () =>
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() =>
            {
                Relogin(success, reloginFailure);
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入单位成功", popupDic, () =>
            {
                Relogin(success, reloginFailure);
            }));
        }, (code, msg) =>
        {
            joinFailure?.Invoke(code, msg);
        });
    }

    /// <summary>
    /// 通过邀请码加入单位
    /// </summary>
    /// <param name="inviteCode"></param>
    /// <param name="success"></param>
    /// <param name="failure"></param>
    public static void JoinCompanyByInviteCode(string inviteCode, UnityAction success, UnityAction<int, string> joinFailure, UnityAction reloginFailure)
    {
        RequestManager.Instance.JoinOrgInviteCode(inviteCode, (shoolId) =>
        {
            Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
            popupDic.Add("确定", new PopupButtonData(() =>
            {
                Relogin(success, reloginFailure);
            }, true));
            UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入单位成功", popupDic, () =>
            {
                Relogin(success, reloginFailure);
            }));
        }, (code, msg) =>
        {
            joinFailure?.Invoke(code, msg);
        });
    }

    private static void Relogin(UnityAction success, UnityAction failure)
    {
        //后台重新登录
        string account = PlayerPrefs.GetString(GlobalInfo.accountCacheKey);
        string password = PlayerPrefs.GetString(GlobalInfo.passwordCacheKey);
        ApiData.AccessToken = null;
        RequestManager.Instance.Login(account, password, (data, message) =>
        {
            ApiData.AccessToken = data.token.accessToken;
            GlobalInfo.account = data;

            //记录服务器返回数据到本地
            var tempSave = RequestBase.GetNewUrl(ApiData.Login);
            string value = ConfigXML.GetData(ConfigType.Cache, DtataType.LocalSever, tempSave);
            if (string.IsNullOrEmpty(value))
                ConfigXML.AddData(ConfigType.Cache, DtataType.LocalSever, tempSave, message);
            else
                ConfigXML.UpdateData(ConfigType.Cache, DtataType.LocalSever, tempSave, message);

            success.Invoke();
        }, (code, errorMessage) =>
        {
            failure?.Invoke();
        });
    }
    ///// <summary>
    ///// 这个方法很特殊会走三次接口 所以错误回调会对应各个阶段 0验证单位阶段 1加入单位阶段 2重新登录阶段
    ///// </summary>
    ///// <param name="content"></param>
    ///// <param name="success"></param>
    ///// <param name="failure"></param>
    //public static void VerifyCompany(string content, UnityEngine.Events.UnityAction success, UnityEngine.Events.UnityAction<int, int> failure = null)
    //{
    //    RequestManager.Instance.VerifyCompany(content, data =>
    //    {
    //        VerifyCompanySuccess(data, success, failure);
    //    }, (code, message) =>
    //    {
    //        failure?.Invoke(0, code);
    //    });
    //}
    //private static void VerifyCompanySuccess(RequestData.VerifyCompanyInfo data, UnityEngine.Events.UnityAction success, UnityEngine.Events.UnityAction<int, int> failure = null)
    //{
    //    string popupInfo = $"是否加入{data.join_school}{(!string.IsNullOrEmpty(data.exit_school) ? $"加入后将自动退出{data.exit_school}" : string.Empty)}";

    //    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
    //    popupDic.Add("取消", new PopupButtonData(() => { }));
    //    popupDic.Add("加入", new PopupButtonData(() => RequestManager.Instance.JoinCompany(() =>
    //    {
    //        JoinCompanySuccess(success, failure);
    //    }, (code, message) =>
    //    {
    //        failure?.Invoke(1, code);
    //    }), true));
    //    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", popupInfo, popupDic));
    //}
    //private static void VerifyCompanyFailure(string failureMessage, UnityEngine.Events.UnityAction<bool> callBack = null)
    //{
    //    Log.Error($"加入单位失败！原因为：{failureMessage}");

    //    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
    //    popupDic.Add("确认", new PopupButtonData(() => callBack.Invoke(false), true));

    //    if (failureMessage == "无效参数")
    //        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "激活码错误，请检查激活码是否正确", popupDic));
    //    else if (failureMessage == "HTTP/1.1 500 Internal Server Error")
    //        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入单位失败，请检查网络或激活码是否正确", popupDic));
    //    else
    //        UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入单位失败，您已加入该单位", popupDic));
    //}
    //private static void JoinCompanySuccess(UnityEngine.Events.UnityAction success, UnityEngine.Events.UnityAction<int, int> failure = null)
    //{
    //    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
    //    popupDic.Add("确定", new PopupButtonData(() =>
    //    {
    //        Relogin(success, failure);
    //    }, true));
    //    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入单位成功", popupDic, () =>
    //    {
    //        Relogin(success, failure);
    //    }));
    //}
    //private static void JoinCompanyFailure(string failureMessage, UnityEngine.Events.UnityAction<bool> callBack)
    //{
    //    Dictionary<string, PopupButtonData> popupDic = new Dictionary<string, PopupButtonData>();
    //    popupDic.Add("确认", new PopupButtonData(() => callBack.Invoke(false), true));
    //    UIManager.Instance.OpenUI<PopupPanel>(UILevel.PopUp, new UIPopupData("提示", "加入单位失败", popupDic));
    //    Log.Error($"删除模板票失败！原因为：{failureMessage}");
    //}
    //private static void Relogin(UnityAction success, UnityAction<int, int> failure)
    //{
    //    //后台重新登录
    //    string account = PlayerPrefs.GetString(GlobalInfo.accountCacheKey);
    //    string password = PlayerPrefs.GetString(GlobalInfo.passwordCacheKey);
    //    //RequestManager.Instance.Login(account, password, (data, message) =>
    //    //{
    //    //    LoginSuccess(data, message, success);
    //    //}, (code, errorMessage) =>
    //    //{
    //    //    failure?.Invoke(2, code);
    //    //});
    //    NewRequestManager.Instance.Login(account, password, (data, message) =>
    //    {
    //        NewLoginSuccess(data, message, success);
    //    }, (code, errorMessage) =>
    //    {
    //        failure?.Invoke(2, code);
    //    });
    //}
    //private static void LoginFailure(string failureMessage, UnityEngine.Events.UnityAction<bool> callBack)
    //{
    //    Log.Error($"刷新数据失败！原因为：{failureMessage}");
    //    callBack.Invoke(false);
    //}
    #endregion

    //private void RefreshView()
    //{
    //    this.GetComponentByChildName<InputField>("PhonenumberInputField").text = GlobalInfo.account.mobile;
    //    this.GetComponentByChildName<Text>("MaskPhonenumber").text = PhonenumberMask(GlobalInfo.account.mobile);
    //}

    private string PhonenumberMask(string target)
    {
        return $"{target.Substring(0, 3)}****{target.Substring(target.Length - 4, 4)}";
    }

    private string PasswordMask(string target)
    {
        string result = "";
        for (int i = 0; i < target.Length; i++)
        {
            result += "●";
        }
        return result;
    }
}