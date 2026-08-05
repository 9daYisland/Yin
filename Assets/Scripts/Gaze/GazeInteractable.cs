using System;
using UnityEngine;

public class GazeInteractable : MonoBehaviour
{
    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float gazeDuration = 1f;

    [Header("Completion")]
    [Tooltip("该物体的 Complete 行为是否只能触发一次。")]
    [SerializeField] private bool completeOnlyOnce = false;

    [Tooltip("未看满时间就移开视线时，是否清除当前进度。")]
    [SerializeField] private bool resetProgressOnExit = true;

    [Tooltip("完成后，再次看向时是否仍然触发 Enter 和 Exit。")]
    [SerializeField] private bool allowEnterAfterComplete = true;

    [Header("Outline")]
    [Tooltip("物体或子物体上的 Outline。留空时自动查找。")]
    [SerializeField] private Outline outline;

    [Tooltip("提示阶段的描边宽度。")]
    [Min(0f)]
    [SerializeField] private float visibleOutlineWidth = 1f;

    [Tooltip("第一次看向该物体时，是否关闭它的描边。")]
    [SerializeField] private bool hideOutlineOnGazeEnter = true;

    [Header("Debug")]
    [SerializeField] private bool logEvents = false;

    private GazeAction[] gazeActions;

    private bool isGazing;
    private bool hasCompleted;
    private bool hasBeenLookedAt;

    // 是否已经向场景总流程报告过“该文物已看完”。
    private bool completionReported;

    private float gazeTime;

    public bool IsGazing => isGazing;
    public bool HasCompleted => hasCompleted;
    public bool HasBeenLookedAt => hasBeenLookedAt;

    public float Progress =>
        gazeDuration <= 0f
            ? 1f
            : Mathf.Clamp01(gazeTime / gazeDuration);
    public static event Action<GazeInteractable> AnyFirstGazeEntered;
    public static event Action<GazeInteractable> AnyGazeEntered;
    public static event Action<GazeInteractable> AnyGazeCompleted;


    private void Awake()
    {
        gazeActions = GetComponents<GazeAction>();

        if (outline == null)
            outline = GetComponentInChildren<Outline>(true);

        HideOutlineImmediately();
    }

    private void Update()
    {
        if (!isGazing)
            return;

        if (completeOnlyOnce && hasCompleted)
            return;

        gazeTime += Time.deltaTime;

        float progress = Progress;

        foreach (GazeAction action in gazeActions)
        {
            if (action != null && action.isActiveAndEnabled)
                action.OnGazeProgress(progress);
        }

        if (gazeTime >= gazeDuration)
            CompleteGaze();
    }

    public void GazeEnter()
    {
        if (isGazing)
            return;

        if (completeOnlyOnce &&
            hasCompleted &&
            !allowEnterAfterComplete)
        {
            return;
        }

        isGazing = true;

        if (!hasBeenLookedAt)
        {
            hasBeenLookedAt = true;

            if (hideOutlineOnGazeEnter)
                HideOutlineImmediately();

            // 只有第一次看向这件文物时触发。
            AnyFirstGazeEntered?.Invoke(this);
        }

        AnyGazeEntered?.Invoke(this);

        if (logEvents)
            Debug.Log($"看到：{gameObject.name}", this);

        foreach (GazeAction action in gazeActions)
        {
            if (action != null && action.isActiveAndEnabled)
                action.OnGazeEnter();
        }

        if (gazeDuration <= 0f)
            CompleteGaze();
    }

    public void GazeExit()
    {
        if (!isGazing)
            return;

        isGazing = false;

        if (logEvents)
            Debug.Log($"离开：{gameObject.name}", this);

        foreach (GazeAction action in gazeActions)
        {
            if (action != null && action.isActiveAndEnabled)
                action.OnGazeExit();
        }

        if (resetProgressOnExit && !hasCompleted)
        {
            gazeTime = 0f;

            foreach (GazeAction action in gazeActions)
            {
                if (action != null && action.isActiveAndEnabled)
                    action.OnGazeReset();
            }
        }
    }

    private void CompleteGaze()
    {
        if (hasCompleted && completeOnlyOnce)
            return;

        hasCompleted = true;
        gazeTime = gazeDuration;

        if (logEvents)
            Debug.Log($"注视完成：{gameObject.name}", this);

        foreach (GazeAction action in gazeActions)
        {
            if (action != null && action.isActiveAndEnabled)
                action.OnGazeComplete();
        }


        if (!completionReported)
        {
            completionReported = true;
            AnyGazeCompleted?.Invoke(this);
        }
    }

    public void ShowOutline()
    {
        if (hasBeenLookedAt || outline == null)
            return;

        outline.enabled = true;
        outline.OutlineWidth = visibleOutlineWidth;
    }

    public void HideOutlineImmediately()
    {
        if (outline == null)
            return;

        outline.enabled = true;
        outline.OutlineWidth = 0f;
    }

    public void ResetInteractable()
    {
        isGazing = false;
        hasCompleted = false;
        hasBeenLookedAt = false;
        completionReported = false;
        gazeTime = 0f;

        HideOutlineImmediately();

        foreach (GazeAction action in gazeActions)
        {
            if (action != null && action.isActiveAndEnabled)
                action.OnGazeReset();
        }
    }
}