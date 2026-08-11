using UnityEngine;

/// <summary>
/// 角色重叠透明。挂在角色根节点上，当附近有其他角色时处理模型透明。
/// 由位置更新驱动：位置更新时调用 OnPositionUpdated，
/// 检测到附近有其他角色则引用对方的材质方法使其透明，保留引用直到对方离开时恢复。
/// </summary>
public class CharacterGhost : MonoBehaviour
{
    public Material ghostMaterial;
    public Renderer[] overlapRenderers;
    public float overlapDistance = 1f;

    /// <summary>
    /// 可选，当角色实际位置的 Transform 与挂载 GameObject 不同时指定（如 GazeIndicator 的 start 子节点）。
    /// 不设置时使用 transform。
    /// </summary>
    [HideInInspector]
    public Transform positionSource;

    private Transform Pos => positionSource != null ? positionSource : transform;

    private Material[][] originalMaterials;
    private int ghostRefCount;

    private void Awake()
    {
        CacheMaterials();
        OverlapDetection.RegisterCharacter(Pos);
    }

    private void OnDestroy()
    {
        OverlapDetection.UnregisterCharacter(Pos);
        Restore();

        if (currentTarget != null)
        {
            currentTarget.RemoveGhostRef();
            currentTarget = null;
        }
    }

    private void CacheMaterials()
    {
        var allRenderers = new System.Collections.Generic.List<Renderer>();
        if (overlapRenderers != null)
            allRenderers.AddRange(overlapRenderers);

        foreach (var sr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (!allRenderers.Contains(sr))
                allRenderers.Add(sr);
        }

        overlapRenderers = allRenderers.ToArray();

        originalMaterials = new Material[overlapRenderers.Length][];
        for (int i = 0; i < overlapRenderers.Length; i++)
        {
            if (overlapRenderers[i] != null)
                originalMaterials[i] = overlapRenderers[i].sharedMaterials;
        }
    }

    /// <summary>
    /// 位置更新后调用。检测附近是否有其他角色，若有则引用对方的 AddGhostRef 使其透明，
    /// 保留引用直到对方离开时调用 RemoveGhostRef 恢复不透明并释放引用。
    /// </summary>
    public void OnPositionUpdated()
    {
        if (ghostMaterial == null || overlapRenderers == null || overlapRenderers.Length == 0)
            return;

        Transform other = OverlapDetection.GetOverlappingCharacter(Pos, overlapDistance);
        CharacterGhost newTarget = other != null ? other.GetComponent<CharacterGhost>() : null;

        if (newTarget == currentTarget)
            return;

        if (currentTarget != null)
            currentTarget.RemoveGhostRef();

        currentTarget = newTarget;

        if (currentTarget != null)
            currentTarget.AddGhostRef();
    }

    /// <summary>
    /// 将自己变透明（当附近有其他角色时），离开时恢复。
    /// 供 GazeIndicator 等远端角色调用——远端角色靠近别人时自己变透明。
    /// </summary>
    public void UpdateSelfGhost()
    {
        if (ghostMaterial == null || overlapRenderers == null || overlapRenderers.Length == 0)
            return;

        bool overlapping = OverlapDetection.IsOverlapping(Pos, overlapDistance);
        if (overlapping && !selfGhosting)
        {
            selfGhosting = true;
            AddGhostRef();
        }
        else if (!overlapping && selfGhosting)
        {
            selfGhosting = false;
            RemoveGhostRef();
        }
    }

    public void AddGhostRef()
    {
        ghostRefCount++;
        if (ghostRefCount == 1)
            Apply();
    }

    public void RemoveGhostRef()
    {
        ghostRefCount--;
        if (ghostRefCount <= 0)
        {
            ghostRefCount = 0;
            Restore();
        }
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

    private CharacterGhost currentTarget;
    private bool selfGhosting;
}
