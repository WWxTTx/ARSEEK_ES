using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 一个动效物体的脚本
/// </summary>
public class PointComponent : MonoBehaviour
{
    private const float animeTime = 0.3f;
    private const float moveDistance = 30;
    private Vector2 smallSize = new Vector2(10, 10);
    private Vector2 normalSize = new Vector2(16, 16);
    private float hideTime = 5f;

    private RectTransform content;
    private GameObject item;
    private CanvasGroup canvasGroup;

    private List<GameObject> points = new List<GameObject>();
    private int topPoint = 0;
    private int currentSelect;
    private bool canMove = false;
    private float currentTime = 0;

    private void Awake()
    {
        content = this.FindChildByName("Content").GetComponent<RectTransform>();
        item = this.FindChildByName("Item").gameObject;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 刷新总点数
    /// </summary>
    /// <param name="size">点数</param>
    public void RefreshPoints(int size)
    {
        canMove = size > 4;

        if (size > points.Count)
        {
            for (int i = 0, length = size - points.Count; i < length; i++)
            {
                var item = Instantiate(this.item, content);
                {
                    item.SetActive(true);
                    points.Add(item);
                }
            }
        }
        else if (size < points.Count)
        {
            foreach (var point in points.Skip(size))
            {
                points.Add(point);
                Destroy(point.gameObject);
            }
        }

        if (canMove)
        {
            if (currentSelect >= points.Count)
            {
                topPoint = points.Count - 4;
                content.sizeDelta = Vector2.up * 30;
                Select(points.Count - 1);
            }

            GetComponent<RectTransform>().sizeDelta = new Vector2(48, 130);
            canvasGroup.alpha = 1;
            currentTime = 0;
            enabled = true;
        }
        else
        {
            switch (points.Count)
            {
                case 4:
                    GetComponent<RectTransform>().sizeDelta = new Vector2(48, 130);
                    break;
                case 3:
                    GetComponent<RectTransform>().sizeDelta = new Vector2(48, 100);
                    break;
                case 2:
                    GetComponent<RectTransform>().sizeDelta = new Vector2(48, 70);
                    break;
                case 1:
                    canvasGroup.alpha = 0;
                    enabled = false;
                    break;
            }

            topPoint = 0;
            content.sizeDelta = Vector2.zero;

            if (currentSelect >= points.Count)
            {
                Select(points.Count - 1);
            }
        }

        UpdateState();
    }
    /// <summary>
    /// 向下翻页 
    /// 整体页面向上滑
    /// </summary>
    [ContextMenu("向上滑")]
    public void MoveUp()
    {
        canvasGroup.DOFade(1, 1f);
        currentTime = 0;

        if (!canMove)
        {
            Select(currentSelect + 1);
            return;
        }

        if (!enabled)
        {
            return;
        }

        enabled = false;

        if (topPoint + 4 < points.Count && (currentSelect > topPoint + 1))
        {
            topPoint++;
            UpdateState();

            content.DOAnchorPos3DY(moveDistance, animeTime).SetRelative().OnComplete(() =>
            {
                Select(currentSelect + 1);
                enabled = true;
            });
        }
        else
        {
            UpdateState();
            Select(currentSelect + 1);
            enabled = true;
        }
    }
    /// <summary>
    /// 向上翻页 
    /// 整体页面向下滑
    /// </summary>
    [ContextMenu("向下滑")]
    public void MoveDown()
    {
        canvasGroup.DOFade(1, 1f);
        currentTime = 0;

        if (!canMove)
        {
            Select(currentSelect - 1);
            return;
        }

        if (!enabled)
        {
            return;
        }

        enabled = false;

        if (topPoint > 0 && currentSelect < topPoint + 2)
        {
            topPoint--;
            UpdateState();
            content.DOAnchorPos3DY(-moveDistance, animeTime).SetRelative().OnComplete(() =>
            {
                Select(currentSelect - 1);
                enabled = true;
            });
        }
        else
        {
            UpdateState();
            Select(currentSelect - 1);
            enabled = true;
        }
    }
    /// <summary>
    /// 更新点的状态
    /// </summary>
    private void UpdateState()
    {
        if (!canMove)
        {
            foreach (var point in points)
            {
                SetPoint(point, normalSize);
            }
            return;
        }

        if (topPoint == 0)
        {
            SetPoint(points[topPoint], normalSize);
            SetPoint(points[topPoint + 1], normalSize);
            SetPoint(points[topPoint + 2], normalSize);
            SetPoint(points[topPoint + 3], smallSize);
            SetPoint(points[topPoint + 4], smallSize);
        }
        else if (topPoint + 4 == points.Count)
        {
            SetPoint(points[topPoint - 1], smallSize);
            SetPoint(points[topPoint], smallSize);
            SetPoint(points[topPoint + 1], normalSize);
            SetPoint(points[topPoint + 2], normalSize);
            SetPoint(points[topPoint + 3], normalSize);
        }
        else
        {
            SetPoint(points[topPoint - 1], smallSize);
            SetPoint(points[topPoint], smallSize);
            SetPoint(points[topPoint + 1], normalSize);
            SetPoint(points[topPoint + 2], normalSize);
            SetPoint(points[topPoint + 3], smallSize);
            SetPoint(points[topPoint + 4], smallSize);
        }
    }
    /// <summary>
    /// 设置点的大小
    /// 使用dotween动画
    /// </summary>
    /// <param name="item">点</param>
    /// <param name="size">大小</param>
    private void SetPoint(GameObject item, Vector2 size)
    {
        item.transform.GetChild(0).GetComponent<RectTransform>().DOSizeDelta(size, animeTime);
    }
    /// <summary>
    /// 选中点
    /// </summary>
    /// <param name="index"></param>
    private void Select(int index)
    {
        if(points.Count>0)
        {
            index = Mathf.Max(0, index);
            index = Mathf.Min(points.Count - 1, index);
            currentSelect = index;
            points[currentSelect].GetComponentInChildren<UnityEngine.UI.Toggle>().isOn = true;
        }
    }

    private void Update()
    {
        currentTime += Time.deltaTime;

        if (currentTime > hideTime)
        {
            canvasGroup.DOFade(0.4f, 1);
            currentTime = 0;
            enabled = false;
        }
    }
}