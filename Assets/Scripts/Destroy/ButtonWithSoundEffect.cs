using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 带有点击音效的Button
/// </summary>
public class ButtonWithSoundEffect : Button
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