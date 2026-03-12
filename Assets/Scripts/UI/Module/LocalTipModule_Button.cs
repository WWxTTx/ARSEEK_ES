using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityFramework.Runtime;

/// <summary>
/// 模块界面内部提示 带按钮
/// </summary>
public class LocalTipModule_Button : UIModuleBase
{
    private Image Image;
    private Text Info;
    private Button Button;
    private Text ButtonText;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        Image = transform.GetComponentByChildName<Image>("Image");
        Info = transform.GetComponentByChildName<Text>("Info");
        Button = transform.GetComponentByChildName<Button>("Button");
        ButtonText = Button.GetComponentInChildren<Text>();
    }

    public override void Show(UIData uiData = null)
    {
        base.Show(uiData);

        if (uiData != null)
        {
            ModuleData moduleData = uiData as ModuleData;

            if(moduleData.Sprite != null)
                Image.sprite = moduleData.Sprite;
            Info.text = moduleData.Info;
            ButtonText.text = moduleData.ButtonText;
            Button.onClick.AddListener(() =>
            {
                moduleData.OnButtonClick?.Invoke();
                UIManager.Instance.CloseModuleUI<LocalTipModule_Button>(ParentPanel);
            });    
            
            if(moduleData.SiblingIndex >= 0)
            {
                transform.SetSiblingIndex(moduleData.SiblingIndex);
            }
        }
    }

    public class ModuleData : UIData
    {
        public Sprite Sprite { get; set; }
        public string Info { get; set; }
        public UnityAction OnButtonClick { get; set; }
        public string ButtonText { get; set; }
        public int SiblingIndex { get; set; }

        public ModuleData(Sprite sprite, string info, string buttonText, UnityAction onButtonClick, int siblingIndex = -1)
        {
            this.Sprite = sprite;
            this.Info = info;
            this.ButtonText = buttonText;
            this.OnButtonClick = onButtonClick;
            this.SiblingIndex = siblingIndex;
        }

        public ModuleData(string info, string buttonText, UnityAction onButtonClick, int siblingIndex = -1)
        {
            this.Info = info;
            this.ButtonText = buttonText;
            this.OnButtonClick = onButtonClick;
            this.SiblingIndex = siblingIndex;
        }
    }
}
