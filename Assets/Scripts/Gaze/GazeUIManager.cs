using TMPro;
using UnityEngine;

public class GazeUIManager : MonoBehaviour
{
    public static GazeUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text text;

    private GazeUIAction currentAction;
    private Transform currentAnchor;
    private Camera mainCamera;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;

        if (root != null)
            root.SetActive(false);
    }

    private void LateUpdate()
    {
        if (root == null || !root.activeSelf || currentAnchor == null)
            return;

        root.transform.position = currentAnchor.position;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        // 让 Canvas 始终正对玩家
        Vector3 direction = root.transform.position - mainCamera.transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            root.transform.rotation =
                Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    public void Show(
        GazeUIAction action,
        string content,
        Transform anchor)
    {
        if (root == null)
        {
            Debug.LogError("GazeUIManager：Root 没有设置。", this);
            return;
        }

        if (text == null)
        {
            Debug.LogError("GazeUIManager：Text 没有设置。", this);
            return;
        }

        if (anchor == null)
        {
            Debug.LogError("GazeUIManager：UI Anchor 为空。", this);
            return;
        }

        currentAction = action;
        currentAnchor = anchor;

        root.transform.position = anchor.position;
        text.text = content;
        root.SetActive(true);

        //Debug.Log($"显示注释：{content}，位置：{anchor.position}");
    }

    public void Hide(GazeUIAction action)
    {
        if (currentAction != action)
            return;

        Hide();
    }

    public void Hide()
    {
        currentAction = null;
        currentAnchor = null;

        if (root != null)
            root.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}