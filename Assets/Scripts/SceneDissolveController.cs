using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SceneDissolveController : MonoBehaviour
{
    [Header("Test")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private float startDelay = 2f;

    [Header("Transition")]
    [SerializeField] private float duration = 5f;

    [Header("Dissolve")]
    [Tooltip("Shader 中 Dissolve 参数的属性名")]
    [SerializeField] private string dissolveProperty = "_DissolveAmount";

    [Header("Normal Materials")]
    [SerializeField] private bool fadeNormalMaterials = true;

    [Tooltip("普通材质透明速度倍率。2 = 比 Dissolve 快一倍")]
    [SerializeField] private float normalFadeSpeedMultiplier = 2f;

    [Header("Skybox")]
    [SerializeField] private bool fadeSkyboxToBlack = true;

    [Tooltip("Skybox 变黑速度倍率")]
    [SerializeField] private float skyboxFadeSpeedMultiplier = 2f;

    [Header("Exclude")]
    [Tooltip("这些 Tag 的对象以及它们的所有子物体都不会参与消散")]
    [SerializeField]
    private string[] excludeTags =
    {
        "Torch"
    };


    // =========================================================
    // Material Data
    // =========================================================

    private readonly List<Material> dissolveMaterials =
        new List<Material>();

    private readonly List<NormalMaterialData> normalMaterials =
        new List<NormalMaterialData>();


    private class NormalMaterialData
    {
        public Material material;
        public string colorProperty;
        public Color originalColor;

        public NormalMaterialData(
            Material material,
            string colorProperty,
            Color originalColor)
        {
            this.material = material;
            this.colorProperty = colorProperty;
            this.originalColor = originalColor;
        }
    }


    // =========================================================
    // Skybox
    // =========================================================

    private Material skyboxMaterial;
    private Color originalSkyColor;

    private bool isPlaying = false;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        CacheSceneMaterials();
        CacheSkybox();
    }


    // =========================================================
    // Test
    // =========================================================

    private IEnumerator Start()
    {
        if (!playOnStart)
            yield break;

        yield return new WaitForSeconds(
            startDelay
        );

        PlayDissolve();
    }


    // =========================================================
    // External Call
    // =========================================================

    public void PlayDissolve()
    {
        if (isPlaying)
            return;

        StartCoroutine(
            DissolveCoroutine()
        );
    }


    // =========================================================
    // Find Materials
    // =========================================================

    private void CacheSceneMaterials()
    {
        dissolveMaterials.Clear();
        normalMaterials.Clear();

        Renderer[] renderers =
            FindObjectsOfType<Renderer>(true);

        HashSet<Material> processedMaterials =
            new HashSet<Material>();


        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;


            // =================================================
            // 排除对象
            // =================================================

            if (ShouldExclude(renderer.transform))
            {
                Debug.Log(
                    "[SceneDissolve] Skip: " +
                    GetHierarchyPath(renderer.transform)
                );

                continue;
            }


            Material[] materials =
                renderer.materials;


            foreach (Material mat in materials)
            {
                if (mat == null)
                    continue;


                // 同一个实例不要重复加入
                if (processedMaterials.Contains(mat))
                    continue;

                processedMaterials.Add(mat);


                // =================================================
                // Dissolve Material
                // =================================================

                if (mat.HasProperty(dissolveProperty))
                {
                    dissolveMaterials.Add(mat);

                    continue;
                }


                // 兼容不带 "_" 的属性名
                if (mat.HasProperty("DissolveAmount"))
                {
                    dissolveMaterials.Add(mat);

                    continue;
                }


                // =================================================
                // Normal Material
                // =================================================

                if (!fadeNormalMaterials)
                    continue;


                string colorProperty =
                    FindColorProperty(mat);


                if (!string.IsNullOrEmpty(colorProperty))
                {
                    Color originalColor =
                        mat.GetColor(colorProperty);


                    normalMaterials.Add(
                        new NormalMaterialData(
                            mat,
                            colorProperty,
                            originalColor
                        )
                    );
                }
            }
        }


        Debug.Log(
            "===== Scene Dissolve =====\n" +
            "Renderer 数量：" + renderers.Length + "\n" +
            "Dissolve 材质：" + dissolveMaterials.Count + "\n" +
            "普通材质：" + normalMaterials.Count
        );
    }


    // =========================================================
    // Exclude
    // =========================================================

    private bool ShouldExclude(
        Transform target)
    {
        Transform current =
            target;


        while (current != null)
        {
            foreach (string tagName in excludeTags)
            {
                if (string.IsNullOrEmpty(tagName))
                    continue;


                // CompareTag 如果 Tag 根本不存在，
                // Unity 会直接报错。
                // 所以这里要求 Inspector 填的是已经创建好的 Tag。
                if (current.CompareTag(tagName))
                {
                    return true;
                }
            }


            current =
                current.parent;
        }


        return false;
    }


    // =========================================================
    // Find Base Color
    // =========================================================

    private string FindColorProperty(
        Material mat)
    {
        // URP/Lit
        if (mat.HasProperty("_BaseColor"))
            return "_BaseColor";


        // Standard / 部分 Shader
        if (mat.HasProperty("_Color"))
            return "_Color";


        // Particles / 一些自定义 Shader
        if (mat.HasProperty("_TintColor"))
            return "_TintColor";


        if (mat.HasProperty("_Tint"))
            return "_Tint";


        return "";
    }


    // =========================================================
    // Skybox
    // =========================================================

    private void CacheSkybox()
    {
        if (RenderSettings.skybox == null)
        {
            Debug.LogWarning(
                "[SceneDissolve] 没有找到 Skybox"
            );

            return;
        }


        // 创建运行时实例
        // 防止直接修改 Project 里的原始材质
        skyboxMaterial =
            new Material(
                RenderSettings.skybox
            );


        RenderSettings.skybox =
            skyboxMaterial;


        Debug.Log(
            "[Skybox] Shader = " +
            skyboxMaterial.shader.name
        );


        if (skyboxMaterial.HasProperty("_Tint"))
        {
            originalSkyColor =
                skyboxMaterial.GetColor("_Tint");
        }
        else if (
            skyboxMaterial.HasProperty("_SkyTint"))
        {
            originalSkyColor =
                skyboxMaterial.GetColor("_SkyTint");
        }
    }


    // =========================================================
    // Dissolve
    // =========================================================

    private IEnumerator DissolveCoroutine()
    {
        isPlaying = true;


        // =====================================================
        // 普通 URP 材质切成 Transparent
        // =====================================================

        if (fadeNormalMaterials)
        {
            foreach (
                NormalMaterialData data
                in normalMaterials)
            {
                if (data.material == null)
                    continue;


                SetupMaterialForTransparency(
                    data.material
                );
            }
        }


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;


            // 主 Dissolve 进度
            float value =
                Mathf.Clamp01(
                    elapsed / duration
                );


            // 普通材质更快
            float normalFadeValue =
                Mathf.Clamp01(
                    value *
                    normalFadeSpeedMultiplier
                );


            // Skybox 更快
            float skyboxFadeValue =
                Mathf.Clamp01(
                    value *
                    skyboxFadeSpeedMultiplier
                );


            // =================================================
            // 1. Dissolve Materials
            // =================================================

            foreach (Material mat in dissolveMaterials)
            {
                if (mat == null)
                    continue;


                if (mat.HasProperty(dissolveProperty))
                {
                    mat.SetFloat(
                        dissolveProperty,
                        value
                    );
                }
                else if (
                    mat.HasProperty("DissolveAmount"))
                {
                    mat.SetFloat(
                        "DissolveAmount",
                        value
                    );
                }
            }


            // =================================================
            // 2. Normal Materials -> Transparent
            // =================================================

            if (fadeNormalMaterials)
            {
                foreach (
                    NormalMaterialData data
                    in normalMaterials)
                {
                    if (data.material == null)
                        continue;


                    Color color =
                        data.originalColor;


                    color.a =
                        Mathf.Lerp(
                            data.originalColor.a,
                            0f,
                            normalFadeValue
                        );


                    data.material.SetColor(
                        data.colorProperty,
                        color
                    );
                }
            }


            // =================================================
            // 3. Skybox -> Black
            // =================================================

            if (
                fadeSkyboxToBlack &&
                skyboxMaterial != null)
            {
                Color skyColor =
                    Color.Lerp(
                        originalSkyColor,
                        Color.black,
                        skyboxFadeValue
                    );


                if (
                    skyboxMaterial.HasProperty("_Tint"))
                {
                    skyboxMaterial.SetColor(
                        "_Tint",
                        skyColor
                    );
                }
                else if (
                    skyboxMaterial.HasProperty("_SkyTint"))
                {
                    skyboxMaterial.SetColor(
                        "_SkyTint",
                        skyColor
                    );
                }
            }


            yield return null;
        }


        SetFinalState();

        isPlaying = false;


        Debug.Log(
            "===== Scene Dissolve Complete ====="
        );
    }


    // =========================================================
    // URP -> Transparent
    // =========================================================

    private void SetupMaterialForTransparency(
        Material mat)
    {
        if (mat == null)
            return;


        // URP Lit / Simple Lit 等通常有 _Surface
        if (!mat.HasProperty("_Surface"))
            return;


        // Surface Type
        // 0 = Opaque
        // 1 = Transparent
        mat.SetFloat(
            "_Surface",
            1f
        );


        // Blend = Alpha
        if (mat.HasProperty("_Blend"))
        {
            mat.SetFloat(
                "_Blend",
                0f
            );
        }


        // Src Blend
        if (mat.HasProperty("_SrcBlend"))
        {
            mat.SetFloat(
                "_SrcBlend",
                (float)BlendMode.SrcAlpha
            );
        }


        // Dst Blend
        if (mat.HasProperty("_DstBlend"))
        {
            mat.SetFloat(
                "_DstBlend",
                (float)BlendMode.OneMinusSrcAlpha
            );
        }


        // ZWrite Off
        if (mat.HasProperty("_ZWrite"))
        {
            mat.SetFloat(
                "_ZWrite",
                0f
            );
        }


        mat.renderQueue =
            (int)RenderQueue.Transparent;


        mat.SetOverrideTag(
            "RenderType",
            "Transparent"
        );


        mat.DisableKeyword(
            "_SURFACE_TYPE_OPAQUE"
        );


        mat.EnableKeyword(
            "_SURFACE_TYPE_TRANSPARENT"
        );


        mat.DisableKeyword(
            "_ALPHAPREMULTIPLY_ON"
        );
    }


    // =========================================================
    // Final
    // =========================================================

    private void SetFinalState()
    {
        // =====================================================
        // Dissolve = 1
        // =====================================================

        foreach (Material mat in dissolveMaterials)
        {
            if (mat == null)
                continue;


            if (mat.HasProperty(dissolveProperty))
            {
                mat.SetFloat(
                    dissolveProperty,
                    1f
                );
            }
            else if (
                mat.HasProperty("DissolveAmount"))
            {
                mat.SetFloat(
                    "DissolveAmount",
                    1f
                );
            }
        }


        // =====================================================
        // Normal alpha = 0
        // =====================================================

        if (fadeNormalMaterials)
        {
            foreach (
                NormalMaterialData data
                in normalMaterials)
            {
                if (data.material == null)
                    continue;


                Color color =
                    data.originalColor;

                color.a = 0f;


                data.material.SetColor(
                    data.colorProperty,
                    color
                );
            }
        }


        // =====================================================
        // Skybox = black
        // =====================================================

        if (
            fadeSkyboxToBlack &&
            skyboxMaterial != null)
        {
            if (
                skyboxMaterial.HasProperty("_Tint"))
            {
                skyboxMaterial.SetColor(
                    "_Tint",
                    Color.black
                );
            }
            else if (
                skyboxMaterial.HasProperty("_SkyTint"))
            {
                skyboxMaterial.SetColor(
                    "_SkyTint",
                    Color.black
                );
            }
        }
    }


    // =========================================================
    // Debug Helper
    // =========================================================

    private string GetHierarchyPath(
        Transform target)
    {
        if (target == null)
            return "null";


        string path =
            target.name;


        Transform current =
            target.parent;


        while (current != null)
        {
            path =
                current.name +
                "/" +
                path;

            current =
                current.parent;
        }


        return path;
    }
}