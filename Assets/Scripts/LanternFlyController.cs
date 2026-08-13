using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Playables;

public class LanternFlyController : MonoBehaviour
{
    // =========================================================
    // Timeline
    // =========================================================

    [Header("Timeline")]

    [Tooltip("场景开始时播放的 Timeline")]
    [SerializeField]
    private PlayableDirector introTimeline;

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
        WaitingForIntro,
        WaitingForGrab,
        HeldUnlit,
        Lit,
        ReleasedLit
    }

    private LanternState currentState =
        LanternState.WaitingForIntro;


    // =========================================================
    // Initial Transform / Rigidbody State
    // =========================================================

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Transform initialParent;

    private bool initialUseGravity;
    private bool initialIsKinematic;

    private Coroutine stabilizationCoroutine;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // 保存初始 Transform
        initialPosition =
            transform.position;

        initialRotation =
            transform.rotation;

        initialParent =
            transform.parent;


        // 自动寻找 Grab Interactable
        if (grabInteractable == null)
        {
            grabInteractable =
                GetComponent<
                    UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
                >();
        }


        // 自动寻找 Rigidbody
        if (lanternRigidbody == null)
        {
            lanternRigidbody =
                GetComponent<Rigidbody>();
        }


        // 保存 Rigidbody 原本设置
        if (lanternRigidbody != null)
        {
            initialUseGravity =
                lanternRigidbody.useGravity;

            initialIsKinematic =
                lanternRigidbody.isKinematic;
        }
    }


    // =========================================================
    // Start
    // =========================================================

    private void Start()
    {
        // =====================================================
        // 1. 一开始隐藏 Hint
        // =====================================================

        HideHint();


        // =====================================================
        // 2. 一开始关闭 Outline
        // =====================================================

        SetLanternOutline(false);
        SetCandleOutline(false);


        // =====================================================
        // 3. 火一开始熄灭
        // =====================================================

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


        // =====================================================
        // 4. 注册 Grab 事件
        // =====================================================

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
        }
        else
        {
            Debug.LogError(
                "[Lantern] 没找到 XRGrabInteractable！",
                this
            );
        }


        // =====================================================
        // 5. 开场时禁用 Grab + Rigidbody
        // =====================================================

        DisableLanternInteraction();


        // =====================================================
        // 6. 开始播放 Intro Timeline
        // =====================================================

        StartCoroutine(
            IntroSequence()
        );
    }


    // =========================================================
    // 开场 Timeline
    // =========================================================

    private IEnumerator IntroSequence()
    {
        currentState =
            LanternState.WaitingForIntro;


        // =====================================================
        // 开场阶段确保无法抓取、无法被物理推动
        // =====================================================

        DisableLanternInteraction();


        // =====================================================
        // 播放 Intro Timeline
        // =====================================================

        if (introTimeline != null)
        {
            introTimeline.time = 0;

            introTimeline.Play();


            Debug.Log(
                "[Lantern] 开始播放 Intro Timeline。"
            );


            // 等待 Timeline 播放完成
            while (
                introTimeline.state ==
                PlayState.Playing
            )
            {
                yield return null;
            }


            Debug.Log(
                "[Lantern] Intro Timeline 播放结束。"
            );
        }
        else
        {
            Debug.LogWarning(
                "[Lantern] Intro Timeline 没有绑定。",
                this
            );
        }


        // =====================================================
        // Timeline 结束
        // 恢复天灯交互和物理
        // =====================================================

        EnableLanternInteraction();


        // =====================================================
        // 显示抓取提示
        // =====================================================

        EnterWaitingForGrabState();
    }


    // =========================================================
    // 开场时彻底禁用天灯
    // =========================================================

    private void DisableLanternInteraction()
    {
        // 禁止抓取
        if (grabInteractable != null)
        {
            grabInteractable.enabled =
                false;
        }


        // 禁止 Rigidbody 物理
        if (lanternRigidbody != null)
        {
            // 清掉可能残留的速度
            lanternRigidbody.linearVelocity =
                Vector3.zero;

            lanternRigidbody.angularVelocity =
                Vector3.zero;


            // Kinematic 后不再受碰撞力、重力等影响
            lanternRigidbody.isKinematic =
                true;

            lanternRigidbody.useGravity =
                false;
        }


        Debug.Log(
            "[Lantern] Grab 和 Rigidbody 已禁用。"
        );
    }


    // =========================================================
    // Intro Timeline 结束后恢复天灯
    // =========================================================

    private void EnableLanternInteraction()
    {
        if (lanternRigidbody != null)
        {
            // 先恢复动态 Rigidbody
            lanternRigidbody.isKinematic =
                false;

            // 你的天灯本来就是无重力放飞
            lanternRigidbody.useGravity =
                false;

            // 防止刚恢复时带着残余速度
            lanternRigidbody.linearVelocity =
                Vector3.zero;

            lanternRigidbody.angularVelocity =
                Vector3.zero;
        }


        if (grabInteractable != null)
        {
            grabInteractable.enabled =
                true;
        }


        Debug.Log(
            "[Lantern] Grab 和 Rigidbody 已恢复。"
        );
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
            "[Lantern] 现在可以抓取天灯。"
        );
    }


    // =========================================================
    // 玩家抓住
    // =========================================================

    private void OnLanternGrabbed(
        SelectEnterEventArgs args)
    {
        // 已经放飞以后不再进入流程
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
        // 情况 1：未点亮松手
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
        // 情况 2：点亮后松手
        // =====================================================

        if (currentState ==
            LanternState.Lit)
        {
            currentState =
                LanternState.ReleasedLit;


            // Hint 消失
            HideHint();


            // Outline 消失
            SetLanternOutline(false);

            SetCandleOutline(false);


            // =================================================
            // 放飞物理
            // =================================================

            StartCoroutine(
                ApplyReleaseFlight()
            );


            // =================================================
            // 播放 Release Timeline
            // =================================================

            if (releaseTimeline != null)
            {
                releaseTimeline.time =
                    0;

                releaseTimeline.Play();
            }
            else
            {
                Debug.LogWarning(
                    "[Lantern] Release Timeline 没有绑定。",
                    this
                );
            }


            // =================================================
            // 同时开始显示 Gallery
            // =================================================

            if (galleryController != null)
            {
                galleryController
                    .StartGallery();
            }
            else
            {
                Debug.LogWarning(
                    "[Lantern] Gallery Controller 没有绑定。",
                    this
                );
            }


            Debug.Log(
                "[Lantern] 天灯已放飞，Release Timeline 和 Gallery 开始。"
            );


            return;
        }
    }


    // =========================================================
    // 点亮天灯
    // =========================================================

    public void IgniteLantern()
    {
        // 必须是玩家正拿着未点亮天灯
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
        // Outline 消失
        // =====================================================

        SetCandleOutline(false);

        SetLanternOutline(false);


        // =====================================================
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
        // 先让 XR Grab 完成松手速度计算
        yield return
            new WaitForFixedUpdate();


        if (lanternRigidbody == null)
        {
            yield break;
        }


        // =====================================================
        // 保证 Rigidbody 处于动态状态
        // =====================================================

        lanternRigidbody.useGravity =
            false;

        lanternRigidbody.isKinematic =
            false;


        // =====================================================
        // 保留玩家 XR 松手速度
        // 再额外增加向上速度
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
        float elapsed =
            0f;


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