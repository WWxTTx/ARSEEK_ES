using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;

public class ShowAnimation : MonoBehaviour
{
    public Vector3 startScale = Vector3.zero;
    public Vector2 startPosition = Vector2.zero;
    public Vector3 endScale = Vector3.one;
    public Vector2 endPosition = Vector2.zero;
    public float useTime = 1f;
    
    private RectTransform rectTransform;

    private Tweener tween1;
    private Tweener tween2;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (endPosition.x == 0 && endPosition.y == 0) 
        {
            endPosition = rectTransform.anchoredPosition;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if(tween1 != null)
            tween1.Kill();
        if (tween2 != null)
            tween2.Kill();

        rectTransform.localScale = startScale;
        rectTransform.anchoredPosition = startPosition;

        tween1 = rectTransform.DOScale(endScale, useTime).SetEase(Ease.Linear);
        tween2 = rectTransform.DOAnchorPos(endPosition, useTime).SetEase(Ease.Linear);
    }

    public void Close() 
    {
        if (tween1 != null)
            tween1.Kill();
        if (tween2 != null)
            tween2.Kill();

        tween1 = rectTransform.DOScale(startScale, useTime).SetEase(Ease.Linear);
        tween2 = rectTransform.DOAnchorPos(startPosition, useTime).SetEase(Ease.Linear);
        tween2.onComplete += () => { gameObject.SetActive(false); };
    }
}
