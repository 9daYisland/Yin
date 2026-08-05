using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

public class GazeSceneIntroController : MonoBehaviour
{
    [Header("Intro Timeline")]
    [Tooltip("进入场景后自动播放的开场 Timeline。")]
    [SerializeField] private PlayableDirector introDirector;

    [Header("Gaze System")]
    [Tooltip("Main Camera 上负责视线射线检测的 GazeTarget。")]
    [SerializeField] private GazeTarget gazeTarget;

    [Tooltip("需要查看的六件文物。鼎也必须放入该数组。")]
    [SerializeField] private GazeInteractable[] artifactInteractables;

    [Min(1)]
    [Tooltip("查看多少件不同文物后进入甲骨阶段。")]
    [SerializeField] private int requiredArtifactCount = 6;

    [Header("Ding")]
    [Tooltip("鼎物体上的 GazeInteractable。")]
    [SerializeField] private GazeInteractable dingInteractable;

    [Tooltip("第一次看向鼎后播放的 Timeline。")]
    [SerializeField] private PlayableDirector dingDirector;

    [Header("Hint UI")]
    [Tooltip("用来显示提示的 TextMeshPro 文本。")]
    [SerializeField] private TMP_Text hintText;

    [Tooltip("包含提示背景和文字的整个 UI 根物体。")]
    [SerializeField] private GameObject hintRoot;

    [TextArea(1, 4)]
    [SerializeField]
    private string exploreHintMessage =
        "Look around to discover more.";

    [TextArea(1, 4)]
    [SerializeField]
    private string oracleBoneHintMessage =
        "Pick up an oracle bone.";

    [Header("Oracle Bone Intro")]
    [Tooltip("看完六件文物后播放的甲骨引导 Timeline。")]
    [SerializeField] private PlayableDirector oracleIntroDirector;

    [Header("Oracle Bone Outlines")]
    [Tooltip("所有可拾取甲骨上的 Outline 组件。")]
    [SerializeField] private Outline[] oracleBoneOutlines;

    [Min(0f)]
    [Tooltip("甲骨提示阶段的 Outline Width。")]
    [SerializeField] private float oracleBoneOutlineWidth = 1f;

    [Min(0f)]
    [Tooltip("甲骨 Outline 从 0 渐变到目标宽度所需时间。")]
    [SerializeField] private float oracleOutlineFadeDuration = 0.4f;

    [Header("Audio 3 → Audio 4")]
    [Tooltip("第一次拿起任意甲骨后播放的音频3 Timeline。")]
    [SerializeField] private PlayableDirector audio3Director;

    [Tooltip("音频3播放结束后自动播放的音频4 Timeline。")]
    [SerializeField] private PlayableDirector audio4Director;

    [Header("Timing")]
    [Min(0f)]
    [Tooltip("Intro Timeline 结束后，延迟多久开启视线检测。")]
    [SerializeField] private float enableGazeDelay = 0.2f;

    [Header("Behaviour")]
    [Tooltip("第一次拿起甲骨后，是否关闭视线检测。")]
    [SerializeField] private bool disableGazeAfterOracleGrab = true;

    [Header("Debug")]
    [SerializeField] private bool logEvents = true;

    [Header("Scene Transition")]
    [Tooltip("音频4播放结束后切换到的场景名称。")]
    [SerializeField] private string nextSceneName;

    private bool waitingForAudio4End;
    private bool sceneLoadStarted;

    // 已经看过的普通文物。HashSet 防止重复计数。
    private readonly HashSet<GazeInteractable> viewedArtifacts =
        new HashSet<GazeInteractable>();

    // 已经被拿起的甲骨 Outline。
    // 整体渐变时，这些 Outline 会一直保持为 0。
    private readonly HashSet<Outline> grabbedOracleOutlines =
        new HashSet<Outline>();

    private Coroutine introCoroutine;
    private Coroutine oracleOutlineCoroutine;

    private bool waitingForFirstArtifactGaze;

    private bool dingAudioStarted;
    private bool dingAudioPlaying;

    private bool oracleStageRequested;
    private bool oracleIntroPlaying;
    private bool oracleGrabStageReady;

    // 仅用于控制音频3只能被触发一次。
    private bool firstOracleBoneGrabbed;

    private bool waitingForAudio3End;

    private void Awake()
    {
        // 场景开始时先关闭视线检测。
        if (gazeTarget != null)
            gazeTarget.enabled = false;

        SetHintVisible(false);

        // 普通文物和甲骨描边初始全部隐藏。
        HideArtifactOutlines();
        SetOracleOutlinesImmediately(0f);

        // 除 Intro 外，其他 Timeline 都由脚本触发。
        DisablePlayOnAwake(dingDirector);
        DisablePlayOnAwake(oracleIntroDirector);
        DisablePlayOnAwake(audio3Director);
        DisablePlayOnAwake(audio4Director);
    }

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

        // 第一次看向文物时计数。
        GazeInteractable.AnyFirstGazeEntered += OnArtifactFirstSeen;
        if (audio4Director != null)
            audio4Director.stopped += OnAudio4Stopped;
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
        if (audio4Director != null)
            audio4Director.stopped -= OnAudio4Stopped;
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
        SetHintText(exploreHintMessage);
        SetHintVisible(true);

        // Intro 结束后开启六件文物的描边。
        ShowArtifactOutlines();

        if (enableGazeDelay > 0f)
            yield return new WaitForSeconds(enableGazeDelay);

        // 第一次看向任意文物后，隐藏初始 Hint。
        StartWaitingForInitialHint();

        if (gazeTarget != null)
            gazeTarget.enabled = true;

        if (logEvents)
            Debug.Log("文物视线交互阶段开始。", this);

        introCoroutine = null;
    }

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
        {
            Debug.Log(
                $"首次看向文物：{interactable.gameObject.name}",
                interactable
            );
        }
    }

    // =========================================================
    // 六件文物计数
    // =========================================================

    private void OnArtifactFirstSeen(
        GazeInteractable interactable)
    {
        if (!IsTrackedArtifact(interactable))
            return;

        // 同一件文物只统计一次。
        if (!viewedArtifacts.Add(interactable))
            return;

        if (logEvents)
        {
            Debug.Log(
                $"已查看：{interactable.gameObject.name}，进度：" +
                $"{viewedArtifacts.Count}/{requiredArtifactCount}",
                interactable
            );
        }

        // 鼎也是六件文物之一，同时播放鼎音频。
        if (interactable == dingInteractable)
            PlayDingAudio();

        // 六件全部查看完毕。
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
        {
            return false;
        }

        foreach (GazeInteractable artifact
                 in artifactInteractables)
        {
            if (artifact == interactable)
                return true;
        }

        return false;
    }

    // =========================================================
    // 鼎音频
    // =========================================================

    private void PlayDingAudio()
    {
        if (dingAudioStarted)
            return;

        dingAudioStarted = true;

        if (dingDirector == null)
        {
            Debug.LogWarning(
                "没有设置鼎对应的 Playable Director。",
                this
            );
            return;
        }

        dingAudioPlaying = true;
        PlayDirectorFromStart(dingDirector);

        if (logEvents)
            Debug.Log("看到鼎，开始播放鼎音频。", this);
    }

    private void OnDingStopped(PlayableDirector director)
    {
        if (director != dingDirector ||
            !dingAudioPlaying)
        {
            return;
        }

        dingAudioPlaying = false;

        if (logEvents)
            Debug.Log("鼎音频播放结束。", this);

        // 六件已看完时，等鼎音频结束后再播放甲骨引导音频。
        if (oracleStageRequested &&
            !oracleIntroPlaying &&
            !oracleGrabStageReady)
        {
            StartOracleIntroAudio();
        }
    }

    // =========================================================
    // 甲骨引导音频
    // =========================================================

    private void RequestOracleStage()
    {
        if (oracleStageRequested)
            return;

        oracleStageRequested = true;

        SetHintVisible(false);
        HideArtifactOutlines();

        if (logEvents)
        {
            Debug.Log(
                "六件文物已查看完毕，准备播放甲骨引导音频。",
                this
            );
        }

        // 如果鼎音频仍在播放，等它结束。
        if (dingAudioPlaying)
        {
            if (logEvents)
                Debug.Log("等待鼎音频结束。", this);

            return;
        }

        StartOracleIntroAudio();
    }

    private void StartOracleIntroAudio()
    {
        if (oracleIntroPlaying ||
            oracleGrabStageReady)
        {
            return;
        }

        if (oracleIntroDirector == null)
        {
            Debug.LogWarning(
                "没有设置甲骨引导音频，将直接显示甲骨提示。",
                this
            );

            BeginOracleGrabStage();
            return;
        }

        oracleIntroPlaying = true;
        PlayDirectorFromStart(oracleIntroDirector);

        if (logEvents)
            Debug.Log("开始播放甲骨引导音频。", this);
    }

    private void OnOracleIntroStopped(
        PlayableDirector director)
    {
        if (director != oracleIntroDirector ||
            !oracleIntroPlaying)
        {
            return;
        }

        oracleIntroPlaying = false;

        if (logEvents)
            Debug.Log("甲骨引导音频播放结束。", this);

        BeginOracleGrabStage();
    }

    // =========================================================
    // 甲骨拿取阶段
    // =========================================================

    private void BeginOracleGrabStage()
    {
        if (oracleGrabStageReady)
            return;

        oracleGrabStageReady = true;

        SetHintText(oracleBoneHintMessage);
        SetHintVisible(true);

        if (oracleOutlineCoroutine != null)
            StopCoroutine(oracleOutlineCoroutine);

        // 所有尚未被拿起的甲骨，从当前宽度渐变到 1。
        oracleOutlineCoroutine = StartCoroutine(
            AnimateOracleOutlines(
                oracleBoneOutlineWidth,
                oracleOutlineFadeDuration
            )
        );

        if (logEvents)
        {
            Debug.Log(
                "显示甲骨提示，甲骨 Outline 开始渐变。",
                this
            );
        }
    }

    /// <summary>
    /// 每一块甲骨的 XR Grab Interactable / Select Entered
    /// 都连接到这个动态参数方法。
    /// </summary>
    public void OnOracleBoneGrabbed(
        SelectEnterEventArgs args)
    {
        // 甲骨引导音频播放结束前，不允许触发后续流程。
        if (!oracleGrabStageReady)
            return;

        if (args == null ||
            args.interactableObject == null)
        {
            Debug.LogWarning(
                "没有收到有效的甲骨 Select Entered 参数。",
                this
            );
            return;
        }

        Transform grabbedTransform =
            args.interactableObject.transform;

        // 在被拿起的甲骨自身或子物体中寻找 Outline。
        Outline grabbedOutline =
            grabbedTransform.GetComponentInChildren<Outline>(true);

        if (grabbedOutline != null)
        {
            // 记录该甲骨，防止整体渐变协程再次把它点亮。
            grabbedOracleOutlines.Add(grabbedOutline);

            grabbedOutline.enabled = true;
            grabbedOutline.OutlineWidth = 0f;

            if (logEvents)
            {
                Debug.Log(
                    $"拿起甲骨：{grabbedTransform.name}。" +
                    "仅关闭该甲骨的 Outline。",
                    grabbedTransform
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"在 {grabbedTransform.name} 下没有找到 Outline。",
                grabbedTransform
            );
        }

        // 第一次拿起任意甲骨后，Hint 消失。
        SetHintVisible(false);

        // 后续拿起其他甲骨，只关闭各自 Outline，
        // 不再重复播放音频3。
        if (firstOracleBoneGrabbed)
            return;

        firstOracleBoneGrabbed = true;

        if (disableGazeAfterOracleGrab &&
            gazeTarget != null)
        {
            gazeTarget.enabled = false;
        }

        PlayAudio3();

        if (logEvents)
            Debug.Log("首次拿起甲骨，开始播放音频3。", this);
    }

    private IEnumerator AnimateOracleOutlines(
        float targetWidth,
        float duration)
    {
        float startWidth =
            GetCurrentOracleOutlineWidth();

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

            float progress =
                Mathf.Clamp01(elapsed / duration);

            float width =
                Mathf.Lerp(startWidth, targetWidth, progress);

            SetOracleOutlinesImmediately(width);

            yield return null;
        }

        SetOracleOutlinesImmediately(targetWidth);
        oracleOutlineCoroutine = null;
    }

    private float GetCurrentOracleOutlineWidth()
    {
        if (oracleBoneOutlines == null)
            return 0f;

        foreach (Outline outline in oracleBoneOutlines)
        {
            if (outline != null &&
                !grabbedOracleOutlines.Contains(outline))
            {
                return outline.OutlineWidth;
            }
        }

        return 0f;
    }

    private void SetOracleOutlinesImmediately(float width)
    {
        if (oracleBoneOutlines == null)
            return;

        foreach (Outline outline in oracleBoneOutlines)
        {
            if (outline == null)
                continue;

            outline.enabled = true;

            // 已经被拿起的甲骨保持为 0。
            if (grabbedOracleOutlines.Contains(outline))
            {
                outline.OutlineWidth = 0f;
                continue;
            }

            outline.OutlineWidth = width;
        }
    }

    // =========================================================
    // 音频3 → 音频4
    // =========================================================

    private void PlayAudio3()
    {
        if (audio3Director == null)
        {
            Debug.LogWarning(
                "没有设置音频3对应的 Playable Director。",
                this
            );
            return;
        }

        waitingForAudio3End = true;
        PlayDirectorFromStart(audio3Director);
    }

    private void OnAudio3Stopped(
        PlayableDirector director)
    {
        if (director != audio3Director ||
            !waitingForAudio3End)
        {
            return;
        }

        waitingForAudio3End = false;

        if (audio4Director == null)
        {
            Debug.LogWarning(
                "音频3结束，但没有设置音频4对应的 Playable Director。",
                this
            );
            return;
        }

        waitingForAudio4End = true;
        PlayDirectorFromStart(audio4Director);

        if (logEvents)
            Debug.Log("音频3结束，开始播放音频4。", this);
    }
    private void OnAudio4Stopped(
    PlayableDirector director)
    {
        if (director != audio4Director ||
            !waitingForAudio4End)
        {
            return;
        }

        waitingForAudio4End = false;

        if (logEvents)
            Debug.Log("音频4结束，准备切换场景。", this);

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (sceneLoadStarted)
            return;

        sceneLoadStarted = true;

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "Next Scene Name 为空，无法切换场景。",
                this
            );
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }



    // =========================================================
    // 普通文物 Outline
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

    private void HideArtifactOutlines()
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
    // Utility
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
}