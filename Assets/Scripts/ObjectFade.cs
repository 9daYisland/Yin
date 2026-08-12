using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectFade : MonoBehaviour
{
    [Header("渐显时间")]
    [SerializeField] private float fadeDuration = 2f;

    [Header("开始时自动渐显")]
    [SerializeField] private bool playOnStart = true;

    private readonly List<Material> materials = new List<Material>();
    private readonly List<Color> originalColors = new List<Color>();

    private Coroutine fadeCoroutine;

    private static readonly int BaseColorID =
        Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        // 找到自己和所有子物体上的 Renderer
        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        Debug.Log($"ObjectFade 找到 Renderer 数量: {renderers.Length}");

        foreach (Renderer r in renderers)
        {
            // 注意这里是 materials，不是 sharedMaterials
            Material[] rendererMaterials = r.materials;

            foreach (Material mat in rendererMaterials)
            {
                if (mat == null)
                    continue;

                if (!mat.HasProperty(BaseColorID))
                {
                    Debug.LogWarning(
                        $"{mat.name} 没有 _BaseColor 属性"
                    );
                    continue;
                }

                materials.Add(mat);

                Color original =
                    mat.GetColor(BaseColorID);

                originalColors.Add(original);

                // 一开始完全透明
                Color transparent = original;
                transparent.a = 0f;

                mat.SetColor(
                    BaseColorID,
                    transparent
                );

                Debug.Log(
                    $"找到材质: {mat.name}，原始 Alpha = {original.a}"
                );
            }
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            FadeIn();
        }
    }

    public void FadeIn()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine =
            StartCoroutine(FadeInRoutine());
    }

    public void FadeOut()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine =
            StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / fadeDuration);

            for (int i = 0; i < materials.Count; i++)
            {
                Color color = originalColors[i];

                color.a = Mathf.Lerp(
                    0f,
                    originalColors[i].a,
                    t
                );

                materials[i].SetColor(
                    BaseColorID,
                    color
                );
            }

            yield return null;
        }

        // 最后恢复准确原值
        for (int i = 0; i < materials.Count; i++)
        {
            materials[i].SetColor(
                BaseColorID,
                originalColors[i]
            );
        }

        fadeCoroutine = null;
    }

    private IEnumerator FadeOutRoutine()
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(timer / fadeDuration);

            for (int i = 0; i < materials.Count; i++)
            {
                Color color = originalColors[i];

                color.a = Mathf.Lerp(
                    originalColors[i].a,
                    0f,
                    t
                );

                materials[i].SetColor(
                    BaseColorID,
                    color
                );
            }

            yield return null;
        }

        fadeCoroutine = null;
    }
}