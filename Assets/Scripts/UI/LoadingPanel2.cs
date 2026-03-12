using UnityFramework.Runtime;

public class LoadingPanel2 : UIPanelBase
{
    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        var data = uiData as PanelData;
        {
            this.GetComponentByChildName<UnityEngine.UI.Text>("Tip").text = data.tip;
            data.slider.Invoke(this.GetComponentByChildName<UnityEngine.UI.Slider>("Slider"));
            //data.slider.Invoke(GetComponentInChildren<UnityEngine.UI.Slider>());
        }
    }

    public class PanelData : UIData
    {
        public string tip { get; set; }
        public UnityEngine.Events.UnityAction<UnityEngine.UI.Slider> slider { get; set; }
    }
}
