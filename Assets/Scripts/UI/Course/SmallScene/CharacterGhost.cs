using UnityEngine;

/// <summary>
/// 角色重叠透明。挂在角色根节点上，当附近有其他角色时把指定渲染器换成透明材质，
/// 避免多个角色模型互相穿插时糊成一团。
/// 本地角色（PlayerController）和远端角色（GazeIndicator 实例化的 Player）共用这个组件。
/// </summary>
public class CharacterGhost : MonoBehaviour
{
    /// <summary>
    /// 重叠时替换上去的透明材质
    /// </summary>
    public Material ghostMaterial;
    /// <summary>
    /// 需要替换材质的渲染器（Box002/Box003/Box004）
    /// </summary>
    public Renderer[] overlapRenderers;
    /// <summary>
    /// 判定为重叠的水平距离
    /// </summary>
    public float overlapDistance = 1f;

    private Material[][] originalMaterials;
    private bool isGhosting;

    private void Awake()
    {
        CacheMaterials();
        OverlapDetection.RegisterCharacter(transform);
    }

    private void LateUpdate()
    {
        UpdateGhost();
    }

    private void OnDestroy()
    {
        OverlapDetection.UnregisterCharacter(transform);
        Restore();
    }

    private void CacheMaterials()
    {
        if (overlapRenderers == null)
            return;

        originalMaterials = new Material[overlapRenderers.Length][];
        for (int i = 0; i < overlapRenderers.Length; i++)
        {
            if (overlapRenderers[i] != null)
                originalMaterials[i] = overlapRenderers[i].sharedMaterials;
        }
    }

    private void UpdateGhost()
    {
        if (ghostMaterial == null || overlapRenderers == null || overlapRenderers.Length == 0)
            return;

        bool overlapping = OverlapDetection.IsOverlapping(transform, overlapDistance);
        if (overlapping == isGhosting)
            return;

        isGhosting = overlapping;
        if (overlapping)
            Apply();
        else
            Restore();
    }

    private void Apply()
    {
        for (int i = 0; i < overlapRenderers.Length; i++)
        {
            Renderer r = overlapRenderers[i];
            if (r == null)
                continue;

            Material[] mats = new Material[r.sharedMaterials.Length];
            for (int j = 0; j < mats.Length; j++)
                mats[j] = ghostMaterial;
            r.sharedMaterials = mats;
        }
    }

    private void Restore()
    {
        if (overlapRenderers == null || originalMaterials == null)
            return;

        for (int i = 0; i < overlapRenderers.Length; i++)
        {
            if (overlapRenderers[i] != null && originalMaterials[i] != null)
                overlapRenderers[i].sharedMaterials = originalMaterials[i];
        }
    }
}
