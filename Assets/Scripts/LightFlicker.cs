using System.Collections;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [Header("Flicker")]
    public float amount = 0.3f;
    public float speed = 7f;

    [Header("Location")]
    public bool adjustLocation = false;
    public float locationAdjustAmount = 1f;

    [Header("Scale")]
    public bool adjustScale = false;
    public float scaleAdjustAmount = 1f;
    public Transform scaleObject;

    private Light lightRef;

    private float initialValue;
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private float initialTime;

    // 1 = 正常亮度
    // 0 = 完全熄灭
    private float fadeMultiplier = 1f;

    private Coroutine fadeCoroutine;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        initialTime = Random.value * 100f;

        lightRef = GetComponent<Light>();

        if (lightRef != null)
        {
            // 记录 Inspector 里原本设置的灯光强度
            initialValue = lightRef.intensity;
        }

        if (scaleObject == null)
        {
            scaleObject = transform;
        }

        initialPosition = transform.position;

        if (scaleObject != null)
        {
            initialScale = scaleObject.localScale;
        }
    }


    // =========================================================
    // Flicker
    // =========================================================

    private void Update()
    {
        float intensityNoise =
            Mathf.PerlinNoise(
                Time.time * speed,
                initialTime
            );


        // =============================================
        // Light
        // =============================================

        if (lightRef != null &&
            lightRef.enabled)
        {
            float flickerValue =
                initialValue +
                intensityNoise * amount;

            // 闪烁强度 × 当前 Fade 强度
            lightRef.intensity =
                flickerValue *
                fadeMultiplier;
        }


        // =============================================
        // Position
        // =============================================

        if (adjustLocation)
        {
            Vector3 offset =
                new Vector3(
                    Mathf.PerlinNoise(
                        Time.time * speed,
                        initialTime + 5f
                    ) - 0.5f,

                    intensityNoise - 0.5f,

                    Mathf.PerlinNoise(
                        Time.time * speed,
                        initialTime + 10f
                    ) - 0.5f
                );

            transform.position =
                initialPosition +
                offset *
                locationAdjustAmount *
                2f;
        }


        // =============================================
        // Scale
        // =============================================

        if (adjustScale &&
            scaleObject != null)
        {
            scaleObject.localScale =
                initialScale *
                (
                    (intensityNoise - 0.5f) *
                    scaleAdjustAmount +
                    1f
                );
        }
    }


    // =========================================================
    // 慢慢点亮
    // FireController.FadeIn() 会调用这里
    // =========================================================

    public void FadeIn(float duration)
    {
        if (lightRef == null)
        {
            lightRef = GetComponent<Light>();
        }

        if (lightRef != null)
        {
            // 点亮前必须确保 Light 是开启的
            lightRef.enabled = true;
        }

        StartFade(
            1f,
            duration
        );
    }


    // =========================================================
    // 慢慢熄灭
    // FireController.FadeOut() 会调用这里
    // =========================================================

    public void FadeOut(float duration)
    {
        if (lightRef == null)
        {
            lightRef = GetComponent<Light>();
        }

        StartFade(
            0f,
            duration
        );
    }


    // =========================================================
    // 立即变成熄灭状态
    //
    // SceneFlowController 开场会通过
    // FireController.SetExtinguishedInstantly()
    // 最终调用这里
    // =========================================================

    public void SetDarkInstantly()
    {
        StopCurrentFade();

        fadeMultiplier = 0f;

        if (lightRef == null)
        {
            lightRef = GetComponent<Light>();
        }

        if (lightRef != null)
        {
            // 不关闭 Light Component
            // 否则之后 FadeIn 不方便
            lightRef.enabled = true;
            lightRef.intensity = 0f;
        }
    }


    // =========================================================
    // 立即恢复正常亮度
    //
    // 用于 FireController 的 Test Fade Out
    // =========================================================

    public void SetLitInstantly()
    {
        StopCurrentFade();

        fadeMultiplier = 1f;

        if (lightRef == null)
        {
            lightRef = GetComponent<Light>();
        }

        if (lightRef != null)
        {
            lightRef.enabled = true;
            lightRef.intensity = initialValue;
        }
    }


    // =========================================================
    // 开始渐变
    // =========================================================

    private void StartFade(
        float targetMultiplier,
        float duration)
    {
        StopCurrentFade();

        fadeCoroutine =
            StartCoroutine(
                FadeCoroutine(
                    targetMultiplier,
                    duration
                )
            );
    }


    // =========================================================
    // Fade Coroutine
    // =========================================================

    private IEnumerator FadeCoroutine(
        float targetMultiplier,
        float duration)
    {
        float startMultiplier =
            fadeMultiplier;


        // Duration <= 0
        // 直接切换
        if (duration <= 0f)
        {
            fadeMultiplier =
                targetMultiplier;

            if (lightRef != null)
            {
                lightRef.intensity =
                    initialValue *
                    fadeMultiplier;
            }

            fadeCoroutine = null;

            yield break;
        }


        float elapsed = 0f;


        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );


            // 使用线性变化
            // 保证整个 Duration 都在渐变
            fadeMultiplier =
                Mathf.Lerp(
                    startMultiplier,
                    targetMultiplier,
                    t
                );


            yield return null;
        }


        // 确保最终值准确
        fadeMultiplier =
            targetMultiplier;


        if (targetMultiplier <= 0f &&
            lightRef != null)
        {
            lightRef.intensity = 0f;
        }


        fadeCoroutine = null;
    }


    // =========================================================
    // 停止当前 Fade
    // =========================================================

    private void StopCurrentFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }
    }
}