using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class GazeSceneIntroController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField] private PlayableDirector introDirector;

    [Header("Gaze System")]
    [Tooltip("Main Camera 上的 GazeTarget。")]
    [SerializeField] private GazeTarget gazeTarget;

    [Header("Hint Text")]
    [Tooltip("用来显示提示内容的 TextMeshPro 文本。")]
    [SerializeField] private TMP_Text hintText;

    [TextArea(1, 4)]
    [SerializeField]
    private string hintMessage =
        "Look around to discover more.";

    [Tooltip("提示 UI 的根物体。留空时直接使用 Hint Text 所在物体。")]
    [SerializeField] private GameObject hintRoot;

    [Header("Timing")]
    [Tooltip("Timeline 结束后多久开启视线交互。")]
    [Min(0f)]
    [SerializeField] private float enableGazeDelay = 0.2f;

    private Coroutine enableCoroutine;
    private bool waitingForFirstGaze;

    private void Awake()
    {
        // 进入场景时禁用视线系统。
        if (gazeTarget != null)
            gazeTarget.enabled = false;

        SetHintVisible(false);
    }

    private void OnEnable()
    {
        if (introDirector != null)
            introDirector.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        if (introDirector != null)
            introDirector.stopped -= OnTimelineStopped;

        StopWaitingForGaze();

        if (enableCoroutine != null)
        {
            StopCoroutine(enableCoroutine);
            enableCoroutine = null;
        }
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        if (director != introDirector)
            return;

        if (enableCoroutine != null)
            StopCoroutine(enableCoroutine);

        enableCoroutine = StartCoroutine(EnableGazeSequence());
    }

    private IEnumerator EnableGazeSequence()
    {
        // 修改文本并显示提示。
        if (hintText != null)
            hintText.text = hintMessage;

        SetHintVisible(true);

        if (enableGazeDelay > 0f)
            yield return new WaitForSeconds(enableGazeDelay);

        // 开始监听第一次视线交互。
        StartWaitingForGaze();

        if (gazeTarget != null)
            gazeTarget.enabled = true;

        Debug.Log("视线交互已开启。");

        enableCoroutine = null;
    }

    private void StartWaitingForGaze()
    {
        if (waitingForFirstGaze)
            return;

        waitingForFirstGaze = true;
        GazeInteractable.AnyGazeEntered += OnFirstGazeInteraction;
    }

    private void StopWaitingForGaze()
    {
        if (!waitingForFirstGaze)
            return;

        waitingForFirstGaze = false;
        GazeInteractable.AnyGazeEntered -= OnFirstGazeInteraction;
    }

    private void OnFirstGazeInteraction(GazeInteractable interactable)
    {
        Debug.Log($"玩家首次触发视线交互：{interactable.gameObject.name}");

        SetHintVisible(false);
        StopWaitingForGaze();
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
}