using System.Collections;
using UnityEngine;

public class FireController : MonoBehaviour
{
    [Header("Fire Parts")]
    [SerializeField] private ParticleSystem flame;
    [SerializeField] private ParticleSystem smoke;
    [SerializeField] private ParticleSystem glow;

    [Header("Light")]
    [SerializeField] private LightFlicker lightFlicker;


    [Header("Fade Settings")]

    [Tooltip("火焰粒子渐变时间")]
    [SerializeField] private float particleFadeDuration = 5f;

    [Tooltip("灯光渐变时间")]
    [SerializeField] private float lightFadeDuration = 5f;

    [Tooltip("Smoke变化速度。越小越慢")]
    [Range(0.1f, 1f)]
    [SerializeField] private float smokeFadeMultiplier = 0.5f;

    [Tooltip("Glow变化速度")]
    [SerializeField] private float glowFadeMultiplier = 1.3f;


    [Header("Test")]

    [Tooltip(
        "勾选后：场景开始时火是灭的，Delay后慢慢点亮"
    )]
    [SerializeField] private bool testFadeIn = false;

    [Tooltip(
        "勾选后：场景开始时火是亮的，Delay后慢慢熄灭"
    )]
    [SerializeField] private bool testFadeOut = false;

    [Tooltip("进入场景后多久开始测试")]
    [SerializeField] private float startDelay = 2f;


    private ParticleData flameData;
    private ParticleData smokeData;
    private ParticleData glowData;

    private Coroutine particleCoroutine;


    // =========================================================
    // 粒子数据
    // =========================================================

    private class ParticleData
    {
        public float emissionRate;

        public ParticleSystem.MinMaxCurve startSize;


        public ParticleData(
            float emissionRate,
            ParticleSystem.MinMaxCurve startSize)
        {
            this.emissionRate =
                emissionRate;

            this.startSize =
                startSize;
        }
    }


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        if (lightFlicker == null)
        {
            lightFlicker =
                GetComponentInChildren
                <LightFlicker>(true);
        }


        // 先记录正常燃烧时的参数
        CacheParticleData();


        // =============================================
        // 测试点亮
        // =============================================

        if (testFadeIn)
        {
            SetExtinguishedInstantly();
        }

        // =============================================
        // 测试熄灭
        // =============================================

        else if (testFadeOut)
        {
            SetLitInstantly();
        }
    }


    // =========================================================
    // Start 测试
    // =========================================================

    private IEnumerator Start()
    {
        if (testFadeIn && testFadeOut)
        {
            Debug.LogWarning(
                "[FireController] " +
                "Test Fade In 和 Test Fade Out 同时勾选。" +
                "优先执行 Test Fade In。"
            );
        }


        // =============================================
        // 测试点亮
        // =============================================

        if (testFadeIn)
        {
            yield return new WaitForSeconds(
                startDelay
            );

            FadeIn();

            yield break;
        }


        // =============================================
        // 测试熄灭
        // =============================================

        if (testFadeOut)
        {
            yield return new WaitForSeconds(
                startDelay
            );

            FadeOut();

            yield break;
        }
    }


    // =========================================================
    // 缓存正常燃烧状态
    // =========================================================

    private void CacheParticleData()
    {
        if (flame != null)
        {
            flameData =
                GetParticleData(flame);
        }


        if (smoke != null)
        {
            smokeData =
                GetParticleData(smoke);
        }


        if (glow != null)
        {
            glowData =
                GetParticleData(glow);
        }
    }


    private ParticleData GetParticleData(
        ParticleSystem ps)
    {
        var emission =
            ps.emission;

        var main =
            ps.main;


        return new ParticleData(
            emission
                .rateOverTime
                .constant,

            main.startSize
        );
    }


    // =========================================================
    // 点亮
    // =========================================================

    public void FadeIn()
    {
        if (particleCoroutine != null)
        {
            StopCoroutine(
                particleCoroutine
            );
        }


        // 确保 ParticleSystem 运行
        StartParticle(flame);
        StartParticle(smoke);
        StartParticle(glow);


        particleCoroutine =
            StartCoroutine(
                FadeParticlesCoroutine(
                    true
                )
            );


        // 灯渐亮
        if (lightFlicker != null)
        {
            lightFlicker.FadeIn(
                lightFadeDuration
            );
        }
    }


    // =========================================================
    // 熄灭
    // =========================================================

    public void FadeOut()
    {
        if (particleCoroutine != null)
        {
            StopCoroutine(
                particleCoroutine
            );
        }


        particleCoroutine =
            StartCoroutine(
                FadeParticlesCoroutine(
                    false
                )
            );


        // 灯渐暗
        if (lightFlicker != null)
        {
            lightFlicker.FadeOut(
                lightFadeDuration
            );
        }
    }


    // =========================================================
    // 粒子渐变
    // =========================================================

    private IEnumerator FadeParticlesCoroutine(
        bool fadeIn)
    {
        float elapsed = 0f;


        while (elapsed <
               particleFadeDuration)
        {
            elapsed +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    elapsed /
                    particleFadeDuration
                );


            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            // =========================================
            // Flame
            // =========================================

            if (flame != null &&
                flameData != null)
            {
                float strength =
                    fadeIn
                        ? smoothT
                        : 1f - smoothT;


                SetParticleStrength(
                    flame,
                    flameData,
                    strength
                );
            }


            // =========================================
            // Glow
            // =========================================

            if (glow != null &&
                glowData != null)
            {
                float glowT =
                    Mathf.Clamp01(
                        smoothT *
                        glowFadeMultiplier
                    );


                float strength =
                    fadeIn
                        ? glowT
                        : 1f - glowT;


                SetParticleStrength(
                    glow,
                    glowData,
                    strength
                );
            }


            // =========================================
            // Smoke
            // =========================================

            if (smoke != null &&
                smokeData != null)
            {
                float smokeT =
                    Mathf.Clamp01(
                        smoothT *
                        smokeFadeMultiplier
                    );


                float strength =
                    fadeIn
                        ? smokeT
                        : 1f - smokeT;


                SetParticleStrength(
                    smoke,
                    smokeData,
                    strength
                );
            }


            yield return null;
        }


        // =====================================================
        // 最终状态
        // =====================================================

        if (fadeIn)
        {
            SetParticleStrength(
                flame,
                flameData,
                1f
            );


            SetParticleStrength(
                smoke,
                smokeData,
                1f
            );


            SetParticleStrength(
                glow,
                glowData,
                1f
            );
        }
        else
        {
            StopParticle(flame);
            StopParticle(smoke);
            StopParticle(glow);
        }


        particleCoroutine = null;
    }


    // =========================================================
    // 测试点亮的初始状态
    // =========================================================

    public void SetExtinguishedInstantly()
    {
        StartParticle(flame);
        StartParticle(smoke);
        StartParticle(glow);

        SetParticleStrength(
            flame,
            flameData,
            0f
        );

        SetParticleStrength(
            smoke,
            smokeData,
            0f
        );

        SetParticleStrength(
            glow,
            glowData,
            0f
        );

        if (lightFlicker != null)
        {
            lightFlicker.SetDarkInstantly();
        }
    }

    // =========================================================
    // 测试熄灭的初始状态
    // =========================================================

    private void SetLitInstantly()
    {
        StartParticle(flame);
        StartParticle(smoke);
        StartParticle(glow);


        SetParticleStrength(
            flame,
            flameData,
            1f
        );


        SetParticleStrength(
            smoke,
            smokeData,
            1f
        );


        SetParticleStrength(
            glow,
            glowData,
            1f
        );


        if (lightFlicker != null)
        {
            lightFlicker
                .SetLitInstantly();
        }
    }


    // =========================================================
    // 控制粒子强度
    // =========================================================

    private void SetParticleStrength(
        ParticleSystem ps,
        ParticleData data,
        float strength)
    {
        if (ps == null ||
            data == null)
        {
            return;
        }


        strength =
            Mathf.Clamp01(
                strength
            );


        // =============================================
        // Emission
        // =============================================

        var emission =
            ps.emission;


        emission.rateOverTime =
            data.emissionRate *
            strength;


        // =============================================
        // Size
        // =============================================

        var main =
            ps.main;


        float sizeMultiplier =
            Mathf.Lerp(
                0.15f,
                1f,
                strength
            );


        switch (
            data.startSize.mode)
        {
            case
                ParticleSystemCurveMode
                    .Constant:
                {
                    main.startSize =
                        data.startSize
                            .constant *
                        sizeMultiplier;

                    break;
                }


            case
                ParticleSystemCurveMode
                    .TwoConstants:
                {
                    float min =
                        data.startSize
                            .constantMin *
                        sizeMultiplier;


                    float max =
                        data.startSize
                            .constantMax *
                        sizeMultiplier;


                    main.startSize =
                        new ParticleSystem
                            .MinMaxCurve(
                                min,
                                max
                            );

                    break;
                }
        }
    }


    // =========================================================
    // 开启粒子
    // =========================================================

    private void StartParticle(
        ParticleSystem ps)
    {
        if (ps == null)
            return;


        if (!ps.gameObject.activeSelf)
        {
            ps.gameObject
                .SetActive(true);
        }


        if (!ps.isPlaying)
        {
            ps.Play();
        }
    }


    // =========================================================
    // 停止粒子继续生成
    // =========================================================

    private void StopParticle(
        ParticleSystem ps)
    {
        if (ps == null)
            return;


        var emission =
            ps.emission;


        emission.rateOverTime =
            0f;


        ps.Stop(
            true,
            ParticleSystemStopBehavior
                .StopEmitting
        );
    }
}