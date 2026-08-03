using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class GazeDingSequenceAction : GazeAction
{
    [Header("Timelines")]
    [Tooltip("看向鼎后播放的 Timeline。")]
    [SerializeField] private PlayableDirector dingDirector;

    [Tooltip("拿起甲骨后播放的音频3 Timeline。")]
    [SerializeField] private PlayableDirector audio3Director;

    [Tooltip("音频3播放完后自动播放的音频4 Timeline。")]
    [SerializeField] private PlayableDirector audio4Director;

    [Header("Hint UI")]
    [SerializeField] private GameObject hintRoot;
    [SerializeField] private TMP_Text hintText;

    [TextArea(1, 4)]
    [SerializeField]
    private string hintMessage =
        "Pick up the oracle bone.";

    [Header("Scene Transition")]
    [Tooltip("音频4播放结束后要切换到的场景名称。")]
    [SerializeField] private string nextSceneName;

    [Header("Behaviour")]
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;
    private bool waitingForDingTimelineEnd;
    private bool oracleBoneSequenceTriggered;
    private bool waitingForAudio3End;
    private bool waitingForAudio4End;
    private bool sceneLoadStarted;

    private void Awake()
    {
        SetHintVisible(false);

        DisablePlayOnAwake(dingDirector);
        DisablePlayOnAwake(audio3Director);
        DisablePlayOnAwake(audio4Director);
    }

    private void OnEnable()
    {
        if (dingDirector != null)
            dingDirector.stopped += OnDingTimelineStopped;

        if (audio3Director != null)
            audio3Director.stopped += OnAudio3Stopped;

        if (audio4Director != null)
            audio4Director.stopped += OnAudio4Stopped;
    }

    private void OnDisable()
    {
        if (dingDirector != null)
            dingDirector.stopped -= OnDingTimelineStopped;

        if (audio3Director != null)
            audio3Director.stopped -= OnAudio3Stopped;

        if (audio4Director != null)
            audio4Director.stopped -= OnAudio4Stopped;
    }

    public override void OnGazeComplete()
    {
        if (triggerOnlyOnce && hasTriggered)
            return;

        if (dingDirector == null)
        {
            Debug.LogWarning(
                $"{gameObject.name} 没有设置 Ding Director。",
                this
            );
            return;
        }

        hasTriggered = true;
        waitingForDingTimelineEnd = true;

        PlayDirectorFromStart(dingDirector);

        Debug.Log("开始播放看向鼎后的 Timeline。", this);
    }

    private void OnDingTimelineStopped(PlayableDirector director)
    {
        if (director != dingDirector || !waitingForDingTimelineEnd)
            return;

        waitingForDingTimelineEnd = false;

        if (hintText != null)
            hintText.text = hintMessage;

        SetHintVisible(true);

        Debug.Log("鼎的 Timeline 播放完成，显示拿取甲骨提示。", this);
    }

    /// <summary>
    /// 由甲骨 XR Grab Interactable 的 Select Entered 调用。
    /// </summary>
    public void OnOracleBoneGrabbed()
    {
        SetHintVisible(false);

        if (oracleBoneSequenceTriggered)
            return;

        oracleBoneSequenceTriggered = true;

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

        Debug.Log("拿起甲骨，开始播放音频3 Timeline。", this);
    }

    private void OnAudio3Stopped(PlayableDirector director)
    {
        if (director != audio3Director || !waitingForAudio3End)
            return;

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

        Debug.Log("音频3播放完成，开始播放音频4 Timeline。", this);
    }

    private void OnAudio4Stopped(PlayableDirector director)
    {
        if (director != audio4Director || !waitingForAudio4End)
            return;

        waitingForAudio4End = false;

        Debug.Log("音频4播放完成，准备切换场景。", this);

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

    private void PlayDirectorFromStart(PlayableDirector director)
    {
        if (director == null)
            return;

        director.Stop();
        director.time = 0;
        director.Evaluate();
        director.Play();
    }

    private void DisablePlayOnAwake(PlayableDirector director)
    {
        if (director != null)
            director.playOnAwake = false;
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

    public void ResetSequence()
    {
        hasTriggered = false;
        waitingForDingTimelineEnd = false;
        oracleBoneSequenceTriggered = false;
        waitingForAudio3End = false;
        waitingForAudio4End = false;
        sceneLoadStarted = false;

        ResetDirector(dingDirector);
        ResetDirector(audio3Director);
        ResetDirector(audio4Director);

        SetHintVisible(false);
    }

    private void ResetDirector(PlayableDirector director)
    {
        if (director == null)
            return;

        director.Stop();
        director.time = 0;
        director.Evaluate();
    }
}