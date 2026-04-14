using DG.Tweening;
using HighlightPlus;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HighlightEffectManager : MonoBehaviour
{
    public static HighlightEffectManager Instance;
    /// <summary>
    /// 屏蔽指定权重层
    /// </summary>
    [HideInInspector]
    public List<int> maskPriorityList = new List<int>();
    private Color defaultColor;
    private Dictionary<Component, Dictionary<int, HighlightData>> cache = new Dictionary<Component, Dictionary<int, HighlightData>>();

    private void Awake()
    {
        Application.targetFrameRate = 60;
        Instance = this;
        defaultColor = "#8CECFF".HexToColor();
    }

    public struct HighlightData
    {
        public Color color;
        public float outlineWidth;
        public Visibility visibility;
        public bool constantWidth;
    }

    public void Add(Component component, Color? color = null, float outlineWidth = 0.15f, Visibility? visibility = null, bool constantWidth = false, int priority = 0)
    {
        if (!cache.ContainsKey(component))
        {
            cache.Add(component, new Dictionary<int, HighlightData>());
        }

        if (!cache[component].ContainsKey(priority))
        {
            cache[component].Add(priority, new HighlightData()
            {
                color = color ?? defaultColor,
                outlineWidth = outlineWidth,
                visibility = visibility ?? Visibility.Normal,
                constantWidth = constantWidth
            });
            //Log.Debug($"{component.name} 物体高亮 权重({priority})", component);
        }
        else
        {
            cache[component][priority] = new HighlightData()
            {
                color = color ?? defaultColor,
                outlineWidth = outlineWidth,
                visibility = visibility ?? Visibility.Normal,
                constantWidth = constantWidth
            };
            //Log.Debug($"{component.name }物体高亮 权重({priority})覆盖", component);
        }

        UpdateHighlight(component);
    }
    public void Remove(Component component, int priority = 0)
    {
        if (!cache.ContainsKey(component))
        {
            return;
        }

        if (cache[component].ContainsKey(priority))
        {
            cache[component].Remove(priority);
        }

        if (cache[component].Count == 0)
        {
            cache.Remove(component);
        }

        UpdateHighlight(component);
    }
    public void UpdateHighlight(Component component)
    {
        if (cache.ContainsKey(component))
        {
            var datas = cache[component].Where(data => !maskPriorityList.Contains(data.Key)).OrderBy(data => data.Key);

            if (datas.Count() > 0)
            {
                HighlightEffect highlighter = component.AutoComponent<HighlightEffect>();
                {
                    highlighter.highlighted = true;
                    highlighter.glow = 0;

                    //1
                    {
                        highlighter.outlineColor = datas.Last().Value.color;
                        highlighter.overlay = 0;
                    }

                    //2
                    //{              
                    //    highlighter.overlay = 1;
                    //    highlighter.outlineColor = datas.Last().Value.color;
                    //    highlighter.overlayColor = datas.Last().Value.color;
                    //    highlighter.overlayBlending = 1;
                    //    highlighter.overlayMinIntensity = 0f;
                    //    highlighter.overlayAnimationSpeed = 0.75f;
                    //}

                    highlighter.seeThrough = SeeThroughMode.Never;
                    highlighter.GPUInstancing = false;
                    highlighter.constantWidth = datas.Last().Value.constantWidth; // false;
                    highlighter.outlineWidth = datas.Last().Value.outlineWidth;//0.15f;
                    highlighter.outlineVisibility = datas.Last().Value.visibility;
                    highlighter.outlineIndependent = false;

                    highlighter.enabled = false;
                    highlighter.enabled = true;
                }
            }
        }
        else if (component.TryGetComponent(out HighlightEffect highlightEffect))
        {
            DestroyImmediate(highlightEffect);
        }
    }
    public void Clear(Component component = null)
    {
        if (component != null)
        {
            if (cache.ContainsKey(component))
            {
                cache.Remove(component);
            }
        }
        else
        {
            cache.Clear();
        }
    }
    [ContextMenu("刷新所有高亮")]
    public void RefreshAllHighlight()
    {
        foreach (var key in cache.Keys)
        {
            UpdateHighlight(key);
        }
    }

    private Dictionary<Component, Tweener> tweeners = new Dictionary<Component, Tweener>();
    /// <summary>
    /// 高亮闪烁
    /// </summary>
    public void HighlightFlashing(Component component)
    {
        if (tweeners.ContainsKey(component))
        {
            //Debug.LogWarning("已存在高亮闪烁：" + component.gameObject.name);
            return;
        }

        HighlightEffect highlighter = component.GetComponent<HighlightEffect>();
        if (highlighter == null)
            return;

        Tweener tweener = DOTween.To(() => highlighter.outline = 0, x => highlighter.outline = x, 1, 1);
        tweener.SetEase(Ease.InOutQuad);
        tweener.SetLoops(-1, LoopType.Yoyo);
        tweener.OnKill(() => highlighter.outline = 1f);
        tweeners.Add(component, tweener);
    }
    /// <summary>
    /// 移除高亮闪烁
    /// </summary>
    public void RemoveHighlightFlashing(Component component)
    {
        if (tweeners.ContainsKey(component))
        {
            tweeners[component].Kill();
            tweeners.Remove(component);
        }
        //else
        //{
        //    Debug.LogWarning("未找到需移除高亮闪烁" + component.gameObject.name);
        //}
    }
}