using UnityEngine;

public class GazeUIAction : GazeAction
{    [TextArea(2, 8)]
    [SerializeField] private string content;

    [Tooltip("注释 UI 出现的位置")]
    [SerializeField] private Transform uiAnchor;

    [SerializeField] private bool showOnComplete = false;

    [Tooltip("移开视线后是否隐藏")]
    [SerializeField] private bool hideWhenExit = true;

    private Transform UIAnchor
    {
        get
        {
            if (uiAnchor != null)
                return uiAnchor;

            return transform;
        }
    }

    public override void OnGazeEnter()
    {
        if (!showOnComplete)
            ShowUI();
    }

    public override void OnGazeComplete()
    {
        if (showOnComplete)
            ShowUI();
    }

    public override void OnGazeExit()
    {
        if (!hideWhenExit)
            return;

        if (GazeUIManager.Instance != null)
            GazeUIManager.Instance.Hide(this);
    }

    private void ShowUI()
    {
        if (GazeUIManager.Instance == null)
        {
            Debug.LogWarning(
                $"{gameObject.name} 找不到场景中的 GazeUIManager。",
                this
            );
            return;
        }

        GazeUIManager.Instance.Show(
            this,
            content,
            UIAnchor
        );
    }
}