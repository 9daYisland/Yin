using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GazeSceneIntroController : MonoBehaviour
{
    // =========================================================
    // Intro
    // =========================================================

    [Header("Intro Timeline")]
    [Tooltip("进入场景后自动播放的开场 Timeline。")]
    [SerializeField] private PlayableDirector introDirector;


    // =========================================================
    // Gaze System
    // =========================================================

    [Header("Gaze System")]
    [Tooltip("Main Camera 上负责视线射线检测的 GazeTarget。")]
    [SerializeField] private GazeTarget gazeTarget;

    [Tooltip("需要查看的六件文物。鼎也必须放进这个数组。")]
    [SerializeField] private GazeInteractable[] artifactInteractables;

    [Min(1)]
    [Tooltip("看完多少件不同文物后进入甲骨阶段。")]
    [SerializeField] private int requiredArtifactCount = 6;


    // =========================================================
    // Ding
    // =========================================================

    [Header("Ding")]
    [Tooltip("鼎物体上的 GazeInteractable。")]
    [SerializeField] private GazeInteractable dingInteractable;

    [Tooltip("第一次看向鼎后播放的 Timeline。")]
    [SerializeField] private PlayableDirector dingDirector;


    // =========================================================
    // Hint UI
    // =========================================================

    [Header("Hint UI")]
    [SerializeField] private TMP_Text hintText;

    [Tooltip("整个 Hint UI 根物体。")]
    [SerializeField] private GameObject hintRoot;

    [TextArea(1, 4)]
    [SerializeField]
    private string exploreHintMessage =
        "Look around to discover more.";

    [TextArea(1, 4)]
    [SerializeField]
    private string oracleBoneHintMessage =
        "Pick up an oracle bone.";


    // =========================================================
    // Oracle Intro
    // =========================================================

    [Header("Oracle Bone Intro")]
    [Tooltip("看完六件文物后播放的甲骨引导 Timeline。")]
    [SerializeField] private PlayableDirector oracleIntroDirector;


    // =========================================================
    // Oracle Outline
    // =========================================================

    [Header("Oracle Bone Outlines")]
    [Tooltip("所有甲骨上的 Outline。")]
    [SerializeField] private Outline[] oracleBoneOutlines;

    [Min(0f)]
    [SerializeField] private float oracleBoneOutlineWidth = 1f;

    [Min(0f)]
    [SerializeField] private float oracleOutlineFadeDuration = 0.4f;


    // =========================================================
    // Oracle Interaction
    // =========================================================

    [Header("Oracle Bone Interaction")]

    [Tooltip("所有甲骨上的 XR Grab Interactable。")]
    [SerializeField]
    private XRGrabInteractable[] oracleBoneGrabInteractables;

    [Tooltip("所有甲骨用于物理碰撞/抓取的 Collider。可以包含多个。")]
    [SerializeField]
    private Collider[] oracleBoneColliders;

    [Tooltip("所有甲骨上的 Rigidbody。")]
    [SerializeField]
    private Rigidbody[] oracleBoneRigidbodies;


    // =========================================================
    // Audio 3 / 4
    // =========================================================

    [Header("Audio 3 → Audio 4")]
    [Tooltip("第一次拿起任意甲骨后播放的音频3 Timeline。")]
    [SerializeField] private PlayableDirector audio3Director;

    [Tooltip("音频3结束后播放的音频4 Timeline。")]
    [SerializeField] private PlayableDirector audio4Director;


    // =========================================================
    // Scene
    // =========================================================

    [Header("Scene Transition")]
    [Tooltip("音频4播放结束后加载的场景名称。")]
    [SerializeField] private string nextSceneName;


    // =========================================================
    // Timing
    // =========================================================

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float enableGazeDelay = 0.2f;


    // =========================================================
    // Behaviour
    // =========================================================

    [Header("Behaviour")]
    [Tooltip("第一次拿起甲骨后是否关闭视线检测。")]
    [SerializeField] private bool disableGazeAfterOracleGrab = true;

    [Header("End Timeline")]
    [Tooltip("Audio 4 播放结束后播放的最终 Timeline。")]
    [SerializeField] private PlayableDirector endDirector;

    private bool waitingForEndTimeline;


    // =========================================================
    // Debug
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool logEvents = true;


    // =========================================================
    // Runtime
    // =========================================================

    private readonly HashSet<GazeInteractable> viewedArtifacts =
        new HashSet<GazeInteractable>();

    private readonly HashSet<Outline> grabbedOracleOutlines =
        new HashSet<Outline>();


    // 保存 Rigidbody 原始状态
    private bool[] originalRigidbodyKinematic;
    private bool[] originalRigidbodyUseGravity;
    private bool[] originalRigidbodyDetectCollisions;

    // 保存 Collider 原始状态
    private bool[] originalColliderEnabled;


    private Coroutine introCoroutine;
    private Coroutine oracleOutlineCoroutine;

    private bool waitingForFirstArtifactGaze;

    private bool dingAudioStarted;
    private bool dingAudioPlaying;

    private bool oracleStageRequested;
    private bool oracleIntroPlaying;
    private bool oracleGrabStageReady;

    private bool firstOracleBoneGrabbed;

    private bool waitingForAudio3End;
    private bool waitingForAudio4End;

    private bool sceneLoadStarted;


    // =========================================================
    // Awake
    // =========================================================

    private void Awake()
    {
        // 缓存甲骨原本的物理状态。
        CacheOracleBonePhysicsState();

        // 初始关闭视线交互。
        if (gazeTarget != null)
            gazeTarget.enabled = false;

        // 初始隐藏 Hint。
        SetHintVisible(false);

        // 六件普通文物 Outline 初始关闭。
        DisableArtifactOutlines();

        // 甲骨 Outline 初始彻底关闭。
        DisableOracleBoneOutlines();

        // 甲骨初始完全不可交互。
        DisableOracleBoneInteraction();

        // 非 Intro Timeline 全部禁止 Play On Awake。
        DisablePlayOnAwake(dingDirector);
        DisablePlayOnAwake(oracleIntroDirector);
        DisablePlayOnAwake(audio3Director);
        DisablePlayOnAwake(audio4Director);
        DisablePlayOnAwake(endDirector);
    }


    // =========================================================
    // Enable / Disable events
    // =========================================================

    private void OnEnable()
    {
        if (introDirector != null)
            introDirector.stopped += OnIntroStopped;

        if (dingDirector != null)
            dingDirector.stopped += OnDingStopped;

        if (oracleIntroDirector != null)
            oracleIntroDirector.stopped += OnOracleIntroStopped;

        if (audio3Director != null)
            audio3Director.stopped += OnAudio3Stopped;

        if (audio4Director != null)
            audio4Director.stopped += OnAudio4Stopped;
        if (endDirector != null)
            endDirector.stopped += OnEndTimelineStopped;

        GazeInteractable.AnyFirstGazeEntered += OnArtifactFirstSeen;
    }

    private void OnDisable()
    {
        if (introDirector != null)
            introDirector.stopped -= OnIntroStopped;

        if (dingDirector != null)
            dingDirector.stopped -= OnDingStopped;

        if (oracleIntroDirector != null)
            oracleIntroDirector.stopped -= OnOracleIntroStopped;

        if (audio3Director != null)
            audio3Director.stopped -= OnAudio3Stopped;

        if (audio4Director != null)
            audio4Director.stopped -= OnAudio4Stopped;
        if (endDirector != null)
            endDirector.stopped -= OnEndTimelineStopped;

        GazeInteractable.AnyFirstGazeEntered -= OnArtifactFirstSeen;

        StopWaitingForInitialHint();

        if (introCoroutine != null)
        {
            StopCoroutine(introCoroutine);
            introCoroutine = null;
        }

        if (oracleOutlineCoroutine != null)
        {
            StopCoroutine(oracleOutlineCoroutine);
            oracleOutlineCoroutine = null;
        }
    }


    // =========================================================
    // Intro
    // =========================================================

    private void OnIntroStopped(PlayableDirector director)
    {
        if (director != introDirector)
            return;

        if (introCoroutine != null)
            StopCoroutine(introCoroutine);

        introCoroutine = StartCoroutine(EnableGazeSequence());
    }

    private IEnumerator EnableGazeSequence()
    {
        // 环顾提示。
        SetHintText(exploreHintMessage);
        SetHintVisible(true);

        // 开启六件普通文物 Outline。
        ShowArtifactOutlines();

        if (enableGazeDelay > 0f)
            yield return new WaitForSeconds(enableGazeDelay);

        StartWaitingForInitialHint();

        if (gazeTarget != null)
            gazeTarget.enabled = true;

        if (logEvents)
            Debug.Log("文物视线交互阶段开始。", this);

        introCoroutine = null;
    }


    // =========================================================
    // Initial gaze hint
    // =========================================================

    private void StartWaitingForInitialHint()
    {
        if (waitingForFirstArtifactGaze)
            return;

        waitingForFirstArtifactGaze = true;

        GazeInteractable.AnyGazeEntered +=
            OnFirstArtifactGazeEntered;
    }

    private void StopWaitingForInitialHint()
    {
        if (!waitingForFirstArtifactGaze)
            return;

        waitingForFirstArtifactGaze = false;

        GazeInteractable.AnyGazeEntered -=
            OnFirstArtifactGazeEntered;
    }

    private void OnFirstArtifactGazeEntered(
        GazeInteractable interactable)
    {
        if (!IsTrackedArtifact(interactable))
            return;

        SetHintVisible(false);
        StopWaitingForInitialHint();

        if (logEvents)
            Debug.Log($"首次看向：{interactable.name}", interactable);
    }


    // =========================================================
    // Artifact counting
    // =========================================================

    private void OnArtifactFirstSeen(
        GazeInteractable interactable)
    {
        if (!IsTrackedArtifact(interactable))
            return;

        if (!viewedArtifacts.Add(interactable))
            return;

        if (logEvents)
        {
            Debug.Log(
                $"已查看：{interactable.name} " +
                $"{viewedArtifacts.Count}/{requiredArtifactCount}",
                interactable
            );
        }

        // 鼎
        if (interactable == dingInteractable)
            PlayDingAudio();

        // 六件全部看完
        if (!oracleStageRequested &&
            viewedArtifacts.Count >= requiredArtifactCount)
        {
            RequestOracleStage();
        }
    }

    private bool IsTrackedArtifact(
        GazeInteractable interactable)
    {
        if (interactable == null ||
            artifactInteractables == null)
            return false;

        foreach (GazeInteractable artifact
                 in artifactInteractables)
        {
            if (artifact == interactable)
                return true;
        }

        return false;
    }


    // =========================================================
    // Ding
    // =========================================================

    private void PlayDingAudio()
    {
        if (dingAudioStarted)
            return;

        dingAudioStarted = true;

        if (dingDirector == null)
        {
            Debug.LogWarning("没有设置鼎 Timeline。", this);
            return;
        }

        dingAudioPlaying = true;

        PlayDirectorFromStart(dingDirector);

        if (logEvents)
            Debug.Log("播放鼎音频。", this);
    }

    private void OnDingStopped(PlayableDirector director)
    {
        if (director != dingDirector ||
            !dingAudioPlaying)
            return;

        dingAudioPlaying = false;

        if (logEvents)
            Debug.Log("鼎音频结束。", this);

        if (oracleStageRequested &&
            !oracleIntroPlaying &&
            !oracleGrabStageReady)
        {
            StartOracleIntroAudio();
        }
    }


    // =========================================================
    // Request oracle stage
    // =========================================================

    private void RequestOracleStage()
    {
        if (oracleStageRequested)
            return;

        oracleStageRequested = true;

        SetHintVisible(false);

        DisableArtifactOutlines();

        if (logEvents)
            Debug.Log("六件文物全部查看完成。", this);

        // 鼎音频还没结束就先等。
        if (dingAudioPlaying)
            return;

        StartOracleIntroAudio();
    }


    // =========================================================
    // Oracle intro audio
    // =========================================================

    private void StartOracleIntroAudio()
    {
        if (oracleIntroPlaying ||
            oracleGrabStageReady)
            return;

        if (oracleIntroDirector == null)
        {
            Debug.LogWarning(
                "没有设置甲骨引导 Timeline，直接进入甲骨交互。",
                this
            );

            BeginOracleGrabStage();
            return;
        }

        oracleIntroPlaying = true;

        PlayDirectorFromStart(oracleIntroDirector);

        if (logEvents)
            Debug.Log("播放甲骨引导音频。", this);
    }

    private void OnOracleIntroStopped(
        PlayableDirector director)
    {
        if (director != oracleIntroDirector ||
            !oracleIntroPlaying)
            return;

        oracleIntroPlaying = false;

        if (logEvents)
            Debug.Log("甲骨引导音频结束。", this);

        BeginOracleGrabStage();
    }


    // =========================================================
    // Enable oracle interaction
    // =========================================================

    private void BeginOracleGrabStage()
    {
        if (oracleGrabStageReady)
            return;

        oracleGrabStageReady = true;

        // 显示“拿起甲骨”提示。
        SetHintText(oracleBoneHintMessage);
        SetHintVisible(true);

        // =====================================================
        // 关键：
        // 现在才恢复甲骨碰撞和 Grab。
        // =====================================================
        EnableOracleBoneInteraction();

        // 现在才开启甲骨 Outline。
        PrepareOracleBoneOutlines();

        if (oracleOutlineCoroutine != null)
            StopCoroutine(oracleOutlineCoroutine);

        oracleOutlineCoroutine = StartCoroutine(
            AnimateOracleOutlines(
                oracleBoneOutlineWidth,
                oracleOutlineFadeDuration
            )
        );

        if (logEvents)
        {
            Debug.Log(
                "甲骨阶段开始：Hint显示，Collider/Grab开启，Outline 0→1。",
                this
            );
        }
    }


    // =========================================================
    // Grab oracle bone
    // =========================================================

    public void OnOracleBoneGrabbed(
        SelectEnterEventArgs args)
    {
        if (!oracleGrabStageReady)
            return;

        if (args == null ||
            args.interactableObject == null)
            return;

        Transform grabbedTransform =
            args.interactableObject.transform;

        Outline grabbedOutline =
            grabbedTransform.GetComponentInChildren<Outline>(true);

        if (grabbedOutline != null)
        {
            grabbedOracleOutlines.Add(grabbedOutline);

            // 只关闭被拿起的这一块。
            grabbedOutline.OutlineWidth = 0f;
            grabbedOutline.enabled = false;

            if (logEvents)
            {
                Debug.Log(
                    $"拿起甲骨 {grabbedTransform.name}，关闭自身 Outline。",
                    grabbedTransform
                );
            }
        }

        // 第一次拿甲骨后 Hint 消失。
        SetHintVisible(false);

        // 后续甲骨只负责关闭自己的 Outline，
        // 不重复播放 Audio3。
        if (firstOracleBoneGrabbed)
            return;

        firstOracleBoneGrabbed = true;

        if (disableGazeAfterOracleGrab &&
            gazeTarget != null)
        {
            gazeTarget.enabled = false;
        }

        PlayAudio3();
    }


    // =========================================================
    // Oracle interaction control
    // =========================================================

    private void CacheOracleBonePhysicsState()
    {
        // Collider
        if (oracleBoneColliders != null)
        {
            originalColliderEnabled =
                new bool[oracleBoneColliders.Length];

            for (int i = 0;
                 i < oracleBoneColliders.Length;
                 i++)
            {
                Collider col = oracleBoneColliders[i];

                if (col != null)
                    originalColliderEnabled[i] = col.enabled;
            }
        }

        // Rigidbody
        if (oracleBoneRigidbodies != null)
        {
            int count = oracleBoneRigidbodies.Length;

            originalRigidbodyKinematic =
                new bool[count];

            originalRigidbodyUseGravity =
                new bool[count];

            originalRigidbodyDetectCollisions =
                new bool[count];

            for (int i = 0; i < count; i++)
            {
                Rigidbody rb =
                    oracleBoneRigidbodies[i];

                if (rb == null)
                    continue;

                originalRigidbodyKinematic[i] =
                    rb.isKinematic;

                originalRigidbodyUseGravity[i] =
                    rb.useGravity;

                originalRigidbodyDetectCollisions[i] =
                    rb.detectCollisions;
            }
        }
    }

    private void DisableOracleBoneInteraction()
    {
        // 1. 先关 Grab
        if (oracleBoneGrabInteractables != null)
        {
            foreach (XRGrabInteractable grab
                     in oracleBoneGrabInteractables)
            {
                if (grab != null)
                    grab.enabled = false;
            }
        }

        // 2. 关闭 Collider
        if (oracleBoneColliders != null)
        {
            foreach (Collider col in oracleBoneColliders)
            {
                if (col != null)
                    col.enabled = false;
            }
        }

        // 3. 停止 Rigidbody 物理
        if (oracleBoneRigidbodies != null)
        {
            foreach (Rigidbody rb in oracleBoneRigidbodies)
            {
                if (rb == null)
                    continue;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                rb.detectCollisions = false;
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
    }

    private void EnableOracleBoneInteraction()
    {
        // =====================================================
        // 顺序很重要：
        // Rigidbody → Collider → Grab
        // =====================================================

        // 1. 恢复 Rigidbody
        if (oracleBoneRigidbodies != null)
        {
            for (int i = 0;
                 i < oracleBoneRigidbodies.Length;
                 i++)
            {
                Rigidbody rb =
                    oracleBoneRigidbodies[i];

                if (rb == null)
                    continue;

                if (originalRigidbodyKinematic != null &&
                    i < originalRigidbodyKinematic.Length)
                {
                    rb.isKinematic =
                        originalRigidbodyKinematic[i];
                }

                if (originalRigidbodyUseGravity != null &&
                    i < originalRigidbodyUseGravity.Length)
                {
                    rb.useGravity =
                        originalRigidbodyUseGravity[i];
                }

                if (originalRigidbodyDetectCollisions != null &&
                    i < originalRigidbodyDetectCollisions.Length)
                {
                    rb.detectCollisions =
                        originalRigidbodyDetectCollisions[i];
                }
                else
                {
                    rb.detectCollisions = true;
                }
            }
        }

        // 2. 恢复 Collider
        if (oracleBoneColliders != null)
        {
            for (int i = 0;
                 i < oracleBoneColliders.Length;
                 i++)
            {
                Collider col =
                    oracleBoneColliders[i];

                if (col == null)
                    continue;

                if (originalColliderEnabled != null &&
                    i < originalColliderEnabled.Length)
                {
                    col.enabled =
                        originalColliderEnabled[i];
                }
                else
                {
                    col.enabled = true;
                }
            }
        }

        // 3. 最后开启 Grab
        if (oracleBoneGrabInteractables != null)
        {
            foreach (XRGrabInteractable grab
                     in oracleBoneGrabInteractables)
            {
                if (grab != null)
                    grab.enabled = true;
            }
        }
    }


    // =========================================================
    // Oracle Outline
    // =========================================================

    private void DisableOracleBoneOutlines()
    {
        if (oracleBoneOutlines == null)
            return;

        foreach (Outline outline
                 in oracleBoneOutlines)
        {
            if (outline == null)
                continue;

            outline.OutlineWidth = 0f;
            outline.enabled = false;
        }
    }

    private void PrepareOracleBoneOutlines()
    {
        if (oracleBoneOutlines == null)
            return;

        foreach (Outline outline
                 in oracleBoneOutlines)
        {
            if (outline == null)
                continue;

            if (grabbedOracleOutlines.Contains(outline))
                continue;

            // 先 Width 0，再 Enabled。
            outline.OutlineWidth = 0f;
            outline.enabled = true;
        }
    }

    private IEnumerator AnimateOracleOutlines(
        float targetWidth,
        float duration)
    {
        if (duration <= 0f)
        {
            SetOracleOutlinesImmediately(targetWidth);
            oracleOutlineCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / duration);

            float width =
                Mathf.Lerp(0f, targetWidth, t);

            SetOracleOutlinesImmediately(width);

            yield return null;
        }

        SetOracleOutlinesImmediately(targetWidth);

        oracleOutlineCoroutine = null;
    }

    private void SetOracleOutlinesImmediately(
        float width)
    {
        if (oracleBoneOutlines == null)
            return;

        foreach (Outline outline
                 in oracleBoneOutlines)
        {
            if (outline == null)
                continue;

            if (grabbedOracleOutlines.Contains(outline))
            {
                outline.OutlineWidth = 0f;
                outline.enabled = false;
                continue;
            }

            if (!oracleGrabStageReady)
            {
                outline.OutlineWidth = 0f;
                outline.enabled = false;
                continue;
            }

            outline.enabled = true;
            outline.OutlineWidth = width;
        }
    }


    // =========================================================
    // Audio 3 → Audio 4
    // =========================================================

    private void PlayAudio3()
    {
        if (audio3Director == null)
        {
            Debug.LogWarning(
                "没有设置 Audio3 Director。",
                this
            );
            return;
        }

        waitingForAudio3End = true;

        PlayDirectorFromStart(audio3Director);

        if (logEvents)
            Debug.Log("播放音频3。", this);
    }

    private void OnAudio3Stopped(
        PlayableDirector director)
    {
        if (director != audio3Director ||
            !waitingForAudio3End)
            return;

        waitingForAudio3End = false;

        if (audio4Director == null)
        {
            Debug.LogWarning(
                "没有设置 Audio4 Director。",
                this
            );
            return;
        }

        waitingForAudio4End = true;

        PlayDirectorFromStart(audio4Director);

        if (logEvents)
            Debug.Log("音频3结束，播放音频4。", this);
    }

    private void OnAudio4Stopped(
    PlayableDirector director)
    {
        if (director != audio4Director ||
            !waitingForAudio4End)
            return;

        waitingForAudio4End = false;

        if (endDirector == null)
        {
            Debug.LogWarning(
                "Audio 4 已结束，但没有设置 End Director，直接切换场景。",
                this
            );

            LoadNextScene();
            return;
        }

        waitingForEndTimeline = true;

        PlayDirectorFromStart(endDirector);

        if (logEvents)
            Debug.Log("音频4结束，开始播放 End Timeline。", this);
    }


    // =========================================================
    // Scene
    // =========================================================

    private void LoadNextScene()
    {
        if (sceneLoadStarted)
            return;

        sceneLoadStarted = true;

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "Next Scene Name 为空。",
                this
            );
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }


    // =========================================================
    // Artifact Outline
    // =========================================================

    private void ShowArtifactOutlines()
    {
        if (artifactInteractables == null)
            return;

        foreach (GazeInteractable artifact
                 in artifactInteractables)
        {
            if (artifact != null)
                artifact.ShowOutline();
        }
    }

    private void DisableArtifactOutlines()
    {
        if (artifactInteractables == null)
            return;

        foreach (GazeInteractable artifact
                 in artifactInteractables)
        {
            if (artifact != null)
                artifact.HideOutlineImmediately();
        }
    }


    // =========================================================
    // Hint
    // =========================================================

    private void SetHintText(string message)
    {
        if (hintText != null)
            hintText.text = message;
    }

    private void SetHintVisible(bool visible)
    {
        if (hintRoot != null)
        {
            hintRoot.SetActive(visible);
            return;
        }

        if (hintText != null)
            hintText.gameObject.SetActive(visible);
    }


    // =========================================================
    // Timeline
    // =========================================================

    private void PlayDirectorFromStart(
        PlayableDirector director)
    {
        if (director == null)
            return;

        director.Stop();
        director.time = 0;
        director.Evaluate();
        director.Play();
    }

    private void DisablePlayOnAwake(
        PlayableDirector director)
    {
        if (director != null)
            director.playOnAwake = false;
    }
    private void OnEndTimelineStopped(
    PlayableDirector director)
    {
        if (director != endDirector ||
            !waitingForEndTimeline)
            return;

        waitingForEndTimeline = false;

        if (logEvents)
            Debug.Log("End Timeline 结束，切换场景。", this);

        LoadNextScene();
    }
}