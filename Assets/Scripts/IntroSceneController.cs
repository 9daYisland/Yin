using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public class IntroSceneController : MonoBehaviour
{
    [Header("Candle")]
    [SerializeField] private XRGrabInteractable candle;

    [Header("Torch Fire")]
    [SerializeField] private FireController fireController;

    [Header("Hint UI")]
    [SerializeField] private GameObject hintCanvas;

    [Tooltip("可以不拖，会自动从 HintCanvas 子物体寻找")]
    [SerializeField] private TMP_Text hintText;

    [Header("Hint Text")]
    [TextArea]
    [SerializeField]
    private string grabHint =
        "抓取蜡烛";

    [TextArea]
    [SerializeField]
    private string lightHint =
        "点燃火把以开始";

    [Header("Scene")]
    [SerializeField] private string nextSceneName;

    [SerializeField] private float sceneChangeDelay = 1f;

    private bool candleGrabbed = false;
    private bool sceneChanging = false;


    private void Start()
    {
        // ==========================================
        // Hint
        // ==========================================

        if (hintCanvas == null)
        {
            Debug.LogError(
                "[IntroScene] HintCanvas 没有绑定！"
            );
        }
        else
        {
            hintCanvas.SetActive(true);

            if (hintText == null)
            {
                hintText =
                    hintCanvas.GetComponentInChildren<TMP_Text>(true);
            }
        }


        if (hintText != null)
        {
            hintText.text = grabHint;
        }
        else
        {
            Debug.LogError(
                "[IntroScene] 找不到 Hint TMP Text！"
            );
        }


        // ==========================================
        // Candle Grab
        // ==========================================

        if (candle != null)
        {
            candle.selectEntered.AddListener(
                OnCandleGrabbed
            );
        }
        else
        {
            Debug.LogError(
                "[IntroScene] Candle 没有绑定！"
            );
        }


        // ==========================================
        // Fire
        // ==========================================

        if (fireController == null)
        {
            Debug.LogError(
                "[IntroScene] FireController 没有绑定！"
            );
        }
    }


    private void Update()
    {
        // 必须先抓过蜡烛
        if (!candleGrabbed)
            return;

        if (sceneChanging)
            return;

        if (fireController == null)
            return;


        // ==========================================
        // Torch 已经被点燃
        // ==========================================

        if (fireController.IsLit)
        {
            sceneChanging = true;

            HideHint();

            StartCoroutine(
                ChangeSceneCoroutine()
            );
        }
    }


    private void OnCandleGrabbed(
        SelectEnterEventArgs args)
    {
        if (candleGrabbed)
            return;


        candleGrabbed = true;


        // 抓到蜡烛以后修改提示
        ShowHint(
            lightHint
        );


        Debug.Log(
            "[IntroScene] Candle Grabbed"
        );
    }


    private void ShowHint(
        string message)
    {
        if (hintCanvas != null)
        {
            hintCanvas.SetActive(true);
        }


        if (hintText != null)
        {
            hintText.text = message;
        }
    }


    private void HideHint()
    {
        if (hintCanvas != null)
        {
            hintCanvas.SetActive(false);
        }
    }


    private IEnumerator ChangeSceneCoroutine()
    {
        Debug.Log(
            "[IntroScene] Torch lit. Changing scene..."
        );


        yield return new WaitForSeconds(
            sceneChangeDelay
        );


        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError(
                "[IntroScene] Next Scene Name 没填写！"
            );

            yield break;
        }


        SceneManager.LoadScene(
            nextSceneName
        );
    }


    private void OnDestroy()
    {
        if (candle != null)
        {
            candle.selectEntered.RemoveListener(
                OnCandleGrabbed
            );
        }
    }
}