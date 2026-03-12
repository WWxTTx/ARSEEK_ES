using DG.Tweening;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityFramework.Runtime;

/// <summary>
/// 过渡状态联动、带有点击音效的Dropdown
/// </summary>
public class Dropdown_LinkMode : Dropdown, ILinkMode, IPointerClickHandler
{
    public bool CanControl => interactable;

    /// <summary>
    /// 子物体联动组件列表
    /// </summary>
    private List<ILinkMode> linkModeComponents = new List<ILinkMode>();

    /// <summary>
    /// 下拉动效时长
    /// </summary>
    private float mAnimTime = 0.3f;

    private RectTransform arrowImage;

    protected override void Awake()
    {
        base.Awake();

        foreach (Transform item in transform)
        {
            if (item.TryGetComponent(out ILinkMode component) && component.CanControl)
            {
                linkModeComponents.Add(component);
            }
        }

        template.AutoComponent<Mask>();
        //透明时间影响这个组件何时删除DropdownList所以要设置为和动画时间相同
        alphaFadeSpeed = mAnimTime;

        var image = template.GetComponent<Image>();
        image.fillMethod = Image.FillMethod.Vertical;
        image.fillOrigin = 1;

        //箭头悬浮纵向移动
        arrowImage = this.FindChildByName("Arrow") as RectTransform;
        UIMoveVertical moveVertical = this.AutoComponent<UIMoveVertical>();
        moveVertical.Init(arrowImage, -3f, 0f);
    }

    protected override void Start()
    {
        base.Start();
        DoStateTransition(currentSelectionState, true);
    }
    public void SetState(int state, bool instant)
    {
        base.DoStateTransition((SelectionState)state, instant);
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        foreach (ILinkMode c in linkModeComponents)
        {
            c.SetState((int)state, instant);
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (interactable)
        {
            SoundManager.Instance.PlayEffect(SoundManager.ButtonClick);
        }
        base.OnPointerClick(eventData);
        DropdownAnim();
    }


    protected override GameObject CreateBlocker(Canvas rootCanvas)
    {
        GameObject blocker = base.CreateBlocker(rootCanvas);
        if (blocker.TryGetComponent(out Button button))
        {
            DestroyImmediate(button);
        }
        blocker.AutoComponent<Button_LinkMode>().onClick.AddListener(Hide);
        return blocker;
    }

    /// <summary>
    /// 下拉动效
    /// </summary>
    private void DropdownAnim()
    {
        var target = this.GetComponentByChildName<Image>("Dropdown List");

        //去除组件自带的透明效果(在DropdownList上添加了CanvasGroup)
        target.GetComponent<CanvasGroup>().enabled = false;

        //展开动效
        target.type = Image.Type.Filled;
        target.fillAmount = 0;
#if UNITY_STANDALONE
        arrowImage.DOLocalRotate(new Vector3(0, 0, 90), mAnimTime);
#else
        arrowImage.DOLocalRotate(new Vector3(0, 0, -180), mAnimTime, RotateMode.LocalAxisAdd);
#endif
        DOTween.To(() => target.fillAmount, value => target.fillAmount = value, 1, mAnimTime).OnComplete(() => target.type = image.type);

        //收起动效
        UnityAction close = () =>
        {
            target.type = Image.Type.Filled;
            target.fillAmount = 1;
#if UNITY_STANDALONE
            arrowImage.DOLocalRotate(new Vector3(0, 0, 270), mAnimTime);
#else
            arrowImage.DOLocalRotate(new Vector3(0, 0, 180), mAnimTime, RotateMode.LocalAxisAdd);
#endif
            DOTween.To(() => target.fillAmount, value => target.fillAmount = value, 0, mAnimTime);
        };

        UIManager.Instance.canvas.GetComponentByChildName<Button>("Blocker").onClick.AddListener(() => close.Invoke());
        //item.onValueChanged 不能覆盖选择当前选项的情况 会导致点击已选中的value不播放关闭动画
        foreach (var toggle in target.GetComponentsInChildren<Toggle>(true))
        {
            toggle.onValueChanged.AddListener(isOn => close.Invoke());
        }
    }


#if UNITY_EDITOR
    [MenuItem("CONTEXT/Dropdown/Convert to Dropdown_LinkMode")]
    static void Convert()
    {
        bool interactable;
        Selectable.Transition transition;
        Graphic targetGraphic;
        ColorBlock colorBlock = ColorBlock.defaultColorBlock;
        SpriteState spriteState;
        AnimationTriggers animationTriggers = null;
        Navigation navigation;
        RectTransform template;
        Text captionText;
        Image captionImage;
        Text itemText;
        Image itemImage;
        int value;
        float alphaFadeSpeed;
        List<Dropdown.OptionData> options;

        Dropdown dropdown = Selection.activeGameObject.GetComponent<Dropdown>();
        interactable = dropdown.interactable;
        transition = dropdown.transition;
        targetGraphic = dropdown.targetGraphic;
        switch (transition)
        {
            case Selectable.Transition.ColorTint:
                colorBlock = dropdown.colors;
                break;
            case Selectable.Transition.SpriteSwap:
                spriteState = dropdown.spriteState;
                break;
            case Selectable.Transition.Animation:
                animationTriggers = dropdown.animationTriggers;
                break;
        }
        navigation = dropdown.navigation;
        template = dropdown.template;
        captionText = dropdown.captionText;
        captionImage = dropdown.captionImage;
        itemText = dropdown.itemText;
        itemImage = dropdown.itemImage;
        value = dropdown.value;
        alphaFadeSpeed = dropdown.alphaFadeSpeed;
        options = dropdown.options;

        DestroyImmediate(dropdown, true);

        Dropdown_LinkMode dropdown_LinkMode = Selection.activeGameObject.AddComponent<Dropdown_LinkMode>();
        dropdown_LinkMode.interactable = interactable;
        dropdown_LinkMode.transition = transition;
        dropdown_LinkMode.targetGraphic = targetGraphic;
        switch (transition)
        {
            case Selectable.Transition.ColorTint:
                dropdown_LinkMode.colors = colorBlock;
                break;
            case Selectable.Transition.SpriteSwap:
                dropdown_LinkMode.spriteState = spriteState;
                break;
            case Selectable.Transition.Animation:
                dropdown_LinkMode.animationTriggers = animationTriggers;
                break;
        }
        dropdown_LinkMode.navigation = navigation;
        dropdown_LinkMode.template = template;
        dropdown_LinkMode.captionText = captionText;
        dropdown_LinkMode.captionImage = captionImage;
        dropdown_LinkMode.itemText = itemText;
        dropdown_LinkMode.itemImage = itemImage;
        dropdown_LinkMode.value = value;
        dropdown_LinkMode.alphaFadeSpeed = alphaFadeSpeed;
        dropdown_LinkMode.options = options;
    }
#endif
}