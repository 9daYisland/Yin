using System.Collections;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    private float initialValue;
    private Vector3 initialPosition;
    private Vector3 initialScale;
    private float initialTime;

    private Light lightRef;
    private Coroutine fadeCoroutine;

    [Header("Flicker")]
    public float amount = 0.01f;
    public float speed = 8f;

    [Header("Location")]
    public bool adjustLocation;
    public float locationAdjustAmount = 1f;

    [Header("Scale")]
    public bool adjustScale = false;
    public float scaleAdjustAmount = 1f;
    public Transform scaleObject;

    // 1 = 正常亮度
    // 0 = 完全熄灭
    private float fadeMultiplier = 1f;


    private void Awake()
    {
        initialTime = Random.value * 100f;

        lightRef = GetComponent<Light>();

        if (lightRef != null)
        {
            initialValue = lightRef.intensity;
        }

        if (scaleObject == null)
        {
            scaleObject = transform;
        }

        initialPosition = transform.position;
        initialScale = scaleObject.localScale;
    }


    private void Update()
    {
        float intensityNoise =
            Mathf.PerlinNoise(
                Time.time * speed,
                initialTime
            );


        // =========================================
        // Light Flicker
        // =========================================

        if (lightRef != null && lightRef.enabled)
        {
            float flickerValue =
                initialValue +
                intensityNoise * amount;

            lightRef.intensity =
                flickerValue *
                fadeMultiplier;
        }


        // =========================================
        // Position Flicker
        // =========================================

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


        // =========================================
        // Scale Flicker
        // =========================================

        if (adjustScale)
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
    // 慢慢熄灭
    // =========================================================

    public void FadeOut(float duration)
    {
        StartFade(
            0f,
            duration
        );
    }


    // =========================================================
    // 慢慢点亮
    // =========================================================

    public void FadeIn(float duration)
    {
        if (lightRef == null)
        {
            lightRef =
                GetComponent<Light>();
        }

        if (lightRef != null)
        {
            lightRef.enabled = true;
        }

        StartFade(
            1f,
            duration
        );
    }


    // =========================================================
    // 立即变暗
    // 用于测试 Fade In 的初始状态
    // =========================================================

    public void SetDarkInstantly()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }

        fadeMultiplier = 0f;

        if (lightRef == null)
        {
            lightRef =
                GetComponent<Light>();
        }

        if (lightRef != null)
        {
            // 保持 enabled
            // 这样之后 FadeIn 可以直接慢慢亮起来
            lightRef.enabled = true;
            lightRef.intensity = 0f;
        }
    }


    // =========================================================
    // 立即恢复正常亮度
    // 用于测试 Fade Out 的初始状态
    // =========================================================

    public void SetLitInstantly()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine = null;
        }

        if (lightRef == null)
        {
            lightRef =
                GetComponent<Light>();
        }

        fadeMultiplier = 1f;

        if (lightRef != null)
        {
            lightRef.enabled = true;
            lightRef.intensity = initialValue;
        }
    }


    // =========================================================
    // 内部渐变
    // =========================================================

    private void StartFade(
        float target,
        float duration)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );
        }

        fadeCoroutine =
            StartCoroutine(
                FadeCoroutine(
                    target,
                    duration
                )
            );
    }


    private IEnumerator FadeCoroutine(
        float target,
        float duration)
    {
        float startMultiplier =
            fadeMultiplier;

        float elapsed = 0f;


        if (duration <= 0f)
        {
            fadeMultiplier = target;

            fadeCoroutine = null;

            yield break;
        }


        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );


            // Linear：
            // 确保真的贯穿整个 Duration
            fadeMultiplier =
                Mathf.Lerp(
                    startMultiplier,
                    target,
                    t
                );


            yield return null;
        }


        fadeMultiplier = target;


        if (target <= 0f &&
            lightRef != null)
        {
            lightRef.intensity = 0f;
        }


        fadeCoroutine = null;
    }
}