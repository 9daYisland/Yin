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

    [Header("Initial State")]
    [Tooltip("勾选后进入场景时火默认熄灭")]
    [SerializeField] private bool startExtinguished = true;

    [Header("Fade Settings")]
    [SerializeField] private float particleFadeDuration = 4f;
    [SerializeField] private float lightFadeDuration = 4f;

    [Range(0.1f, 1f)]
    [SerializeField] private float smokeFadeMultiplier = 0.5f;

    [SerializeField] private float glowFadeMultiplier = 1.3f;

    [Header("Test")]
    [SerializeField] private bool testFadeIn = false;
    [SerializeField] private bool testFadeOut = false;
    [SerializeField] private float startDelay = 2f;

    public bool IsLit { get; private set; } = false;

    private ParticleData flameData;
    private ParticleData smokeData;
    private ParticleData glowData;

    private Coroutine particleCoroutine;

    private class ParticleData
    {
        public float emissionRate;
        public ParticleSystem.MinMaxCurve startSize;

        public ParticleData(
            float emissionRate,
            ParticleSystem.MinMaxCurve startSize)
        {
            this.emissionRate = emissionRate;
            this.startSize = startSize;
        }
    }

    private void Awake()
    {
        if (lightFlicker == null)
        {
            lightFlicker =
                GetComponentInChildren<LightFlicker>(true);
        }

        // 必须先缓存正常状态
        CacheParticleData();

        // 然后再设置初始状态
        if (startExtinguished)
        {
            SetExtinguishedInstantly();
        }
        else
        {
            SetLitInstantly();
        }
    }

    private IEnumerator Start()
    {
        if (testFadeIn)
        {
            SetExtinguishedInstantly();

            yield return new WaitForSeconds(
                startDelay
            );

            FadeIn();
            yield break;
        }

        if (testFadeOut)
        {
            SetLitInstantly();

            yield return new WaitForSeconds(
                startDelay
            );

            FadeOut();
        }
    }

    private void CacheParticleData()
    {
        if (flame != null)
            flameData = GetParticleData(flame);

        if (smoke != null)
            smokeData = GetParticleData(smoke);

        if (glow != null)
            glowData = GetParticleData(glow);
    }

    private ParticleData GetParticleData(
        ParticleSystem ps)
    {
        var emission = ps.emission;
        var main = ps.main;

        return new ParticleData(
            emission.rateOverTime.constant,
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

        StartParticle(flame);
        StartParticle(smoke);
        StartParticle(glow);

        particleCoroutine =
            StartCoroutine(
                FadeParticlesCoroutine(true)
            );

        if (lightFlicker != null)
        {
            lightFlicker.FadeIn(
                lightFadeDuration
            );
        }

        // 一调用点亮，就视为已经被点燃
        IsLit = true;
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
                FadeParticlesCoroutine(false)
            );

        if (lightFlicker != null)
        {
            lightFlicker.FadeOut(
                lightFadeDuration
            );
        }

        IsLit = false;
    }

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

        IsLit = false;
    }

    public void SetLitInstantly()
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
            lightFlicker.SetLitInstantly();
        }

        IsLit = true;
    }

    private IEnumerator FadeParticlesCoroutine(
        bool fadeIn)
    {
        float elapsed = 0f;

        while (elapsed < particleFadeDuration)
        {
            elapsed += Time.deltaTime;

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

            // Flame
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

            // Glow
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

            // Smoke
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

    private void SetParticleStrength(
        ParticleSystem ps,
        ParticleData data,
        float strength)
    {
        if (ps == null ||
            data == null)
            return;

        strength =
            Mathf.Clamp01(strength);

        var emission =
            ps.emission;

        emission.rateOverTime =
            data.emissionRate *
            strength;

        var main =
            ps.main;

        float sizeMultiplier =
            Mathf.Lerp(
                0.15f,
                1f,
                strength
            );

        switch (data.startSize.mode)
        {
            case ParticleSystemCurveMode.Constant:
                {
                    main.startSize =
                        data.startSize.constant *
                        sizeMultiplier;

                    break;
                }

            case ParticleSystemCurveMode.TwoConstants:
                {
                    main.startSize =
                        new ParticleSystem.MinMaxCurve(
                            data.startSize.constantMin *
                            sizeMultiplier,

                            data.startSize.constantMax *
                            sizeMultiplier
                        );

                    break;
                }
        }
    }

    private void StartParticle(
        ParticleSystem ps)
    {
        if (ps == null)
            return;

        if (!ps.gameObject.activeSelf)
        {
            ps.gameObject.SetActive(true);
        }

        if (!ps.isPlaying)
        {
            ps.Play();
        }
    }

    private void StopParticle(
        ParticleSystem ps)
    {
        if (ps == null)
            return;

        var emission =
            ps.emission;

        emission.rateOverTime = 0f;

        ps.Stop(
            true,
            ParticleSystemStopBehavior.StopEmitting
        );
    }
}