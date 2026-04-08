using DG.Tweening;
using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;


public class TSQ_Dydzcl : MonoBehaviour, IBaseBehaviour
{
    bool IBaseBehaviour.UseCallback(int step) => false;
    public Type GetStatusEnumType() => typeof(AvailableStatus);
    [SerializeField]
    public enum AvailableStatus
    {
        导叶传感器电压 = 0,
        无短路测试 = 1,
        综合输出 = 2,
    }

    public Transform wyb;
    public TextMeshPro Number;
    /// <summary>
    /// 两个电笔测量位置在流程中配置，这里只做触发的显隐控制
    /// </summary>
    public GameObject A;
    public GameObject B;
    public GameObject dy;
    public GameObject dz;

    UnityAction callback;
    UISmallSceneModule smallSceneModule;

    /// <param name="callback"></param>
    void IBaseBehaviour.Execute(int step, UnityAction callback)
    {
        if(smallSceneModule == null)
        {
            smallSceneModule = Transform.FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>();
        }

        Show(step);
        this.callback = callback;
    }

    void IBaseBehaviour.SetFinalState()
    {
    }

 

    /// <summary>
    /// 显示电压电阻变化
    /// </summary>
    /// <param name="step"></param>
    void Show(int step)
    {
        A.SetActive(true);
        B.SetActive(true);
        float target = 0;
        string tip = "";
        switch(step)
        {
            case 0:
                dy.SetActive(true);
                dz.SetActive(false);
                target = UnityEngine.Random.Range(1f, 4f);
                tip = "传感器电压小于7V，导叶传感器异常,需要停机更换设备";
                break;
            case 1:
                dy.SetActive(false);
                dz.SetActive(true);
                target = 9999.99f;
                tip = "传感器电阻大于量程，测量点无短路";
                break;
            case 2:
                dy.SetActive(true);
                dz.SetActive(false);
                target = 23 + UnityEngine.Random.Range(0.9f, 1.1f); 
                tip = "综合输出电压24V±5%，正常";
                break;
        }

        wyb.gameObject.SetActive(true);
        wyb.DOLocalMove(new Vector3(-0.261f, 0.21f, -0.36f), 1);
        //切换镜头后显示万用表
        DOVirtual.DelayedCall(1f, () =>
        {
            DOVirtual.DelayedCall(2, () =>
            {
                DOTween.To(() => 0, (x) =>
                {
                    Number.text = ConvertToSpriteString(x);
                }, target, 1);

            });
            DOVirtual.DelayedCall(5, () =>
            {
                smallSceneModule.ShowHint(tip, -1);
                DOVirtual.DelayedCall(2, () => {
                    wyb.gameObject.SetActive(false);
                    A.SetActive(false);
                    B.SetActive(false);
                    Number.text = "";
                    smallSceneModule.ModelState = ModelState.Unselect;;
                    callback?.Invoke();
                });
            });
        });
    }

    /// <summary>
    /// 将数值转化成meshpro标签
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    string ConvertToSpriteString(float input)
    {
        // 格式化为两位小数（使用不变文化确保小数点格式）
        string formatted = input.ToString("0.00", CultureInfo.InvariantCulture);

        StringBuilder result = new StringBuilder();

        foreach (char c in formatted)
        {
            // 替换数字和小数点为 <sprite> 标签
            result.Append($"<sprite name=\"{c}\">");
        }

        return result.ToString();
    }
}
