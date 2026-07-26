using UnityEngine;

public class GazeDebugAction : GazeAction
{
    [SerializeField] private bool logProgress = false;

    public override void OnGazeEnter()
    {
        Debug.Log($"Gaze Enter£º{gameObject.name}", this);
    }

    public override void OnGazeProgress(float progress)
    {
        if (!logProgress)
            return;

        Debug.Log(
            $"Gaze Progress£º{gameObject.name}£¬{progress:P0}",
            this
        );
    }

    public override void OnGazeComplete()
    {
        Debug.Log($"Gaze Complete£º{gameObject.name}", this);
    }

    public override void OnGazeExit()
    {
        Debug.Log($"Gaze Exit£º{gameObject.name}", this);
    }

    public override void OnGazeReset()
    {
        Debug.Log($"Gaze Reset£º{gameObject.name}", this);
    }
}