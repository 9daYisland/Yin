using UnityEngine;

public abstract class GazeAction : MonoBehaviour
{
    public virtual void OnGazeEnter()
    {
    }

    public virtual void OnGazeProgress(float progress)
    {
    }

    public virtual void OnGazeComplete()
    {
    }

    public virtual void OnGazeExit()
    {
    }

    public virtual void OnGazeReset()
    {
    }
}