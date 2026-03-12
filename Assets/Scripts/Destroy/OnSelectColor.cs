using UnityEngine;
using UnityEngine.UI;

public class OnSelectColor : MonoBehaviour
{
    public Graphic targetGraphic;
    public Color normalColor;
    public Color selectColor;

    private void Awake()
    {
        targetGraphic.CrossFadeColor(normalColor, 0, true, true);
    }

    public void SetColor(bool isOn)
    {
        if (isOn)
            targetGraphic.CrossFadeColor(selectColor, 0.1f, true, true);
        else
            targetGraphic.CrossFadeColor(normalColor, 0.1f, true, true);
    }
}