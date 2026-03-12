using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 带有点击音效的Toggle
/// </summary>
public class ToggleWithSoundEffect : Toggle
{
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (interactable)
        {
            SoundManager.Instance.PlayEffect(SoundManager.ButtonClick);
        }
        base.OnPointerClick(eventData);
    }
}