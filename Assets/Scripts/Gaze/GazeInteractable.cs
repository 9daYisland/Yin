using UnityEngine;

public class GazeInteractable : MonoBehaviour
{
    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float gazeDuration = 1f;

    [Header("Completion")]
    [Tooltip("是否只能完成一次")]
    [SerializeField] private bool completeOnlyOnce = false;

    [Tooltip("移开视线后是否清除当前进度")]
    [SerializeField] private bool resetProgressOnExit = true;

    [Tooltip("完成后，再次看向时是否仍触发")]
    [SerializeField] private bool allowEnterAfterComplete = true;

    [Header("Debug")]
    [SerializeField] private bool logEvents = false;

    private GazeAction[] gazeActions;

    private bool isGazing;
    private bool hasCompleted;
    private float gazeTime;

    public bool IsGazing => isGazing;
    public bool HasCompleted => hasCompleted;
    public float Progress =>
        gazeDuration <= 0f ? 1f : Mathf.Clamp01(gazeTime / gazeDuration);

    private void Awake()
    {
        gazeActions = GetComponents<GazeAction>();
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

        if (completeOnlyOnce && hasCompleted && !allowEnterAfterComplete)
            return;

        isGazing = true;

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
    }

    public void ResetInteractable()
    {
        isGazing = false;
        hasCompleted = false;
        gazeTime = 0f;

        foreach (GazeAction action in gazeActions)
        {
            if (action != null && action.isActiveAndEnabled)
                action.OnGazeReset();
        }
    }
}