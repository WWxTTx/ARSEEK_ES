//using ch.sycoforge.Decal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// shader重赋值（解决某些shader在移动端无法正确显示的问题）
/// </summary>
public static class ShaderRecover
{
    /// <summary>
    /// 强制刷新，包含未显示对象（创建相同参数的新材质，解决部分材质刷新后才能正常显示问题）
    /// </summary>
    /// <param name="obj">刷新对象</param>
    public static void Refresh(GameObject obj)
    {
        if (obj == null)
            return;

        List<GameObject> hideObj = new List<GameObject>();
        Transform[] meshSkinRenderer = obj.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < meshSkinRenderer.Length; i++)
        {
            if (!meshSkinRenderer[i].gameObject.activeSelf)
            {
                hideObj.Add(meshSkinRenderer[i].gameObject);
                meshSkinRenderer[i].gameObject.SetActive(true);
            }
        }
        //暂时停用重置shader 问题可能被变体解决了
        //SetShader(obj);
        UpdateEasyDecal(obj);
        for (int i = 0; i < hideObj.Count; i++)
            hideObj[i].SetActive(false);
    }
    /// <summary>
    /// 强制刷新已显示的对象（创建相同参数的新材质，解决部分材质刷新后才能正常显示问题）
    /// </summary>
    /// <param name="obj">刷新对象</param>
    public static void SetShader(GameObject obj)
    {
        if (obj == null) return;

        Renderer[] meshSkinRenderer = obj.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < meshSkinRenderer.Length; i++)
        {
            Material[] sharedMats = meshSkinRenderer[i].sharedMaterials;
            Material[] mats = new Material[sharedMats.Length];
            if (mats == null || mats.Length == 0 || meshSkinRenderer[i].GetComponent<ParticleSystem>())
                continue;

            for (int j = 0; j < mats.Length; j++)
            {
                if (sharedMats[j] == null || sharedMats[j].shader == null || sharedMats[j].shader.name.Equals(""))
                {
                    Log.Error(string.Format("<color=red>{0}</color>物体的材质球丢失请注意！", meshSkinRenderer[i].gameObject.name));
                    continue;
                }
                Shader shader = Shader.Find(sharedMats[j].shader.name);
                if (shader != null)
                {
                    mats[j] = new Material(shader);
                    mats[j].CopyPropertiesFromMaterial(sharedMats[j]);
                }
                else Log.Error("未找到shader：" + sharedMats[j].shader.name);
            }

            meshSkinRenderer[i].sharedMaterials = mats;
        }
    }

    public static void UpdateEasyDecal(GameObject obj)
    {
        if (obj == null) return;

        //EasyDecal[] meshSkinRenderer = obj.GetComponentsInChildren<EasyDecal>();
        //for (int i = 0; i < meshSkinRenderer.Length; i++)
        //{
        //    if (meshSkinRenderer[i] == null || meshSkinRenderer[i].DecalMaterial == null || 
        //        meshSkinRenderer[i].DecalMaterial.shader == null || meshSkinRenderer[i].DecalMaterial.shader.name.Equals(""))
        //    {
        //        Log.Error(string.Format("<color=red>{0}</color>物体的材质球丢失请注意！", meshSkinRenderer[i].gameObject.name));
        //        continue;
        //    }
        //    Shader shader = Shader.Find(meshSkinRenderer[i].DecalMaterial.shader.name);
        //    if (shader != null)
        //    {
        //        Material mat = new Material(shader);
        //        mat.CopyPropertiesFromMaterial(meshSkinRenderer[i].DecalMaterial);
        //        meshSkinRenderer[i].DecalMaterial = mat;
        //    }
        //    else Log.Error("未找到shader：" + meshSkinRenderer[i].DecalMaterial.shader.name);
        //}
    }
}
