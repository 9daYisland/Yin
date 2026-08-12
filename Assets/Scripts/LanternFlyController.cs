using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Playables;

public class LanternFlyController : MonoBehaviour
{
    // =========================================================
    // Audio
    // =========================================================

    [Header("音频")]

    [Tooltip("开场讲解音频")]
    [SerializeField]
    private AudioSource introAudio;

    [Header("放飞 Timeline")]

    [Tooltip("放飞天灯后播放的 Timeline")]
    [SerializeField]
    private PlayableDirector releaseTimeline;


    // =========================================================
    // Hint UI
    // =========================================================

    [Header("Hint UI")]

    [SerializeField]
    private GameObject hintCanvas;

    [SerializeField]
    private TMP_Text hintText;

    [TextArea]
    [SerializeField]
    private string grabHint =
        "Pick up the lantern.";

    [TextArea]
    [SerializeField]
    private string igniteHint =
        "Light the candle.";

    [TextArea]
    [SerializeField]
    private string releaseHint =
        "Release the lantern.";


    // =========================================================
    // Outline
    // =========================================================

    [Header("Outline")]

    [Tooltip("整个天灯的 Outline")]
    [SerializeField]
    private Outline lanternOutline;

    [Tooltip("蜡烛 / 灯芯的 Outline")]
    [SerializeField]
    private Outline candleOutline;

    [Range(0f, 10f)]
    [SerializeField]
    private float lanternOutlineWidth = 4f;

    [Range(0f, 10f)]
    [SerializeField]
    private float candleOutlineWidth = 4f;


    // =========================================================
    // XR Grab
    // =========================================================

    [Header("XR Grab")]

    [SerializeField]
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    [SerializeField]
    private Rigidbody lanternRigidbody;


    // =========================================================
    // Fire
    // =========================================================

    [Header("Fire")]

    [Tooltip("蜡烛上的 FireController")]
    [SerializeField]
    private FireController fireController;


    // =========================================================
    // Reset
    // =========================================================

    [Header("未点亮松手后回位")]

    [Tooltip("松手后多久回到初始位置")]
    [SerializeField]
    private float resetDelay = 0.1f;


    // =========================================================
    // Release Flight
    // =========================================================

    [Header("放飞设置")]

    [Tooltip("点亮后松手，额外增加多少向上速度")]
    [SerializeField]
    private float releaseUpwardVelocity = 0.6f;

    [Tooltip("X/Z 翻滚衰减速度，越大越快稳定")]
    [SerializeField]
    private float tiltDamping = 5f;

    [Tooltip("Y轴旋转衰减速度")]
    [SerializeField]
    private float yawDamping = 0.5f;

    [Tooltip("放飞后多久停止主动稳定旋转")]
    [SerializeField]
    private float stabilizationDuration = 5f;
    // =========================================================
    // Gallery
    // =========================================================

    [Header("放飞后展板")]

    [Tooltip("放飞天灯后开始显示的 Gallery")]
    [SerializeField]
    private VRGalleryController galleryController;


    // =========================================================
    // State
    // =========================================================

    private enum LanternState
    {
        WaitingForAudio,
        WaitingForGrab,
        HeldUnlit,
        Lit,
        ReleasedLit
    }

    private LanternState currentState =
        LanternState.WaitingForAudio;


    // =========================================================
    // Initial Transform
    // =========================================================

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Transform initialParent;

    private Coroutine stabilizationCoroutine;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        initialPosition =
            transform.position;

        initialRotation =
            transform.rotation;

        initialParent =
            transform.parent;


        if (grabInteractable == null)
        {
            grabInteractable =
                GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }


        if (lanternRigidbody == null)
        {
            lanternRigidbody =
                GetComponent<Rigidbody>();
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        // -----------------------------------------
        // 一开始隐藏 Hint
        // -----------------------------------------

        HideHint();


        // -----------------------------------------
        // 一开始关闭 Outline
        // -----------------------------------------

        SetLanternOutline(false);
        SetCandleOutline(false);


        // -----------------------------------------
        // 火一开始熄灭
        // -----------------------------------------

        if (fireController != null)
        {
            fireController
                .SetExtinguishedInstantly();
        }
        else
        {
            Debug.LogWarning(
                "[Lantern] FireController 没有绑定。",
                this
            );
        }


        // -----------------------------------------
        // 无重力
        // -----------------------------------------

        if (lanternRigidbody != null)
        {
            lanternRigidbody.useGravity =
                false;
        }


        // -----------------------------------------
        // 注册 Grab 事件
        // -----------------------------------------

        if (grabInteractable != null)
        {
            grabInteractable
                .selectEntered
                .AddListener(
                    OnLanternGrabbed
                );

            grabInteractable
                .selectExited
                .AddListener(
                    OnLanternReleased
                );


            // =================================================
            // 关键：
            // 开场音频播放期间不能抓
            // =================================================

            grabInteractable.enabled =
                false;
        }
        else
        {
            Debug.LogError(
                "[Lantern] 没找到 XRGrabInteractable！",
                this
            );
        }


        StartCoroutine(
            IntroSequence()
        );
    }


    // =========================================================
    // 开场音频
    // =========================================================

    private IEnumerator IntroSequence()
    {
        currentState =
            LanternState.WaitingForAudio;


        // =====================================================
        // 音频期间 Grab 禁用
        // =====================================================

        if (grabInteractable != null)
        {
            grabInteractable.enabled =
                false;
        }


        if (introAudio != null)
        {
            // 如果 AudioSource 不是 Play On Awake，
            // 这里也可以主动播放。
            if (!introAudio.isPlaying)
            {
                introAudio.Play();
            }


            // 等到播放结束
            while (introAudio.isPlaying)
            {
                yield return null;
            }
        }


        // =====================================================
        // 音频结束
        // 现在才允许抓
        // =====================================================

        if (grabInteractable != null)
        {
            grabInteractable.enabled =
                true;
        }


        EnterWaitingForGrabState();
    }


    // =========================================================
    // 等待玩家抓取
    // =========================================================

    private void EnterWaitingForGrabState()
    {
        currentState =
            LanternState.WaitingForGrab;


        ShowHint(
            grabHint
        );


        SetLanternOutline(true);

        SetCandleOutline(false);


        Debug.Log(
            "[Lantern] 音频结束，现在可以抓天灯。"
        );
    }


    // =========================================================
    // 玩家抓住
    // =========================================================

    private void OnLanternGrabbed(
        SelectEnterEventArgs args)
    {
        // 已经放飞后不再进入流程
        if (currentState ==
            LanternState.ReleasedLit)
        {
            return;
        }


        if (currentState !=
            LanternState.WaitingForGrab)
        {
            return;
        }


        currentState =
            LanternState.HeldUnlit;


        // 抓取提示消失
        HideHint();


        // 天灯 Outline 消失
        SetLanternOutline(false);


        // 显示点火提示
        ShowHint(
            igniteHint
        );


        // 蜡烛 Outline
        SetCandleOutline(true);


        Debug.Log(
            "[Lantern] 已抓取，请点亮蜡烛。"
        );
    }


    // =========================================================
    // 玩家松手
    // =========================================================

    private void OnLanternReleased(
        SelectExitEventArgs args)
    {
        // =====================================================
        // 情况 1：
        // 没点亮就松手
        // =====================================================

        if (currentState ==
            LanternState.HeldUnlit)
        {
            Debug.Log(
                "[Lantern] 未点亮松手，恢复原位。"
            );


            HideHint();

            SetLanternOutline(false);

            SetCandleOutline(false);


            StartCoroutine(
                ResetLantern()
            );

            return;
        }


        // =====================================================
        // 情况 2：
        // 点亮后松手
        // =====================================================

        if (currentState == LanternState.Lit)
        {
            currentState =
                LanternState.ReleasedLit;


            // =====================================================
            // Hint 消失
            // =====================================================

            HideHint();


            // =====================================================
            // Outline 消失
            // =====================================================

            SetLanternOutline(false);

            SetCandleOutline(false);


            // =====================================================
            // 放飞物理
            // =====================================================

            StartCoroutine(
                ApplyReleaseFlight()
            );

            // =====================================================
            // 播放放飞 Timeline
            // =====================================================

            if (releaseTimeline != null)
            {
                releaseTimeline.Play();
            }
            else
            {
                Debug.LogWarning(
                    "[Lantern] Release Timeline 没有绑定。",
                    this
                );
            }


            // =====================================================
            // 同时开始显示展板
            // =====================================================

            if (galleryController != null)
            {
                galleryController.StartGallery();
            }
            else
            {
                Debug.LogWarning(
                    "[Lantern] Gallery Controller 没有绑定。",
                    this
                );
            }


            Debug.Log(
                "[Lantern] 天灯已放飞，音频和展板同时开始。"
            );


            return;
        }
    }


    // =========================================================
    // 点亮天灯
    //
    // 由点火 Trigger 调用
    // =========================================================

    public void IgniteLantern()
    {
        // 必须是拿着未点亮的天灯
        if (currentState !=
            LanternState.HeldUnlit)
        {
            Debug.Log(
                "[Lantern] 当前状态不能点亮。"
            );

            return;
        }


        currentState =
            LanternState.Lit;


        Debug.Log(
            "[Lantern] 天灯点亮成功。"
        );


        // =====================================================
        // 火焰渐亮
        // =====================================================

        if (fireController != null)
        {
            fireController.FadeIn();
        }
        else
        {
            Debug.LogError(
                "[Lantern] FireController 没有绑定！",
                this
            );
        }


        // =====================================================
        // 蜡烛 Outline 消失
        // =====================================================

        SetCandleOutline(false);

        SetLanternOutline(false);


        // =====================================================
        // 新 Hint：
        // 提醒玩家松手放飞
        // =====================================================

        ShowHint(
            releaseHint
        );


        Debug.Log(
            "[Lantern] 已点亮，等待玩家松手放飞。"
        );
    }


    // =========================================================
    // 放飞物理
    // =========================================================

    private IEnumerator ApplyReleaseFlight()
    {
        // 让 XR 先完成松手速度计算
        yield return new WaitForFixedUpdate();


        if (lanternRigidbody == null)
        {
            yield break;
        }


        lanternRigidbody.useGravity =
            false;

        lanternRigidbody.isKinematic =
            false;


        // =====================================================
        // 保留原有 XR Throw Velocity
        // 再增加一点向上速度
        // =====================================================

        Vector3 velocity =
            lanternRigidbody.linearVelocity;


        velocity.y +=
            releaseUpwardVelocity;


        lanternRigidbody.linearVelocity =
            velocity;


        // =====================================================
        // 开始减少 X/Z 翻滚
        // =====================================================

        if (stabilizationCoroutine != null)
        {
            StopCoroutine(
                stabilizationCoroutine
            );
        }


        stabilizationCoroutine =
            StartCoroutine(
                StabilizeRotation()
            );
    }


    // =========================================================
    // 旋转稳定
    // =========================================================

    private IEnumerator StabilizeRotation()
    {
        float elapsed = 0f;


        while (
            elapsed <
            stabilizationDuration)
        {
            elapsed +=
                Time.fixedDeltaTime;


            if (lanternRigidbody == null)
            {
                yield break;
            }


            Vector3 angular =
                lanternRigidbody.angularVelocity;


            // X 翻滚衰减
            angular.x =
                Mathf.Lerp(
                    angular.x,
                    0f,
                    tiltDamping *
                    Time.fixedDeltaTime
                );


            // Z 翻滚衰减
            angular.z =
                Mathf.Lerp(
                    angular.z,
                    0f,
                    tiltDamping *
                    Time.fixedDeltaTime
                );


            // Y 保留更多自然旋转
            angular.y =
                Mathf.Lerp(
                    angular.y,
                    0f,
                    yawDamping *
                    Time.fixedDeltaTime
                );


            lanternRigidbody.angularVelocity =
                angular;


            yield return
                new WaitForFixedUpdate();
        }


        stabilizationCoroutine =
            null;
    }


    // =========================================================
    // 未点亮松手 → 回原位
    // =========================================================

    private IEnumerator ResetLantern()
    {
        yield return
            new WaitForSeconds(
                resetDelay
            );


        if (lanternRigidbody != null)
        {
            lanternRigidbody.linearVelocity =
                Vector3.zero;

            lanternRigidbody.angularVelocity =
                Vector3.zero;
        }


        transform.SetParent(
            initialParent
        );


        transform.position =
            initialPosition;

        transform.rotation =
            initialRotation;


        EnterWaitingForGrabState();
    }


    // =========================================================
    // Hint
    // =========================================================

    private void ShowHint(
        string message)
    {
        if (hintText != null)
        {
            hintText.text =
                message;
        }


        if (hintCanvas != null)
        {
            hintCanvas.SetActive(
                true
            );
        }
    }


    private void HideHint()
    {
        if (hintCanvas != null)
        {
            hintCanvas.SetActive(
                false
            );
        }
    }


    // =========================================================
    // Lantern Outline
    // =========================================================

    private void SetLanternOutline(
        bool visible)
    {
        if (lanternOutline == null)
        {
            return;
        }


        if (visible)
        {
            lanternOutline.OutlineWidth =
                lanternOutlineWidth;
        }


        lanternOutline.enabled =
            visible;
    }


    // =========================================================
    // Candle Outline
    // =========================================================

    private void SetCandleOutline(
        bool visible)
    {
        if (candleOutline == null)
        {
            return;
        }


        if (visible)
        {
            candleOutline.OutlineWidth =
                candleOutlineWidth;
        }


        candleOutline.enabled =
            visible;
    }


    // =========================================================
    // Cleanup
    // =========================================================

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable
                .selectEntered
                .RemoveListener(
                    OnLanternGrabbed
                );

            grabInteractable
                .selectExited
                .RemoveListener(
                    OnLanternReleased
                );
        }


        if (stabilizationCoroutine != null)
        {
            StopCoroutine(
                stabilizationCoroutine
            );
        }
    }
}