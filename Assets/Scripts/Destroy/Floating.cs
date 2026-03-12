using DG.Tweening;
using UnityEngine;

/// <summary>
/// 首页模块浮动动效
/// </summary>
public class Floating : MonoBehaviour
{
    public float Height = 10f;
    public float LoopTime = 2;
    private Sequence animeList;

    private void Awake()
    {
        animeList = DOTween.Sequence();
        animeList.Append(transform.DOLocalMoveY(transform.localPosition.y + Height, LoopTime).SetEase(Ease.InOutSine));
        animeList.Append(transform.DOLocalMoveY(transform.localPosition.y, LoopTime).SetEase(Ease.InOutSine));
    }
    private void OnEnable()
    {
        animeList.Play().SetLoops(-1);
    }
    private void OnDisable()
    {
        animeList.Pause();
    }
}