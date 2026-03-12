using UnityEngine.UI;
using UnityFramework.Runtime;

public class Option_AboutModule : UIModuleBase
{
    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);
        Init();
    }

    private void Init()
    {
        this.GetComponentByChildName<Text>("VersionText").text = $"V{UnityEngine.Application.version}";
        //this.GetComponentByChildName<Button>("ShowAgreement").onClick.AddListener(() =>
        //{
        //    UIManager.Instance.OpenModuleUI<UserAgreementModule>(ParentPanel, transform.parent);
        //});
    }
}
