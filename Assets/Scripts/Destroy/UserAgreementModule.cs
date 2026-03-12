using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;
using UnityFramework.Runtime;

public class UserAgreementModule : UIModuleBase// UIPanelBase
{
    protected override void InitComponents()
    {
        base.InitComponents();
        transform.GetComponentByChildName<Button>("BackBtn").onClick.AddListener(Back);
    }

    private void Back()
    {
        UIManager.Instance.CloseModuleUI<UserAgreementModule>(ParentPanel, null, () =>
        {
            SendMsg(new MsgBase((ushort)LoginEvent.Register));
        });
    }

    #region 动效
    public override void JoinAnim(UnityAction callback)
    {
        var background = this.FindChildByName("Background");
        if (background)
        {
            background.localScale = UnityEngine.Vector3.one * 0.01f;
            JoinSequence.Append(background.DOScale(UnityEngine.Vector3.one, JoinAnimePlayTime));
        }
        base.JoinAnim(callback);
    }

    public override void ExitAnim(UnityAction callback)
    {
        var background = this.FindChildByName("Background");
        if (background)
        {
            ExitSequence.Append(background.DOScale(UnityEngine.Vector3.one * 0.01f, ExitAnimePlayTime));
        }
        base.ExitAnim(callback);
    }
    #endregion
}